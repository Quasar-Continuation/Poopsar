using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RemoteShell
{
    [MessagePackObject]
    public class DoShellExecuteResponse : IMessage
    {
        [Key(1)]
        public string Output { get; set; }

        [Key(2)]
        public bool IsError { get; set; }
    }
}
