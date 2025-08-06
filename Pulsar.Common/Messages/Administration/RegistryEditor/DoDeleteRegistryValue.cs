using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class DoDeleteRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public string ValueName { get; set; }
    }
}
