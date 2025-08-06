using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoSwapMouseButtons : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}
