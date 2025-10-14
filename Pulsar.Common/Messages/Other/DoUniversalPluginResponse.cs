using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public sealed class DoUniversalPluginResponse : IMessage
    {
        [Key(1)]
        public string PluginId { get; set; }

        [Key(2)]
        public string Command { get; set; }

        [Key(3)]
        public bool Success { get; set; }

        [Key(4)]
        public string Message { get; set; }

        [Key(5)]
        public byte[] Data { get; set; }

        [Key(6)]
        public bool ShouldUnload { get; set; }

        [Key(7)]
        public string NextCommand { get; set; }
    }
}

