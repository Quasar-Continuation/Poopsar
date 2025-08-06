using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Audio
{
    [MessagePackObject]
    public class GetMicrophoneResponse : IMessage
    {
        [Key(1)]
        public byte[] Audio { get; set; }

        [Key(2)]
        public int Device { get; set; }
    }
}
