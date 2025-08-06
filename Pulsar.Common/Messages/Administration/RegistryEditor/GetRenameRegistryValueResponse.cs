using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class GetRenameRegistryValueResponse : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public string OldValueName { get; set; }

        [Key(3)]
        public string NewValueName { get; set; }

        [Key(4)]
        public bool IsError { get; set; }

        [Key(5)]
        public string ErrorMsg { get; set; }
    }
}
