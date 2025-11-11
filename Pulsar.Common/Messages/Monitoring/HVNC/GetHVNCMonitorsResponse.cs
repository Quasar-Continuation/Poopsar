using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.HVNC
{
    [MessagePackObject]
    public class GetHVNCMonitorsResponse : IMessage
    {
        [Key(1)]
        public int Number { get; set; }
    }
}
