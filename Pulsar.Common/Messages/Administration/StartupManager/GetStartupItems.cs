using MessagePack;
using Pulsar.Common.Messages.Other;

namespace Pulsar.Common.Messages.Administration.StartupManager
{
    [MessagePackObject]
    public class GetStartupItems : IMessage
    {
    }
}
