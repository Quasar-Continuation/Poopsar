using MessagePack;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.RemoteDesktop
{
    [MessagePackObject]
    public class DoMouseEvent : IMessage
    {
        [Key(1)]
        public MouseAction Action { get; set; }

        [Key(2)]
        public bool IsMouseDown { get; set; }

        [Key(3)]
        public int X { get; set; }

        [Key(4)]
        public int Y { get; set; }

        [Key(5)]
        public int MonitorIndex { get; set; }
    }
}
