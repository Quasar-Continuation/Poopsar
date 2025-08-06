using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.VirtualMonitor
{
    [MessagePackObject]
    public class DoUninstallVirtualMonitor : IMessage
    {
    }
}