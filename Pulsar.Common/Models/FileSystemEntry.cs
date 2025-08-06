using MessagePack;
using Pulsar.Common.Enums;
using System;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class FileSystemEntry
    {
        [Key(1)]
        public FileType EntryType { get; set; }

        [Key(2)]
        public string Name { get; set; }

        [Key(3)]
        public long Size { get; set; }

        [Key(4)]
        public DateTime LastAccessTimeUtc { get; set; }

        [Key(5)]
        public ContentType? ContentType { get; set; }
    }
}
