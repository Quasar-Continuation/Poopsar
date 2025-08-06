using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.UserSupport.RemoteChat
{
    [MessagePackObject]
    public class DoStartChatForm : IMessage
    {
        [Key(1)]
        public string Title { get; set; }

        [Key(2)]
        public string WelcomeMessage { get; set; }
        [Key(3)]
        public bool TopMost { get; set; }
        [Key(4)]
        public bool DisableClose { get; set; }
        [Key(5)]
        public bool DisableType { get; set; }
    }
}
