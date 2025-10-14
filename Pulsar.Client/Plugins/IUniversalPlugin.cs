using System;

namespace Pulsar.Client.Plugins
{
    public interface IUniversalPlugin
    {
        string PluginId { get; }
        string Version { get; }
        string[] SupportedCommands { get; }
        bool IsComplete { get; }

        void Initialize(byte[] initData);
        object ExecuteCommand(string command, byte[] parameters);
        void Cleanup();
    }

    public class PluginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public byte[] Data { get; set; }
        public bool ShouldUnload { get; set; }
        public string NextCommand { get; set; }
    }
}