using Microsoft.Win32;
using MessagePack;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class RegValueData
    {
        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public RegistryValueKind Kind { get; set; }

        [Key(3)]
        public byte[] Data { get; set; }
    }
}
