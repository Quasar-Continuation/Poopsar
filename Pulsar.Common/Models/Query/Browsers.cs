using MessagePack;

namespace Pulsar.Common.Models.Query.Browsers
{
    [MessagePackObject]
    public class QueryBrowsers
    {
        [Key(1)]
        public string Location { get; set; }

        [Key(2)]
        public string Browser { get; set; }
    }
}
