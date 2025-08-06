using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class FileTransferRequest : IMessage
    {
        [Key(1)]
        public int Id { get; set; }

        [Key(2)]
        public string RemotePath { get; set; }
    }
}
