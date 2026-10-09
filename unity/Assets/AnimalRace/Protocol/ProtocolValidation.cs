public static class ProtocolValidation
{
    public const string ApplicationName = "AnimalRace";
    public const string ApplicationVersion = "1.2.0";
    public const string ProtocolVersion = "3.2";
    public const int NormalFrameLimit = 1024;
    public const int FullStateFrameLimit = 1024;

    public static bool IsCriticalDisplayCommand(ProtocolCommand command)
    {
        return command == ProtocolCommand.FullState ||
               command == ProtocolCommand.ShowRace ||
               command == ProtocolCommand.ShowResult ||
               command == ProtocolCommand.ShowWinReady;
    }

    public static bool RequiresAck(ProtocolCommand command)
    {
        return command != ProtocolCommand.Hello &&
               command != ProtocolCommand.Ping &&
               command != ProtocolCommand.Pong;
    }
}
