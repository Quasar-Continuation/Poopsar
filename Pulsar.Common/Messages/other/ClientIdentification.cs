using MessagePack;

namespace Pulsar.Common.Messages.Other
{
    [MessagePackObject]
    public class ClientIdentification : IMessage
    {
        [Key(1)]
        public string Version { get; set; }

        [Key(2)]
        public string OperatingSystem { get; set; }

        [Key(3)]
        public string AccountType { get; set; }

        [Key(4)]
        public string Country { get; set; }

        [Key(5)]
        public string CountryCode { get; set; }

        [Key(6)]
        public int ImageIndex { get; set; }

        [Key(7)]
        public string Id { get; set; }

        [Key(8)]
        public string Username { get; set; }

        [Key(9)]
        public string PcName { get; set; }

        [Key(10)]
        public string Tag { get; set; }

        [Key(11)]
        public string EncryptionKey { get; set; }

        [Key(12)]
        public byte[] Signature { get; set; }
    }
}
