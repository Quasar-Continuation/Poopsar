using MessagePack;
using Pulsar.Common.Enums;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class TcpConnection
    {
        [Key(1)]
        public string ProcessName { get; set; }

        [Key(2)]
        public string LocalAddress { get; set; }

        [Key(3)]
        public ushort LocalPort { get; set; }

        [Key(4)]
        public string RemoteAddress { get; set; }

        [Key(5)]
        public ushort RemotePort { get; set; }

        [Key(6)]
        public ConnectionState State { get; set; }
    }
}
