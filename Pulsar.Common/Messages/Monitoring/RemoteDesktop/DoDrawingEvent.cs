using MessagePack;
using Pulsar.Common.Messages.Other;
using System.Drawing;

namespace Pulsar.Common.Messages.Monitoring.RemoteDesktop
{
    [MessagePackObject]
    public class DoDrawingEvent : IMessage
    {
        [Key(1)]
        public int X { get; set; }

        [Key(2)]
        public int Y { get; set; }

        [Key(3)]
        public int PrevX { get; set; }

        [Key(4)]
        public int PrevY { get; set; }

        [Key(5)]
        public int StrokeWidth { get; set; }

        [Key(6)]
        public int ColorArgb { get; set; }

        [Key(7)]
        public bool IsEraser { get; set; }

        [Key(8)]
        public bool IsClearAll { get; set; }

        [Key(9)]
        public int MonitorIndex { get; set; }
    }
} 