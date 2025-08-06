using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class DoRenameRegistryKey : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }

        [Key(2)]
        public string OldKeyName { get; set; }

        [Key(3)]
        public string NewKeyName { get; set; }
    }
}
