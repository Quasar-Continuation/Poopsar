using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class FileTransferChunk : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

        [Key(2)]
        public string FilePath { get; set; }

        [Key(3)]
        public long FileSize { get; set; }

        [Key(4)]
        public FileChunk Chunk { get; set; }

        [Key(5)]
        public string FileExtension { get; set; }
    }
}
