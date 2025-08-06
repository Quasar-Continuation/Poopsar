using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TCPConnections
{
    [MessagePackObject]
    public class GetConnections : IMessage
    {
    }
}
