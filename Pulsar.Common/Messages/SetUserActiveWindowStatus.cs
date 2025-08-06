using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class SetUserActiveWindowStatus : IMessage
    {
        [Key(1)]
        public string WindowTitle { get; set; }
    }
}