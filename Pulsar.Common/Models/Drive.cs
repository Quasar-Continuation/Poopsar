using MessagePack;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class Drive
    {
        [Key(1)]
        public string DisplayName { get; set; }

        [Key(2)]
        public string RootDirectory { get; set; }
    }
}
