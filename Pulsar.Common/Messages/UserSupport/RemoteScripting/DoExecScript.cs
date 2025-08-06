using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.UserSupport.MessageBox
{
    [MessagePackObject]
    public class DoExecScript : IMessage
    {
        [Key(1)]
        public string Language { get; set; }

        [Key(2)]
        public string Script { get; set; }
        [Key(3)]
        public bool Hidden { get; set; }
    }
}
