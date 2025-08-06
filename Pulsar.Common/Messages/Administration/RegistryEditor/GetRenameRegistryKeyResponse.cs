using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class GetRenameRegistryKeyResponse : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }

        [Key(2)]
        public string OldKeyName { get; set; }

        [Key(3)]
        public string NewKeyName { get; set; }

        [Key(4)]
        public bool IsError { get; set; }

        [Key(5)]
        public string ErrorMsg { get; set; }
    }
}
