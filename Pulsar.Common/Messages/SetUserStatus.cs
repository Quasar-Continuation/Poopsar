using MessagePack;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class SetUserStatus : IMessage
    {
        [Key(1)]
        public UserStatus Message { get; set; }
    }
}
