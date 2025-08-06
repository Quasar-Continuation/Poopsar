using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class SetUserClipboardStatus : IMessage
    {
        [Key(1)]
        public string ClipboardText { get; set; }
    }
}
