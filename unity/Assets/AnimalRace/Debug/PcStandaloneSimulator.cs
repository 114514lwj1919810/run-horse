using System.Collections;
using UnityEngine;

public sealed class PcStandaloneSimulator : MonoBehaviour
{
    public const uint InitialBettingRound = 0U;
    public const int DefaultRaceSeconds = 15;
    public const int MinimumRaceSeconds = 5;
    public const int MaximumRaceSeconds = 60;
    private const string DurationPreference = "PcRaceDurationSeconds_v1";
    private const string DefaultPcSettingsValues =
        "15000|3000|10|10|60000|100|9500|24|24|8|6|3|3|9|1|1|3|30|0|1000|1000|1|0|1|9|0|0|0|0|0|0|0|400000|200|2|255|255";
    [SerializeField] private DisplayStateController displayController;
    [SerializeField] private AnimalRaceAnimationController animationController;
    [SerializeField] private ResultDisplay resultDisplay;
    [SerializeField] private AnimalRaceUI gameUi;
    [SerializeField] private SettingsDisplay settingsDisplay;

    public Font uiFont;

    private readonly int[] bets = new int[AnimalCatalog.Count];
    private readonly int[] odds = { 24, 15, 10, 6, 3, 3 };
    private uint sequence = 1000U;
    private uint round = InitialBettingRound;
    private int credit = 600;
    private int selectedId = 1;
    private int winnerId;
    private int winTotal;
    private uint standbyRound;
    private bool standbyDemoActive;
    private bool visible;
    private int raceDurationSeconds = DefaultRaceSeconds;
    private bool settingsOpen;
    private string durationDraft = "15";
    private string pcSettingsValues = DefaultPcSettingsValues;
    private uint pcSettingsRevision = 1U;
    public int RaceDurationSeconds { get { return raceDurationSeconds; } }
    public bool SettingsOpen { get { return settingsOpen; } }
    private Coroutine collectRoutine;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;

    public DisplayStateController DisplayController
    {
        get { return displayController; }
        set { displayController = value; }
    }

    public AnimalRaceAnimationController AnimationController
    {
        get { return animationController; }
        set { animationController = value; }
    }

    public ResultDisplay ResultOverlay
    {
        get { return resultDisplay; }
        set { resultDisplay = value; }
    }

    public AnimalRaceUI GameUi
    {
        get { return gameUi; }
        set { gameUi = value; }
    }

    public SettingsDisplay SettingsOverlay
    {
        get { return settingsDisplay; }
        set { settingsDisplay = value; }
    }

    public bool IsPcSimulation
    {
        get
        {
            return Application.isEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.LinuxPlayer;
        }
    }

    private void Awake()
    {
        if (displayController == null) displayController = FindObjectOfType<DisplayStateController>();
        if (animationController == null) animationController = FindObjectOfType<AnimalRaceAnimationController>();
        if (resultDisplay == null) resultDisplay = FindObjectOfType<ResultDisplay>();
        if (gameUi == null) gameUi = FindObjectOfType<AnimalRaceUI>();
        if (settingsDisplay == null) settingsDisplay = FindObjectOfType<SettingsDisplay>();
        enabled = IsPcSimulation;
        if (enabled) raceDurationSeconds = Mathf.Clamp(PlayerPrefs.GetInt(DurationPreference, DefaultRaceSeconds),
            MinimumRaceSeconds, MaximumRaceSeconds);
    }

    private void OnEnable()
    {
        visible = false;
        if (animationController != null) animationController.RaceCompleted += HandleRaceCompleted;
        if (resultDisplay != null) resultDisplay.ResultCompleted += HandleResultCompleted;
    }

    private void Start()
    {
        if (enabled)
        {
            if (gameUi != null)
            {
                if (HasCommandLineArgument("-visualSceneDesert")) gameUi.ForcedSceneIndex = 0;
                if (HasCommandLineArgument("-visualSceneJungle")) gameUi.ForcedSceneIndex = 1;
                if (HasCommandLineArgument("-visualSceneCastle")) gameUi.ForcedSceneIndex = 2;
            }
            if (HasCommandLineArgument("-visualCompareIdle"))
            {
                credit = 10;
                selectedId = 3;
                bets[1] = 10;
                bets[2] = 50;
                visible = false;
            }
            ShowBetting();
            InitializePcSettingsDevice();
            if (HasCommandLineArgument("-visualPhotoReference"))
            {
                gameUi.PhotoReferencePreview = true;
                gameUi.PhotoReferenceFinish = HasCommandLineArgument("-visualPhotoFinish");
                StartCoroutine(CapturePhotoReference());
            }
            int previewSeconds;
            if (int.TryParse(GetCommandLineValue("-visualDurationSeconds="), out previewSeconds))
                SetRaceDurationSeconds(previewSeconds);
            if (HasCommandLineArgument("-visualSettings"))
            {
                StartCoroutine(CaptureSettingsPreview());
            }
            if (HasCommandLineArgument("-visualScoreCheck"))
            {
                StartCoroutine(CaptureScoreChecks());
            }
            else if (HasCommandLineArgument("-visualAutoRace"))
            {
                StartCoroutine(BeginVisualAutoRace());
            }
            else if (!HasVisualCommandLineArgument())
            {
                StartCoroutine(BeginStandbyDemo());
            }
        }
    }

    private IEnumerator BeginStandbyDemo()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        if (!settingsOpen && TotalBet() == 0 && displayController != null &&
            (displayController.CurrentPage == DisplayPage.Betting ||
             displayController.CurrentPage == DisplayPage.Idle))
        {
            StartStandbyRace();
        }
    }

    private void StartStandbyRace()
    {
        standbyDemoActive = true;
        standbyRound++;
        if (standbyRound == 0U) standbyRound = 1U;
        winnerId = Random.Range(1, AnimalCatalog.Count + 1);
        winTotal = 0;
        Submit("SHOW_RACE:seq=" + NextSequence() +
               ",round=" + standbyRound +
               ",winner=" + winnerId +
               ",duration_ms=" + (raceDurationSeconds * 1000) +
               ",bets=0|0|0|0|0|0,odds=" + Join(odds) + ",demo=1");
    }

    private void StopStandbyDemo()
    {
        if (!standbyDemoActive) return;
        standbyDemoActive = false;
        winnerId = 0;
        winTotal = 0;
        ShowBetting();
    }

    private IEnumerator BeginVisualAutoRace()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        PlaceBet();
        yield return new WaitForSecondsRealtime(0.2f);
        visible = false;
        StartRace();
        float preview;
        if (float.TryParse(GetCommandLineValue("-visualObstacleProgress="),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out preview))
        {
            yield return null;
            animationController.VisualPreviewProgress = Mathf.Clamp01(preview);
        }
        string capturePath = GetCommandLineValue("-visualCapturePath=");
        if (!string.IsNullOrEmpty(capturePath))
        {
            if (HasCommandLineArgument("-visualFullRound"))
            {
                string folder = System.IO.Path.GetDirectoryName(capturePath);
                yield return CaptureVisualFrame(System.IO.Path.Combine(folder,"normal_race_start.png"));
                yield return new WaitForSecondsRealtime(raceDurationSeconds * 0.5f);
                yield return CaptureVisualFrame(System.IO.Path.Combine(folder,"normal_race_middle.png"));
                yield return new WaitForSecondsRealtime(raceDurationSeconds * 0.5f + 2f);
                Debug.Log("PHOTO_LAYOUT_ROUND_CHECK page=" + displayController.CurrentPage +
                    " locked=" + animationController.IsFinishSceneLocked);
            }
            yield return CaptureVisualFrame(capturePath);
            yield return new WaitForSecondsRealtime(0.6f);
            if (HasCommandLineArgument("-visualCaptureQuit")) Application.Quit();
        }
    }

    private IEnumerator CapturePhotoReference()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        string path = GetCommandLineValue("-visualCapturePath=");
        if (!string.IsNullOrEmpty(path)) yield return CaptureVisualFrame(path);
        if (HasCommandLineArgument("-visualCaptureQuit")) Application.Quit();
    }

    private IEnumerator CaptureSettingsPreview()
    {
        // Wait until the initial SHOW_BETTING has reached DisplayStateController.
        yield return null;
        OpenSettings();
        string path = GetCommandLineValue("-visualCapturePath=");
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsGallery") &&
            !string.IsNullOrEmpty(path))
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            string stem = System.IO.Path.GetFileNameWithoutExtension(path);
            settingsDisplay.OpenLocalMenu();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_01_root.png"));
            settingsDisplay.OpenBranchMachineMenu();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_02_branch_menu.png"));
            settingsDisplay.OpenBranchParameterAdjustment();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_03_branch_parameters.png"));
            settingsDisplay.OpenBranchAccount();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_04_branch_account.png"));
            settingsDisplay.OpenSingleMachineMenu();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_05_single_menu.png"));
            settingsDisplay.OpenParameterAdjustmentOne();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_06_single_parameters1.png"));
            settingsDisplay.OpenParameterAdjustmentTwo();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_07_single_parameters2.png"));
            settingsDisplay.OpenSingleAccount();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_08_single_account.png"));
            settingsDisplay.OpenReportConfirmation();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_09_report.png"));
            settingsDisplay.OpenSystemPassword();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_10_password.png"));
            settingsDisplay.OpenSystemSettingsPreview();
            yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_11_system_settings.png"));
            if (HasCommandLineArgument("-visualCaptureQuit")) Application.Quit();
            yield break;
        }
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsRootLeft"))
            settingsDisplay.SelectCurrentAdjustmentDirection(-1);
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsRootRight"))
            settingsDisplay.SelectCurrentAdjustmentDirection(1);
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsSingle"))
            settingsDisplay.OpenSingleMachineMenu();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsBranch"))
            settingsDisplay.OpenBranchMachineMenu();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsBranchParameters"))
            settingsDisplay.OpenBranchParameterAdjustment();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsBranchAccount"))
            settingsDisplay.OpenBranchAccount();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsParameter1"))
            settingsDisplay.OpenParameterAdjustmentOne();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsParameter2"))
            settingsDisplay.OpenParameterAdjustmentTwo();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsSingleAccount"))
            settingsDisplay.OpenSingleAccount();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsReport"))
            settingsDisplay.OpenReportConfirmation();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsPassword"))
            settingsDisplay.OpenSystemPassword();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsSystem"))
            settingsDisplay.OpenSystemSettingsPreview();
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsParameter1Left"))
            settingsDisplay.SelectParameterAdjustmentDirection(-1);
        if (settingsDisplay != null && HasCommandLineArgument("-visualSettingsParameter1Right"))
            settingsDisplay.SelectParameterAdjustmentDirection(1);
        if (!string.IsNullOrEmpty(path)) yield return CaptureVisualFrame(path);
        if (HasCommandLineArgument("-visualCaptureQuit")) Application.Quit();
    }

    private IEnumerator CaptureScoreChecks()
    {
        visible = false;
        string path = GetCommandLineValue("-visualCapturePath=");
        if (string.IsNullOrEmpty(path)) yield break;
        string directory = System.IO.Path.GetDirectoryName(path);
        string stem = System.IO.Path.GetFileNameWithoutExtension(path);
        yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_600.png"));
        AddCredit();
        yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_700.png"));
        PlaceBet();
        yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_690.png"));
        Submit("SHOW_WIN_READY:seq=" + NextSequence() + ",round=0,win=240,credit=" + credit);
        yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_win240.png"));
        Submit("SHOW_WIN_READY:seq=" + NextSequence() + ",round=0,win=1234567,credit=1234567");
        yield return CaptureVisualFrame(System.IO.Path.Combine(directory, stem + "_large.png"));
        if (HasCommandLineArgument("-visualCaptureQuit")) Application.Quit();
    }

    private static IEnumerator CaptureVisualFrame(string path)
    {
        yield return new WaitForSecondsRealtime(0.6f);
        yield return new WaitForEndOfFrame();
        var screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();
        System.IO.File.WriteAllBytes(path, screenshot.EncodeToPNG());
        Destroy(screenshot);
    }

    private static string GetCommandLineValue(string prefix)
    {
        foreach (string argument in System.Environment.GetCommandLineArgs())
            if (argument.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return argument.Substring(prefix.Length);
        return string.Empty;
    }

    private static bool HasCommandLineArgument(string expected)
    {
        string[] arguments = System.Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], expected, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVisualCommandLineArgument()
    {
        foreach (string argument in System.Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith("-visual", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        if (standbyDemoActive && Input.anyKeyDown)
        {
            StopStandbyDemo();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (settingsOpen) CancelSettings(); else OpenSettings();
        }
        if (settingsOpen)
        {
            if (settingsDisplay != null && !settingsDisplay.LocalMenuVisible) settingsOpen = false;
            else if (settingsDisplay == null)
            {
                if (Input.GetKeyDown(KeyCode.Backspace)) settingsOpen = false;
                if (Input.GetKeyDown(KeyCode.Return)) SaveSettings();
            }
            return;
        }
        if (Input.GetKeyDown(KeyCode.Tab)) visible = !visible;
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow)) SelectPrevious();
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow)) SelectNext();
        if (Input.GetKeyDown(KeyCode.C)) AddCredit();
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.DownArrow)) PlaceBet();
        if (Input.GetKeyDown(KeyCode.T)) NextScene();
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartOrCollect();
    }

    public void OpenSettings()
    {
        if (standbyDemoActive) StopStandbyDemo();
        if (!CanEditBet()) return;
        InitializePcSettingsDevice();
        durationDraft = raceDurationSeconds.ToString();
        settingsOpen = true;
        if (settingsDisplay != null) settingsDisplay.OpenLocalMenu();
    }

    public bool SetRaceDurationSeconds(int seconds)
    {
        if (!CanEditBet() || seconds < MinimumRaceSeconds || seconds > MaximumRaceSeconds) return false;
        raceDurationSeconds = seconds;
        return true;
    }

    public void ApplyRaceDurationFromSettings(int seconds)
    {
        raceDurationSeconds = Mathf.Clamp(seconds, MinimumRaceSeconds, MaximumRaceSeconds);
        durationDraft = raceDurationSeconds.ToString();
        PlayerPrefs.SetInt(DurationPreference, raceDurationSeconds);
        PlayerPrefs.Save();
    }

    public void CancelSettings()
    {
        settingsOpen = false;
        if (settingsDisplay != null) settingsDisplay.CloseLocalMenu();
    }

    private void SaveSettings()
    {
        int seconds;
        if (!int.TryParse(durationDraft, out seconds) || !SetRaceDurationSeconds(seconds)) return;
        PlayerPrefs.SetInt(DurationPreference, raceDurationSeconds);
        PlayerPrefs.Save();
        settingsOpen = false;
        if (settingsDisplay != null) settingsDisplay.CloseLocalMenu();
    }

    public void AddCredit()
    {
        if (!CanEditBet()) return;
        credit += 100;
        ShowBetting();
    }

    public void SelectPrevious()
    {
        if (!CanEditBet()) return;
        selectedId = selectedId <= 1 ? AnimalCatalog.Count : selectedId - 1;
        ShowBetting();
    }

    public void SelectNext()
    {
        if (!CanEditBet()) return;
        selectedId = selectedId >= AnimalCatalog.Count ? 1 : selectedId + 1;
        ShowBetting();
    }

    public void PlaceBet()
    {
        if (!CanEditBet() || credit < 10) return;
        credit -= 10;
        bets[selectedId - 1] += 10;
        ShowBetting();
    }

    public void NextScene()
    {
        if (!CanEditBet() || gameUi == null) return;
        int scene = gameUi.GetActiveSceneIndex();
        gameUi.ForcedSceneIndex = (scene + 1) % 3;
    }

    public void StartOrCollect()
    {
        if (displayController == null) return;

        if (displayController.CurrentPage == DisplayPage.Betting ||
            displayController.CurrentPage == DisplayPage.Idle)
        {
            StartRace();
        }
        else if (displayController.CurrentPage == DisplayPage.WinReady)
        {
            if (displayController.WinPoints > 0)
            {
                BeginCollect();
            }
            else
            {
                BeginNextRound();
            }
        }
        else if (displayController.CurrentPage == DisplayPage.WinCollecting)
        {
            FinishCollect(true);
        }
    }

    private void StartRace()
    {
        if (TotalBet() <= 0) return;
        standbyDemoActive = false;
        round = GetNextRaceRound(round);
        winnerId = Random.Range(1, AnimalCatalog.Count + 1);
        if (!string.IsNullOrEmpty(GetCommandLineValue("-visualObstacleProgress="))) winnerId = 1;
        winTotal = bets[winnerId - 1] * odds[winnerId - 1];
        Submit("SHOW_RACE:seq=" + NextSequence() +
               ",round=" + round +
               ",winner=" + winnerId +
               ",duration_ms=" + (raceDurationSeconds * 1000) + ",bets=" + Join(bets) +
               ",odds=" + Join(odds));
    }

    private void HandleRaceCompleted()
    {
        if (!enabled || displayController == null || displayController.CurrentPage != DisplayPage.Race) return;
        if (standbyDemoActive)
        {
            Submit("SHOW_RESULT:seq=" + NextSequence() +
                   ",round=" + standbyRound +
                   ",result=LOSE,winner=" + winnerId +
                   ",odds=" + odds[winnerId - 1] +
                   ",win_total=0,win_remaining=0,credit=" + credit +
                   ",duration_ms=3000,demo=1");
            return;
        }
        Submit("SHOW_RESULT:seq=" + NextSequence() +
               ",round=" + round +
               ",result=" + (winTotal > 0 ? "WIN" : "LOSE") +
               ",winner=" + winnerId +
               ",odds=" + odds[winnerId - 1] +
               ",win_total=" + winTotal +
               ",win_remaining=" + winTotal +
               ",credit=" + credit +
               ",duration_ms=3000");
    }

    private void HandleResultCompleted()
    {
        if (!enabled || displayController == null || displayController.CurrentPage != DisplayPage.Result) return;
        if (standbyDemoActive)
        {
            StartStandbyRace();
            return;
        }
        Submit("SHOW_WIN_READY:seq=" + NextSequence() +
               ",round=" + round + ",win=" + winTotal + ",credit=" + credit);
    }

    private void BeginCollect()
    {
        if (collectRoutine != null) StopCoroutine(collectRoutine);
        collectRoutine = StartCoroutine(CollectPoints());
    }

    private IEnumerator CollectPoints()
    {
        int remaining = winTotal;
        while (remaining > 0)
        {
            int delta = Mathf.Min(20, remaining);
            remaining -= delta;
            credit += delta;
            Submit("WIN_TRANSFER_UPDATE:seq=" + NextSequence() +
                   ",round=" + round +
                   ",win=" + remaining +
                   ",credit=" + credit +
                   ",delta=" + delta + ",source=PC_SIM");
            yield return new WaitForSecondsRealtime(0.08f);
        }

        FinishCollect(false);
    }

    private void FinishCollect(bool fast)
    {
        if (collectRoutine != null)
        {
            StopCoroutine(collectRoutine);
            collectRoutine = null;
        }

        int remaining = displayController == null ? 0 : displayController.WinPoints;
        credit += remaining;
        Submit("WIN_TRANSFER_DONE:seq=" + NextSequence() +
               ",round=" + round + ",win=0,credit=" + credit +
               ",fast=" + (fast ? 1 : 0));
        BeginNextRound();
    }

    private void BeginNextRound()
    {
        winnerId = 0;
        winTotal = 0;
        for (int index = 0; index < bets.Length; index++) bets[index] = 0;
        if (gameUi != null) gameUi.ForcedSceneIndex = -1;
        ShowBetting();
    }

    public static uint GetNextRaceRound(uint completedRound)
    {
        return completedRound + 1U;
    }

    private bool CanEditBet()
    {
        return displayController != null &&
               (displayController.CurrentPage == DisplayPage.Betting ||
                displayController.CurrentPage == DisplayPage.Idle);
    }

    private void ShowBetting()
    {
        Submit("SHOW_BETTING:seq=" + NextSequence() +
               ",round=" + round +
               ",credit=" + credit +
               ",total_bet=" + TotalBet() +
               ",selected=" + selectedId +
               ",bets=" + Join(bets) +
               ",odds=" + Join(odds));
    }

    private void Submit(string line)
    {
        if (displayController != null)
        {
            displayController.ProcessRawLine(line);
            // ProcessRawLine restores the hardware transport. The standalone
            // build must keep its local settings device connected afterwards.
            if (IsPcSimulation) displayController.Machine.Send = HandlePcMachineCommand;
        }
    }

    private void InitializePcSettingsDevice()
    {
        if (displayController == null) return;
        MachineSettingsClient machine = displayController.Machine;
        if (!machine.HasSettings)
            machine.Process("SETTINGS_SNAPSHOT:schema=1,rev=" + pcSettingsRevision +
                ",storage=1,values=" + pcSettingsValues);
        if (!machine.HasAccount)
            machine.Process("A:v=1,d=0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0");
        machine.Send = HandlePcMachineCommand;
    }

    private bool HandlePcMachineCommand(string line)
    {
        if (displayController == null || string.IsNullOrEmpty(line)) return false;
        uint request = ReadUnsignedField(line, "req", 0U);
        string status = "OK";
        bool publishSettings = false;

        if (line.StartsWith("SERVICE_ENTER:", System.StringComparison.Ordinal))
            displayController.Machine.Process("SERVICE_STATE:open=1");
        else if (line.StartsWith("SERVICE_EXIT:", System.StringComparison.Ordinal))
            displayController.Machine.Process("SERVICE_STATE:open=0");
        else if (line.StartsWith("SERVICE_AUTH:", System.StringComparison.Ordinal))
        {
            uint password = ReadUnsignedField(line, "password", 0U);
            uint configured = ReadPcPassword();
            if (password != (uint)SettingsDisplay.DefaultSystemPassword && password != configured)
                status = "AUTH_REQUIRED";
        }
        else if (line.StartsWith("SETTINGS_APPLY:", System.StringComparison.Ordinal))
        {
            string values = ReadTextField(line, "values");
            int[] parsed;
            if (!TryParsePcSettings(values, out parsed)) status = "INVALID_VALUE";
            else
            {
                pcSettingsValues = values;
                pcSettingsRevision++;
                if (pcSettingsRevision == 0U) pcSettingsRevision = 1U;
                publishSettings = true;
                raceDurationSeconds = Mathf.Clamp(parsed[0] / 1000,
                    MinimumRaceSeconds, MaximumRaceSeconds);
            }
        }
        else if (line.StartsWith("SETTINGS_GET", System.StringComparison.Ordinal))
        {
            StartCoroutine(PublishPcSettingsNextFrame());
            return true;
        }

        if (request != 0U)
            StartCoroutine(CompletePcMachineRequest(request, status, publishSettings));
        return true;
    }

    private IEnumerator CompletePcMachineRequest(uint request, string status, bool publishSettings)
    {
        yield return null;
        displayController.Machine.Process("CONTROL_RESULT:req=" + request +
            ",status=" + status + ",rev=" + pcSettingsRevision);
        if (publishSettings && status == "OK")
        {
            yield return null;
            PublishPcSettings();
        }
        displayController.Machine.Send = HandlePcMachineCommand;
    }

    private IEnumerator PublishPcSettingsNextFrame()
    {
        yield return null;
        PublishPcSettings();
    }

    private void PublishPcSettings()
    {
        displayController.Machine.Process("SETTINGS_SNAPSHOT:schema=1,rev=" +
            pcSettingsRevision + ",storage=1,values=" + pcSettingsValues);
        displayController.Machine.Send = HandlePcMachineCommand;
    }

    private uint ReadPcPassword()
    {
        string[] values = pcSettingsValues.Split('|');
        uint password;
        return values.Length > 31 && uint.TryParse(values[31], out password) ? password : 0U;
    }

    private static bool TryParsePcSettings(string text, out int[] values)
    {
        string[] parts = string.IsNullOrEmpty(text) ? new string[0] : text.Split('|');
        values = new int[parts.Length];
        if (parts.Length != MachineSettingsClient.Count) return false;
        for (int index = 0; index < parts.Length; index++)
            if (!int.TryParse(parts[index], out values[index])) return false;
        return MachineSettingsClient.Validate(values);
    }

    private static uint ReadUnsignedField(string line, string key, uint fallback)
    {
        string text = ReadTextField(line, key);
        uint value;
        return uint.TryParse(text, out value) ? value : fallback;
    }

    private static string ReadTextField(string line, string key)
    {
        string marker = key + "=";
        int start = line.IndexOf(marker, System.StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        start += marker.Length;
        int end = line.IndexOf(',', start);
        return end < 0 ? line.Substring(start) : line.Substring(start, end - start);
    }

    private uint NextSequence()
    {
        return sequence++;
    }

    private int TotalBet()
    {
        int total = 0;
        for (int index = 0; index < bets.Length; index++) total += bets[index];
        return total;
    }

    private static string Join(int[] values)
    {
        string text = string.Empty;
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0) text += "|";
            text += values[index].ToString();
        }
        return text;
    }

    private void OnGUI()
    {
        if (!enabled) return;
        GUI.depth = -1000;
        EnsureStyles();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));

        if (settingsOpen)
        {
            if (settingsDisplay == null) DrawPcSettings();
            GUI.matrix = previous;
            return;
        }

        if (!visible)
        {
            GUI.matrix = previous;
            return;
        }

        GUILayout.BeginArea(new Rect(940f, 74f, 328f, 260f), panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("电脑试玩", titleStyle);
        if (GUILayout.Button("收起", buttonStyle, GUILayout.Width(70f))) visible = false;
        GUILayout.EndHorizontal();
        GUILayout.Label("比赛时间  " + raceDurationSeconds + " 秒", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("投入 100", buttonStyle)) AddCredit();
        if (GUILayout.Button("切换场景", buttonStyle)) NextScene();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("上一只", buttonStyle)) SelectPrevious();
        if (GUILayout.Button("下一只", buttonStyle)) SelectNext();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("下注 10", buttonStyle)) PlaceBet();
        if (GUILayout.Button("开始 / 收分", buttonStyle)) StartOrCollect();
        GUILayout.EndHorizontal();
        GUI.enabled = CanEditBet();
        if (GUILayout.Button("设置", buttonStyle)) OpenSettings();
        GUI.enabled = true;
        GUILayout.EndArea();
        GUI.matrix = previous;
    }

    private void DrawPcSettings()
    {
        GUILayout.BeginArea(new Rect(450f, 235f, 380f, 245f), panelStyle);
        GUILayout.Label("电脑设置", titleStyle);
        GUILayout.Space(16f);
        GUILayout.Label("比赛时间", labelStyle);
        GUILayout.BeginHorizontal();
        int seconds;
        if (!int.TryParse(durationDraft, out seconds)) seconds = raceDurationSeconds;
        if (GUILayout.Button("-", buttonStyle, GUILayout.Width(48f)))
            durationDraft = Mathf.Max(MinimumRaceSeconds, seconds - 1).ToString();
        durationDraft = GUILayout.TextField(durationDraft, 2, buttonStyle, GUILayout.Width(130f));
        if (GUILayout.Button("+", buttonStyle, GUILayout.Width(48f)))
            durationDraft = Mathf.Min(MaximumRaceSeconds, seconds + 1).ToString();
        GUILayout.Label("秒", labelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(16f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("取消", buttonStyle)) settingsOpen = false;
        GUI.enabled = int.TryParse(durationDraft, out seconds) && seconds >= MinimumRaceSeconds && seconds <= MaximumRaceSeconds;
        if (GUILayout.Button("保存", buttonStyle)) SaveSettings();
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            fontSize = 16,
            fixedHeight = 34f
        };
    }

    private void OnDisable()
    {
        if (animationController != null) animationController.RaceCompleted -= HandleRaceCompleted;
        if (resultDisplay != null) resultDisplay.ResultCompleted -= HandleResultCompleted;
    }
}
