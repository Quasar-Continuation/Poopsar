using Newtonsoft.Json;
using System;

namespace Pulsar.Server.Models
{
    public class BuilderProfileModel
    {
        [JsonProperty("hosts")]
        public string Hosts { get; set; } = "";

        [JsonProperty("pastebin")]
        public string Pastebin { get; set; } = "";

        [JsonProperty("tag")]
        public string Tag { get; set; } = "Office04";

        [JsonProperty("delay")]
        public int Delay { get; set; } = 3000;

        [JsonProperty("mutex")]
        public string Mutex { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("installClient")]
        public bool InstallClient { get; set; } = false;

        [JsonProperty("installName")]
        public string InstallName { get; set; } = "Client";

        [JsonProperty("installPath")]
        public short InstallPath { get; set; } = 1;

        [JsonProperty("installSub")]
        public string InstallSub { get; set; } = "SubDir";

        [JsonProperty("hideFile")]
        public bool HideFile { get; set; } = false;

        [JsonProperty("hideSubDirectory")]
        public bool HideSubDirectory { get; set; } = false;

        [JsonProperty("addStartup")]
        public bool AddStartup { get; set; } = false;

        [JsonProperty("registryName")]
        public string RegistryName { get; set; } = "Pulsar Client Startup";

        [JsonProperty("changeIcon")]
        public bool ChangeIcon { get; set; } = false;

        [JsonProperty("iconPath")]
        public string IconPath { get; set; } = "";

        [JsonProperty("changeAsmInfo")]
        public bool ChangeAsmInfo { get; set; } = false;

        [JsonProperty("keylogger")]
        public bool Keylogger { get; set; } = false;

        [JsonProperty("logDirectoryName")]
        public string LogDirectoryName { get; set; } = "Logs";

        [JsonProperty("hideLogDirectory")]
        public bool HideLogDirectory { get; set; } = false;

        [JsonProperty("enablePastebin")]
        public bool EnablePastebin { get; set; } = false;

        [JsonProperty("enableAntiVM")]
        public bool EnableAntiVM { get; set; } = false;

        [JsonProperty("enableAntiDebug")]
        public bool EnableAntiDebug { get; set; } = false;

        [JsonProperty("enableObfuscate")]
        public bool EnableObfuscate { get; set; } = false;

        [JsonProperty("enablePack")]
        public bool EnablePack { get; set; } = false;

        [JsonProperty("enableCriticalProcess")]
        public bool EnableCriticalProcess { get; set; } = false;

        [JsonProperty("enableUACBypass")]
        public bool EnableUACBypass { get; set; } = false;

        [JsonProperty("productName")]
        public string ProductName { get; set; } = "";

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("companyName")]
        public string CompanyName { get; set; } = "";

        [JsonProperty("copyright")]
        public string Copyright { get; set; } = "";

        [JsonProperty("trademarks")]
        public string Trademarks { get; set; } = "";

        [JsonProperty("originalFilename")]
        public string OriginalFilename { get; set; } = "";

        [JsonProperty("productVersion")]
        public string ProductVersion { get; set; } = "";

        [JsonProperty("fileVersion")]
        public string FileVersion { get; set; } = "";
    }
}
