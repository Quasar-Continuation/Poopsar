using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.ReverseProxy
{
    [MessagePackObject]
    public class ReverseProxyData : IMessage
    {
        [Key(1)]
        public int ConnectionId { get; set; }

        [Key(2)]
        public byte[] Data { get; set; }
    }
}
