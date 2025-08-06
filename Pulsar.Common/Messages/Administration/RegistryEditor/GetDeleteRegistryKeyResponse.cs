using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class GetDeleteRegistryKeyResponse : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }

        [Key(2)]
        public string KeyName { get; set; }

        [Key(3)]
        public bool IsError { get; set; }

        [Key(4)]
        public string ErrorMsg { get; set; }
    }
}
