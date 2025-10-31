using System;
using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoMonitorsOff : IMessage
    {
        [Key(0)]
        public bool Off { get; set; }

        [Key(1)]
        public bool On { get; set; }

        public DoMonitorsOff() { }

        public DoMonitorsOff(bool off = false, bool on = false)
        {
            Off = off;
            On = on;
        }
    }
}
