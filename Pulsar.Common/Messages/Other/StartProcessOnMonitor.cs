using System;
using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Other
{
    [MessagePackObject]
    public class StartProcessOnMonitor : IMessage
    {
        [Key(1)]
        public string Application { get; set; }

        [Key(2)]
        public int MonitorID { get; set; }
    }
}