using System;
using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class PingResponse : IMessage
    {
    }
}
