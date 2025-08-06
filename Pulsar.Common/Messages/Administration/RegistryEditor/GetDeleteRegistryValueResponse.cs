using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class GetDeleteRegistryValueResponse : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public string ValueName { get; set; }

        [Key(3)]
        public bool IsError { get; set; }

        [Key(4)]
        public string ErrorMsg { get; set; }
    }
}
