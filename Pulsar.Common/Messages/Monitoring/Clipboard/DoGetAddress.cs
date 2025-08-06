using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.Clipboard
{
    [MessagePackObject]
    public class DoGetAddress : IMessage
    {
        [Key(1)]
        public string Type { get; set; }
    }
}
