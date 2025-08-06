using System;
using MessagePack;
using Pulsar.Common.Networking;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class PingRequest : IMessage
    {
    }
}
