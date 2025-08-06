using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class SetStatus : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}
