using System;

public enum ProtocolCommand
{
    Hello,
    FullState,
    ShowIdle,
    ShowBetting,
    BalanceUpdate,
    ShowRace,
    ShowResult,
    ShowWinReady,
    WinTransferUpdate,
    WinTransferDone,
    ShowSetting,
    ShowPayout,
    ShowPayoutProgress,
    ShowPayoutDone,
    ShowFault,
    Ping,
    Pong
}

public enum DisplayPage
{
    Loading,
    Idle,
    Betting,
    Race,
    Result,
    WinReady,
    WinCollecting,
    Setting,
    Payout,
    Fault
}

public enum DisplayMode
{
    Boot,
    Run,
    Setting,
    Fault,
    Service
}

public enum DisplayPhase
{
    Unknown,
    Idle,
    Betting,
    RacePrepare,
    WaitRaceOver,
    ResultPrepare,
    WaitResultOver,
    WinWaitStart,
    WinCollecting,
    RoundReset,
    Payout,
    Ticket
}

public static class AnimalCatalog
{
    private static readonly string[] Names =
    {
        "\u72EE\u5B50", "\u5927\u8C61", "\u7329\u7329", "\u6591\u9A6C", "\u957F\u9888\u9E7F", "\u9A86\u9A7C"
    };

    private static readonly string[] ResourceNames =
    {
        "Animal_01_Lion", "Animal_02_Elephant", "Animal_03_Gorilla",
        "Animal_04_Zebra", "Animal_05_Giraffe", "Animal_06_Camel"
    };

    public const int Count = 6;

    public static bool IsValidId(int animalId)
    {
        return animalId >= 1 && animalId <= Count;
    }

    public static string GetName(int animalId)
    {
        return IsValidId(animalId) ? Names[animalId - 1] : "未知动物";
    }

    public static string GetResourceName(int animalId)
    {
        return IsValidId(animalId) ? ResourceNames[animalId - 1] : string.Empty;
    }
}

public abstract class ProtocolMessage
{
    protected ProtocolMessage(ProtocolCommand command, string rawLine)
    {
        Command = command;
        RawLine = rawLine;
    }

    public ProtocolCommand Command { get; private set; }
    public string RawLine { get; private set; }
}

public abstract class SequencedProtocolMessage : ProtocolMessage
{
    protected SequencedProtocolMessage(ProtocolCommand command, string rawLine, uint sequence)
        : base(command, rawLine)
    {
        Sequence = sequence;
    }

    public uint Sequence { get; private set; }
}

public sealed class HelloMessage : ProtocolMessage
{
    public HelloMessage(string rawLine, int deviceId, string firmware, string protocolVersion)
        : base(ProtocolCommand.Hello, rawLine)
    {
        DeviceId = deviceId;
        Firmware = firmware;
        ProtocolVersion = protocolVersion;
    }

    public int DeviceId { get; private set; }
    public string Firmware { get; private set; }
    public string ProtocolVersion { get; private set; }
}

public sealed class FullStateMessage : SequencedProtocolMessage
{
    public FullStateMessage(
        string rawLine,
        uint sequence,
        string mode,
        string phase,
        string resumePhase,
        uint round,
        int credit,
        int totalBet,
        int selectedId,
        int[] bets,
        int[] odds,
        int winnerId,
        int winTotal,
        int winRemaining,
        int lastWin,
        uint raceSequence,
        int raceRemainingMilliseconds,
        uint resultSequence,
        int resultRemainingMilliseconds,
        uint collectSequence,
        bool collectActive,
        int collectRemainingMilliseconds,
        int payoutTarget,
        int payoutDone,
        int settingIndex,
        int faultCode)
        : base(ProtocolCommand.FullState, rawLine, sequence)
    {
        Mode = mode;
        Phase = phase;
        ResumePhase = resumePhase;
        Round = round;
        Credit = credit;
        TotalBet = totalBet;
        SelectedId = selectedId;
        Bets = bets;
        Odds = odds;
        WinnerId = winnerId;
        WinTotal = winTotal;
        WinRemaining = winRemaining;
        LastWin = lastWin;
        RaceSequence = raceSequence;
        RaceRemainingMilliseconds = raceRemainingMilliseconds;
        ResultSequence = resultSequence;
        ResultRemainingMilliseconds = resultRemainingMilliseconds;
        CollectSequence = collectSequence;
        CollectActive = collectActive;
        CollectRemainingMilliseconds = collectRemainingMilliseconds;
        PayoutTarget = payoutTarget;
        PayoutDone = payoutDone;
        SettingIndex = settingIndex;
        FaultCode = faultCode;
    }

    public string Mode { get; private set; }
    public string Phase { get; private set; }
    public string State { get { return Phase; } }
    public string ResumePhase { get; private set; }
    public uint Round { get; private set; }
    public int Credit { get; private set; }
    public int Balance { get { return Credit; } }
    public int TotalBet { get; private set; }
    public int SelectedId { get; private set; }
    public int[] Bets { get; private set; }
    public int[] Odds { get; private set; }
    public int WinnerId { get; private set; }
    public int WinTotal { get; private set; }
    public int WinRemaining { get; private set; }
    public int LastWin { get; private set; }
    public uint RaceSequence { get; private set; }
    public int RaceRemainingMilliseconds { get; private set; }
    public uint ResultSequence { get; private set; }
    public int ResultRemainingMilliseconds { get; private set; }
    public uint CollectSequence { get; private set; }
    public bool CollectActive { get; private set; }
    public int CollectRemainingMilliseconds { get; private set; }
    public int PayoutTarget { get; private set; }
    public int PendingPayout { get { return Math.Max(0, PayoutTarget - PayoutDone); } }
    public int PayoutDone { get; private set; }
    public int SettingIndex { get; private set; }
    public int FaultCode { get; private set; }
}

public sealed class ShowIdleMessage : SequencedProtocolMessage
{
    public ShowIdleMessage(string rawLine, uint sequence, uint round, int credit, int lastWin, int winRemaining)
        : base(ProtocolCommand.ShowIdle, rawLine, sequence)
    {
        Round = round;
        Credit = credit;
        LastWin = lastWin;
        WinRemaining = winRemaining;
    }

    public uint Round { get; private set; }
    public int Credit { get; private set; }
    public int Balance { get { return Credit; } }
    public int LastWin { get; private set; }
    public int WinRemaining { get; private set; }
}

public sealed class ShowBettingMessage : SequencedProtocolMessage
{
    public ShowBettingMessage(string rawLine, uint sequence, uint round, int credit, int totalBet, int selectedId, int[] bets, int[] odds)
        : base(ProtocolCommand.ShowBetting, rawLine, sequence)
    {
        Round = round;
        Credit = credit;
        TotalBet = totalBet;
        SelectedId = selectedId;
        Bets = bets;
        Odds = odds;
    }

    public uint Round { get; private set; }
    public int Credit { get; private set; }
    public int Balance { get { return Credit; } }
    public int TotalBet { get; private set; }
    public int SelectedId { get; private set; }
    public int[] Bets { get; private set; }
    public int[] Odds { get; private set; }
}

public sealed class BalanceUpdateMessage : SequencedProtocolMessage
{
    public BalanceUpdateMessage(string rawLine, uint sequence, uint round, int credit, int win, int delta, string source)
        : base(ProtocolCommand.BalanceUpdate, rawLine, sequence)
    {
        Round = round;
        Credit = credit;
        Win = win;
        Delta = delta;
        Source = source;
    }

    public uint Round { get; private set; }
    public int Credit { get; private set; }
    public int Win { get; private set; }
    public int Delta { get; private set; }
    public string Source { get; private set; }
}

public sealed class ShowRaceMessage : SequencedProtocolMessage
{
    public ShowRaceMessage(string rawLine, uint sequence, uint round, int winnerId, int durationMilliseconds, int[] bets, int[] odds, bool isDemo)
        : base(ProtocolCommand.ShowRace, rawLine, sequence)
    {
        Round = round;
        WinnerId = winnerId;
        DurationMilliseconds = durationMilliseconds;
        Bets = bets;
        Odds = odds;
        IsDemo = isDemo;
    }

    public uint Round { get; private set; }
    public int WinnerId { get; private set; }
    public int DurationMilliseconds { get; private set; }
    public int[] Bets { get; private set; }
    public int[] Odds { get; private set; }
    public bool IsDemo { get; private set; }
}

public sealed class ShowResultMessage : SequencedProtocolMessage
{
    public ShowResultMessage(string rawLine, uint sequence, uint round, string result, int winnerId, int odds, int winTotal, int winRemaining, int credit, int durationMilliseconds, bool isDemo)
        : base(ProtocolCommand.ShowResult, rawLine, sequence)
    {
        Round = round;
        Result = result;
        WinnerId = winnerId;
        Odds = odds;
        WinTotal = winTotal;
        WinRemaining = winRemaining;
        Credit = credit;
        DurationMilliseconds = durationMilliseconds;
        IsDemo = isDemo;
    }

    public uint Round { get; private set; }
    public string Result { get; private set; }
    public int WinnerId { get; private set; }
    public int Odds { get; private set; }
    public int WinTotal { get; private set; }
    public int WinRemaining { get; private set; }
    public int WinPoints { get { return WinTotal; } }
    public int Credit { get; private set; }
    public int Balance { get { return Credit; } }
    public int DurationMilliseconds { get; private set; }
    public bool IsDemo { get; private set; }
}

public sealed class ShowWinReadyMessage : SequencedProtocolMessage
{
    public ShowWinReadyMessage(string rawLine, uint sequence, uint round, int win, int credit)
        : base(ProtocolCommand.ShowWinReady, rawLine, sequence)
    {
        Round = round;
        Win = win;
        Credit = credit;
    }

    public uint Round { get; private set; }
    public int Win { get; private set; }
    public int Credit { get; private set; }
}

public sealed class WinTransferUpdateMessage : SequencedProtocolMessage
{
    public WinTransferUpdateMessage(string rawLine, uint sequence, uint round, int win, int credit, int delta, string source)
        : base(ProtocolCommand.WinTransferUpdate, rawLine, sequence)
    {
        Round = round;
        Win = win;
        Credit = credit;
        Delta = delta;
        Source = source;
    }

    public uint Round { get; private set; }
    public int Win { get; private set; }
    public int Credit { get; private set; }
    public int Delta { get; private set; }
    public string Source { get; private set; }
}

public sealed class WinTransferDoneMessage : SequencedProtocolMessage
{
    public WinTransferDoneMessage(string rawLine, uint sequence, uint round, int win, int credit, bool fast)
        : base(ProtocolCommand.WinTransferDone, rawLine, sequence)
    {
        Round = round;
        Win = win;
        Credit = credit;
        Fast = fast;
    }

    public uint Round { get; private set; }
    public int Win { get; private set; }
    public int Credit { get; private set; }
    public bool Fast { get; private set; }
}

public sealed class ShowSettingMessage : SequencedProtocolMessage
{
    public ShowSettingMessage(string rawLine, uint sequence, int index, int total, string item, string value, string minimum, string maximum, string resumePhase)
        : base(ProtocolCommand.ShowSetting, rawLine, sequence)
    {
        Index = index;
        Total = total;
        Item = item;
        Value = value;
        Minimum = minimum;
        Maximum = maximum;
        ResumePhase = resumePhase;
    }

    public int Index { get; private set; }
    public int Total { get; private set; }
    public string Item { get; private set; }
    public string Value { get; private set; }
    public string Minimum { get; private set; }
    public string Maximum { get; private set; }
    public string ResumePhase { get; private set; }
}

public sealed class ShowPayoutMessage : SequencedProtocolMessage
{
    public ShowPayoutMessage(string rawLine, uint sequence, int balanceBefore, int coins, int remainder)
        : base(ProtocolCommand.ShowPayout, rawLine, sequence)
    {
        BalanceBefore = balanceBefore;
        Coins = coins;
        Remainder = remainder;
    }

    public int BalanceBefore { get; private set; }
    public int Coins { get; private set; }
    public int Remainder { get; private set; }
}

public sealed class ShowPayoutProgressMessage : SequencedProtocolMessage
{
    public ShowPayoutProgressMessage(string rawLine, uint sequence, int target, int done, int remainder)
        : base(ProtocolCommand.ShowPayoutProgress, rawLine, sequence)
    {
        Target = target;
        Done = done;
        Remainder = remainder;
    }

    public int Target { get; private set; }
    public int Done { get; private set; }
    public int Remainder { get; private set; }
}

public sealed class ShowPayoutDoneMessage : SequencedProtocolMessage
{
    public ShowPayoutDoneMessage(string rawLine, uint sequence, int coins, int credit)
        : base(ProtocolCommand.ShowPayoutDone, rawLine, sequence)
    {
        Coins = coins;
        Credit = credit;
    }

    public int Coins { get; private set; }
    public int Credit { get; private set; }
    public int Balance { get { return Credit; } }
}

public sealed class ShowFaultMessage : SequencedProtocolMessage
{
    public ShowFaultMessage(string rawLine, uint sequence, int code, string detail, bool recoverable)
        : base(ProtocolCommand.ShowFault, rawLine, sequence)
    {
        Code = code;
        Detail = detail;
        Recoverable = recoverable;
    }

    public int Code { get; private set; }
    public string Detail { get; private set; }
    public bool Recoverable { get; private set; }
}

public sealed class LinkMessage : ProtocolMessage
{
    public LinkMessage(ProtocolCommand command, string rawLine, uint sequence)
        : base(command, rawLine)
    {
        Sequence = sequence;
    }

    public uint Sequence { get; private set; }
}
