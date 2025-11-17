using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessStart : IMessage
    {
        [Key(0)]
        public bool IsFromFileManager { get; set; }

        [Key(1)]
        public string DownloadUrl { get; set; }

        [Key(2)]
        public string FilePath { get; set; }

        [Key(3)]
        public bool IsUpdate { get; set; }

        [Key(4)]
        public bool ExecuteInMemoryDotNet { get; set; }

        [Key(5)]
        public bool UseRunPE { get; set; }

        [Key(6)]
        public string RunPETarget { get; set; }

        [Key(7)]
        public string RunPECustomPath { get; set; }

        [Key(8)]
        public byte[] FileBytes { get; set; }

        [Key(9)]
        public string FileExtension { get; set; }
    }
}
