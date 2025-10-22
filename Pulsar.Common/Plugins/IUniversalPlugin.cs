using System;

namespace Pulsar.Common.Plugins
{
    public interface IUniversalPlugin
    {
        string PluginId { get; }
        string Version { get; }
        string[] SupportedCommands { get; }

        void Initialize(byte[] initData);
        PluginResult ExecuteCommand(string command, byte[] parameters);
        void Cleanup();
        bool IsComplete { get; }
    }

    public class PluginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        // I haven't tested this too much but I would try and make sure
        // that you only use primitive data types in this or well known data types
        // Don't send back classes that can't be deserialized due to them not being in
        // Pulsar.Server or Pulsar.Common. Otherwise you will get serialization issues.
        public Object Data { get; set; }
        public bool ShouldUnload { get; set; }
        public string NextCommand { get; set; }
    }
}