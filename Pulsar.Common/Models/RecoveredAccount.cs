using MessagePack;

namespace Pulsar.Common.Models
{
    [MessagePackObject]
    public class RecoveredAccount
    {
        [Key(1)]
        public string Username { get; set; }

        [Key(2)]
        public string Password { get; set; }

        [Key(3)]
        public string Url { get; set; }

        [Key(4)]
        public string Application { get; set; }
    }
}
