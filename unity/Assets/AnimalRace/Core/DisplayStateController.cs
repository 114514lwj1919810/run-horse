using System;
using UnityEngine;

public sealed class DisplayStateController : MonoBehaviour
{
    public const float WinCollectPointsPerSecond = 10f;
    public const float WinCollectDoneHoldSeconds = 0.35f;
    public const float WinCollectFastDoneHoldSeconds = 0.75f;

    public Font uiFont;

    [SerializeField]
    private AndroidSerialService serialService;

    [SerializeField]
    private SerialDebugPanel debugPanel;

    [SerializeField]
    private bool showLegacyUi;

    private readonly GameProtocolParser parser = new GameProtocolParser();
    private DisplaySyncManager displaySync;
    private int[] bets = new int[AnimalCatalog.Count];
    private int[] odds = { 24, 24, 8, 6, 3, 3 };
    private readonly int[] winnerHistory = new int[10];
    private uint lastWinnerHistoryRound = uint.MaxValue;
    private bool subscribed;
    private float nextReadyAllowedAt;
    private ShowResultMessage deferredResult;
    private ProtocolMessage deferredPostResultMessage;
    private ProtocolMessage deferredPostCollectMessage;
    private bool racePresentationCompleted;
    private bool resultPresentationCompleted;
    private bool collectDonePresentationActive;
    private float collectDoneShownAt;
    private float collectPresentationStartedAt = -1f;

    public event Action DisplayChanged;
    public event Action<DisplaySnapshot> SnapshotChanged;

    public DisplayMode CurrentMode { get; private set; }
    public DisplayPhase CurrentPhase { get; private set; }
    public DisplayPhase ResumePhase { get; private set; }
    public DisplayPage CurrentPage { get; private set; }
    public uint CurrentSequence { get; private set; }
    public uint CurrentRound { get; private set; }
    public int Balance { get; private set; }
    public int Credit { get { return Balance; } }
    public int TotalBet { get; private set; }
    public int SelectedId { get; private set; }
    public int WinnerId { get; private set; }
    public int WinPoints { get; private set; }
    public int WinTotalPoints { get; private set; }
    public int BonusPoints { get; private set; }
    public int PayoutCoins { get; private set; }
    public int PayoutDoneCoins { get; private set; }
    public int Remainder { get; private set; }
    public int RaceDurationMilliseconds { get; private set; }
    public int ResultDurationMilliseconds { get; private set; }
    public int CollectRemainingMilliseconds { get; private set; }
    public int CollectDelta { get; private set; }
    public string CollectSource { get; private set; }
    public bool CollectActive { get; private set; }
    public bool CollectFastFinished { get; private set; }
    public int ResultOdds { get; private set; }
    public string ResultStatus { get; private set; }
    public bool PayoutComplete { get; private set; }
    public string SettingItem { get; private set; }
    public string SettingValue { get; private set; }
    public string SettingMinimum { get; private set; }
    public string SettingMaximum { get; private set; }
    public int SettingIndex { get; private set; }
    public int SettingTotal { get; private set; }
    public int FaultCode { get; private set; }
    public string FaultDetail { get; private set; }
    public bool FaultRecoverable { get; private set; }
    public string LastRawLine { get; private set; }
    public string LastError { get; private set; }
    public string DeviceInfo { get; private set; }
    public string SettingInfo { get; private set; }
    public string FaultInfo { get; private set; }
    public bool HandshakeComplete { get; private set; }
    public bool IsStandbyDemo { get; private set; }
    public readonly MachineSettingsClient Machine = new MachineSettingsClient();
    public int ReadySendCount { get; private set; }
    public DisplaySnapshot CurrentSnapshot { get; private set; }

    public bool ShowLegacyUi
    {
        get { return showLegacyUi; }
        set { showLegacyUi = value; }
    }

    public AndroidSerialService SerialService
    {
        get { return serialService; }
        set
        {
            serialService = value;
            if (displaySync == null)
            {
                displaySync = new DisplaySyncManager(serialService);
            }
            else
            {
                displaySync.SetSerialService(serialService);
            }
        }
    }

    public SerialDebugPanel DebugPanel
    {
        get { return debugPanel; }
        set { debugPanel = value; }
    }

    public int[] Bets { get { return (int[])bets.Clone(); } }
    public int[] Odds { get { return (int[])odds.Clone(); } }
    public int[] WinnerHistory { get { return (int[])winnerHistory.Clone(); } }
    public string WinnerName { get { return AnimalCatalog.GetName(WinnerId); } }

    private void Awake()
    {
        CurrentMode = DisplayMode.Boot;
        CurrentPhase = DisplayPhase.Unknown;
        ResumePhase = DisplayPhase.Unknown;
        CurrentPage = DisplayPage.Loading;
        SelectedId = 1;
        LastRawLine = "等待协议消息";
        LastError = string.Empty;
        DeviceInfo = "等待 hello / FULL_STATE V3.1";
        SettingInfo = string.Empty;
        FaultInfo = string.Empty;
        ResultStatus = string.Empty;
        CollectSource = string.Empty;
        SettingItem = string.Empty;
        SettingValue = string.Empty;
        SettingMinimum = string.Empty;
        SettingMaximum = string.Empty;
        FaultDetail = string.Empty;
        RaceDurationMilliseconds = 15000;
        ResultDurationMilliseconds = 3000;

        if (serialService == null)
        {
            serialService = FindObjectOfType<AndroidSerialService>();
        }
        if (debugPanel == null)
        {
            debugPanel = FindObjectOfType<SerialDebugPanel>();
        }
        displaySync = new DisplaySyncManager(serialService);
        PublishSnapshot();
    }

    private void Start()
    {
        SubscribeToSerial();
        if (serialService != null && serialService.IsOpen)
        {
            BeginHandshake();
        }
    }

    private void Update()
    {
        if (collectDonePresentationActive && IsWinCollectPresentationComplete(Time.unscaledTime))
        {
            CompleteWinTransferPresentation(CurrentRound);
        }
    }

    private bool IsWinCollectPresentationComplete(float now)
    {
        if (CollectFastFinished)
        {
            return now - collectDoneShownAt >= WinCollectFastDoneHoldSeconds;
        }

        float startedAt = collectPresentationStartedAt >= 0f
            ? collectPresentationStartedAt
            : collectDoneShownAt;
        return now - startedAt >= GetWinCollectVisualDurationSeconds(WinTotalPoints) &&
               now - collectDoneShownAt >= WinCollectDoneHoldSeconds;
    }

    public static float GetWinCollectVisualDurationSeconds(int winPoints)
    {
        return Mathf.Max(0, winPoints) / WinCollectPointsPerSecond;
    }

    private void SubscribeToSerial()
    {
        if (!subscribed && serialService != null)
        {
            serialService.OnLineReceived += ProcessSerialLine;
            serialService.OnConnectionChanged += HandleConnectionChanged;
            subscribed = true;
        }
    }

    private void HandleConnectionChanged(bool open)
    {
        Machine.Reset();
        HandshakeComplete = false;
        if (open)
        {
            BeginHandshake();
        }
    }

    private void ProcessSerialLine(string rawLine)
    {
        ProcessRawLine(rawLine);
    }

    public bool ProcessRawLine(string rawLine)
    {
        EnsureDisplaySync();
        Machine.Send = SendMachineCommand;
        if (MachineSettingsClient.IsFrame(rawLine)) return Machine.Process(rawLine);
        ProtocolMessage message;
        string error;
        if (!parser.TryParse(rawLine, out message, out error))
        {
            LastError = error;
            if (debugPanel != null)
            {
                debugPanel.AddError("Protocol: " + error + " | " + rawLine);
            }
            Debug.LogWarning("Protocol parse failed: " + error + " | " + rawLine);
            return false;
        }

        LastError = string.Empty;
        LastRawLine = message.RawLine;
        bool shouldApply = displaySync == null || displaySync.ShouldApply(message);
        bool deferred = shouldApply && TryDeferUntilRaceCompletes(message);
        if (shouldApply && !deferred)
        {
            ApplyMessage(message);
        }

        if (message.Command == ProtocolCommand.Hello)
        {
            Machine.Reset();
            bool wasComplete = HandshakeComplete;
            HandshakeComplete = false;
            SendUnityReady(wasComplete);
        }
        else if (message.Command == ProtocolCommand.FullState)
        {
            HandshakeComplete = true;
        }

        if (displaySync != null)
        {
            displaySync.Acknowledge(message);
        }

        if (shouldApply && !deferred)
        {
            PublishSnapshot();
        }
        return true;
    }

    private bool TryDeferResultLegacy(ProtocolMessage message)
    {
        ShowResultMessage result = message as ShowResultMessage;
        if (result == null || CurrentPage != DisplayPage.Race || result.Round != CurrentRound)
        {
            return false;
        }

        deferredResult = result;
        DeviceInfo = "比赛动画进行中，结果已缓存";
        return true;
    }

    private bool TryDeferUntilRaceCompletes(ProtocolMessage message)
    {
        ShowResultMessage incomingResult = message as ShowResultMessage;
        if (IsStandbyDemo && (incomingResult == null || !incomingResult.IsDemo))
        {
            // Coin/service/paid-game authority always interrupts an attract-mode presentation.
            return false;
        }

        ShowResultMessage result = incomingResult;
        if (result != null && CurrentPage == DisplayPage.Race && result.Round == CurrentRound)
        {
            if (racePresentationCompleted)
            {
                return false;
            }

            deferredResult = result;
            DeviceInfo = "Race presentation active; result buffered";
            return true;
        }

        if ((CurrentPage == DisplayPage.Race ||
             (CurrentPage == DisplayPage.Result && racePresentationCompleted &&
              !resultPresentationCompleted)) &&
            IsPostResultTransitionForCurrentRound(message))
        {
            deferredPostResultMessage = message;
            DeviceInfo = "Timed presentation active; next page buffered";
            return true;
        }

        if (collectDonePresentationActive &&
            IsPostResultTransitionForCurrentRound(message))
        {
            deferredPostCollectMessage = message;
            DeviceInfo = "Win collect result active; next page buffered";
            return true;
        }

        return false;
    }

    private bool IsPostResultTransitionForCurrentRound(ProtocolMessage message)
    {
        ShowBettingMessage betting = message as ShowBettingMessage;
        if (betting != null)
        {
            return betting.Round == CurrentRound;
        }

        ShowIdleMessage idle = message as ShowIdleMessage;
        if (idle != null)
        {
            return idle.Round == CurrentRound;
        }

        ShowWinReadyMessage winReady = message as ShowWinReadyMessage;
        return winReady != null && winReady.Round == CurrentRound;
    }

    public bool CompleteRacePresentation(uint round)
    {
        racePresentationCompleted = true;
        if (deferredResult == null || deferredResult.Round != round)
        {
            return false;
        }

        ShowResultMessage result = deferredResult;
        deferredResult = null;
        ApplyResult(result);
        PublishSnapshot();
        return true;
    }

    public bool CompleteResultPresentation(uint round)
    {
        if (CurrentPage != DisplayPage.Result || CurrentRound != round)
        {
            return false;
        }

        resultPresentationCompleted = true;
        if (deferredPostResultMessage == null)
        {
            return false;
        }

        ProtocolMessage nextMessage = deferredPostResultMessage;
        deferredPostResultMessage = null;
        ApplyMessage(nextMessage);
        PublishSnapshot();
        return true;
    }

    public bool CompleteWinTransferPresentation(uint round)
    {
        if (!collectDonePresentationActive || CurrentRound != round)
        {
            return false;
        }

        collectDonePresentationActive = false;
        if (deferredPostCollectMessage != null)
        {
            ProtocolMessage nextMessage = deferredPostCollectMessage;
            deferredPostCollectMessage = null;
            ApplyMessage(nextMessage);
        }
        else
        {
            CurrentPhase = Balance > 0 ? DisplayPhase.Betting : DisplayPhase.Idle;
            CurrentPage = Balance > 0 ? DisplayPage.Betting : DisplayPage.Idle;
            ClearRoundDisplay();
        }

        PublishSnapshot();
        return true;
    }

    public bool BeginHandshake()
    {
        EnsureDisplaySync();
        HandshakeComplete = false;
        return SendUnityReady(true);
    }

    public bool SendMachineCommand(string line)
    {
        return HandshakeComplete && serialService != null && serialService.SendText(line + "\r\n");
    }

    private bool SendUnityReady(bool force)
    {
        float now = Time.realtimeSinceStartup;
        if (!force && now < nextReadyAllowedAt)
        {
            return false;
        }
        if (displaySync == null || !displaySync.SendUnityReady())
        {
            return false;
        }
        ReadySendCount++;
        nextReadyAllowedAt = now + 0.25f;
        DeviceInfo = "UNITY_READY V3.1 已发送，等待 FULL_STATE";
        return true;
    }

    private void EnsureDisplaySync()
    {
        if (displaySync == null)
        {
            displaySync = new DisplaySyncManager(serialService);
        }
    }

    private void ApplyMessage(ProtocolMessage message)
    {
        switch (message.Command)
        {
            case ProtocolCommand.Hello: ApplyHello((HelloMessage)message); break;
            case ProtocolCommand.FullState: ApplyFullState((FullStateMessage)message); break;
            case ProtocolCommand.ShowIdle: ApplyIdle((ShowIdleMessage)message); break;
            case ProtocolCommand.ShowBetting: ApplyBetting((ShowBettingMessage)message); break;
            case ProtocolCommand.BalanceUpdate: ApplyBalanceUpdate((BalanceUpdateMessage)message); break;
            case ProtocolCommand.ShowRace: ApplyRace((ShowRaceMessage)message); break;
            case ProtocolCommand.ShowResult: ApplyResult((ShowResultMessage)message); break;
            case ProtocolCommand.ShowWinReady: ApplyWinReady((ShowWinReadyMessage)message); break;
            case ProtocolCommand.WinTransferUpdate: ApplyWinTransferUpdate((WinTransferUpdateMessage)message); break;
            case ProtocolCommand.WinTransferDone: ApplyWinTransferDone((WinTransferDoneMessage)message); break;
            case ProtocolCommand.ShowSetting: ApplySetting((ShowSettingMessage)message); break;
            case ProtocolCommand.ShowPayout: ApplyPayout((ShowPayoutMessage)message); break;
            case ProtocolCommand.ShowPayoutProgress: ApplyPayoutProgress((ShowPayoutProgressMessage)message); break;
            case ProtocolCommand.ShowPayoutDone: ApplyPayoutDone((ShowPayoutDoneMessage)message); break;
            case ProtocolCommand.ShowFault: ApplyFault((ShowFaultMessage)message); break;
        }
    }

    private void ApplyHello(HelloMessage message)
    {
        DeviceInfo = message.RawLine == "hello"
            ? "收到 H743 hello"
            : "设备 " + message.DeviceId + "  固件 " + message.Firmware + "  协议 V" + message.ProtocolVersion;
    }

    private void ApplyFullState(FullStateMessage message)
    {
        deferredResult = null;
        deferredPostResultMessage = null;
        deferredPostCollectMessage = null;
        racePresentationCompleted = false;
        resultPresentationCompleted = false;
        collectDonePresentationActive = false;
        IsStandbyDemo = false;
        CurrentMode = ParseMode(message.Mode);
        CurrentPhase = ParsePhase(message.Phase);
        ResumePhase = ParsePhase(message.ResumePhase);
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        Balance = message.Credit;
        TotalBet = message.TotalBet;
        SelectedId = message.SelectedId;
        bets = (int[])message.Bets.Clone();
        odds = (int[])message.Odds.Clone();
        WinnerId = message.WinnerId;
        WinTotalPoints = message.WinTotal;
        WinPoints = message.WinRemaining;
        PayoutCoins = message.PayoutTarget;
        PayoutDoneCoins = message.PayoutDone;
        Remainder = message.Credit;
        PayoutComplete = message.PayoutTarget > 0 && message.PayoutDone >= message.PayoutTarget;
        CollectActive = message.CollectActive;
        CollectRemainingMilliseconds = message.CollectRemainingMilliseconds;
        FaultCode = message.FaultCode;
        FaultDetail = message.FaultCode == 0 ? string.Empty : "fault_" + message.FaultCode;
        FaultInfo = message.FaultCode == 0 ? string.Empty : "故障码 " + message.FaultCode;

        if (CurrentPhase == DisplayPhase.WaitRaceOver && message.RaceSequence != 0U)
        {
            CurrentSequence = message.RaceSequence;
            RaceDurationMilliseconds = message.RaceRemainingMilliseconds > 0
                ? message.RaceRemainingMilliseconds
                : RaceDurationMilliseconds;
        }
        else if (CurrentPhase == DisplayPhase.WaitResultOver && message.ResultSequence != 0U)
        {
            CurrentSequence = message.ResultSequence;
            ResultDurationMilliseconds = message.ResultRemainingMilliseconds > 0
                ? message.ResultRemainingMilliseconds
                : ResultDurationMilliseconds;
        }
        else if (CurrentPhase == DisplayPhase.WinCollecting && message.CollectSequence != 0U)
        {
            CurrentSequence = message.CollectSequence;
        }

        CurrentPage = message.FaultCode == 0
            ? PageFromModeAndPhase(CurrentMode, CurrentPhase)
            : DisplayPage.Fault;
        DeviceInfo = "FULL_STATE V3.2 已同步，SEQ " + message.Sequence;
    }

    private void ApplyIdle(ShowIdleMessage message)
    {
        deferredResult = null;
        deferredPostResultMessage = null;
        deferredPostCollectMessage = null;
        racePresentationCompleted = false;
        resultPresentationCompleted = false;
        collectDonePresentationActive = false;
        IsStandbyDemo = false;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.Idle;
        CurrentPage = DisplayPage.Idle;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        Balance = message.Credit;
        WinTotalPoints = message.LastWin;
        WinPoints = message.WinRemaining;
        ClearRoundDisplay();
    }

    private void ApplyBetting(ShowBettingMessage message)
    {
        deferredResult = null;
        deferredPostResultMessage = null;
        deferredPostCollectMessage = null;
        racePresentationCompleted = false;
        resultPresentationCompleted = false;
        collectDonePresentationActive = false;
        IsStandbyDemo = false;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.Betting;
        CurrentPage = DisplayPage.Betting;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        Balance = message.Credit;
        TotalBet = message.TotalBet;
        SelectedId = message.SelectedId;
        bets = (int[])message.Bets.Clone();
        odds = (int[])message.Odds.Clone();
        WinnerId = 0;
        WinTotalPoints = 0;
        WinPoints = 0;
        CollectActive = false;
    }

    private void ApplyBalanceUpdate(BalanceUpdateMessage message)
    {
        // A coin pulse must not restart the animation or change its completion sequence.
        if(CurrentPhase!=DisplayPhase.WaitRaceOver && CurrentPhase!=DisplayPhase.WaitResultOver)
            CurrentSequence = message.Sequence;
        if (message.Round != 0U)
        {
            CurrentRound = message.Round;
        }
        Balance = message.Credit;
        WinPoints = message.Win;
        CollectDelta = message.Delta;
        CollectSource = message.Source;
    }

    private void ApplyRace(ShowRaceMessage message)
    {
        deferredResult = null;
        deferredPostResultMessage = null;
        deferredPostCollectMessage = null;
        racePresentationCompleted = false;
        resultPresentationCompleted = false;
        collectDonePresentationActive = false;
        IsStandbyDemo = message.IsDemo;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.WaitRaceOver;
        CurrentPage = DisplayPage.Race;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        WinnerId = message.WinnerId;
        RaceDurationMilliseconds = message.DurationMilliseconds;
        bets = (int[])message.Bets.Clone();
        odds = (int[])message.Odds.Clone();
    }

    private void ApplyResult(ShowResultMessage message)
    {
        resultPresentationCompleted = false;
        IsStandbyDemo = message.IsDemo;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.WaitResultOver;
        CurrentPage = DisplayPage.Result;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        WinnerId = message.WinnerId;
        WinTotalPoints = message.WinTotal;
        WinPoints = message.WinRemaining;
        Balance = message.Credit;
        ResultStatus = message.Result;
        ResultOdds = message.Odds;
        ResultDurationMilliseconds = message.DurationMilliseconds;
        if (!message.IsDemo)
        {
            RecordWinner(message.Round, message.WinnerId);
        }
    }

    private void RecordWinner(uint round, int winnerId)
    {
        if (!AnimalCatalog.IsValidId(winnerId) || round == lastWinnerHistoryRound)
        {
            return;
        }

        for (int index = 0; index < winnerHistory.Length - 1; index++)
        {
            winnerHistory[index] = winnerHistory[index + 1];
        }

        winnerHistory[winnerHistory.Length - 1] = winnerId;
        lastWinnerHistoryRound = round;
    }

    private void ApplyWinReady(ShowWinReadyMessage message)
    {
        IsStandbyDemo = false;
        deferredPostCollectMessage = null;
        collectDonePresentationActive = false;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.WinWaitStart;
        CurrentPage = DisplayPage.WinReady;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        WinPoints = message.Win;
        if (WinTotalPoints < message.Win)
        {
            WinTotalPoints = message.Win;
        }
        Balance = message.Credit;
        CollectActive = false;
        CollectFastFinished = false;
        collectPresentationStartedAt = -1f;
    }

    private void ApplyWinTransferUpdate(WinTransferUpdateMessage message)
    {
        IsStandbyDemo = false;
        deferredPostCollectMessage = null;
        collectDonePresentationActive = false;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.WinCollecting;
        CurrentPage = DisplayPage.WinCollecting;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        WinPoints = message.Win;
        Balance = message.Credit;
        CollectDelta = message.Delta;
        CollectSource = message.Source;
        CollectActive = true;
        CollectFastFinished = false;
        if (collectPresentationStartedAt < 0f)
        {
            collectPresentationStartedAt = Time.unscaledTime;
        }
    }

    private void ApplyWinTransferDone(WinTransferDoneMessage message)
    {
        IsStandbyDemo = false;
        CurrentMode = DisplayMode.Run;
        CurrentPhase = DisplayPhase.WinCollecting;
        CurrentPage = DisplayPage.WinCollecting;
        CurrentSequence = message.Sequence;
        CurrentRound = message.Round;
        WinPoints = message.Win;
        Balance = message.Credit;
        CollectActive = false;
        CollectFastFinished = message.Fast;
        collectDonePresentationActive = true;
        collectDoneShownAt = Time.unscaledTime;
        if (!message.Fast && collectPresentationStartedAt < 0f)
        {
            collectPresentationStartedAt = collectDoneShownAt;
        }
    }

    private void ApplySetting(ShowSettingMessage message)
    {
        IsStandbyDemo = false;
        if (CurrentMode != DisplayMode.Setting)
        {
            ResumePhase = CurrentPhase;
        }
        CurrentMode = DisplayMode.Setting;
        CurrentPage = DisplayPage.Setting;
        CurrentSequence = message.Sequence;
        SettingIndex = message.Index;
        SettingTotal = message.Total;
        SettingItem = message.Item;
        SettingValue = message.Value;
        SettingMinimum = message.Minimum;
        SettingMaximum = message.Maximum;
        if (!string.IsNullOrEmpty(message.ResumePhase))
        {
            ResumePhase = ParsePhase(message.ResumePhase);
        }
        SettingInfo = message.Item + " = " + message.Value + "  范围 " + message.Minimum + " - " + message.Maximum;
    }

    private void ApplyPayout(ShowPayoutMessage message)
    {
        IsStandbyDemo = false;
        CurrentPhase = DisplayPhase.Payout;
        CurrentPage = DisplayPage.Payout;
        CurrentSequence = message.Sequence;
        Balance = message.BalanceBefore;
        PayoutCoins = message.Coins;
        PayoutDoneCoins = 0;
        Remainder = message.Remainder;
        PayoutComplete = false;
    }

    private void ApplyPayoutProgress(ShowPayoutProgressMessage message)
    {
        IsStandbyDemo = false;
        CurrentPhase = DisplayPhase.Payout;
        CurrentPage = DisplayPage.Payout;
        CurrentSequence = message.Sequence;
        PayoutCoins = message.Target;
        PayoutDoneCoins = message.Done;
        Remainder = message.Remainder;
        PayoutComplete = message.Done >= message.Target;
    }

    private void ApplyPayoutDone(ShowPayoutDoneMessage message)
    {
        IsStandbyDemo = false;
        CurrentPage = DisplayPage.Payout;
        CurrentSequence = message.Sequence;
        PayoutCoins = message.Coins;
        PayoutDoneCoins = message.Coins;
        Balance = message.Credit;
        Remainder = message.Credit;
        PayoutComplete = true;
    }

    private void ApplyFault(ShowFaultMessage message)
    {
        IsStandbyDemo = false;
        CurrentMode = DisplayMode.Fault;
        CurrentPage = DisplayPage.Fault;
        CurrentSequence = message.Sequence;
        FaultCode = message.Code;
        FaultDetail = message.Detail;
        FaultRecoverable = message.Recoverable;
        FaultInfo = "故障 " + message.Code + "：" + message.Detail;
    }

    private void ClearRoundDisplay()
    {
        TotalBet = 0;
        SelectedId = 1;
        WinnerId = 0;
        bets = new int[AnimalCatalog.Count];
    }

    private void PublishSnapshot()
    {
        CurrentSnapshot = new DisplaySnapshot(
            CurrentMode, CurrentPhase, ResumePhase, CurrentPage, CurrentSequence,
            CurrentRound, Balance, WinTotalPoints, WinPoints, BonusPoints, TotalBet,
            SelectedId, WinnerId, bets, odds, CollectActive,
            CollectRemainingMilliseconds, FaultCode);

        Action<DisplaySnapshot> snapshotChanged = SnapshotChanged;
        if (snapshotChanged != null)
        {
            snapshotChanged(CurrentSnapshot);
        }
        Action changed = DisplayChanged;
        if (changed != null)
        {
            changed();
        }
    }

    private static DisplayMode ParseMode(string mode)
    {
        switch ((mode ?? string.Empty).ToUpperInvariant())
        {
            case "SETTING": return DisplayMode.Setting;
            case "FAULT": return DisplayMode.Fault;
            case "SERVICE": return DisplayMode.Service;
            case "BOOT": return DisplayMode.Boot;
            default: return DisplayMode.Run;
        }
    }

    private static DisplayPhase ParsePhase(string phase)
    {
        switch ((phase ?? string.Empty).ToUpperInvariant())
        {
            case "IDLE": return DisplayPhase.Idle;
            case "BETTING": return DisplayPhase.Betting;
            case "RACE_PREPARE": return DisplayPhase.RacePrepare;
            case "WAIT_RACE_OVER": return DisplayPhase.WaitRaceOver;
            case "RESULT":
            case "RESULT_PREPARE": return DisplayPhase.ResultPrepare;
            case "WAIT_RESULT_OVER": return DisplayPhase.WaitResultOver;
            case "WIN_WAIT_START": return DisplayPhase.WinWaitStart;
            case "WIN_COLLECTING": return DisplayPhase.WinCollecting;
            case "ROUND_RESET": return DisplayPhase.RoundReset;
            case "PAYOUT": return DisplayPhase.Payout;
            case "TICKET": return DisplayPhase.Ticket;
            default: return DisplayPhase.Unknown;
        }
    }

    private static DisplayPage PageFromModeAndPhase(DisplayMode mode, DisplayPhase phase)
    {
        if (mode == DisplayMode.Setting) return DisplayPage.Setting;
        if (mode == DisplayMode.Fault) return DisplayPage.Fault;
        switch (phase)
        {
            case DisplayPhase.Idle: return DisplayPage.Idle;
            case DisplayPhase.Betting:
            case DisplayPhase.RacePrepare:
            case DisplayPhase.RoundReset: return DisplayPage.Betting;
            case DisplayPhase.WaitRaceOver: return DisplayPage.Race;
            case DisplayPhase.ResultPrepare:
            case DisplayPhase.WaitResultOver: return DisplayPage.Result;
            case DisplayPhase.WinWaitStart: return DisplayPage.WinReady;
            case DisplayPhase.WinCollecting: return DisplayPage.WinCollecting;
            case DisplayPhase.Payout: return DisplayPage.Payout;
            default: return DisplayPage.Loading;
        }
    }

    private void OnGUI()
    {
        if (!showLegacyUi)
        {
            return;
        }
        GUI.Box(new Rect(8f, 8f, 430f, 100f), string.Empty);
        GUI.Label(new Rect(20f, 18f, 400f, 24f), "V3.1 " + CurrentMode + " / " + CurrentPhase);
        GUI.Label(new Rect(20f, 44f, 400f, 24f), "CREDIT " + Balance + "  WIN " + WinPoints + "  ROUND " + CurrentRound);
        GUI.Label(new Rect(20f, 70f, 400f, 24f), DeviceInfo + (string.IsNullOrEmpty(LastError) ? string.Empty : "  ERR " + LastError));
    }

    private void OnDestroy()
    {
        if (subscribed && serialService != null)
        {
            serialService.OnLineReceived -= ProcessSerialLine;
            serialService.OnConnectionChanged -= HandleConnectionChanged;
        }
        subscribed = false;
    }
}
