public sealed class DisplaySyncManager
{
    private readonly DuplicateSequenceCache duplicates = new DuplicateSequenceCache();
    private ProtocolSender sender;

    public DisplaySyncManager(AndroidSerialService serialService)
    {
        sender = new ProtocolSender(serialService);
    }

    public void SetSerialService(AndroidSerialService serialService)
    {
        sender = new ProtocolSender(serialService);
    }

    public bool ShouldApply(ProtocolMessage message)
    {
        SequencedProtocolMessage sequenced = message as SequencedProtocolMessage;
        if (sequenced == null)
        {
            return true;
        }

        if (message.Command == ProtocolCommand.FullState)
        {
            duplicates.Clear();
            duplicates.TryRemember(message.Command, sequenced.Sequence);
            var state=(FullStateMessage)message;
            if(state.RaceSequence!=0) duplicates.TryRemember(ProtocolCommand.ShowRace,state.RaceSequence);
            if(state.ResultSequence!=0) duplicates.TryRemember(ProtocolCommand.ShowResult,state.ResultSequence);
            if(state.CollectSequence!=0) duplicates.TryRemember(ProtocolCommand.WinTransferUpdate,state.CollectSequence);
            return true;
        }
        return duplicates.TryRemember(message.Command, sequenced.Sequence);
    }

    public void Acknowledge(ProtocolMessage message)
    {
        SequencedProtocolMessage sequenced = message as SequencedProtocolMessage;
        if (sequenced != null && ProtocolValidation.RequiresAck(message.Command))
        {
            sender.SendAck(sequenced.Sequence);
        }
        LinkMessage link = message as LinkMessage;
        if (link != null && link.Command == ProtocolCommand.Ping)
        {
            sender.SendPong(link.Sequence);
        }
    }

    public bool SendUnityReady()
    {
        return sender.SendUnityReady();
    }
}
