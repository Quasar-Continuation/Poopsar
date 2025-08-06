using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Audio
{
    [MessagePackObject]
    public class GetOutput : IMessage
    {
        [Key(1)]
        public bool CreateNew { get; set; }

        [Key(2)]
        public int DeviceIndex { get; set; }

        [Key(3)]
        public int Bitrate { get; set; }

        [Key(4)]
        public bool Destroy { get; set; }
    }
}
