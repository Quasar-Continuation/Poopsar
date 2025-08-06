using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class GetDrivesResponse : IMessage
    {
        [Key(1)]
        public Drive[] Drives { get; set; }
    }
}
