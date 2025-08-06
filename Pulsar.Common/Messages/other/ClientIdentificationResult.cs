using MessagePack;

namespace Pulsar.Common.Messages.Other
{
    [MessagePackObject]
    public class ClientIdentificationResult : IMessage
    {
        [Key(1)]
        public bool Result { get; set; }
    }
}
