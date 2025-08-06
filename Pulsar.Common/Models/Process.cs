using MessagePack;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class Process
    {
        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public int Id { get; set; }

        [Key(3)]
        public string MainWindowTitle { get; set; }
    }
}
