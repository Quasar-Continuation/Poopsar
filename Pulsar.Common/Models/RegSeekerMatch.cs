using MessagePack;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class RegSeekerMatch
    {
        [Key(1)]
        public string Key { get; set; }

        [Key(2)]
        public RegValueData[] Data { get; set; }

        [Key(3)]
        public bool HasSubKeys { get; set; }

        public override string ToString()
        {
            return $"({Key}:{Data})";
        }
    }
}
