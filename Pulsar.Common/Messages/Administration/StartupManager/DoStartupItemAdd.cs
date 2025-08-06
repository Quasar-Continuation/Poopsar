using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class DoStartupItemAdd : IMessage
    {
        [Key(1)]
        public StartupItem StartupItem { get; set; }
    }
}
