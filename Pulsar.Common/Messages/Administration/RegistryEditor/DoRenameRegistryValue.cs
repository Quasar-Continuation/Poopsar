using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoRenameRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public string OldValueName { get; set; }

        [Key(3)]
        public string NewValueName { get; set; }
    }
}
