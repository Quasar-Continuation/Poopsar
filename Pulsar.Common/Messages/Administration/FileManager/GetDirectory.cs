using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class GetDirectory : IMessage
    {
        [Key(1)]
        public string RemotePath { get; set; }
    }
}
