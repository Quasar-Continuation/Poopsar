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
        public Object Data { get; set; }
        public bool ShouldUnload { get; set; }
        public string NextCommand { get; set; }
    }
}