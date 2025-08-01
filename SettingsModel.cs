using Newtonsoft.Json;

namespace Pulsar.Server.Models
{
    public class SettingsModel
    {
        [JsonProperty("darkMode")]
        public bool DarkMode { get; set; } = false;

        [JsonProperty("hideFromScreenCapture")]
        public bool HideFromScreenCapture { get; set; } = false;

        [JsonProperty("discordRPC")]
        public bool DiscordRPC { get; set; } = false;

        [JsonProperty("listenPort")]
        public ushort ListenPort { get; set; } = 4782;

        [JsonProperty("ipv6Support")]
        public bool IPv6Support { get; set; } = false;

        [JsonProperty("autoListen")]
        public bool AutoListen { get; set; } = false;

        [JsonProperty("eventLog")]
        public bool EventLog { get; set; } = false;

        [JsonProperty("telegramNotifications")]
        public bool TelegramNotifications { get; set; } = false;

        [JsonProperty("showPopup")]
        public bool ShowPopup { get; set; } = false;

        [JsonProperty("useUPnP")]
        public bool UseUPnP { get; set; } = false;

        [JsonProperty("showToolTip")]
        public bool ShowToolTip { get; set; } = false;

        [JsonProperty("enableNoIPUpdater")]
        public bool EnableNoIPUpdater { get; set; } = false;

        [JsonProperty("telegramChatID")]
        public string TelegramChatID { get; set; } = "";

        [JsonProperty("telegramBotToken")]
        public string TelegramBotToken { get; set; } = "";

        [JsonProperty("noIPHost")]
        public string NoIPHost { get; set; } = "";

        [JsonProperty("noIPUsername")]
        public string NoIPUsername { get; set; } = "";

        [JsonProperty("noIPPassword")]
        public string NoIPPassword { get; set; } = "";

        [JsonProperty("saveFormat")]
        public string SaveFormat { get; set; } = "APP - URL - USER:PASS";

        [JsonProperty("reverseProxyPort")]
        public ushort ReverseProxyPort { get; set; } = 3128;

        [JsonProperty("AllowLocalhost")]
        public bool AllowLocalhost { get; set; } = false;
    }
}
