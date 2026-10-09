public sealed class DisplaySnapshot
{
    public DisplaySnapshot(
        DisplayMode mode,
        DisplayPhase phase,
        DisplayPhase resumePhase,
        DisplayPage page,
        uint sequence,
        uint round,
        int credit,
        int winTotal,
        int winRemaining,
        int bonus,
        int totalBet,
        int selectedId,
        int winnerId,
        int[] bets,
        int[] odds,
        bool collectActive,
        int collectRemainingMilliseconds,
        int faultCode)
    {
        Mode = mode;
        Phase = phase;
        ResumePhase = resumePhase;
        Page = page;
        Sequence = sequence;
        Round = round;
        Credit = credit;
        WinTotal = winTotal;
        WinRemaining = winRemaining;
        Bonus = bonus;
        TotalBet = totalBet;
        SelectedId = selectedId;
        WinnerId = winnerId;
        Bets = bets == null ? new int[AnimalCatalog.Count] : (int[])bets.Clone();
        Odds = odds == null ? new[] { 24, 24, 8, 6, 3, 3 } : (int[])odds.Clone();
        CollectActive = collectActive;
        CollectRemainingMilliseconds = collectRemainingMilliseconds;
        FaultCode = faultCode;
    }

    public DisplayMode Mode { get; private set; }
    public DisplayPhase Phase { get; private set; }
    public DisplayPhase ResumePhase { get; private set; }
    public DisplayPage Page { get; private set; }
    public uint Sequence { get; private set; }
    public uint Round { get; private set; }
    public int Credit { get; private set; }
    public int WinTotal { get; private set; }
    public int WinRemaining { get; private set; }
    public int Bonus { get; private set; }
    public int TotalBet { get; private set; }
    public int SelectedId { get; private set; }
    public int WinnerId { get; private set; }
    public int[] Bets { get; private set; }
    public int[] Odds { get; private set; }
    public bool CollectActive { get; private set; }
    public int CollectRemainingMilliseconds { get; private set; }
    public int FaultCode { get; private set; }
}
