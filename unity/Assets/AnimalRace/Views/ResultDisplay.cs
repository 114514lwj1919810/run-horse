using System;
using UnityEngine;

public sealed class ResultDisplay : MonoBehaviour
{
    public const float MaximumResultHoldSeconds = 1.5f;

    public event Action ResultCompleted;
    [SerializeField]
    private DisplayStateController displayController;

    [SerializeField]
    private AndroidSerialService serialService;

    private uint activeSequence = uint.MaxValue;
    private float shownAt;
    private bool completionSent;
    private bool activeDemo;

    public DisplayStateController DisplayController
    {
        get { return displayController; }
        set { displayController = value; }
    }

    public AndroidSerialService SerialService
    {
        get { return serialService; }
        set { serialService = value; }
    }

    public bool IsVisible
    {
        get { return displayController != null && displayController.CurrentPage == DisplayPage.Result; }
    }

    public bool CompletionSent { get { return completionSent; } }

    private void Awake()
    {
        if (displayController == null)
        {
            displayController = FindObjectOfType<DisplayStateController>();
        }

        if (serialService == null)
        {
            serialService = FindObjectOfType<AndroidSerialService>();
        }
    }

    private void Update()
    {
        if (!IsVisible)
        {
            activeSequence = uint.MaxValue;
            return;
        }

        if (activeSequence != displayController.CurrentSequence)
        {
            activeSequence = displayController.CurrentSequence;
            shownAt = Time.unscaledTime;
            completionSent = false;
            activeDemo = displayController.IsStandbyDemo;
        }

        float durationSeconds = GetPresentationDurationSeconds(
            displayController.ResultDurationMilliseconds, activeDemo);
        if (!completionSent && Time.unscaledTime - shownAt >= durationSeconds)
        {
            completionSent = true;
            uint completedRound = displayController.CurrentRound;
            if (serialService != null)
            {
                serialService.SendText(
                    (activeDemo ? "DEMO_RESULT_OVER:seq=" : "RESULT_OVER:seq=") +
                    displayController.CurrentSequence +
                    ",round=" + displayController.CurrentRound + "\r\n");
            }

            Action completed = ResultCompleted;
            if (completed != null)
            {
                completed();
            }

            displayController.CompleteResultPresentation(completedRound);
        }
    }

    public static float GetPresentationDurationSeconds(int requestedMilliseconds)
    {
        return GetPresentationDurationSeconds(requestedMilliseconds, false);
    }

    public static float GetPresentationDurationSeconds(int requestedMilliseconds, bool isDemo)
    {
        float requestedSeconds = requestedMilliseconds / 1000f;
        if (isDemo)
        {
            return Mathf.Max(0.25f, requestedSeconds);
        }
        return Mathf.Min(MaximumResultHoldSeconds, Mathf.Max(0.25f, requestedSeconds));
    }

    public string Headline
    {
        get { return displayController == null ? string.Empty : displayController.WinnerName + "获胜"; }
    }

    public string Detail
    {
        get
        {
            if (displayController == null)
            {
                return string.Empty;
            }

            if (displayController.IsStandbyDemo)
            {
                return "待机演示局（不计分、不结算）";
            }

            return "本局中奖：" + displayController.WinTotalPoints + "积分\n" +
                   "赔率：" + displayController.ResultOdds + "倍\n" +
                   "奖励已进入 WIN，尚未加入 CREDIT\n" +
                   "WIN：" + displayController.WinPoints + "  CREDIT：" +
                   displayController.Balance;
        }
    }
}
