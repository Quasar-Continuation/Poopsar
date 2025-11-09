using MessagePack;
using Pulsar.Common.Messages.Other;

[MessagePackObject]
public class DoSuspendProcess : IMessage
{
    [Key(1)]
    public int Pid { get; set; }

    [Key(2)]
    public bool Suspend { get; set; } // true = suspend, false = resume
}
