using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.QuickCommands
{
    [MessagePackObject]
    public class DoSendQuickCommand : IMessage
    {
        [Key(1)]
        public string Command { get; set; }
        [Key(2)]
        public string Host { get; set; }
    }
}
