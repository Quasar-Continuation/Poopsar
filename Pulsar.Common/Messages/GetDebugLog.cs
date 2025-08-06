using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class GetDebugLog : IMessage
    {
        [Key(1)]
        public string Log { get; set; }
    }
}
