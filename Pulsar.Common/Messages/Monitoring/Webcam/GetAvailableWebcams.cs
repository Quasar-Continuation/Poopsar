using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Webcam
{
    [MessagePackObject]
    public class GetAvailableWebcams : IMessage
    {
    }
}
