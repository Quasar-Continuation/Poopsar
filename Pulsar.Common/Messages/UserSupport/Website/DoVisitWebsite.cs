using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.UserSupport.Website
{
    [MessagePackObject]
    public class DoVisitWebsite : IMessage
    {
        [Key(1)]
        public string Url { get; set; }

        [Key(2)]
        public bool Hidden { get; set; }
    }
}
