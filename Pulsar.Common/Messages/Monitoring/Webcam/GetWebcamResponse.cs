using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Video;

namespace Pulsar.Common.Messages.Webcam
{
    [MessagePackObject]
    public class GetWebcamResponse : IMessage
    {
        [Key(1)]
        public byte[] Image { get; set; }

        [Key(2)]
        public int Quality { get; set; }

        [Key(3)]
        public int Monitor { get; set; }

        [Key(4)]
        public Resolution Resolution { get; set; }

        [Key(5)]
        public long Timestamp { get; set; }

        [Key(6)]
        public bool IsLastRequestedFrame { get; set; }

        [Key(7)]
        public float FrameRate { get; set; }
    }
}
