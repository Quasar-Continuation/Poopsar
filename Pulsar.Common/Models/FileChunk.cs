using MessagePack;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class FileChunk
    {
        [Key(1)]
        public long Offset { get; set; }

        [Key(2)]
        public byte[] Data { get; set; }
    }
}
