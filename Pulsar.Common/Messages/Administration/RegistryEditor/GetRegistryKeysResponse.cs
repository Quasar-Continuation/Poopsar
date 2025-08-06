using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class GetRegistryKeysResponse : IMessage
    {
        [Key(1)]
        public RegSeekerMatch[] Matches { get; set; }

        [Key(2)]
        public string RootKey { get; set; }

        [Key(3)]
        public bool IsError { get; set; }

        [Key(4)]
        public string ErrorMsg { get; set; }
    }
}
