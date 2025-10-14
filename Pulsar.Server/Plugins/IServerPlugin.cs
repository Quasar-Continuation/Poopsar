using System;

namespace Pulsar.Server.Plugins
{
    public interface IServerPlugin
    {
        string Name { get; }
        Version Version { get; }
        string Description { get; }
        string Type { get; }

        void Initialize(IServerContext context);
        void Cleanup();
    }
}