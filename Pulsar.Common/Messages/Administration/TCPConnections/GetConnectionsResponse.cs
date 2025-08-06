using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.TCPConnections
{
    [MessagePackObject]
    public class GetConnectionsResponse : IMessage
    {
        [Key(1)]
        public TcpConnection[] Connections { get; set; }
    }
}
