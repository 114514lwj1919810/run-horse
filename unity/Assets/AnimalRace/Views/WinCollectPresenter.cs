using UnityEngine;

public sealed class WinCollectPresenter : MonoBehaviour
{
    [SerializeField]
    private DisplayStateController displayController;

    private bool visualTransferActive;
    private float visualTransferStartedAt;
    private int visualStartWin;
    private int visualStartCredit;
    private DisplayPage previousPage = DisplayPage.Loading;

    public DisplayStateController DisplayController
    {
        get { return displayController; }
        set { displayController = value; }
    }

    public int DisplayedWinPoints { get; private set; }
    public int DisplayedCredit { get; private set; }

    public bool IsVisible
    {
        get
        {
            return displayController != null &&
                   (displayController.CurrentPage == DisplayPage.WinReady ||
                    displayController.CurrentPage == DisplayPage.WinCollecting);
        }
    }

    public string Headline
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }
            if (displayController.CurrentPage == DisplayPage.WinReady)
            {
                return "中奖积分等待收分";
            }
            if (displayController.CollectFastFinished)
            {
                return "快速收分完成";
            }
            return displayController.CollectActive ? "正在收分" : "收分完成";
        }
    }

    public string Subtitle
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }
            if (displayController.CurrentPage == DisplayPage.WinReady)
            {
                return "按开始键开始收分";
            }
            if (displayController.CollectFastFinished)
            {
                return "剩余 WIN 已一次转入 CREDIT";
            }
            return displayController.CollectActive
                ? "再次按开始键可快速收分"
                : "全部积分已经转入 CREDIT";
        }
    }

    public string Detail
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }

            string delta = displayController.CollectDelta > 0 && displayController.CollectActive
                ? "\n本次转移：" + displayController.CollectDelta
                : string.Empty;
            return "WIN：" + DisplayedWinPoints +
                   "\nCREDIT：" + DisplayedCredit + delta;
        }
    }

    private void Awake()
    {
        if (displayController == null)
        {
            displayController = FindObjectOfType<DisplayStateController>();
        }
        SyncToAuthoritativeValues();
    }

    private void Update()
    {
        if (displayController == null)
        {
            return;
        }

        DisplayPage page = displayController.CurrentPage;
        if (page == DisplayPage.WinReady)
        {
            visualTransferActive = false;
            SyncToAuthoritativeValues();
        }
        else if (page == DisplayPage.WinCollecting)
        {
            if (displayController.CollectFastFinished)
            {
                visualTransferActive = false;
                SyncToAuthoritativeValues();
            }
            else
            {
                if (!visualTransferActive || previousPage != DisplayPage.WinCollecting)
                {
                    visualTransferActive = true;
                    visualTransferStartedAt = Time.unscaledTime;
                    int knownWinTotal = Mathf.Max(
                        displayController.WinTotalPoints,
                        displayController.WinPoints);
                    int alreadyTransferred = Mathf.Max(
                        0, knownWinTotal - displayController.WinPoints);
                    bool continuingFromReady = previousPage == DisplayPage.WinReady;
                    visualStartWin = continuingFromReady
                        ? DisplayedWinPoints
                        : knownWinTotal;
                    visualStartCredit = continuingFromReady
                        ? DisplayedCredit
                        : Mathf.Max(0, displayController.Balance - alreadyTransferred);
                }

                int predictedWin;
                int predictedCredit;
                EvaluateVisualTransfer(
                    visualStartWin,
                    visualStartCredit,
                    Time.unscaledTime - visualTransferStartedAt,
                    out predictedWin,
                    out predictedCredit);

                int displayedWin;
                int displayedCredit;
                ApplyTimerDrivenVisualValues(
                    predictedWin,
                    predictedCredit,
                    out displayedWin,
                    out displayedCredit);
                DisplayedWinPoints = displayedWin;
                DisplayedCredit = displayedCredit;

                if (!displayController.CollectActive &&
                    Time.unscaledTime - visualTransferStartedAt >=
                    DisplayStateController.GetWinCollectVisualDurationSeconds(
                        visualStartWin))
                {
                    visualTransferActive = false;
                    SyncToAuthoritativeValues();
                }
            }
        }
        else
        {
            visualTransferActive = false;
            SyncToAuthoritativeValues();
        }

        previousPage = page;
    }

    private void SyncToAuthoritativeValues()
    {
        DisplayedWinPoints = displayController == null ? 0 : displayController.WinPoints;
        DisplayedCredit = displayController == null ? 0 : displayController.Balance;
    }

    public static void EvaluateVisualTransfer(
        int startWin,
        int startCredit,
        float elapsedSeconds,
        out int displayedWin,
        out int displayedCredit)
    {
        int safeWin = Mathf.Max(0, startWin);
        int transferred = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) *
                             DisplayStateController.WinCollectPointsPerSecond),
            0,
            safeWin);
        displayedWin = safeWin - transferred;
        displayedCredit = Mathf.Max(0, startCredit) + transferred;
    }

    public static void ApplyTimerDrivenVisualValues(
        int predictedWin,
        int predictedCredit,
        out int displayedWin,
        out int displayedCredit)
    {
        // Some cabinets send only a delta=0 start frame followed by the final
        // snapshot. Keep the visible counter moving at 10 points per second;
        // the final device message still supplies the authoritative totals.
        displayedWin = Mathf.Max(0, predictedWin);
        displayedCredit = Mathf.Max(0, predictedCredit);
    }
}
