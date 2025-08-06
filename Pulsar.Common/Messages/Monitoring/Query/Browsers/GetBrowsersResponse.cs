using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models.Query.Browsers;
using System.Collections.Generic;

namespace Pulsar.Common.Messages.Monitoring.Query.Browsers
{
    [MessagePackObject]
    public class GetBrowsersResponse : IMessage
    {
        [Key(1)]
        public List<QueryBrowsers> QueryBrowsers { get; set; }
    }
}
