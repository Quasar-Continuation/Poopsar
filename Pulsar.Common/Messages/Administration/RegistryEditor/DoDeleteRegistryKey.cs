using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoDeleteRegistryKey : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }

        [Key(2)]
        public string KeyName { get; set; }
    }
}
