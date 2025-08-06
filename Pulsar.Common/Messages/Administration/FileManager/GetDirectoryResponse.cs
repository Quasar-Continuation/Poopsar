using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class GetDirectoryResponse : IMessage
    {
        [Key(1)]
        public string RemotePath { get; set; }

        [Key(2)]
        public FileSystemEntry[] Items { get; set; }
    }
}
