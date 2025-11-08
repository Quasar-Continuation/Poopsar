using MessagePack;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Webcam
{
    [MessagePackObject]
    public class GetWebcam : IMessage
    {
        [Key(1)]
        public bool CreateNew { get; set; }

        [Key(2)]
        public int Quality { get; set; }

        [Key(3)]
        public int DisplayIndex { get; set; }

        [Key(4)]
        public RemoteWebcamStatus Status { get; set; }

        [Key(5)]
        public bool UseGPU { get; set; }

        [Key(6)]
        public int FramesRequested { get; set; } = 1;

        [Key(7)]
        public bool IsBufferedMode { get; set; } = true;
    }
}
