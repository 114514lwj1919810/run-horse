using UnityEngine;

public sealed partial class SettingsDisplay : MonoBehaviour
{
    private const float LogicalWidth = 640f;
    private const float LogicalHeight = 480f;

    private enum SettingsPage
    {
        Root, BranchMenu, BranchParameters, BranchAccount,
        SingleMenu, SingleParameters1, SingleParameters2, SingleAccount,
        ReportConfirm, SystemPassword, SystemSettings, SystemValueEditor, Advanced
    }

    private enum SystemValueTarget
    {
        MachineNumber, Formula, Password
    }

    private static readonly string[] RootItems = { "品种选择", "分机设置", "单机设置", "【退出】" };
    private static readonly string[] BranchItems = { "参数调整", "查帐", "归零", "【退出】" };
    private static readonly string[] SingleItems = { "参数调整1", "参数调整2", "查帐", "报帐", "系统设置", "返奖与硬件", "【退出】" };
    private static readonly int[] SingleAtlasRows = { 96, 112, 16, 32, 48, -1, 64 };
    private static readonly string[] SystemItems = { "运行时间", "机台号码", "计算公式", "系统密码", "保存并退出", "不保存退出" };

    private static readonly string[] BranchParameterItems =
    {
        "分机号码", "背景音量", "投退币器类型", "投退币器来源",
        "恢复默认值", "保存并退出", "不保存退出"
    };
    private static readonly int[] BranchParameterAtlasRows = { -1, 32, 128, 144, 160, 176, 192 };

    private static readonly string[] Parameter1Items =
    {
        "投币比例", "难度设置", "彩票比例", "狮子最大押注", "其他最大押注",
        "最小押分切换", "恢复默认值", "保存并退出", "不保存退出"
    };
    private static readonly int[] Parameter1AtlasRows = { 0, 16, 368, 320, 96, 112, 160, 176, 192 };

    private static readonly string[] Parameter2Items =
    {
        "背景音量", "倍数选择", "投退币器类型", "投退币器来源",
        "恢复默认值", "保存并退出", "不保存退出"
    };
    private static readonly int[] Parameter2AtlasRows = { 32, 304, 128, 144, 160, 176, 192 };

    private static readonly string[] Difficulties = { "最容易", "容易", "普通", "困难", "最困难" };
    private static readonly string[] AcceptorTypes = { "常开型", "常闭型" };
    private static readonly string[] AcceptorSources = { "12P插座", "娱乐麻将接口" };
    private static readonly string[] Multipliers =
    {
        "全部倍数随机", "2-4-8-12-24-24", "2-4-10-12-15-24",
        "2-5-6-12-20-24", "3-3-6-8-24-24", "3-3-6-10-15-24", "3-3-6-12-12-24"
    };
    private static readonly int[] CoinPoints = { 1, 2, 5, 10, 20, 50, 100 };
    private static readonly int[] MinimumBets = { 1, 5, 10, 20, 50, 100 };

    [SerializeField] private DisplayStateController displayController;
    [SerializeField] private Font uiFont;
    [SerializeField] private Texture2D titleAtlas;
    [SerializeField] private Texture2D cursorArtwork;
    [SerializeField] private Texture2D navigationHints;
    [SerializeField] private Texture2D confirmHints;
    [SerializeField] private Texture2D singleMachineMenuAtlas;
    [SerializeField] private Texture2D parameterLabelsAtlas;
    [SerializeField] private Texture2D accountLabelsAtlas;
    [SerializeField] private Texture2D passwordPanelArtwork;
    [SerializeField] private Texture2D systemLabelsAtlas;

    private bool localMenuVisible;
    private SettingsPage currentPage;
    private int selectedRow;
    private int selectedAdjustmentDirection = -1;
    private bool reportYes;
    private readonly int[] passwordDigits = new int[8];
    private readonly bool[] passwordDigitTouched = new bool[8];
    private int passwordCursor;
    private bool passwordError;
    private readonly int[] editorDigits = new int[8];
    private int editorDigitCount;
    private int editorCursor;
    private SystemValueTarget editorTarget;

    private int machineMode, branchNumber, branchVolume, branchType, branchSource;
    private int coinPointIndex, difficultyIndex, ticketCoins, lionMaxBet, otherMaxBet, minimumBetIndex;
    private int singleVolume, multiplierIndex, singleType, singleSource;
    private int runningMinutes, systemMachineNumber, systemFormula, systemPassword;

    private Texture2D whiteTexture, backgroundTexture, panelTexture, idlePillTexture, selectedPillTexture;
    private Texture2D leftArrowTexture, rightArrowTexture;
    private GUIStyle menuStyle, valueStyle, titleNumberStyle, hintStyle, smallStyle;

    public DisplayStateController DisplayController { get { return displayController; } set { displayController = value; } }
    public Font UiFont { get { return uiFont; } set { uiFont = value; } }
    public Texture2D TitleAtlas { get { return titleAtlas; } set { titleAtlas = value; } }
    public Texture2D CursorArtwork { get { return cursorArtwork; } set { cursorArtwork = value; } }
    public Texture2D NavigationHints { get { return navigationHints; } set { navigationHints = value; } }
    public Texture2D ConfirmHints { get { return confirmHints; } set { confirmHints = value; } }
    public Texture2D SingleMachineMenuAtlas { get { return singleMachineMenuAtlas; } set { singleMachineMenuAtlas = value; } }
    public Texture2D ParameterLabelsAtlas { get { return parameterLabelsAtlas; } set { parameterLabelsAtlas = value; } }
    public Texture2D AccountLabelsAtlas { get { return accountLabelsAtlas; } set { accountLabelsAtlas = value; } }
    public Texture2D PasswordPanelArtwork { get { return passwordPanelArtwork; } set { passwordPanelArtwork = value; } }
    public Texture2D SystemLabelsAtlas { get { return systemLabelsAtlas; } set { systemLabelsAtlas = value; } }

    public bool IsVisible { get { return displayController != null && displayController.CurrentPage == DisplayPage.Setting; } }
    public bool LocalMenuVisible { get { return localMenuVisible; } }
    public bool ShouldRender { get { return IsVisible || localMenuVisible; } }
    public int SelectedRow { get { return selectedRow; } }
    public string CurrentMenuName { get { return currentPage.ToString(); } }
    public bool ParameterArrowFocus { get { return false; } }
    public bool AdjustmentFocus { get { return false; } }
    public int SelectedAdjustmentDirection { get { return selectedAdjustmentDirection; } }
    public const int DefaultSystemPassword = 0;
    public static int RecoveredVideoPageCount { get { return 11; } }
    public string ItemName { get { return displayController == null ? string.Empty : displayController.SettingItem; } }
    public string Detail { get { return "[上下][AB]移动  [左右][CD]修改  [开始]确定"; } }

    private void Awake()
    {
        if (displayController == null) displayController = FindObjectOfType<DisplayStateController>();
        BindDevice();
        LoadValues();
    }

    public void OpenLocalMenu() { BindDevice(); if (Device != null) Device.Request("SERVICE_ENTER", ""); }
    public void OpenBranchMachineMenu() { Open(SettingsPage.BranchMenu, 0); }
    public void OpenBranchParameterAdjustment() { Open(SettingsPage.BranchParameters, 0); }
    public void OpenBranchAccount() { Open(SettingsPage.BranchAccount, 0); }
    public void OpenSingleMachineMenu() { Open(SettingsPage.SingleMenu, 0); }
    public void OpenParameterAdjustmentOne() { Open(SettingsPage.SingleParameters1, 0); }
    public void OpenParameterAdjustmentTwo() { Open(SettingsPage.SingleParameters2, 0); }
    public void OpenSingleAccount() { Open(SettingsPage.SingleAccount, 0); }
    public void OpenReportConfirmation() { reportYes = false; Open(SettingsPage.ReportConfirm, 3); }
    public void OpenSystemPassword()
    {
        passwordCursor = 0;
        passwordError = false;
        for (int i = 0; i < passwordDigits.Length; i++)
        {
            passwordDigits[i] = 0;
            passwordDigitTouched[i] = false;
        }
        Open(SettingsPage.SystemPassword, 4);
    }
    public void OpenSystemSettingsPreview() { Open(SettingsPage.SystemSettings, 0); }

    private void Open(SettingsPage page, int row)
    {
        localMenuVisible = true;
        currentPage = page;
        selectedRow = row;
    }

    public void SelectParameterAdjustmentDirection(int direction)
    {
        if (currentPage == SettingsPage.SingleParameters1) SelectAdjustmentDirection(direction);
    }
    public void SelectCurrentAdjustmentDirection(int direction) { SelectAdjustmentDirection(direction); }
    public void CloseLocalMenu() { if (Device != null) Device.Request("SERVICE_EXIT", ""); }

    private void Update()
    {
        BindDevice();
        if (Device != null) Device.Tick();
        if (!ShouldRender) return;
        if (Device == null || !Device.HasSettings || Device.Busy) return;
        if (!localMenuVisible && displayController != null)
        {
            currentPage = SettingsPage.Root;
            selectedRow = Mathf.Clamp(displayController.SettingIndex, 0, RootItems.Length - 1);
        }

        if (currentPage == SettingsPage.SystemPassword)
        {
            if (UpPressed()) ChangePasswordDigit(1);
            if (DownPressed()) ChangePasswordDigit(-1);
            if (LeftPressed()) MovePasswordCursor(-1);
            if (RightPressed()) MovePasswordCursor(1);
            if (ConfirmPressed()) ConfirmPassword();
            if (BackPressed()) Open(SettingsPage.SingleMenu, 4);
            return;
        }
        if (currentPage == SettingsPage.SystemValueEditor)
        {
            if (UpPressed()) ChangeEditorDigit(1);
            if (DownPressed()) ChangeEditorDigit(-1);
            if (LeftPressed()) editorCursor = Mathf.Max(0, editorCursor - 1);
            if (RightPressed()) editorCursor = Mathf.Min(editorDigitCount - 1, editorCursor + 1);
            if (ConfirmPressed()) ConfirmSystemValueEditor();
            if (BackPressed()) Open(SettingsPage.SystemSettings, (int)editorTarget + 1);
            return;
        }
        if (currentPage == SettingsPage.ReportConfirm)
        {
            if (LeftPressed()) reportYes = true;
            if (RightPressed()) reportYes = false;
            if (ConfirmPressed()) ConfirmReport();
            if (BackPressed()) Open(SettingsPage.SingleMenu, 3);
            return;
        }

        if (UpPressed()) MoveSelection(-1);
        if (DownPressed()) MoveSelection(1);
        if (LeftPressed()) SelectAdjustmentDirection(-1);
        if (RightPressed()) SelectAdjustmentDirection(1);
        if (ConfirmPressed()) ConfirmSelection();
        if (localMenuVisible && BackPressed()) NavigateBack();
    }

    private static bool UpPressed() { return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.A); }
    private static bool DownPressed() { return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.B); }
    private static bool LeftPressed() { return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.C); }
    private static bool RightPressed() { return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D); }
    private static bool BackPressed() { return Input.GetKeyDown(KeyCode.Backspace); }
    private static bool ConfirmPressed()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
    }

    private void MoveSelection(int direction)
    {
        int count = GetRowCount(currentPage);
        if (count > 1) selectedRow = (selectedRow + direction + count) % count;
    }

    private void SelectAdjustmentDirection(int direction)
    {
        if (!HasAdjustment(currentPage, selectedRow)) return;
        selectedAdjustmentDirection = direction < 0 ? -1 : 1;
        if (currentPage == SettingsPage.Root)
        {
            machineMode = direction < 0 ? 0 : 1;
            SaveAndReturn(SettingsPage.Root, 0);
        }
        else if (currentPage == SettingsPage.BranchParameters)
        {
            if (selectedRow == 0) branchNumber = Step(branchNumber, direction, 1, 8, 1);
            else if (selectedRow == 1) branchVolume = Step(branchVolume, direction, 0, 16, 1);
            else if (selectedRow == 2) branchType = Wrap(branchType, direction, AcceptorTypes.Length);
            else if (selectedRow == 3) branchSource = Wrap(branchSource, direction, AcceptorSources.Length);
        }
        else if (currentPage == SettingsPage.SingleParameters1)
        {
            if (selectedRow == 0) coinPointIndex = Wrap(coinPointIndex, direction, CoinPoints.Length);
            else if (selectedRow == 1) difficultyIndex = Wrap(difficultyIndex, direction, Difficulties.Length);
            else if (selectedRow == 2) ticketCoins = Step(ticketCoins, direction, 1, 99, 1);
            else if (selectedRow == 3) lionMaxBet = Step(lionMaxBet, direction, 100, 10000, 100);
            else if (selectedRow == 4) otherMaxBet = Step(otherMaxBet, direction, 100, 10000, 100);
            else if (selectedRow == 5) minimumBetIndex = Wrap(minimumBetIndex, direction, MinimumBets.Length);
        }
        else if (currentPage == SettingsPage.SingleParameters2)
        {
                if (selectedRow == 0) singleVolume = Step(singleVolume, direction, 0, 16, 1);
            else if (selectedRow == 1) multiplierIndex = Wrap(multiplierIndex, direction, Multipliers.Length);
            else if (selectedRow == 2) singleType = Wrap(singleType, direction, AcceptorTypes.Length);
                else if (selectedRow == 3) singleSource = Wrap(singleSource, direction, AcceptorSources.Length);
        }
        else if (currentPage == SettingsPage.SystemSettings && selectedRow == 0)
        {
            runningMinutes = Step(runningMinutes, direction, 0, 99900, 100);
        }
        else if (currentPage == SettingsPage.Advanced) AdjustAdvanced(direction);
    }

    private void ConfirmSelection()
    {
        if (!localMenuVisible) return;
        if (currentPage == SettingsPage.Root)
        {
            if (selectedRow == 1) OpenBranchMachineMenu();
            else if (selectedRow == 2) OpenSingleMachineMenu();
            else if (selectedRow == 3) CloseLocalMenu();
        }
        else if (currentPage == SettingsPage.BranchMenu)
        {
            if (selectedRow == 0) OpenBranchParameterAdjustment();
            else if (selectedRow == 1) OpenBranchAccount();
            else if (selectedRow == 2) RequestReport(true);
            else Open(SettingsPage.Root, 1);
        }
        else if (currentPage == SettingsPage.SingleMenu)
        {
            if (selectedRow == 0) OpenParameterAdjustmentOne();
            else if (selectedRow == 1) OpenParameterAdjustmentTwo();
            else if (selectedRow == 2) OpenSingleAccount();
            else if (selectedRow == 3) OpenReportConfirmation();
            else if (selectedRow == 4) OpenSystemPassword();
            else if (selectedRow == 5) Open(SettingsPage.Advanced, 0);
            else Open(SettingsPage.Root, 2);
        }
        else if (currentPage == SettingsPage.BranchParameters)
        {
            if (selectedRow == 4) RestoreBranchDefaults();
            else if (selectedRow == 5) SaveAndReturn(SettingsPage.BranchMenu, 0);
            else if (selectedRow == 6) { LoadValues(); OpenBranchMachineMenu(); }
        }
        else if (currentPage == SettingsPage.SingleParameters1)
        {
            if (selectedRow == 6) RestoreParameter1Defaults();
            else if (selectedRow == 7) SaveAndReturn(SettingsPage.SingleMenu, 0);
            else if (selectedRow == 8) { LoadValues(); OpenSingleMachineMenu(); }
        }
        else if (currentPage == SettingsPage.SingleParameters2)
        {
            if (selectedRow == 4) RestoreParameter2Defaults();
            else if (selectedRow == 5) SaveAndReturn(SettingsPage.SingleMenu, 1);
            else if (selectedRow == 6) { LoadValues(); OpenSingleMachineMenu(); }
        }
        else if (currentPage == SettingsPage.SystemSettings)
        {
            if (selectedRow == 1) OpenSystemValueEditor(SystemValueTarget.MachineNumber, systemMachineNumber, 8);
            else if (selectedRow == 2) OpenSystemValueEditor(SystemValueTarget.Formula, systemFormula, 8);
            else if (selectedRow == 3) OpenSystemValueEditor(SystemValueTarget.Password, systemPassword, 8);
            else if (selectedRow == 4) SaveAndReturn(SettingsPage.SingleMenu, 4);
            else if (selectedRow == 5) { LoadValues(); Open(SettingsPage.SingleMenu, 4); }
        }
        else if (currentPage == SettingsPage.BranchAccount) OpenBranchMachineMenu();
        else if (currentPage == SettingsPage.SingleAccount) Open(SettingsPage.SingleMenu, 2);
        else if (currentPage == SettingsPage.Advanced) {
            if (selectedRow == AdvancedFields.Length) SaveAndReturn(SettingsPage.SingleMenu, 5);
            else if (selectedRow == AdvancedFields.Length + 1) { LoadValues(); Open(SettingsPage.SingleMenu, 5); }
        }
    }

    private void NavigateBack()
    {
        LoadValues();
        if (currentPage == SettingsPage.Advanced) { OpenSingleMachineMenu(); return; }
        if (currentPage == SettingsPage.BranchParameters || currentPage == SettingsPage.BranchAccount) OpenBranchMachineMenu();
        else if (currentPage == SettingsPage.SingleParameters1 || currentPage == SettingsPage.SingleParameters2 || currentPage == SettingsPage.SingleAccount) OpenSingleMachineMenu();
        else if (currentPage == SettingsPage.BranchMenu) Open(SettingsPage.Root, 1);
        else if (currentPage == SettingsPage.SingleMenu) Open(SettingsPage.Root, 2);
        else if (currentPage == SettingsPage.SystemSettings) Open(SettingsPage.SingleMenu, 4);
        else CloseLocalMenu();
    }

    private void ConfirmReport()
    {
        if (reportYes) RequestReport(false);
        else Open(SettingsPage.SingleMenu, 3);
    }

    private void MovePasswordCursor(int direction)
    {
        if (direction > 0 && !CanAdvancePasswordCursor(passwordDigitTouched[passwordCursor])) return;
        passwordCursor = Mathf.Clamp(passwordCursor + direction, 0, passwordDigits.Length - 1);
        passwordError = false;
    }

    public static bool CanAdvancePasswordCursor(bool currentDigitEntered)
    {
        return currentDigitEntered;
    }

    private void ChangePasswordDigit(int direction)
    {
        passwordDigits[passwordCursor] = (passwordDigits[passwordCursor] + direction + 10) % 10;
        passwordDigitTouched[passwordCursor] = true;
        passwordError = false;
    }

    private void ConfirmPassword()
    {
        bool complete = true;
        for (int i = 0; i < passwordDigitTouched.Length; i++)
            if (!passwordDigitTouched[i]) complete = false;
        if (complete && Device != null)
            authRequest = Device.Request("SERVICE_AUTH", ",password=" + GetDigitsValue(passwordDigits, passwordDigits.Length));
        else
            passwordError = true;
    }

    private void OpenSystemValueEditor(SystemValueTarget target, int value, int digitCount)
    {
        editorTarget = target;
        editorDigitCount = digitCount;
        editorCursor = 0;
        int divisor = 1;
        for (int i = 1; i < digitCount; i++) divisor *= 10;
        for (int i = 0; i < digitCount; i++)
        {
            editorDigits[i] = (value / divisor) % 10;
            divisor = Mathf.Max(1, divisor / 10);
        }
        Open(SettingsPage.SystemValueEditor, (int)target + 1);
    }

    private void ChangeEditorDigit(int direction)
    {
        editorDigits[editorCursor] = (editorDigits[editorCursor] + direction + 10) % 10;
    }

    private void ConfirmSystemValueEditor()
    {
        int value = GetDigitsValue(editorDigits, editorDigitCount);
        if (editorTarget == SystemValueTarget.MachineNumber) systemMachineNumber = value;
        else if (editorTarget == SystemValueTarget.Formula) systemFormula = value;
        else systemPassword = value;
        Open(SettingsPage.SystemSettings, (int)editorTarget + 1);
    }

    private static int GetDigitsValue(int[] digits, int count)
    {
        int value = 0;
        for (int i = 0; i < count; i++) value = value * 10 + digits[i];
        return value;
    }

    public static bool IsDefaultSystemPassword(int[] digits)
    {
        return digits != null && digits.Length == 8 && GetDigitsValue(digits, 8) == DefaultSystemPassword;
    }

    private static bool HasAdjustment(SettingsPage page, int row)
    {
        return (page == SettingsPage.Root && row == 0) ||
               (page == SettingsPage.BranchParameters && row < 4) ||
               (page == SettingsPage.SingleParameters1 && row < 6) ||
               (page == SettingsPage.SingleParameters2 && row < 4) ||
               (page == SettingsPage.SystemSettings && row == 0) ||
               (page == SettingsPage.Advanced && row < AdvancedFields.Length);
    }

    private static int GetRowCount(SettingsPage page)
    {
        if (page == SettingsPage.Advanced) return AdvancedFields.Length + 2;
        if (page == SettingsPage.Root) return RootItems.Length;
        if (page == SettingsPage.BranchMenu) return BranchItems.Length;
        if (page == SettingsPage.BranchParameters) return BranchParameterItems.Length;
        if (page == SettingsPage.SingleMenu || page == SettingsPage.ReportConfirm || page == SettingsPage.SystemPassword || page == SettingsPage.SystemValueEditor) return SingleItems.Length;
        if (page == SettingsPage.SystemSettings) return SystemItems.Length;
        if (page == SettingsPage.SingleParameters1) return Parameter1Items.Length;
        if (page == SettingsPage.SingleParameters2) return Parameter2Items.Length;
        return 1;
    }

    public static int GetDirectAdjustedValue(int value, int direction, int minimum, int maximum)
    {
        return Mathf.Clamp(value + (direction < 0 ? -1 : 1), minimum, maximum);
    }
    private static int Step(int value, int direction, int min, int max, int amount)
    {
        return Mathf.Clamp(value + (direction < 0 ? -amount : amount), min, max);
    }
    private static int Wrap(int value, int direction, int count)
    {
        return (value + (direction < 0 ? -1 : 1) + count) % count;
    }

    private void RestoreBranchDefaults() { branchNumber = 1; branchVolume = 9; branchType = 0; branchSource = 0; }
    private void RestoreParameter1Defaults()
    {
        coinPointIndex = 3; difficultyIndex = 2; ticketCoins = 1;
        lionMaxBet = 1000; otherMaxBet = 1000; minimumBetIndex = 2;
    }
    private void RestoreParameter2Defaults() { singleVolume = 9; multiplierIndex = 0; singleType = 0; singleSource = 0; }

    private void OnGUI()
    {
        if (!ShouldRender) return;
        EnsureArtwork();
        GUI.depth = -2000;
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / LogicalWidth, Screen.height / LogicalHeight, 1));
        GUI.color = Color.white;
        DrawPage();
        DrawDeviceStatus();
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    private void DrawPage()
    {
        GUI.DrawTexture(new Rect(0, 0, LogicalWidth, LogicalHeight), backgroundTexture);
        DrawColorRect(new Rect(56, 102, 528, 260), new Color32(0, 23, 81, 255));
        GUI.DrawTexture(new Rect(58, 104, 524, 256), panelTexture);
        DrawColorRect(new Rect(58, 104, 524, 3), new Color32(43, 158, 231, 255));
        DrawColorRect(new Rect(58, 356, 524, 4), new Color32(0, 43, 119, 255));
        DrawTitle();

        if (currentPage == SettingsPage.Root) { DrawRoot(); DrawHints(true); }
        else if (currentPage == SettingsPage.BranchMenu) { DrawPlainMenu(BranchItems, 139, 35, 108); DrawHints(false); }
        else if (currentPage == SettingsPage.SingleMenu) { DrawSingleMenu(); DrawHints(false); }
        else if (currentPage == SettingsPage.BranchParameters) { DrawParameters(BranchParameterItems, BranchParameterAtlasRows, 4); DrawHints(true); }
        else if (currentPage == SettingsPage.SingleParameters1) { DrawParameters(Parameter1Items, Parameter1AtlasRows, 6); DrawHints(true); }
        else if (currentPage == SettingsPage.SingleParameters2) { DrawParameters(Parameter2Items, Parameter2AtlasRows, 4); DrawHints(true); }
        else if (currentPage == SettingsPage.BranchAccount) DrawBranchAccount();
        else if (currentPage == SettingsPage.SingleAccount) DrawSingleAccount();
        else if (currentPage == SettingsPage.ReportConfirm) { DrawSingleMenu(); DrawReport(); }
        else if (currentPage == SettingsPage.SystemPassword) { DrawSingleMenu(); DrawPassword(); }
        else if (currentPage == SettingsPage.SystemSettings) { DrawSystemSettings(); DrawHints(true); }
        else if (currentPage == SettingsPage.SystemValueEditor) { DrawSystemSettings(); DrawSystemValueEditor(); }
        else if (currentPage == SettingsPage.Advanced) { DrawAdvanced(); DrawHints(true); }
    }

    private void DrawTitle()
    {
        int x = 0, y = 0, width = 112;
        string fallback = "参数调整";
        if (currentPage == SettingsPage.Root) { y = 56; fallback = "品种选择"; }
        else if (currentPage == SettingsPage.BranchMenu) { y = 112; fallback = "分机设置"; }
        else if (currentPage == SettingsPage.SingleMenu || currentPage == SettingsPage.ReportConfirm || currentPage == SettingsPage.SystemPassword) { y = 84; fallback = "单机设置"; }
        else if (currentPage == SettingsPage.SystemSettings || currentPage == SettingsPage.SystemValueEditor) { y = 28; fallback = "系统设置"; }
        else if (currentPage == SettingsPage.BranchAccount || currentPage == SettingsPage.SingleAccount) { x = 112; width = 56; fallback = "查帐"; }
        float drawWidth = width == 56 ? 64 : 112;
        Rect destination = new Rect(568 - drawWidth, 108, drawWidth, 28);
        if (titleAtlas != null) DrawCrop(destination, titleAtlas, x, y, width, 28);
        else DrawShadow(destination, fallback, valueStyle);
        if (currentPage == SettingsPage.SingleParameters1 || currentPage == SettingsPage.SingleParameters2)
            DrawShadow(new Rect(566, 106, 20, 30), currentPage == SettingsPage.SingleParameters1 ? "1" : "2", titleNumberStyle);
    }

    private void DrawRoot()
    {
        float[] rows = { 137, 172, 207, 242 };
        for (int i = 0; i < RootItems.Length; i++)
        {
            Rect pill = new Rect(120, rows[i], i == 3 ? 80 : 96, 24);
            GUI.DrawTexture(pill, i == selectedRow ? selectedPillTexture : idlePillTexture);
            DrawShadow(pill, RootItems[i], menuStyle);
            if (i == selectedRow) DrawCursor(85, rows[i]);
        }
        DrawArrow(new Rect(358, rows[0] + 4, 8, 14), leftArrowTexture);
        DrawArrow(new Rect(486, rows[0] + 4, 8, 14), rightArrowTexture);
        DrawColored(new Rect(370, rows[0] - 1, 112, 24), machineMode == 0 ? "单机" : "分机", valueStyle, Cyan());
    }

    private void DrawPlainMenu(string[] labels, float firstY, float step, float width)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            float y = firstY + i * step;
            Rect pill = new Rect(120, y, labels[i] == "【退出】" ? 80 : width, 24);
            GUI.DrawTexture(pill, i == selectedRow ? selectedPillTexture : idlePillTexture);
            DrawShadow(pill, labels[i], menuStyle);
            if (i == selectedRow) DrawCursor(85, y);
        }
    }

    private void DrawSingleMenu()
    {
        float[] rows = { 124, 155, 186, 217, 248, 279, 310 };
        for (int i = 0; i < SingleItems.Length; i++)
        {
            Rect pill = new Rect(120, rows[i], 88, 22);
            GUI.DrawTexture(pill, i == selectedRow ? selectedPillTexture : idlePillTexture);
            if (singleMachineMenuAtlas != null && SingleAtlasRows[i] >= 0) DrawCrop(new Rect(128, rows[i] + 3, 72, 16), singleMachineMenuAtlas, 0, SingleAtlasRows[i], 72, 16);
            else DrawShadow(pill, SingleItems[i], hintStyle);
            if (i == selectedRow) DrawCursor(85, rows[i]);
        }
    }

    private void DrawParameters(string[] labels, int[] atlasRows, int adjustableRows)
    {
        // Keep the first adjustable row below the 28-pixel recovered title.
        // The nine-row page uses the original compact spacing so all actions
        // still fit above the navigation hints.
        float firstY = labels.Length >= 9 ? 138 : 139;
        float step = labels.Length >= 9 ? 20 : 26;
        for (int i = 0; i < labels.Length; i++)
        {
            float y = firstY + i * step;
            Rect pill = new Rect(112, y, 128, 20);
            GUI.DrawTexture(pill, (i & 1) == 0 ? idlePillTexture : selectedPillTexture);
            if (parameterLabelsAtlas != null && atlasRows[i] >= 0) DrawCrop(new Rect(112, y + 2, 128, 16), parameterLabelsAtlas, 0, atlasRows[i], 128, 16);
            else DrawShadow(pill, labels[i], smallStyle);
            if (i == selectedRow) DrawCursor(77, y);
            if (i < adjustableRows)
            {
                bool selected = i == selectedRow;
                DrawColored(new Rect(350, y - 1, 150, 22), GetParameterValue(currentPage, i), smallStyle, selected ? Cyan() : Color.white);
                if (selected)
                {
                    DrawArrow(new Rect(340, y + 3, 8, 14), leftArrowTexture);
                    DrawArrow(new Rect(502, y + 3, 8, 14), rightArrowTexture);
                }
            }
        }
    }

    private string GetParameterValue(SettingsPage page, int row)
    {
        if (page == SettingsPage.BranchParameters)
        {
            if (row == 0) return branchNumber + "号";
            if (row == 1) return branchVolume + "级";
            if (row == 2) return AcceptorTypes[branchType];
            if (row == 3) return AcceptorSources[branchSource];
        }
        else if (page == SettingsPage.SingleParameters1)
        {
            if (row == 0) return "1币" + CoinPoints[coinPointIndex] + "分";
            if (row == 1) return Difficulties[difficultyIndex];
            if (row == 2) return ticketCoins + "币2张";
            if (row == 3) return lionMaxBet + "分";
            if (row == 4) return otherMaxBet + "分";
            if (row == 5) return MinimumBets[minimumBetIndex] + "分";
        }
        else if (page == SettingsPage.SingleParameters2)
        {
            if (row == 0) return singleVolume + "级";
            if (row == 1) return Multipliers[multiplierIndex];
            if (row == 2) return AcceptorTypes[singleType];
            if (row == 3) return AcceptorSources[singleSource];
        }
        return string.Empty;
    }

    private void DrawBranchAccount()
    {
        DrawLiveAccount();
    }

    private void DrawSingleAccount()
    {
        DrawLiveAccount();
    }

    private void DrawAccountColumn(float x, float firstY, int[] rows, string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            float y = firstY + i * 22;
            Rect pill = new Rect(x, y, 120, 20);
            GUI.DrawTexture(pill, (i & 1) == 0 ? idlePillTexture : selectedPillTexture);
            if (accountLabelsAtlas != null) DrawCrop(new Rect(x + 12, y + 2, 96, 16), accountLabelsAtlas, 0, rows[i], 96, 16);
            DrawColored(new Rect(x + 122, y, 104, 20), values[i], smallStyle, Color.white);
        }
    }

    private void DrawAccountExit(float y)
    {
        Rect pill = new Rect(430, y, 96, 22);
        GUI.DrawTexture(pill, idlePillTexture);
        DrawShadow(pill, "【退出】", menuStyle);
        DrawCursor(392, y);
        DrawShadow(new Rect(235, 327, 120, 16), "[开始]确定", hintStyle);
    }

    private void DrawReport()
    {
        DrawColored(new Rect(345, 184, 190, 24), "你确定要报帐吗？", menuStyle, new Color32(255, 115, 134, 255));
        DrawChoice(new Rect(374, 211, 48, 24), "是", reportYes);
        DrawChoice(new Rect(444, 211, 48, 24), "否", !reportYes);
        DrawShadow(new Rect(342, 252, 190, 18), "[左右][CD]移动", hintStyle);
        DrawShadow(new Rect(342, 272, 150, 18), "[开始]确定", hintStyle);
    }

    private void DrawChoice(Rect rect, string text, bool selected)
    {
        if (selected)
        {
            DrawColorRect(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), new Color32(246, 55, 90, 255));
            DrawColorRect(rect, new Color32(0, 78, 117, 255));
        }
        DrawColored(rect, text, valueStyle, selected ? Cyan() : Color.white);
    }

    private void DrawPassword()
    {
        Rect panel = new Rect(288, 154, 200, 136);
        if (passwordPanelArtwork != null) GUI.DrawTexture(panel, passwordPanelArtwork);
        else DrawColorRect(panel, new Color32(102, 67, 48, 255));
        DrawColored(new Rect(304, 160, 168, 20), passwordError ? "密码错误，请重新输入" : "请输入系统密码", hintStyle, Color.white);
        DrawColorRect(new Rect(318, 184, 140, 20), new Color32(239, 238, 222, 255));
        float spacing = passwordDigits.Length == 8 ? 16f : 20f;
        float startX = passwordDigits.Length == 8 ? 323f : 327f;
        for (int i = 0; i < passwordDigits.Length; i++)
        {
            Rect digit = new Rect(startX + i * spacing, 184, spacing, 20);
            if (passwordDigitTouched[i])
                DrawColored(digit, passwordDigits[i].ToString(), hintStyle, i == passwordCursor ? Cyan() : Color.black);
        }
        DrawColored(new Rect(270, 184, 24, 20), "◀", valueStyle, new Color32(49, 101, 255, 255));
        DrawColored(new Rect(300, 210, 180, 17), "1. [左右][CD]移动光标", hintStyle, Color.white);
        DrawColored(new Rect(300, 227, 180, 17), "2. [上下][AB]改变数字", hintStyle, Color.white);
        DrawColored(new Rect(300, 244, 180, 17), "3. [开始]确定", hintStyle, Color.white);
        DrawColored(new Rect(300, 261, 180, 17), "4. [取消]取消", hintStyle, Color.white);
    }

    private void DrawSystemSettings()
    {
        float[] rows = { 124, 157, 190, 223, 256, 289 };
        for (int i = 0; i < SystemItems.Length; i++)
        {
            Rect pill = new Rect(120, rows[i], 96, 22);
            GUI.DrawTexture(pill, i == selectedRow ? selectedPillTexture : idlePillTexture);
            if (systemLabelsAtlas != null)
                DrawCrop(new Rect(128, rows[i] + 3, 80, 16), systemLabelsAtlas, 0, i * 16, 80, 16);
            else
                DrawShadow(pill, SystemItems[i], hintStyle);
            if (i == selectedRow) DrawCursor(85, rows[i]);
        }

        DrawSystemValue(0, runningMinutes + "分钟", true, rows[0]);
        DrawSystemValue(1, systemMachineNumber.ToString("D8"), false, rows[1]);
        DrawSystemValue(2, systemFormula.ToString("D8"), false, rows[2]);
    }

    private void DrawSystemValue(int row, string value, bool adjustable, float y)
    {
        bool selected = selectedRow == row;
        DrawColored(new Rect(350, y - 1, 150, 22), value, smallStyle, selected ? Cyan() : Color.white);
        if (adjustable && selected)
        {
            DrawArrow(new Rect(340, y + 3, 8, 14), leftArrowTexture);
            DrawArrow(new Rect(502, y + 3, 8, 14), rightArrowTexture);
        }
    }

    private void DrawSystemValueEditor()
    {
        Rect panel = new Rect(288, 154, 200, 136);
        if (passwordPanelArtwork != null) GUI.DrawTexture(panel, passwordPanelArtwork);
        else DrawColorRect(panel, new Color32(102, 67, 48, 255));
        string prompt = editorTarget == SystemValueTarget.MachineNumber ? "请输入机台号码" :
            (editorTarget == SystemValueTarget.Formula ? "请输入计算公式" : "请输入系统密码");
        DrawColored(new Rect(304, 160, 168, 20), prompt, hintStyle, Color.white);
        DrawColorRect(new Rect(318, 184, 140, 20), new Color32(239, 238, 222, 255));
        float spacing = editorDigitCount == 8 ? 16f : 20f;
        float startX = editorDigitCount == 8 ? 323f : 327f;
        for (int i = 0; i < editorDigitCount; i++)
        {
            Rect digit = new Rect(startX + i * spacing, 184, spacing, 20);
            DrawColored(digit, editorDigits[i].ToString(), hintStyle, i == editorCursor ? Cyan() : Color.black);
        }
        DrawColored(new Rect(270, 184, 24, 20), "◀", valueStyle, new Color32(49, 101, 255, 255));
        DrawColored(new Rect(300, 210, 180, 17), "1. [左右][CD]移动光标", hintStyle, Color.white);
        DrawColored(new Rect(300, 227, 180, 17), "2. [上下][AB]改变数字", hintStyle, Color.white);
        DrawColored(new Rect(300, 244, 180, 17), "3. [开始]确定", hintStyle, Color.white);
        DrawColored(new Rect(300, 261, 180, 17), "4. [取消]取消", hintStyle, Color.white);
    }

    private void DrawHints(bool modify)
    {
        if (navigationHints != null)
        {
            DrawCrop(new Rect(113, 327, 136, 16), navigationHints, 0, 0, 136, 16);
            if (modify) DrawCrop(new Rect(265, 327, 136, 16), navigationHints, 0, 16, 136, 16);
        }
        else
        {
            DrawShadow(new Rect(113, 327, 136, 16), "[上下][AB]移动", hintStyle);
            if (modify) DrawShadow(new Rect(265, 327, 136, 16), "[左右][CD]修改", hintStyle);
        }
        DrawShadow(new Rect(modify ? 421 : 285, 327, 90, 16), "[开始]确定", hintStyle);
    }

    private void DrawCursor(float x, float y)
    {
        if (cursorArtwork != null) GUI.DrawTexture(new Rect(x, y + 3, 32, 16), cursorArtwork);
        else DrawShadow(new Rect(x + 3, y - 1, 28, 24), "▶", menuStyle);
    }
    private static Color Cyan() { return new Color32(44, 239, 242, 255); }
    private static void DrawArrow(Rect rect, Texture2D texture) { GUI.DrawTexture(rect, texture); }
    private static void DrawColored(Rect rect, string text, GUIStyle style, Color color)
    {
        Color old = GUI.color; GUI.color = color; GUI.Label(rect, text, style); GUI.color = old;
    }
    private void DrawShadow(Rect rect, string text, GUIStyle style)
    {
        DrawColored(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style, new Color32(126, 0, 126, 255));
        DrawColored(rect, text, style, Color.white);
    }
    private static void DrawCrop(Rect destination, Texture2D texture, int x, int topY, int width, int height)
    {
        Rect uv = new Rect((float)x / texture.width, 1f - (float)(topY + height) / texture.height,
            (float)width / texture.width, (float)height / texture.height);
        GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
    }
    private void DrawColorRect(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, whiteTexture); GUI.color = old;
    }

    private void EnsureArtwork()
    {
        if (whiteTexture != null) return;
        whiteTexture = CreateSolid(2, 2, Color.white);
        backgroundTexture = new Texture2D(8, 480, TextureFormat.RGBA32, false);
        backgroundTexture.filterMode = FilterMode.Point; backgroundTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < backgroundTexture.height; y++)
        {
            float distance = Mathf.Abs(y - 240f) / 240f;
            Color color = Color.Lerp(new Color32(4, 35, 112, 255), new Color32(0, 12, 53, 255), distance);
            for (int x = 0; x < backgroundTexture.width; x++) backgroundTexture.SetPixel(x, y, color);
        }
        backgroundTexture.Apply(false, true);
        panelTexture = CreatePanel();
        idlePillTexture = CreatePill(new Color32(0, 89, 112, 255), new Color32(0, 182, 189, 255));
        selectedPillTexture = CreatePill(new Color32(111, 41, 137, 255), new Color32(223, 82, 226, 255));
        leftArrowTexture = CreateArrow(false); rightArrowTexture = CreateArrow(true);
        menuStyle = CreateStyle(17); valueStyle = CreateStyle(18); titleNumberStyle = CreateStyle(24);
        hintStyle = CreateStyle(12); smallStyle = CreateStyle(15);
    }

    private GUIStyle CreateStyle(int size)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.font = uiFont; style.fontSize = size; style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter; style.clipping = TextClipping.Overflow;
        style.normal.textColor = Color.white; return style;
    }
    private static Texture2D CreateSolid(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height]; for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        texture.SetPixels(pixels); texture.Apply(false, true); return texture;
    }
    private static Texture2D CreatePanel()
    {
        Texture2D texture = new Texture2D(8, 64, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 64; y++)
        {
            Color c = Color.Lerp(new Color32(5, 111, 188, 255), new Color32(1, 70, 150, 255), y / 63f);
            if ((y % 8) == 0) c = new Color32(22, 128, 197, 255);
            for (int x = 0; x < 8; x++) texture.SetPixel(x, y, c);
        }
        texture.Apply(false, true); return texture;
    }
    private static Texture2D CreatePill(Color center, Color edge)
    {
        const int width = 128, height = 24; const float radius = 11;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            float dx = x < radius ? radius - x : (x > width - radius - 1 ? x - (width - radius - 1) : 0);
            float dy = y < radius ? radius - y : (y > height - radius - 1 ? y - (height - radius - 1) : 0);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            texture.SetPixel(x, y, d > radius ? Color.clear : (d > radius - 2 ? edge : center));
        }
        texture.Apply(false, true); return texture;
    }
    private static Texture2D CreateArrow(bool right)
    {
        Texture2D texture = new Texture2D(8, 14, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 14; y++)
        {
            int extent = y < 7 ? y : 13 - y;
            for (int x = 0; x < 8; x++)
            {
                bool inside = right ? x <= extent : x >= 7 - extent;
                texture.SetPixel(x, y, inside ? (Color)new Color32(250, 41, 50, 255) : Color.clear);
            }
        }
        texture.Apply(false, true); return texture;
    }

    private void OnDestroy()
    {
        UnbindDevice();
        DestroyTexture(whiteTexture); DestroyTexture(backgroundTexture); DestroyTexture(panelTexture);
        DestroyTexture(idlePillTexture); DestroyTexture(selectedPillTexture);
        DestroyTexture(leftArrowTexture); DestroyTexture(rightArrowTexture);
    }
    private static void DestroyTexture(Texture2D texture) { if (texture != null) Destroy(texture); }
}
