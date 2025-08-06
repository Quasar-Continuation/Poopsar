using MessagePack;
using Pulsar.Common.Messages.Other;
using System;
using System.Collections.Generic;

namespace Pulsar.Common.Messages.Audio
{
    [MessagePackObject]
    public class GetMicrophoneDeviceResponse : IMessage
    {
        [Key(1)]
        public List<Tuple<int, string>> DeviceInfos { get; set; }
    }
}
