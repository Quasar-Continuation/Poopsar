using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class DoZipFolder : IMessage
    {
        [Key(1)]
        public string SourcePath { get; set; }

        [Key(2)]
        public string DestinationPath { get; set; }

        [Key(3)]
        public int CompressionLevel { get; set; } = (int)System.IO.Compression.CompressionLevel.Optimal;
    }
}
