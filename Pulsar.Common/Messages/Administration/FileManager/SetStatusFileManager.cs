using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [MessagePackObject]
    public class SetStatusFileManager : IMessage
    {
        [Key(1)]
        public string Message { get; set; }

        [Key(2)]
        public bool SetLastDirectorySeen { get; set; }
    }
}
