using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [MessagePackObject]
    public class DoChangeRegistryValue : IMessage
    {
        [Key(1)]
        public string KeyPath { get; set; }

        [Key(2)]
        public RegValueData Value { get; set; }
    }
}
