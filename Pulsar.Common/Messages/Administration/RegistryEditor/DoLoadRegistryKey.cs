using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoLoadRegistryKey : IMessage
    {
        [Key(1)]
        public string RootKeyName { get; set; }
    }
}
