using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.UserSupport.RemoteChat
{
    [MessagePackObject]
    public class GetChat : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}
