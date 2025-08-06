using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.ReverseProxy
{
    [MessagePackObject]
    public class ReverseProxyDisconnect : IMessage
    {
        [Key(1)]
        public int ConnectionId { get; set; }
    }
}
