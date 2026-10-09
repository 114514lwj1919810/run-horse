using System;
using UnityEngine;

public sealed class AnimalRaceAnimationController : MonoBehaviour
{
    public const float FinishPosition01 = RaceCourseGeometry.CourseLength /
        (RaceCourseGeometry.StartWorldX - RaceCourseGeometry.EndWorldX);
    public const float FinishHoldSeconds = 1.5f;

    public event Action RaceCompleted;
    [SerializeField]
    private DisplayStateController displayController;

    [SerializeField]
    private AndroidSerialService serialService;

    private readonly float[] positions = new float[AnimalCatalog.Count];
    private RacePresentationPlan presentation;
    private readonly RaceObstaclePose[] obstaclePoses = new RaceObstaclePose[AnimalCatalog.Count];
    private int activeCourseScene = -1;
    [NonSerialized] public int ObstacleSceneIndexOverride = -1;
    [NonSerialized] public float VisualPreviewProgress = -1f;
    private uint activeSequence = uint.MaxValue;
    private uint activeRound;
    private int activeWinner;
    private float raceStartedAt;
    private float raceMotionDurationSeconds = 1f;
    private bool finishHoldActive;
    private float finishHoldStartedAt;
    private bool finishSceneLocked;
    private bool raceOverSent;
    private bool raceCompletionNotified;
    private bool pausedByOverlay;
    private float pausedAt;
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

    public bool IsRunning { get; private set; }
    public bool IsFinishHoldActive { get { return finishHoldActive; } }
    public bool IsFinishSceneLocked { get { return finishSceneLocked; } }
    public float FinishHoldElapsedSeconds
    {
        get
        {
            return finishHoldActive
                ? Mathf.Max(0f, Time.unscaledTime - finishHoldStartedAt)
                : 0f;
        }
    }
    public float FinishSceneElapsedSeconds
    {
        get
        {
            return finishSceneLocked
                ? Mathf.Max(0f, Time.unscaledTime - finishHoldStartedAt)
                : 0f;
        }
    }
    public bool RaceOverSent { get { return raceOverSent; } }
    public float Progress01 { get; private set; }
    public int ActiveCourseScene { get { return activeCourseScene; } }
    public bool HasObstacleCourse { get { return activeCourseScene >= 0; } }
    public RacePresentationPlan Presentation { get { return presentation; } }
    public RaceCourseLayout Course { get { return presentation == null ? RaceCourseLayout.Default : presentation.Course; } }
    public float CameraLeft
    {
        get
        {
            if (!IsRunning && !finishHoldActive && !finishSceneLocked && VisualPreviewProgress < 0f)
            {
                return Course.InitialCameraLeft;
            }

            // The camera follows the configured race clock, while the animals keep
            // their own seeded pace and can move ahead of or behind the camera.
            float elapsed = VisualPreviewProgress >= 0f
                ? VisualPreviewProgress * Course.Duration
                : Mathf.Clamp(Time.unscaledTime - raceStartedAt, 0f, Course.Duration);
            return Course.GetCameraLeft(elapsed);
        }
    }
    public float GetWorldX(int animalId)
    {
        return presentation == null ? Course.StartWorldX + RaceCourseLayout.GetStartOffset(animalId)
            : GetObstaclePose(animalId).Foot.x;
    }

    public RaceObstaclePose GetObstaclePose(int animalId)
    {
        return AnimalCatalog.IsValidId(animalId) ? obstaclePoses[animalId - 1] : new RaceObstaclePose();
    }

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

        ResetToStart();
    }

    private void Update()
    {
        if (displayController == null)
        {
            return;
        }

        if (displayController.CurrentPage == DisplayPage.Setting &&
            (IsRunning || finishHoldActive))
        {
            if (!pausedByOverlay)
            {
                pausedByOverlay = true;
                pausedAt = Time.unscaledTime;
            }
            return;
        }

        if (displayController.CurrentPage == DisplayPage.Race)
        {
            if (pausedByOverlay)
            {
                float pausedDuration = Time.unscaledTime - pausedAt;
                raceStartedAt += pausedDuration;
                if (finishHoldActive)
                {
                    finishHoldStartedAt += pausedDuration;
                }
                pausedByOverlay = false;
            }
            if (activeSequence != displayController.CurrentSequence ||
                activeWinner != displayController.WinnerId)
            {
                BeginRace(
                    displayController.CurrentSequence,
                    displayController.CurrentRound,
                    displayController.WinnerId,
                    displayController.RaceDurationMilliseconds);
            }

            if (finishHoldActive)
            {
                TryCompleteFinishHold(Time.unscaledTime - finishHoldStartedAt);
                return;
            }

            if (IsRunning)
            {
                if (VisualPreviewProgress >= 0f)
                {
                    Evaluate(VisualPreviewProgress);
                    return;
                }
                float progress = Mathf.Clamp01(
                    (Time.unscaledTime - raceStartedAt) / raceMotionDurationSeconds);
                Evaluate(progress);
                if (!TryFinishWhenWinnerCrosses() && progress >= 1f)
                {
                    FinishRace();
                }
            }
        }
        else if (displayController.CurrentPage == DisplayPage.Idle ||
                 displayController.CurrentPage == DisplayPage.Betting ||
                 displayController.CurrentPage == DisplayPage.Loading)
        {
            if (IsRunning || activeSequence != uint.MaxValue)
            {
                ResetToStart();
            }
        }
    }

    public float GetPosition01(int animalId)
    {
        return AnimalCatalog.IsValidId(animalId) ? positions[animalId - 1] : 0f;
    }

    public void BeginRace(uint sequence, uint round, int winnerId, int durationMilliseconds)
    {
        activeSequence = sequence;
        activeRound = round;
        activeWinner = winnerId;
        activeDemo = displayController != null && displayController.IsStandbyDemo;
        float totalDurationSeconds = Mathf.Max(0.25f, durationMilliseconds / 1000f);
        raceMotionDurationSeconds = totalDurationSeconds;
        raceStartedAt = Time.unscaledTime;
        finishHoldActive = false;
        finishHoldStartedAt = 0f;
        finishSceneLocked = false;
        raceOverSent = false;
        raceCompletionNotified = false;
        pausedByOverlay = false;
        IsRunning = true;
        activeCourseScene = -1;
        AnimalRaceUI ui = GetComponent<AnimalRaceUI>();
        if (ObstacleSceneIndexOverride >= 0)
            activeCourseScene = Mathf.Clamp(ObstacleSceneIndexOverride, 0, 2);
        else if (ui != null && ui.HasObstacleArtwork)
            activeCourseScene = ui.GetActiveSceneIndex();
        presentation = new RacePresentationPlan(new RacePresentationRequest(winnerId,
            activeCourseScene, raceMotionDurationSeconds, RacePresentationPlan.CreateSeed(sequence, round)));
        Evaluate(0f);
    }

    public void Evaluate(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        Progress01 = clamped;
        for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
        {
            int index = animalId - 1;
            if (presentation != null)
            {
                obstaclePoses[index] = presentation.Sample(animalId, clamped * raceMotionDurationSeconds);
                positions[index] = Course.ToPosition01(obstaclePoses[index].Foot.x);
            }
            else
                positions[index] = EvaluatePosition(animalId, activeWinner, clamped);
        }
    }

    public static float GetCourseFinishPosition(int animalId, int winnerId, uint sequence, uint round)
    {
        return RacePresentationPlan.GetFinishPosition(animalId, winnerId, sequence, round);
    }

    public static float EvaluatePosition(int animalId, int winnerId, float progress)
    {
        return RacePresentationPlan.EvaluatePreviewPosition(animalId, winnerId, progress);
    }

    public void FinishRace()
    {
        IsRunning = false;
        finishHoldActive = false;
        finishSceneLocked = false;

        NotifyRaceCompleted();

        if (displayController != null)
        {
            displayController.CompleteRacePresentation(activeRound);
        }
    }

    private void NotifyRaceCompleted()
    {
        if (raceCompletionNotified)
        {
            return;
        }

        raceCompletionNotified = true;

        Action completed = RaceCompleted;
        if (completed != null)
        {
            completed();
        }

        if (!raceOverSent && serialService != null)
        {
            raceOverSent = serialService.SendText(
                (activeDemo ? "DEMO_RACE_OVER:seq=" : "RACE_OVER:seq=") +
                activeSequence + ",round=" + activeRound + "\r\n");
        }

    }

    public bool TryFinishWhenWinnerCrosses()
    {
        float finishContact = AnimalCatalog.IsValidId(activeWinner)
            ? Course.ToPosition01(RaceCourseGeometry.GetFinishContactWorldX(activeWinner))
            : 1f;
        if (!IsRunning || finishHoldActive || !AnimalCatalog.IsValidId(activeWinner) ||
            positions[activeWinner - 1] < finishContact - 0.001f / Course.ProgressSpan)
        {
            return false;
        }

        positions[activeWinner - 1] = finishContact;

        finishHoldActive = true;
        finishHoldStartedAt = Time.unscaledTime;
        finishSceneLocked = true;
        IsRunning = false;
        NotifyRaceCompleted();
        return true;
    }

    public bool TryCompleteFinishHold(float elapsedSeconds)
    {
        if (!finishHoldActive || elapsedSeconds < FinishHoldSeconds)
        {
            return false;
        }

        finishHoldActive = false;
        if (displayController != null)
        {
            displayController.CompleteRacePresentation(activeRound);
        }
        return true;
    }

    public void ResetToStart()
    {
        for (int index = 0; index < positions.Length; index++)
        {
            positions[index] = 0f;
            obstaclePoses[index] = new RaceObstaclePose { Foot = new Vector2(RaceCourseGeometry.StartWorldX,
                RaceCourseGeometry.GetLaneFootBaseline(index + 1)),
                BallWorldX = RaceObstacleTimeline.GetInitialBallWorldX(index + 1) };
        }

        activeCourseScene = -1;
        presentation = null;

        activeSequence = uint.MaxValue;
        activeRound = 0;
        activeWinner = 0;
        Progress01 = 0f;
        IsRunning = false;
        raceOverSent = false;
        raceCompletionNotified = false;
        finishHoldActive = false;
        finishHoldStartedAt = 0f;
        finishSceneLocked = false;
        pausedByOverlay = false;
        activeDemo = false;
    }
}
