using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.RemoteShell
{
    [MessagePackObject]
    public class DoShellExecute : IMessage
    {
        [Key(1)]
        public string Command { get; set; }
    }
}
