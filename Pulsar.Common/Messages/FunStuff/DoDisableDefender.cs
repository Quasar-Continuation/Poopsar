using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoDisableDefender : IMessage
    {
        // You can add parameters later if needed
        [Key(0)]
        public bool Disable { get; set; }

        public DoDisableDefender() { }

        public DoDisableDefender(bool disable)
        {
            Disable = disable;
        }
    }
}
