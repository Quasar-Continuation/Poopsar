using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using System.Collections.Generic;

namespace Pulsar.Common.Messages.Administration.StartupManager
{
    [MessagePackObject]
    public class GetStartupItemsResponse : IMessage
    {
        [Key(1)]
        public List<StartupItem> StartupItems { get; set; }
    }
}
