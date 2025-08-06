using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoHideTaskbar : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}
