using Microsoft.Win32;
using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class DoCreateRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public RegistryValueKind Kind { get; set; }
    }
}
