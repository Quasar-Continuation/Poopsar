using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public sealed class DoExecuteUniversalCommand : IMessage
    {
        [Key(1)]
        public string PluginId { get; set; }

        [Key(2)]
        public string Command { get; set; }

        [Key(3)]
        public byte[] Parameters { get; set; }
    }
}

