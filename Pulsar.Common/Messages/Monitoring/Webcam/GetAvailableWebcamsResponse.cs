using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Webcam
{
    [MessagePackObject]
    public class GetAvailableWebcamsResponse : IMessage
    {
        [Key(1)]
        public string[] Webcams { get; set; }
    }
}
