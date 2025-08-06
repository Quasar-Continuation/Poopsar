using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.ReverseProxy
{
    [MessagePackObject]
    public class ReverseProxyConnectResponse : IMessage
    {
        [Key(1)]
        public int ConnectionId { get; set; }

        [Key(2)]
        public bool IsConnected { get; set; }

        [Key(3)]
        public byte[] LocalAddress { get; set; }

        [Key(4)]
        public int LocalPort { get; set; }

        [Key(5)]
        public string HostName { get; set; }
    }
}
