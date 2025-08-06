using MessagePack;
using Pulsar.Common.Enums;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class StartupItem
    {
        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public string Path { get; set; }

        [Key(3)]
        public StartupType Type { get; set; }
    }
}
