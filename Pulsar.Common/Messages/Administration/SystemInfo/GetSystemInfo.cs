using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.SystemInfo
{
    [MessagePackObject]
    public class GetSystemInfo : IMessage
    {
    }
}
