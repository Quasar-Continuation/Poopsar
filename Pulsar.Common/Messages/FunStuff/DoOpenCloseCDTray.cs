using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoCDTray : IMessage
    {
        [Key(0)]
        public bool Open { get; set; }

        public DoCDTray() { }

        public DoCDTray(bool open)
        {
            Open = open;
        }
    }
}
