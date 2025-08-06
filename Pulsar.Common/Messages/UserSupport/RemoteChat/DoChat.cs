using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.UserSupport.RemoteChat
{
    [MessagePackObject]
    public class DoChat : IMessage
    {
        [Key(1)]
        public string PacketDms { get; set; }
        [Key(2)]
        public string User { get; set; }
    }
}
