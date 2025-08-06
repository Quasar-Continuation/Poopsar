using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.Clipboard
{
    [MessagePackObject]
    public class DoSendAddress : IMessage
    {
        [Key(1)]
        public string Address { get; set; }
    }
}
