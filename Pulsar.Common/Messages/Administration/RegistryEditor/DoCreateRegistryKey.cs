using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoCreateRegistryKey : IMessage
    {
        [Key(1)]
        public string ParentPath { get; set; }
    }
}
