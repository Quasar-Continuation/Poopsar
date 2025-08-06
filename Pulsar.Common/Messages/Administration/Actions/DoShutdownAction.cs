using MessagePack;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.Actions
{
    [MessagePackObject]
    public class DoShutdownAction : IMessage
    {
        [Key(1)]
        public ShutdownAction Action { get; set; }
    }
}
