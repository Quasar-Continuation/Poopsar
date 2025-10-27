using DarkModeForms;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Client.Utilities.DarkMode
{
    public class DarkModeManager
    {
        private static readonly DarkModeCS.DisplayMode lightMode = DarkModeCS.DisplayMode.ClearMode;
        private static readonly DarkModeCS.DisplayMode darkMode = DarkModeCS.DisplayMode.DarkMode;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private const int DWMWA_BORDER_COLOR = 34; // DWM attribute for border color

        public static void ApplyDarkMode(Form form)
        {
            bool shouldUseDarkMode = IsDarkModeEnabled();

            DarkModeCS _ = new DarkModeCS(form)
            {
                ColorMode = shouldUseDarkMode ? darkMode : lightMode,
                ColorizeIcons = false,
            };

            Color borderColor = shouldUseDarkMode ? Color.DimGray : Color.DimGray;
            SetBorderColor(form, borderColor);
        }

        private static void SetBorderColor(Form form, Color color)
        {
            int colorValue = color.R | (color.G << 8) | (color.B << 16);
            DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref colorValue, sizeof(int));
        }

        public static bool IsDarkModeEnabled()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            using (RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    object appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                    if (appsUseLightTheme != null && appsUseLightTheme is int value)
                    {
                        return value == 0;
                    }
                }
            }

            return false;
        }
    }
}
