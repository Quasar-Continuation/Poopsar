using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoBlockKeyboardInput : IMessage
    {
        [Key(0)]
        public bool Block { get; set; }

        public DoBlockKeyboardInput() { }

        public DoBlockKeyboardInput(bool block)
        {
            Block = block;
        }
    }
}