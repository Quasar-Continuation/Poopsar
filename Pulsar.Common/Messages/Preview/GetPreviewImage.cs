using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Preview
{
    [MessagePackObject]
    public class GetPreviewImage : IMessage
    {
        [Key(2)]
        public int Quality { get; set; }

        [Key(3)]
        public int DisplayIndex { get; set; }
    }
}
