public sealed class ProtocolSender
{
    private readonly AndroidSerialService serialService;

    public ProtocolSender(AndroidSerialService serialService)
    {
        this.serialService = serialService;
    }

    public bool SendUnityReady()
    {
        return Send("UNITY_READY:app=" + ProtocolValidation.ApplicationName +
                    ",version=" + ProtocolValidation.ApplicationVersion +
                    ",protocol=" + ProtocolValidation.ProtocolVersion + "\r\n");
    }

    public bool SendAck(uint sequence)
    {
        return Send("ACK:seq=" + sequence + ",status=OK\r\n");
    }

    public bool SendRaceOver(uint sequence, uint round)
    {
        return Send("RACE_OVER:seq=" + sequence + ",round=" + round + "\r\n");
    }

    public bool SendResultOver(uint sequence, uint round)
    {
        return Send("RESULT_OVER:seq=" + sequence + ",round=" + round + "\r\n");
    }

    public bool SendDemoRaceOver(uint sequence, uint round)
    {
        return Send("DEMO_RACE_OVER:seq=" + sequence + ",round=" + round + "\r\n");
    }

    public bool SendDemoResultOver(uint sequence, uint round)
    {
        return Send("DEMO_RESULT_OVER:seq=" + sequence + ",round=" + round + "\r\n");
    }

    public bool SendDisplayError(uint sequence, string reason)
    {
        string safeReason = string.IsNullOrEmpty(reason)
            ? "UNKNOWN"
            : reason.Replace(',', ';').Replace('\r', ' ').Replace('\n', ' ');
        return Send("DISPLAY_ERROR:seq=" + sequence + ",reason=" + safeReason + "\r\n");
    }

    public bool SendPong(uint sequence)
    {
        return Send("PONG:seq=" + sequence + "\r\n");
    }

    private bool Send(string line)
    {
        return serialService != null && serialService.SendText(line);
    }
}
