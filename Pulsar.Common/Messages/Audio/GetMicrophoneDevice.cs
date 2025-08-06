using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Audio
{
    [MessagePackObject]
    public class GetMicrophoneDevice : IMessage
    {
    }
}
