using UnityEngine;

public sealed class AnimalRaceUI : MonoBehaviour
{
    public const float FinishShutterCloseStartSeconds = 0f;
    public const float FinishShutterClosedSeconds = 0.38f;
    public const float FinishShutterOpenStartSeconds = 0.54f;
    public const float FinishShutterEndSeconds = 0.88f;
    public const float FinishRankFlashStartSeconds = FinishShutterEndSeconds;
    public const float HistoryPanelSlideSeconds = 0.30f;
    public const float SelectedRunSpeedScale = 0.50f;
    public const float WinnerRunSpeedScale = 0.50f;
    private const float FinishShutterMinimumOpen = 0f;
    private const float WorldMinX = -375f;
    private const float WorldMaxX = RaceCourseGeometry.WorldMaxX;
    private const float WorldWidth = WorldMaxX - WorldMinX;
    private const float ViewportWidth = 640f;
    private const float StartWorldX = RaceCourseGeometry.StartWorldX;
    private const float StartMarkerWorldX = RaceCourseGeometry.StartMarkerWorldX;
    private const float FinishWorldX = RaceCourseGeometry.FinishWorldX;
    private const float ObstacleWorldX = RaceCourseGeometry.ObstacleWorldX;
    private const float InitialCameraLeft = RaceCourseGeometry.InitialCameraLeft;
    private const float PackScreenX = 425f;
    private const float HistoryPanelHiddenOffsetX = -160f;

    private float historyPanelSlide01;
    private float historyPanelLastUpdateAt = -1f;

    public const float RecoveredFramePadding = 6f;
    public const float GroundFootInset = RaceCourseGeometry.GroundFootInset;

    private static readonly float[] LaneLabelY =
    {
        184f, 240f, 296f, 352f, 408f, 474f
    };

    // Crops only transparent source padding. The visible portraits remain complete.
    private static readonly Rect[] PortraitUv =
    {
        new Rect(0.0352f, 0.0234f, 0.9277f, 0.9238f),
        new Rect(0.0352f, 0.0234f, 0.9297f, 0.5488f),
        new Rect(0.0312f, 0.0000f, 0.9375f, 0.9492f),
        new Rect(0.0352f, 0.0234f, 0.9277f, 0.8984f),
        new Rect(0.1484f, 0.0234f, 0.7031f, 0.9297f),
        new Rect(0.0352f, 0.0234f, 0.9297f, 0.6562f)
    };

    // The recovered 360x56 strip stores each portrait in a 60-pixel cell.
    // These crops retain a six-pixel breathing margin around the painted head,
    // instead of stretching the large transparent margins from the source ROM.
    private static readonly Rect[] RecoveredPortraitUv =
    {
        new Rect(11f / 360f, 9f / 56f, 40f / 360f, 44f / 56f),
        new Rect(66f / 360f, 5f / 56f, 49f / 360f, 48f / 56f),
        new Rect(128f / 360f, 13f / 56f, 41f / 360f, 38f / 56f),
        new Rect(185f / 360f, 13f / 56f, 48f / 360f, 40f / 56f),
        new Rect(248f / 360f, 11f / 56f, 41f / 360f, 45f / 56f),
        new Rect(308f / 360f, 14f / 56f, 40f / 360f, 39f / 56f)
    };

    // Higher-resolution head crops from the recovered idle artwork. The old
    // 360x56 portrait strip only supplied about 40x44 pixels per animal.
    private static readonly Rect[] HighResolutionPortraitUv =
    {
        new Rect(42f / 2172f, 220f / 724f, 265f / 2172f, 313f / 724f),
        new Rect(378f / 2172f, 194f / 724f, 325f / 2172f, 339f / 724f),
        new Rect(760f / 2172f, 248f / 724f, 277f / 2172f, 265f / 724f),
        new Rect(1106f / 2172f, 242f / 724f, 319f / 2172f, 291f / 724f),
        new Rect(1490f / 2172f, 232f / 724f, 279f / 2172f, 327f / 724f),
        new Rect(1858f / 2172f, 248f / 724f, 257f / 2172f, 277f / 724f)
    };

    private static readonly float[] BottomPortraitY =
    {
        379f, 383f, 385f, 376f, 375f, 382f
    };

    private static readonly float[] BottomPortraitHeight =
    {
        82f, 83f, 66f, 74f, 73f, 71f
    };

    private static readonly int[] RecoveredAvatarFrameStartX =
    {
        0, 107, 214, 320, 427, 534
    };

    private static readonly int[] RecoveredAvatarFrameWidth =
    {
        107, 107, 106, 107, 107, 106
    };

    private static readonly float[] RacerBottomPadding =
    {
        0f, 0f, 0f, 0f, 0f, 0f
    };

    private static readonly Color[] AnimalColors =
    {
        new Color(0.92f, 0.55f, 0.16f),
        new Color(0.42f, 0.70f, 0.78f),
        new Color(0.55f, 0.42f, 0.33f),
        new Color(0.76f, 0.80f, 0.86f),
        new Color(0.95f, 0.76f, 0.20f),
        new Color(0.84f, 0.72f, 0.57f)
    };

    private static readonly Color[] PortraitBorderColors =
    {
        new Color(0.96f, 0.63f, 0.12f),
        new Color(0.93f, 0.42f, 0.64f),
        new Color(0.66f, 0.48f, 0.80f),
        new Color(0.42f, 0.75f, 0.91f),
        new Color(0.31f, 0.48f, 0.96f),
        new Color(0.31f, 0.77f, 0.46f)
    };

    private static readonly float[] RunFramesPerSecond =
    {
        12f, 10f, 11f, 13f, 11f, 10f
    };

    public Font uiFont;
    public Font arcadeFont;
    public Font scoreFont;
    public Texture2D originalBetDigits;
    public Texture2D originalHistoryPanel;
    public Texture2D originalPlaqueDigits;
    // Only enabled by the explicit screenshot fixture, never by normal play.
    public bool PhotoReferencePreview;
    public bool PhotoReferenceFinish;
    private static readonly int[] PhotoHistory = { 4, 3, 6, 5, 5, 6, 5, 6, 6, 5 };
    private GUIStyle recoveredScoreStyle;
    public Texture2D animalRunSheet;
    public Texture2D[] animalRunSheets;
    public Texture2D[] animalIdleSheets;
    public Texture2D[] animalObstacleSheets;
    public bool HasObstacleArtwork
    {
        get { return animalObstacleSheets != null && animalObstacleSheets.Length == AnimalCatalog.Count; }
    }
    public Texture2D[] animalPortraits;
    public Texture2D[] animalRunFrames;
    public Texture2D[] animalFallFrames;
    public Texture2D[] animalJumpFrames;
    public Texture2D[] animalBallFrames;
    public Texture2D[] sceneBackgrounds;
    public Texture2D hayWallTexture;
    public Texture2D hayFragmentsTexture;
    public Texture2D lilyPadPoolTexture;
    public Texture2D beachBallStackTexture;
    public Texture2D beachBallSingleTexture;
    public Texture2D topBannerBlueTexture;
    public Texture2D topBannerPinkTexture;
    public Texture2D leftIconPanelTexture;
    public Texture2D finishRankNumbersTexture;
    public Texture2D singleAvatarFrameTexture;
    public Texture2D smallNumberPlaqueTexture;
    public Texture2D bottomAvatarBarTexture;
    public Texture2D[] originalSceneTopStrips;
    public Texture2D[] originalSceneTrackTiles;
    public Texture2D originalAvatarFrameStrip;
    public Texture2D originalPortraitStrip;
    public Texture2D originalBetBoxStrip;
    public Texture2D originalWinLabel;
    public Texture2D originalBonusLabel;
    public Texture2D originalCreditLabel;
    public Texture2D originalTopDigits;
    public Texture2D originalOddsGlyphs;
    public Texture2D originalSmallPlaque;
    public Texture2D originalFinishLine;
    public Texture2D originalFinishRanks;

    public int ForcedSceneIndex = -1;

    [SerializeField]
    private DisplayStateController displayController;

    [SerializeField]
    private AnimalRaceAnimationController animationController;

    [SerializeField]
    private ResultDisplay resultDisplay;

    [SerializeField]
    private PayoutDisplay payoutDisplay;

    [SerializeField]
    private FaultDisplay faultDisplay;

    [SerializeField]
    private SettingsDisplay settingsDisplay;

    [SerializeField]
    private WinCollectPresenter winCollectPresenter;

    private GUIStyle topLabelStyle;
    private GUIStyle topValueStyle;
    private GUIStyle laneLabelStyle;
    private GUIStyle animalLabelStyle;
    private GUIStyle slotTitleStyle;
    private GUIStyle slotValueStyle;
    private GUIStyle slotOddsStyle;
    private GUIStyle slotBetStyle;
    private GUIStyle hintStyle;
    private GUIStyle overlayTitleStyle;
    private GUIStyle overlayBodyStyle;
    private GUIStyle stateStyle;
    private GUIStyle panelStyle;
    private GUIStyle selectedPanelStyle;
    private Texture2D whiteTexture;
    private Texture2D circleTexture;
    private Texture2D darkTexture;
    private Texture2D panelTexture;
    private Texture2D selectedTexture;
    private readonly Texture2D[] clippedHistoryPortraits = new Texture2D[6];
    private readonly Texture2D[] clippedBottomPortraits = new Texture2D[6];

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

    public PayoutDisplay PayoutOverlay
    {
        get { return payoutDisplay; }
        set { payoutDisplay = value; }
    }

    public FaultDisplay FaultOverlay
    {
        get { return faultDisplay; }
        set { faultDisplay = value; }
    }

    public SettingsDisplay SettingsOverlay
    {
        get { return settingsDisplay; }
        set { settingsDisplay = value; }
    }

    public WinCollectPresenter WinCollectOverlay
    {
        get { return winCollectPresenter; }
        set { winCollectPresenter = value; }
    }

    private void Awake()
    {
        if (displayController == null)
        {
            displayController = FindObjectOfType<DisplayStateController>();
        }

        if (animationController == null)
        {
            animationController = FindObjectOfType<AnimalRaceAnimationController>();
        }

        if (resultDisplay == null)
        {
            resultDisplay = FindObjectOfType<ResultDisplay>();
        }

        if (payoutDisplay == null)
        {
            payoutDisplay = FindObjectOfType<PayoutDisplay>();
        }

        if (faultDisplay == null)
        {
            faultDisplay = FindObjectOfType<FaultDisplay>();
        }

        if (settingsDisplay == null)
        {
            settingsDisplay = FindObjectOfType<SettingsDisplay>();
        }

        if (winCollectPresenter == null)
        {
            winCollectPresenter = FindObjectOfType<WinCollectPresenter>();
        }
    }

    private void OnGUI()
    {
        if (displayController == null)
        {
            return;
        }

        EnsureStyles();
        Matrix4x4 previousMatrix = GUI.matrix;
        if (HasRecoveredOriginalArtwork())
        {
            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(Screen.width / 640f, Screen.height / 480f, 1f));
            DrawRecoveredOriginalFrame();

            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            DrawActiveOverlay();
        }
        else
        {
            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            DrawBackground();
            DrawRaceArea();
            DrawBettingSlots();
            DrawTopBar();
            DrawActiveOverlay();
        }

        GUI.matrix = previousMatrix;
    }

    public static Vector2 GetLogicalCanvasSize()
    {
        return new Vector2(640f, 480f);
    }

    private bool HasRecoveredOriginalArtwork()
    {
        return originalSceneTopStrips != null && originalSceneTopStrips.Length == 3 &&
               originalSceneTrackTiles != null && originalSceneTrackTiles.Length == 3 &&
               originalAvatarFrameStrip != null && originalPortraitStrip != null &&
               originalBetBoxStrip != null && originalTopDigits != null &&
               originalOddsGlyphs != null;
    }

    private void DrawRecoveredOriginalFrame()
    {
        int sceneIndex = GetActiveSceneIndex();
        float cameraLeft = ActiveCameraLeft;
        DrawRecoveredScene(sceneIndex, cameraLeft);

        DrawRecoveredHistoryPanelAnimated();

        float startX = WorldToScreenX(ActiveCourse.GetStartMarkerWorldX(), cameraLeft);
        if (PhotoReferencePreview) startX = PhotoReferenceFinish ? -200f : 486f;
        if (startX > -60f && startX < 700f)
        {
            DrawRecoveredTrackMarker(startX, false, false);
        }

        float finishX = WorldToScreenX(FinishWorldX, cameraLeft);
        if (PhotoReferencePreview) finishX = PhotoReferenceFinish ? 110f : -200f;
        if (finishX > -110f && finishX < 700f)
        {
            int winnerId = displayController.WinnerId;
            DrawRecoveredTrackMarker(finishX, true,
                animationController != null && AnimalCatalog.IsValidId(winnerId) &&
                animationController.GetWorldX(winnerId) <= RaceCourseGeometry.GetFinishContactWorldX(winnerId) + 0.001f);
        }

        if (sceneIndex == 1 && lilyPadPoolTexture != null)
        {
            Rect pond = ActiveCourse.PondWorldBounds;
            pond.x = WorldToScreenX(pond.x, cameraLeft);
            GUI.DrawTexture(pond, lilyPadPoolTexture, ScaleMode.StretchToFill, true);
        }
        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            int animalId = index + 1;
            DrawRecoveredObstacle(sceneIndex, animalId, cameraLeft);
            float animalWorldX = GetActiveWorldX(animalId);
            float racerCenterX = WorldToScreenX(animalWorldX, cameraLeft);
            if (!PhotoReferencePreview && animationController != null &&
                animationController.IsFinishSceneLocked && animalId == displayController.WinnerId)
            {
                // Carry the winner through the line while the original shutter
                // closes; the crossing event itself still uses the physical nose.
                racerCenterX -= Mathf.SmoothStep(0f, 40f,
                    Mathf.Clamp01(animationController.FinishSceneElapsedSeconds / 0.45f));
            }
            if (PhotoReferencePreview)
            {
                racerCenterX = PhotoReferenceFinish
                    ? (animalId == 4 ? 164f : RaceCourseLayout.GetFinishPackScreenX(animalId))
                    : RaceCourseLayout.StartScreenX + RaceCourseLayout.GetStartOffset(animalId);
            }
            Vector2 racerSize = GetRacerBoundsSize(animalId);
            DrawAnimal(
                animalId,
                new Rect(racerCenterX - racerSize.x * 0.5f,
                    GetLaneFootBaseline(animalId) - racerSize.y,
                    racerSize.x,
                    racerSize.y),
                animalWorldX,
                sceneIndex);
        }

        DrawRecoveredFinishShutter();

        DrawRecoveredTopBar();
        DrawRecoveredBettingSlots();
    }

    private void DrawRecoveredScene(int sceneIndex, float cameraLeft)
    {
        Texture2D topStrip = originalSceneTopStrips[sceneIndex];
        Texture2D trackTile = originalSceneTrackTiles[sceneIndex];
        float topOffset = Mathf.Repeat(cameraLeft / 640f, 1f);
        float trackOffset = Mathf.Repeat(cameraLeft / 256f, 1f);
        GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 640f, 120f), topStrip,
            new Rect(topOffset, 0f, 1f, 1f), true);
        GUI.DrawTextureWithTexCoords(new Rect(0f, 102f, 640f, 378f), trackTile,
            new Rect(trackOffset, 0f, 2.5f, 1f), true);
    }

    public static Rect GetSceneBottomExtensionBounds() { return new Rect(0f,470f,640f,10f); }

    private void DrawRecoveredTopBar()
    {
        int displayedWin = winCollectPresenter != null && winCollectPresenter.IsVisible
            ? winCollectPresenter.DisplayedWinPoints
            : displayController.WinPoints;
        int displayedCredit = winCollectPresenter != null && winCollectPresenter.IsVisible
            ? winCollectPresenter.DisplayedCredit
            : displayController.Balance;
        if (PhotoReferencePreview)
        {
            displayedWin = PhotoReferenceFinish ? 120 : 0;
            displayedCredit = PhotoReferenceFinish ? 570 : 690;
        }

        GUI.DrawTexture(new Rect(4f, 5f, 72f, 24f), originalWinLabel, ScaleMode.StretchToFill, true);
        DrawTopDigits(FormatScore(displayedWin), GetTopValueBounds(0));
        GUI.DrawTexture(new Rect(173f, 5f, 124f, 24f), originalBonusLabel, ScaleMode.StretchToFill, true);
        DrawTopDigits(PhotoReferencePreview ? (PhotoReferenceFinish ? "012000" : "011740") : displayController.BonusPoints.ToString("D6"), GetTopValueBounds(1));
        GUI.DrawTexture(new Rect(399f, 5f, 124f, 24f), originalCreditLabel, ScaleMode.StretchToFill, true);
        DrawTopDigits(FormatScore(displayedCredit), GetTopValueBounds(2));

        if (originalSmallPlaque != null && !PhotoReferenceFinish &&
            (displayController.CurrentPage == DisplayPage.Betting || displayController.CurrentPage == DisplayPage.Idle))
        {
            GUI.DrawTextureWithTexCoords(new Rect(4f, 41f, 68f, 24f), originalSmallPlaque,
                new Rect(0f, 0f, 0.5f, 1f), true);
            if (originalPlaqueDigits != null)
            {
                GUI.DrawTextureWithTexCoords(new Rect(22f, 44f, 16f, 18f), originalPlaqueDigits, new Rect(.1f,0,.1f,1),true);
                GUI.DrawTextureWithTexCoords(new Rect(43f, 44f, 16f, 18f), originalPlaqueDigits, new Rect(0,0,.1f,1),true);
            }
        }
    }

    public static string FormatScore(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static Rect GetTopValueBounds(int metric)
    {
        if (metric == 0) return new Rect(60f, 0f, 112f, 32f);
        if (metric == 1) return new Rect(271f, 0f, 126f, 32f);
        return new Rect(502f, 0f, 130f, 32f);
    }

    public static Rect GetTopDigitBounds(Rect area, int digitCount, int index)
    {
        float width = Mathf.Min(21f, area.width / Mathf.Max(1, digitCount));
        float height = Mathf.Min(32f, width * 32f / 21f);
        return new Rect(area.x + index * width, area.y + (area.height - height) * 0.5f, width, height);
    }

    private void DrawTopDigits(string value, Rect area)
    {
        if (scoreFont != null)
        {
            if (recoveredScoreStyle == null)
            {
                recoveredScoreStyle = new GUIStyle(GUI.skin.label);
                recoveredScoreStyle.font = scoreFont;
                recoveredScoreStyle.fontSize = 33;
                recoveredScoreStyle.padding = new RectOffset();
                recoveredScoreStyle.alignment = TextAnchor.MiddleCenter;
                recoveredScoreStyle.normal.textColor = Color.white;
            }
            Color previous = GUI.color;
            // The original HUD has plain red-to-yellow digits, unlike the outlined bet font.
            for (int index = 0; index < value.Length; index++)
            {
                Rect digit = GetTopDigitBounds(area, value.Length, index);
                recoveredScoreStyle.fontSize = Mathf.RoundToInt(33f * digit.height / 32f);
                for (int row = 0; row < 32; row++)
                {
                    GUI.color = Color.Lerp(new Color(1f,.12f,.08f),new Color(1f,.90f,.05f),Mathf.Clamp01((row-6f)/22f));
                    GUI.BeginGroup(new Rect(digit.x, digit.y + row, digit.width, 1f));
                    GUI.Label(new Rect(0,-row,digit.width,digit.height),value[index].ToString(),recoveredScoreStyle);
                    GUI.EndGroup();
                }
            }
            GUI.color = previous;
            return;
        }
        for (int index = 0; index < value.Length; index++)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 9) continue;
            GUI.DrawTextureWithTexCoords(
                GetTopDigitBounds(area, value.Length, index),
                originalTopDigits,
                new Rect(digit / 10f, 0f, 0.1f, 1f),
                true);
        }
    }

    private void DrawRecoveredBettingSlots()
    {
        int[] bets = displayController.Bets;
        int[] odds = displayController.Odds;
        const float slotWidth = 640f / 6f;
        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            int animalId = index + 1;
            float x = index * slotWidth;
            bool selected = animalId == displayController.SelectedId &&
                            displayController.CurrentPage == DisplayPage.Betting;
            if (PhotoReferencePreview) selected = false;
            Rect frame=GetRecoveredAvatarFrameBounds(index,selected,Time.unscaledTime);
            // Preserve the recovered ROM artwork exactly. The two source rows
            // already contain every white bulb, their intended colours and the
            // half-step positional offset that produces the rotation effect.
            // Point-filtered, uncompressed import keeps the 2x PC presentation
            // sharp without repainting or changing any source pixel.
            GUI.DrawTextureWithTexCoords(frame, originalAvatarFrameStrip,
                GetRecoveredAvatarFrameUv(index,selected,Time.unscaledTime), true);
            if(clippedBottomPortraits[index]==null)
            {
                Rect head=GetBottomPortraitBounds(index);
                Rect uv=originalPortraitStrip.width>1000?GetHighResolutionPortraitUv(animalId):GetRecoveredPortraitUv(animalId);
                clippedBottomPortraits[index]=ArcadeAvatarArtwork.CreateMaskedPortrait(originalPortraitStrip,uv,
                    new Rect((head.x-frame.x)/frame.width,(head.y-frame.y)/frame.height,
                        head.width/frame.width,head.height/frame.height),false);
            }
            GUI.DrawTexture(frame,clippedBottomPortraits[index]);
            int shownOdds = PhotoReferencePreview ? new[] { 24,15,10,6,3,3 }[index] : odds[index];
            DrawOddsText(index == 0 ? "24~40" : "X" + shownOdds, GetRecoveredOddsBounds(index, shownOdds));

            // The score box is deliberately drawn after the portrait so any
            // lowered neck or mane is cleanly hidden behind its upper edge.
            GUI.DrawTextureWithTexCoords(new Rect(x + 7f, 448f, slotWidth - 11f, 30f),
                originalBetBoxStrip, GetRecoveredBetBoxUv(selected), true);
            DrawBetDigits((PhotoReferencePreview ? (PhotoReferenceFinish ? 20 : 0) : bets[index]).ToString(), x + slotWidth * 0.5f + 1f, 448f);
        }
    }

    private void DrawRecoveredPortrait(int animalId, Rect rect)
    {
        Rect uv = originalPortraitStrip.width > 1000
            ? GetHighResolutionPortraitUv(animalId)
            : GetRecoveredPortraitUv(animalId);
        GUI.DrawTextureWithTexCoords(rect, originalPortraitStrip,
            uv, true);
    }

    public static Rect GetBottomPortraitBounds(int index)
    {
        const float slotWidth = 640f / 6f;
        int safeIndex = Mathf.Clamp(index, 0, AnimalCatalog.Count - 1);
        float[] inset = { 17f, 8f, 12f, 9f, 13f, 10f };
        float[] offset = { -2f, 0f, -5f, 2f, -10f, 0f };
        return new Rect(safeIndex * slotWidth + inset[safeIndex] + offset[safeIndex], BottomPortraitY[safeIndex],
            slotWidth - inset[safeIndex] * 2f, BottomPortraitHeight[safeIndex]);
    }

    public static Rect GetHighResolutionPortraitUv(int animalId)
    {
        return HighResolutionPortraitUv[Mathf.Clamp(animalId - 1, 0,
            HighResolutionPortraitUv.Length - 1)];
    }

    public static Rect GetRecoveredPortraitUv(int animalId)
    {
        return RecoveredPortraitUv[Mathf.Clamp(animalId - 1, 0,
            RecoveredPortraitUv.Length - 1)];
    }

    public static Rect GetRecoveredAvatarFrameUv(int index, bool selected, float time)
    {
        int safeIndex = Mathf.Clamp(index, 0, AnimalCatalog.Count - 1);
        bool useWhiteRow = IsRecoveredAvatarWhiteFrame(selected, time);
        float x = RecoveredAvatarFrameStartX[safeIndex] / 640f;
        float width = RecoveredAvatarFrameWidth[safeIndex] / 640f;
        // Both circles are 97 scanlines tall. The colored circle spans source
        // rows 6..102, crossing the nominal row boundary; rows 100..102 are
        // its lower tip, not part of the white frame. The white circle starts
        // at row 103. Cropping these exact ranges keeps both frames complete.
        float y = useWhiteRow ? 97f / 200f : 194f / 200f;
        float height = -97f / 200f;
        return new Rect(x, y, width, height);
    }

    public static Rect GetRecoveredAvatarFrameBounds(int index, bool selected, float time)
    {
        int safeIndex = Mathf.Clamp(index, 0, AnimalCatalog.Count - 1);
        // The source crops above now have identical painted bounds, so both
        // flash states use the same destination rectangle without bobbing.
        return new Rect(RecoveredAvatarFrameStartX[safeIndex], 380f,
            RecoveredAvatarFrameWidth[safeIndex], 97f);
    }

    private static bool IsRecoveredAvatarWhiteFrame(bool selected, float time)
    {
        return !selected || (Mathf.FloorToInt(time * 4f) & 1) == 0;
    }

    public static Rect GetRecoveredBetBoxUv(bool selected)
    {
        const float textureWidth = 384f;
        float segmentStart = selected ? 96f : 0f;
        float segmentWidth = 94f;
        return new Rect((segmentStart + 1.5f) / textureWidth, 0.5f / 32f,
            (segmentWidth - 3f) / textureWidth, 31f / 32f);
    }

    public static Rect GetRecoveredOddsBounds(int slotIndex, int odds)
    {
        const float slotWidth = 640f / 6f;
        float width = (slotIndex == 0 ? 5 : ("X" + odds).Length) * 14f;
        return new Rect((slotIndex + 1) * slotWidth - 5f - width, 423f, width, 24f);
    }

    private void DrawOddsText(string text, Rect bounds)
    {
        for (int index = 0; index < text.Length; index++)
        {
            int glyph = text[index] == 'X' || text[index] == '×'
                ? 0
                : (text[index] == '~' ? 11 : text[index] - '0' + 1);
            if (glyph < 0 || glyph > 11) continue;
            float glyphWidth = bounds.width / text.Length;
            GUI.DrawTextureWithTexCoords(new Rect(bounds.x + index * glyphWidth, bounds.y, glyphWidth, bounds.height),
                originalOddsGlyphs, new Rect(glyph / 12f, 0f, 1f / 12f, 1f), true);
        }
    }

    private void DrawBetDigits(string text, float centerX, float y)
    {
        float digitWidth = Mathf.Min(21f, 88f / Mathf.Max(1,text.Length));
        float startX = centerX - text.Length * digitWidth * 0.5f;
        for (int index = 0; index < text.Length; index++)
        {
            int digit = text[index] - '0';
            if (digit < 0 || digit > 9) continue;
            GUI.DrawTextureWithTexCoords(new Rect(startX + index * digitWidth, y, digitWidth, 32f),
                originalBetDigits != null ? originalBetDigits : originalTopDigits,
                originalBetDigits != null ? new Rect((digit + 1) / 11f,0f,1f/11f,1f) : new Rect(digit / 10f,0f,.1f,1f), true);
        }
    }

    private void DrawRecoveredHistoryPanelAnimated()
    {
        bool shouldShow = displayController.CurrentPage == DisplayPage.Idle ||
                          displayController.CurrentPage == DisplayPage.Betting;
        if (PhotoReferenceFinish) shouldShow = false;
        float now = Time.unscaledTime;
        float deltaTime = historyPanelLastUpdateAt < 0f
            ? 0f
            : Mathf.Clamp(now - historyPanelLastUpdateAt, 0f, 0.1f);
        historyPanelLastUpdateAt = now;
        historyPanelSlide01 = Mathf.MoveTowards(
            historyPanelSlide01,
            shouldShow ? 1f : 0f,
            deltaTime / HistoryPanelSlideSeconds);

        if (!shouldShow && historyPanelSlide01 <= 0f)
        {
            return;
        }

        DrawRecoveredHistoryPanel(GetHistoryPanelOffsetX(historyPanelSlide01));
    }

    private void DrawRecoveredHistoryPanel(float offsetX)
    {
        DrawColorRect(new Rect(10f + offsetX, 106f, 122f, 270f),
            new Color(0.10f, 0.20f, 0.30f));
        DrawColorRect(new Rect(13f + offsetX, 109f, 116f, 264f),
            new Color(0.95f, 0.97f, 0.98f, 1f));
        if (originalHistoryPanel != null)
            GUI.DrawTexture(new Rect(10f + offsetX,106f,122f,270f),originalHistoryPanel);
        int[] history = PhotoReferencePreview ? PhotoHistory : displayController.WinnerHistory;
        for (int index = 0; index < history.Length; index++)
        {
            int animalId = history[index];
            if (!AnimalCatalog.IsValidId(animalId))
            {
                continue;
            }

            Rect icon = GetHistoryIconBounds(index, offsetX);
            if(clippedHistoryPortraits[animalId-1]==null)
            {
                Rect uv=originalPortraitStrip.width>1000?GetHighResolutionPortraitUv(animalId):GetRecoveredPortraitUv(animalId);
                clippedHistoryPortraits[animalId-1]=ArcadeAvatarArtwork.CreateHistoryPortrait(originalPortraitStrip,uv);
            }
            GUI.DrawTexture(icon,clippedHistoryPortraits[animalId-1]);
        }
    }

    public static Rect GetHistoryIconBounds(int index)
    {
        return GetHistoryIconBounds(index, 0f);
    }

    public static Rect GetHistoryIconBounds(int index, float offsetX)
    {
        int safeIndex = Mathf.Clamp(index, 0, 9);
        int column = safeIndex / 5;
        int row = safeIndex % 5;
        // Exact transform of the 120x272 ROM panel into its 122x270 destination.
        const float sx = 122f / 120f;
        const float sy = 270f / 272f;
        return new Rect(10f + (8f + column * 57f) * sx + offsetX,
            106f + (4f + row * 54f) * sy, 47f * sx, 47f * sy);
    }

    public static float GetHistoryPanelOffsetX(float slide01)
    {
        float clamped = Mathf.Clamp01(slide01);
        float eased = 1f - Mathf.Pow(1f - clamped, 3f);
        return Mathf.Lerp(HistoryPanelHiddenOffsetX, 0f, eased);
    }

    public static Color GetHistoryFrameColor()
    {
        return new Color(0.46f, 0.46f, 0.44f, 1f);
    }

    private void DrawRecoveredTrackMarker(
        float markerX, bool finishMarker, bool winnerReachedFinish)
    {
        if (originalFinishRanks != null)
        {
            int blinkingLane = GetBlinkingTrackNumber(
                displayController.CurrentPage,
                PhotoReferencePreview ? 4 : displayController.SelectedId,
                displayController.WinnerId,
                finishMarker,
                winnerReachedFinish);
            for (int laneId = 1; laneId <= AnimalCatalog.Count; laneId++)
            {
                bool highlighted = finishMarker && winnerReachedFinish &&
                                   animationController != null &&
                                   animationController.IsFinishSceneLocked
                    ? ShouldHighlightFinishTrackNumber(
                        laneId,
                        displayController.WinnerId,
                        animationController.FinishSceneElapsedSeconds,
                        Time.unscaledTime)
                    : ShouldHighlightTrackNumber(
                        laneId, blinkingLane, Time.unscaledTime);
                DrawRecoveredTrackNumber(markerX, laneId, highlighted);
            }
        }
        if (originalFinishLine != null)
        {
            GUI.DrawTexture(new Rect(markerX, 103f, 8f, 280f),
                originalFinishLine, ScaleMode.StretchToFill, true);
        }
    }

    public static Rect GetTrackNumberBounds(float markerX, int laneId)
    {
        int safeLane = Mathf.Clamp(laneId, 1, AnimalCatalog.Count);
        return new Rect(markerX - 53f, 112f + (safeLane - 1) * 44.5f,
            48f, 36f);
    }

    private void DrawRecoveredTrackNumber(
        float markerX, int laneId, bool highlighted)
    {
        Rect target = GetTrackNumberBounds(markerX, laneId);
        // ROM glyphs are already rotated anticlockwise; draw the native crop once.
        GUI.DrawTextureWithTexCoords(target, originalFinishRanks,
            GetTrackNumberUv(laneId, highlighted),true);
    }

    public static float GetTrackNumberRotationDegrees()
    {
        return -90f;
    }

    public static Rect GetTrackNumberUv(int laneId, bool highlighted)
    {
        int safeLane = Mathf.Clamp(laneId, 1, AnimalCatalog.Count);
        int displayedNumber = AnimalCatalog.Count + 1 - safeLane;
        float x = highlighted ? 0.5f : 0f;
        float y = 1f - displayedNumber / (float)AnimalCatalog.Count;
        return new Rect(x, y, 0.5f, 1f / AnimalCatalog.Count);
    }

    public static bool ShouldHighlightTrackNumber(
        int laneId, int blinkingLaneId, float time)
    {
        return laneId == blinkingLaneId &&
               (Mathf.FloorToInt(time * 4f) & 1) != 0;
    }

    public static bool ShouldHighlightFinishTrackNumber(
        int laneId, int winnerId, float finishElapsedSeconds, float time)
    {
        if (laneId != winnerId || !AnimalCatalog.IsValidId(winnerId))
        {
            return false;
        }

        return finishElapsedSeconds < FinishRankFlashStartSeconds ||
               ShouldHighlightTrackNumber(laneId, winnerId, time);
    }

    public static float GetFinishShutterOpen01(float finishElapsedSeconds)
    {
        if (finishElapsedSeconds < FinishShutterCloseStartSeconds ||
            finishElapsedSeconds >= FinishShutterEndSeconds)
        {
            return 1f;
        }

        if (finishElapsedSeconds < FinishShutterClosedSeconds)
        {
            float close = Mathf.InverseLerp(
                FinishShutterCloseStartSeconds,
                FinishShutterClosedSeconds,
                finishElapsedSeconds);
            return Mathf.Lerp(1f, FinishShutterMinimumOpen,
                Mathf.SmoothStep(0f, 1f, close));
        }

        if (finishElapsedSeconds < FinishShutterOpenStartSeconds)
        {
            return FinishShutterMinimumOpen;
        }

        float open = Mathf.InverseLerp(
            FinishShutterOpenStartSeconds,
            FinishShutterEndSeconds,
            finishElapsedSeconds);
        return Mathf.Lerp(FinishShutterMinimumOpen, 1f,
            Mathf.SmoothStep(0f, 1f, open));
    }

    private void DrawRecoveredFinishShutter()
    {
        if (animationController == null || !animationController.IsFinishHoldActive ||
            displayController == null ||
            !AnimalCatalog.IsValidId(displayController.WinnerId))
        {
            return;
        }

        float open01 = GetFinishShutterOpen01(
            animationController.FinishHoldElapsedSeconds);
        if (open01 >= 0.999f)
        {
            return;
        }

        DrawHexagonalShutter(GetFinishShutterCenter(), open01);
    }

    public static Vector2 GetFinishShutterCenter()
    {
        return new Vector2(320f, 205f);
    }

    private void DrawHexagonalShutter(Vector2 center, float open01)
    {
        const float viewportTop = 0f;
        const float viewportBottom = 380f;
        const float rowStep = 2f;
        const float rowHeight = 2.35f;
        float halfWidth = 780f * Mathf.Clamp01(open01);
        float halfHeight = 500f * Mathf.Clamp01(open01);
        Color shutterColor = new Color(0.055f, 0.085f, 0.15f, 1f);

        if (halfWidth <= 0.001f || halfHeight <= 0.001f)
        {
            DrawColorRect(new Rect(0f, viewportTop, 640f,
                viewportBottom - viewportTop), shutterColor);
            return;
        }

        for (float y = viewportTop; y < viewportBottom; y += rowStep)
        {
            float sampleY = y + rowHeight * 0.5f;
            float distance = Mathf.Abs(sampleY - center.y);
            float apertureHalfWidth;
            if (distance >= halfHeight)
            {
                apertureHalfWidth = 0f;
            }
            else if (distance <= halfHeight * 0.5f)
            {
                apertureHalfWidth = halfWidth;
            }
            else
            {
                apertureHalfWidth = halfWidth *
                    (2f - 2f * distance / halfHeight);
            }

            float left = Mathf.Clamp(center.x - apertureHalfWidth, 0f, 640f);
            float right = Mathf.Clamp(center.x + apertureHalfWidth, 0f, 640f);
            if (left > 0f)
            {
                DrawColorRect(new Rect(0f, y, left, rowHeight), shutterColor);
            }
            if (right < 640f)
            {
                DrawColorRect(new Rect(right, y, 640f - right, rowHeight),
                    shutterColor);
            }
        }
    }

    public static int GetBlinkingTrackNumber(
        DisplayPage page,
        int selectedId,
        int winnerId,
        bool finishMarker,
        bool winnerReachedFinish)
    {
        if (!finishMarker && page == DisplayPage.Betting)
        {
            return AnimalCatalog.IsValidId(selectedId) ? selectedId : 0;
        }

        if (finishMarker && winnerReachedFinish &&
            (page == DisplayPage.Race ||
             page == DisplayPage.Result ||
             page == DisplayPage.WinReady ||
             page == DisplayPage.WinCollecting))
        {
            return AnimalCatalog.IsValidId(winnerId) ? winnerId : 0;
        }

        return 0;
    }

    private void DrawRecoveredObstacle(int sceneIndex, int animalId, float cameraLeft)
    {
        if (sceneIndex == 0 && hayWallTexture != null)
        {
            float barrelX = animationController != null && animationController.HasObstacleCourse
                ? animationController.GetObstaclePose(animalId).BarrelWorldX
                : RaceObstacleTimeline.GetInitialBarrelWorldX(animalId, 0U, 0U);
            Rect barrel = RaceObstacleTimeline.GetBarrelWorldBounds(animalId, barrelX);
            barrel.x = WorldToScreenX(barrel.x, cameraLeft);
            GUI.DrawTexture(barrel, hayWallTexture, ScaleMode.StretchToFill, true);
            return;
        }
        if (sceneIndex == 2 && beachBallStackTexture != null)
        {
            RaceObstaclePose pose = animationController != null && animationController.HasObstacleCourse
                ? animationController.GetObstaclePose(animalId)
                : new RaceObstaclePose { BallWorldX = RaceObstacleTimeline.GetInitialBallWorldX(animalId) };
            Rect uv = new Rect(pose.BallFrame * 0.5f, 0f, 0.5f, 1f);
            Rect ball = RaceObstacleTimeline.GetBallWorldBounds(animalId, pose.BallWorldX);
            ball.x = WorldToScreenX(ball.x, cameraLeft);
            GUI.DrawTextureWithTexCoords(ball, beachBallStackTexture, uv, true);
        }
    }

    private void DrawBackground()
    {
        DrawColorRect(new Rect(0f, 0f, 1280f, 720f), new Color(0.82f, 0.88f, 0.90f));
    }

    private void DrawTopBar()
    {
        int displayedWin = winCollectPresenter != null && winCollectPresenter.IsVisible
            ? winCollectPresenter.DisplayedWinPoints
            : displayController.WinPoints;
        int displayedCredit = winCollectPresenter != null && winCollectPresenter.IsVisible
            ? winCollectPresenter.DisplayedCredit
            : displayController.Balance;

        DrawMetric(24f, 151f, "WIN", displayedWin.ToString(),
            new Color(1f, 0.90f, 0.78f), new Color(1f, 0.78f, 0.18f));
        DrawMetric(410f, 613f, "BONUS", displayController.BonusPoints.ToString("D6"),
            new Color(1f, 0.76f, 0.84f), new Color(1f, 0.82f, 0.18f));
        DrawMetric(994f, 1144f, "CREDIT", displayedCredit.ToString(),
            new Color(1f, 0.83f, 0.80f), new Color(1f, 0.90f, 0.35f));
    }

    private void DrawMetric(float labelX, float valueX, string label, string value,
        Color labelColor, Color valueColor)
    {
        Color previousLabel = topLabelStyle.normal.textColor;
        topLabelStyle.normal.textColor = labelColor;
        DrawOutlinedLabel(new Rect(labelX, 10f, 210f, 52f), label, topLabelStyle,
            new Color(0.34f, 0.05f, 0.03f), 3f);
        topLabelStyle.normal.textColor = previousLabel;

        Color previous = topValueStyle.normal.textColor;
        topValueStyle.normal.textColor = valueColor;
        DrawOutlinedLabel(new Rect(valueX, 8f, 245f, 54f), value, topValueStyle,
            new Color(0.16f, 0.08f, 0.02f), 3f);
        topValueStyle.normal.textColor = previous;
    }

    private void DrawRaceArea()
    {
        const float top = 0f;
        const float laneHeight = 90f;
        float raceProgress = animationController == null ? 0f : animationController.Progress01;
        int sceneIndex = GetActiveSceneIndex();

        float cameraLeft = ActiveCameraLeft;

        Texture2D background = GetSceneTexture(sceneIndex);
        if (background != null)
        {
            const float usableUStart = 0.012f;
            const float usableUWidth = 0.976f;
            // The settings screen changes the world length. Keep the scenery UV
            // window mapped to that active course instead of the 15-second default.
            float activeWorldMax = ActiveCourse.StartWorldX + 200f;
            float activeWorldWidth = activeWorldMax - WorldMinX;
            float viewportUWidth = usableUWidth * (ViewportWidth / activeWorldWidth);
            float camera01 = Mathf.InverseLerp(WorldMinX, activeWorldMax - ViewportWidth, cameraLeft);
            float viewportUStart = usableUStart +
                                   camera01 * (usableUWidth - viewportUWidth);
            GUI.DrawTextureWithTexCoords(
                new Rect(0f, -100f, ViewportWidth, 680f),
                background,
                new Rect(viewportUStart, 0.02f, viewportUWidth, 0.94f),
                true);
            DrawBottomSceneExtension(sceneIndex);
        }

        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            int animalId = index + 1;
            if (background == null)
            {
                float laneTop = index == 0 ? 250f : GetLaneFootBaseline(index);
                float laneBottom = GetLaneFootBaseline(animalId);
                Color laneColor = index % 2 == 0
                    ? new Color(0.86f, 0.90f, 0.91f)
                    : new Color(0.75f, 0.82f, 0.84f);
                DrawColorRect(new Rect(0f, laneTop, 1280f, laneBottom - laneTop), laneColor);
            }
        }

        if (leftIconPanelTexture != null &&
            (displayController.CurrentPage == DisplayPage.Idle ||
             displayController.CurrentPage == DisplayPage.Betting))
        {
            DrawHistoryPanel(new Rect(22f, 178f, 250f, 382f));
        }

        if (background == null)
        {
            DrawScrollingScenery(top, laneHeight * AnimalCatalog.Count, raceProgress);
        }

        float startX = WorldToScreenX(ActiveCourse.GetStartMarkerWorldX(), cameraLeft);
        if (startX > -80f && startX < 1280f)
        {
            DrawStartLine(startX, top, laneHeight * AnimalCatalog.Count);
        }

        float finishX = WorldToScreenX(FinishWorldX, cameraLeft);
        if (finishX > -80f && finishX < 1280f)
        {
            if (finishRankNumbersTexture != null)
            {
                GUI.DrawTexture(new Rect(finishX - 82f, top + 55f, 112f, 470f),
                    finishRankNumbersTexture, ScaleMode.StretchToFill, true);
            }
            else
            {
                DrawFinishLine(finishX, top, laneHeight * AnimalCatalog.Count);
            }
        }

        DrawLaneObstacles(
            sceneIndex,
            WorldToScreenX(ActiveCourse.ObstacleWorldX, cameraLeft));

        // Top lane is farthest away. Drawing downward makes lane 6 the foreground.
        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            int animalId = index + 1;
            float animalWorldX = GetActiveWorldX(animalId);
            float racerCenterX = WorldToScreenX(animalWorldX, cameraLeft);
            Vector2 racerSize = GetRacerBoundsSize(animalId);
            float width = racerSize.x;
            float height = racerSize.y;
            float baselineY = GetLaneFootBaseline(animalId);
            DrawAnimal(
                animalId,
                new Rect(racerCenterX - width * 0.5f, baselineY - height, width, height),
                animalWorldX,
                sceneIndex);
        }

    }

    public int GetActiveSceneIndex()
    {
        if (animationController != null && animationController.HasObstacleCourse &&
            (animationController.IsRunning || animationController.IsFinishSceneLocked))
            return animationController.ActiveCourseScene;
        if (ForcedSceneIndex >= 0)
        {
            return Mathf.Clamp(ForcedSceneIndex, 0, 2);
        }

        if (displayController == null)
        {
            return 0;
        }

        return GetSceneIndexForState(
            displayController.CurrentRound,
            displayController.CurrentPage,
            displayController.ResumePhase);
    }

    public static int GetSceneIndexForState(
        uint round,
        DisplayPage page,
        DisplayPhase resumePhase)
    {
        bool currentRoundScene =
            page == DisplayPage.Race ||
            page == DisplayPage.Result ||
            page == DisplayPage.WinReady ||
            page == DisplayPage.WinCollecting;

        if (page == DisplayPage.Setting)
        {
            currentRoundScene =
                resumePhase == DisplayPhase.RacePrepare ||
                resumePhase == DisplayPhase.WaitRaceOver ||
                resumePhase == DisplayPhase.ResultPrepare ||
                resumePhase == DisplayPhase.WaitResultOver ||
                resumePhase == DisplayPhase.WinWaitStart ||
                resumePhase == DisplayPhase.WinCollecting;
        }

        if (currentRoundScene && round > 0U)
        {
            return (int)((round - 1U) % 3U);
        }

        return (int)(round % 3U);
    }

    public static string GetSceneName(int sceneIndex)
    {
        switch (sceneIndex)
        {
            case 1: return "丛林";
            case 2: return "城堡";
            default: return "沙漠";
        }
    }

    private Texture2D GetSceneTexture(int sceneIndex)
    {
        return sceneBackgrounds != null && sceneIndex >= 0 && sceneIndex < sceneBackgrounds.Length
            ? sceneBackgrounds[sceneIndex]
            : null;
    }

    private void DrawHistoryPanel(Rect rect)
    {
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.94f);
        GUI.DrawTexture(rect, leftIconPanelTexture, ScaleMode.StretchToFill, true);
        GUI.color = previous;

        int[] history = { 4, 5, 4, 2, 5, 5, 2, 6, 5, 6 };
        const float iconWidth = 88f;
        const float iconHeight = 64f;
        for (int index = 0; index < history.Length; index++)
        {
            int column = index % 2;
            int row = index / 2;
            Rect icon = new Rect(
                rect.x + 30f + column * 108f,
                rect.y + 17f + row * 70f,
                iconWidth,
                iconHeight);
            DrawPortraitContent(history[index], icon);
        }
    }

    private void DrawLaneObstacles(int sceneIndex, float screenX)
    {
        float cameraLeft = ObstacleWorldX - screenX;
        if (sceneIndex == 1 && lilyPadPoolTexture != null)
        {
            Rect pond = RaceObstacleTimeline.PondWorldBounds;
            pond.x = WorldToScreenX(pond.x, cameraLeft);
            GUI.DrawTexture(pond, lilyPadPoolTexture, ScaleMode.StretchToFill, true);
            return;
        }
        for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
            DrawRecoveredObstacle(sceneIndex, animalId, cameraLeft);
    }

    private static void DrawTextureSlice(Texture2D texture, Rect rect, Rect uv)
    {
        if (texture != null)
        {
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        }
    }

    public static int GetObstaclePhase(float animalWorldX)
    {
        const float contactDistance = 72.5f;
        if (animalWorldX > ObstacleWorldX + contactDistance)
        {
            return 0;
        }

        return animalWorldX < ObstacleWorldX - contactDistance ? 2 : 1;
    }

    public static float RacePositionToScreenX(float position)
    {
        return Mathf.Lerp(515.5f, 64f, Mathf.Clamp01(position));
    }

    public static float RacePositionToWorldX(float position)
    {
        return RaceCourseGeometry.ToWorldX(position);
    }

    public static float GetLaneFootBaseline(int animalId)
    {
        return RaceCourseGeometry.GetLaneFootBaseline(animalId);
    }

    public static float GetLaneLowerEdgeY(int animalId)
    {
        return RaceCourseGeometry.GetLaneLowerEdgeY(animalId);
    }

    public static Vector2 GetRacerBoundsSize(int animalId)
    {
        switch (animalId)
        {
            case 1: return new Vector2(220f, 141f);
            case 2: return new Vector2(220f, 124f);
            case 3: return new Vector2(190f, 138f);
            case 4: return new Vector2(220f, 159f);
            case 5: return new Vector2(220f, 217f);
            default: return new Vector2(235f, 158f);
        }
    }

    public static float GetRacerHorizontalScale(int animalId)
    {
        return 1f;
    }

    public static float GetRacerBottomPadding(int animalId)
    {
        int index = Mathf.Clamp(animalId - 1, 0, RacerBottomPadding.Length - 1);
        return RacerBottomPadding[index];
    }

    public static float GetCameraLeft(float raceProgress, float trackedPosition)
    {
        return RaceCourseLayout.Default.GetCameraLeft(Mathf.Clamp01(raceProgress) * RaceCourseLayout.BaseSeconds);
    }

    private RaceCourseLayout ActiveCourse
    {
        get { return animationController == null ? RaceCourseLayout.Default : animationController.Course; }
    }

    private float ActiveCameraLeft
    {
        get { return animationController == null ? ActiveCourse.InitialCameraLeft : animationController.CameraLeft; }
    }

    private float GetActiveWorldX(int animalId)
    {
        return animationController == null ? ActiveCourse.StartWorldX + RaceCourseLayout.GetStartOffset(animalId)
            : animationController.GetWorldX(animalId);
    }

    public static float WorldToScreenX(float worldX, float cameraLeft)
    {
        return worldX - cameraLeft;
    }

    public static float GetObstacleScreenX(float raceProgress, float averagePosition)
    {
        return WorldToScreenX(ObstacleWorldX, GetCameraLeft(raceProgress, averagePosition));
    }

    public static bool IsFinishVisible(float raceProgress)
    {
        return raceProgress >= 0.55f;
    }

    public static float GetStartLineX(float raceProgress)
    {
        return WorldToScreenX(StartMarkerWorldX,
            GetCameraLeft(raceProgress, raceProgress));
    }

    public static float GetFinishLineX(float raceProgress)
    {
        return WorldToScreenX(FinishWorldX, GetCameraLeft(raceProgress, raceProgress));
    }

    public static float GetFinishContactPosition01(int animalId)
    {
        return RaceCourseGeometry.ToPosition01(RaceCourseGeometry.GetFinishContactWorldX(animalId));
    }

    public static float GetRacerVisibleLeadingEdgeOffset(int animalId)
    {
        return RaceCourseGeometry.GetLeadingEdgeOffset(animalId);
    }

    public static bool HasWinnerReachedFinish(int animalId, float winnerPosition)
    {
        return winnerPosition >= GetFinishContactPosition01(animalId);
    }

    public static bool HasWinnerReachedFinish(float winnerPosition)
    {
        return HasWinnerReachedFinish(1, winnerPosition);
    }

    private void DrawScrollingScenery(float top, float height, float raceProgress)
    {
        if (raceProgress <= 0f)
        {
            return;
        }

        float worldOffset = raceProgress * 1850f;
        for (int index = 0; index < 7; index++)
        {
            float x = Mathf.Repeat(index * 240f + worldOffset, 1680f) - 160f;
            DrawColorRect(new Rect(x, top + 5f, 5f, height - 10f), new Color(0.34f, 0.46f, 0.49f, 0.22f));
            DrawColorRect(new Rect(x + 18f, top + 20f, 3f, height - 40f), new Color(1f, 1f, 1f, 0.35f));
        }

        for (int index = 0; index < 5; index++)
        {
            float x = Mathf.Repeat(index * 360f + worldOffset * 0.72f, 1900f) - 220f;
            Color old = GUI.color;
            GUI.color = new Color(0.13f, 0.45f, 0.48f, 0.26f);
            GUI.DrawTexture(new Rect(x, top + 12f, 70f, 34f), circleTexture);
            GUI.color = old;
        }
    }

    private int GetRunningSpriteFrame(int animalId, float frameRate, int frameCount)
    {
        float frameClock = Time.unscaledTime * frameRate;
        if (animationController != null && animationController.IsRunning && animationController.Presentation != null)
            frameClock = animationController.GetObstaclePose(animalId).RunDistance * frameRate / 250f;
        return Mathf.FloorToInt(frameClock + animalId * 0.63f) % frameCount;
    }

    private void DrawAnimal(int animalId, Rect rect, float worldX, int sceneIndex)
    {
        DisplayPage page = displayController == null
            ? DisplayPage.Idle
            : displayController.CurrentPage;
        int selectedId = displayController == null ? 0 : displayController.SelectedId;
        bool finishSceneLocked = animationController != null &&
                                 animationController.IsFinishSceneLocked;
        int winnerId = displayController == null ? 0 : displayController.WinnerId;
        // Photo fixtures change state/positions only; they share the production drawing path.
        if (PhotoReferencePreview)
        {
            page = PhotoReferenceFinish ? DisplayPage.WinReady : DisplayPage.Betting;
            selectedId = 4;
            finishSceneLocked = PhotoReferenceFinish;
            winnerId = 4;
        }
        bool freezeRunFrame = IsFinishScenePage(page) && finishSceneLocked &&
                              animalId != winnerId;
        bool shouldRun = ShouldAnimateAnimal(
            page, selectedId, animalId, finishSceneLocked, winnerId);
        if (animationController != null && animationController.HasObstacleCourse && !finishSceneLocked)
        {
            RaceObstaclePose pose = animationController.GetObstaclePose(animalId);
            Texture2D actions = GetAnimalTexture(animalObstacleSheets, animalId);
            Texture2D run = GetAnimalTexture(animalRunSheets, animalId);
            if (pose.SourceFrame >= 5 && actions != null && run != null)
            {
                float scale = Mathf.Min(rect.width / (run.width / 4f), rect.height / run.height);
                float cellWidth = actions.width / 2f;
                float cellHeight = actions.height / 3f;
                Rect target = new Rect(rect.center.x - cellWidth * scale * 0.5f,
                    pose.Foot.y - (cellHeight - RecoveredFramePadding) * scale,
                    cellWidth * scale, cellHeight * scale);
                int frame = pose.SourceFrame - 5;
                GUI.DrawTextureWithTexCoords(target, actions,
                    new Rect((frame % 2) * 0.5f, (2 - frame / 2) / 3f, 0.5f, 1f / 3f), true);
                return;
            }
        }
        if (shouldRun || freezeRunFrame)
        {
            float frameRate = GetAnimalAnimationFrameRate(
                page, selectedId, animalId, finishSceneLocked, winnerId);
            int runFrame = freezeRunFrame
                ? GetFrozenFinishFrame(animalId)
                : GetRunningSpriteFrame(animalId, frameRate, 4);
            Texture2D frameTexture = GetFlatAnimalFrame(animalRunFrames, animalId, runFrame, 4);
            if (frameTexture != null)
            {
                DrawAnimalFrame(frameTexture, 0, 1, rect, true,
                    GetRacerBottomPadding(animalId), GetRacerHorizontalScale(animalId));
                return;
            }
        }

        Texture2D runSheet = GetAnimalTexture(animalRunSheets, animalId);
        Texture2D idleSheet = GetAnimalTexture(animalIdleSheets, animalId);
        if (runSheet != null || idleSheet != null)
        {
            Texture2D texture = (shouldRun || freezeRunFrame) && runSheet != null
                ? runSheet
                : (idleSheet != null ? idleSheet : runSheet);
            int frameCount = texture == runSheet ? 4 : 1;
            // Idle and running ROM cells have different transparent extents.
            // Calibrate their painted silhouettes, not the outer padded cells.
            if (texture == idleSheet)
            {
                if (animalId == 1) rect.height = 126f;
                if (animalId == 5) rect.height = 203f;
            }
            int frame = freezeRunFrame
                ? GetFrozenFinishFrame(animalId) % frameCount
                : (shouldRun
                    ? GetRunningSpriteFrame(animalId,
                        GetAnimalAnimationFrameRate(
                            page, selectedId, animalId, finishSceneLocked, winnerId), frameCount)
                    : Mathf.FloorToInt(Time.unscaledTime * 1.8f + animalId) % frameCount);

            if (HasRecoveredOriginalArtwork())
                DrawRecoveredRacerFrame(texture, frame, frameCount, rect, animalId);
            else
                DrawAnimalFrame(texture, frame, frameCount, rect, true,
                    GetRacerBottomPadding(animalId), GetRacerHorizontalScale(animalId));
            return;
        }

        if (animalRunSheet != null)
        {
            int frame = shouldRun
                ? Mathf.FloorToInt(Time.unscaledTime *
                    GetAnimalAnimationFrameRate(
                        page, selectedId, animalId, finishSceneLocked, winnerId) + animalId) % 4
                : 0;
            DrawAnimalSprite(animalId, frame, rect);
            return;
        }

        Color animalColor = AnimalColors[animalId - 1];
        Color oldColor = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.20f);
        GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 39f, 66f, 12f), circleTexture);

        GUI.color = animalColor;
        GUI.DrawTexture(new Rect(rect.x + 15f, rect.y + 15f, 58f, 32f), circleTexture);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 8f, 27f, 27f), circleTexture);

        GUI.color = new Color(0.07f, 0.09f, 0.10f);
        GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 16f, 5f, 5f), circleTexture);
        GUI.color = oldColor;

        GUI.Label(new Rect(rect.x + 23f, rect.y + 13f, 45f, 30f), animalId.ToString(), animalLabelStyle);
    }

    public static bool ShouldAnimateAnimal(DisplayPage page, int selectedId, int animalId)
    {
        return ShouldAnimateAnimal(page, selectedId, animalId, false, 0);
    }

    public static float GetAnimalAnimationFrameRate(DisplayPage page, int selectedId, int animalId)
    {
        return GetAnimalAnimationFrameRate(page, selectedId, animalId, false, 0);
    }

    public static float GetAnimalAnimationFrameRate(
        DisplayPage page,
        int selectedId,
        int animalId,
        bool finishSceneLocked,
        int winnerId)
    {
        int safeIndex = Mathf.Clamp(animalId - 1, 0, RunFramesPerSecond.Length - 1);
        float frameRate = RunFramesPerSecond[safeIndex];
        if (page == DisplayPage.Betting && selectedId == animalId)
            frameRate *= SelectedRunSpeedScale;
        else if (finishSceneLocked && animalId == winnerId && IsFinishScenePage(page))
            frameRate *= WinnerRunSpeedScale;
        return frameRate;
    }

    public static bool ShouldAnimateAnimal(
        DisplayPage page,
        int selectedId,
        int animalId,
        bool finishSceneLocked,
        int winnerId)
    {
        return (page == DisplayPage.Race && !finishSceneLocked) ||
               (IsFinishScenePage(page) && finishSceneLocked &&
                animalId == winnerId) ||
               (page == DisplayPage.Betting && selectedId == animalId);
    }

    public static bool IsFinishScenePage(DisplayPage page)
    {
        return page == DisplayPage.Race ||
               page == DisplayPage.Result ||
               page == DisplayPage.WinReady ||
               page == DisplayPage.WinCollecting;
    }

    public static bool ShouldSuppressPostRaceOverlay(
        DisplayPage page, bool finishSceneLocked)
    {
        return finishSceneLocked &&
               (page == DisplayPage.Result ||
                page == DisplayPage.WinReady ||
                page == DisplayPage.WinCollecting);
    }

    public static int GetFrozenFinishFrame(int animalId)
    {
        // The non-winning poses in all four rectified finish photographs.
        switch (animalId)
        {
            case 1: return 3;
            case 2: return 2;
            case 3: return 0;
            case 5: return 1;
            case 6: return 2;
            default: return 0;
        }
    }

    private static Texture2D GetAnimalTexture(Texture2D[] textures, int animalId)
    {
        int index = animalId - 1;
        return textures != null && index >= 0 && index < textures.Length
            ? textures[index]
            : null;
    }

    private static Texture2D GetFlatAnimalFrame(Texture2D[] textures, int animalId, int frame, int framesPerAnimal)
    {
        int index = (animalId - 1) * framesPerAnimal + frame;
        return textures != null && index >= 0 && index < textures.Length
            ? textures[index]
            : null;
    }

    public static Rect GetGroundedRacerBounds(Rect bounds, float frameWidth, float frameHeight,
        float paintedBottomY)
    {
        float scale = Mathf.Min(bounds.width / frameWidth, bounds.height / frameHeight);
        float width = frameWidth * scale;
        return new Rect(bounds.center.x - width * 0.5f,
            paintedBottomY - (frameHeight - RecoveredFramePadding) * scale,
            width, frameHeight * scale);
    }

    public static Rect GetRacerGroundShadowBounds(Rect bounds, int animalId)
    {
        float width = bounds.width * 0.56f;
        return new Rect(bounds.center.x - width * 0.5f,
            GetLaneLowerEdgeY(animalId) - 10f, width, 10f);
    }

    private void DrawRecoveredRacerFrame(Texture2D texture, int frame, int frameCount, Rect bounds,
        int animalId)
    {
        float ground = GetLaneLowerEdgeY(animalId);
        bool airbornePose = frameCount == 4 && frame == 3;
        // Most recovered frames already contain their original shadow. Only the
        // airborne running frame needs one; its feet stay above that shadow.
        if (airbornePose)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(GetRacerGroundShadowBounds(bounds, animalId), circleTexture);
            GUI.color = previous;
        }
        Rect target = GetGroundedRacerBounds(bounds, texture.width / (float)frameCount,
            texture.height, ground - (airbornePose ? 7f : 0f));
        GUI.DrawTextureWithTexCoords(target, texture,
            new Rect(frame / (float)frameCount, 0f, 1f / frameCount, 1f), true);
    }

    private void DrawAnimalFrame(
        Texture2D texture,
        int frame,
        int frameCount,
        Rect rect,
        bool shadow,
        float bottomPadding01 = 0f,
        float horizontalScale = 1f)
    {
        if (texture == null || frameCount <= 0)
        {
            return;
        }

        if (shadow)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            float shadowWidth = rect.width * 0.56f;
            GUI.DrawTexture(new Rect(
                rect.center.x - shadowWidth * 0.5f,
                rect.y + rect.height - 11f,
                shadowWidth,
                10f), circleTexture);
            GUI.color = previous;
        }

        float frameAspect = (texture.width / (float)frameCount) / texture.height;
        frameAspect *= Mathf.Max(0.1f, horizontalScale);
        Rect fitted = FitRect(rect, frameAspect);
        fitted.y += fitted.height * Mathf.Clamp01(bottomPadding01);
        var uv = new Rect(Mathf.Clamp(frame, 0, frameCount - 1) / (float)frameCount, 0f,
            1f / frameCount, 1f);
        GUI.DrawTextureWithTexCoords(fitted, texture, uv, true);
    }

    private static Rect FitRect(Rect bounds, float aspect)
    {
        if (aspect <= 0f)
        {
            return bounds;
        }

        float boundsAspect = bounds.width / bounds.height;
        if (aspect > boundsAspect)
        {
            float height = bounds.width / aspect;
            return new Rect(bounds.x, bounds.y + (bounds.height - height) * 0.5f, bounds.width, height);
        }

        float width = bounds.height * aspect;
        return new Rect(bounds.x + (bounds.width - width) * 0.5f, bounds.y, width, bounds.height);
    }

    private void DrawAnimalSprite(int animalId, int frame, Rect rect)
    {
        int row = Mathf.Clamp(animalId - 1, 0, AnimalCatalog.Count - 1);
        float cellWidth = 0.25f;
        float cellHeight = 1f / AnimalCatalog.Count;
        var textureCoordinates = new Rect(
            Mathf.Clamp(frame, 0, 3) * cellWidth,
            1f - (row + 1) * cellHeight,
            cellWidth,
            cellHeight);

        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.18f);
        GUI.DrawTexture(new Rect(rect.x + 11f, rect.y + rect.height - 12f, rect.width - 18f, 11f), circleTexture);
        GUI.color = old;
        GUI.DrawTextureWithTexCoords(rect, animalRunSheet, textureCoordinates, true);
    }

    private void DrawStartLine(float x, float y, float height)
    {
        DrawColorRect(new Rect(x, y, 4f, height), new Color(0.14f, 0.48f, 0.55f));
        GUI.Label(new Rect(x - 32f, y + 3f, 70f, 24f), "起点", slotTitleStyle);
    }

    private void DrawFinishLine(float x, float y, float height)
    {
        const float cell = 10f;
        int rows = Mathf.CeilToInt(height / cell);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                Color color = (row + column) % 2 == 0 ? Color.white : new Color(0.08f, 0.09f, 0.1f);
                DrawColorRect(new Rect(x + column * cell, y + row * cell, cell, cell), color);
            }
        }

        GUI.Label(new Rect(x - 15f, y + 3f, 70f, 24f), "终点", slotTitleStyle);
    }

    private void DrawBettingSlots()
    {
        int[] bets = displayController.Bets;
        int[] odds = displayController.Odds;
        const float gap = 10f;
        const float slotWidth = 200f;

        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            int animalId = index + 1;
            float x = 15f + index * (slotWidth + gap);
            Rect frameRect = new Rect(x, 555f, slotWidth, 165f);

            Color previous = GUI.color;
            if (singleAvatarFrameTexture != null)
            {
                GUI.color = Color.Lerp(PortraitBorderColors[index], Color.white, 0.18f);
                GUI.DrawTexture(frameRect, singleAvatarFrameTexture, ScaleMode.StretchToFill, true);
            }
            GUI.color = previous;

            if (singleAvatarFrameTexture == null)
            {
                GUI.Box(frameRect, GUIContent.none, panelStyle);
            }

            // The cabinet portrait intentionally overlaps the inner arch.
            DrawPortraitContent(animalId, new Rect(x + 14f, 548f, 172f, 118f));

            if (animalId == displayController.SelectedId &&
                displayController.CurrentPage == DisplayPage.Betting)
            {
                Color selected = new Color(1f, 0.88f, 0.08f, 0.96f);
                DrawColorRect(new Rect(x + 4f, 556f, slotWidth - 8f, 4f), selected);
                DrawColorRect(new Rect(x + 4f, 715f, slotWidth - 8f, 4f), selected);
            }

            DrawOutlinedLabel(new Rect(x + 101f, 624f, 92f, 38f), "×" + odds[index],
                slotOddsStyle, new Color(0.45f, 0.12f, 0.28f), 2f);
            DrawOutlinedLabel(new Rect(x + 12f, 672f, 176f, 45f), bets[index].ToString(),
                slotBetStyle, new Color(0.20f, 0.10f, 0.02f), 2f);
        }
    }

    private void DrawPortraitContent(int animalId, Rect rect)
    {
        Texture2D portrait = GetAnimalTexture(animalPortraits, animalId);
        if (portrait == null)
        {
            DrawAnimalSprite(animalId, 0, rect);
            return;
        }

        Rect uv = GetPortraitUv(animalId);
        float aspect = (portrait.width * uv.width) / (portrait.height * uv.height);
        Rect fitted = FitRect(rect, aspect);
        GUI.DrawTextureWithTexCoords(fitted, portrait, uv, true);
    }

    private void DrawBottomSceneExtension(int sceneIndex)
    {
        Color floor;
        switch (sceneIndex)
        {
            case 1:
                floor = new Color(0.66f, 0.86f, 0.50f);
                break;
            case 2:
                floor = new Color(0.88f, 0.90f, 0.97f);
                break;
            default:
                floor = new Color(0.96f, 0.84f, 0.53f);
                break;
        }

        DrawColorRect(new Rect(0f, 555f, 1280f, 165f), floor);
        DrawColorRect(new Rect(0f, 555f, 1280f, 7f), new Color(1f, 1f, 1f, 0.34f));
    }

    public static Rect GetPortraitUv(int animalId)
    {
        int index = Mathf.Clamp(animalId - 1, 0, PortraitUv.Length - 1);
        return PortraitUv[index];
    }

    private void DrawPortrait(int animalId, Rect rect)
    {
        int index = Mathf.Clamp(animalId - 1, 0, AnimalCatalog.Count - 1);
        Texture2D portrait = GetAnimalTexture(animalPortraits, animalId);
        if (portrait == null)
        {
            DrawAnimalSprite(animalId, 0, rect);
            return;
        }

        Color previous = GUI.color;
        if (singleAvatarFrameTexture != null)
        {
            GUI.DrawTexture(rect, singleAvatarFrameTexture, ScaleMode.StretchToFill, true);
        }
        else
        {
            GUI.color = PortraitBorderColors[index];
            GUI.DrawTexture(rect, circleTexture);
            GUI.color = new Color(0.93f, 0.97f, 0.98f);
            GUI.DrawTexture(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), circleTexture);
        }
        GUI.color = previous;
        DrawAnimalFrame(portrait, 0, 1,
            new Rect(rect.x + 13f, rect.y + 8f, rect.width - 26f, rect.height - 20f), false);
    }

    private void DrawStateHint()
    {
        string hint = string.Empty;
        if (displayController.CurrentPage == DisplayPage.Idle)
        {
            hint = "等待投币或开始操作";
        }
        else if (displayController.CurrentPage == DisplayPage.Betting)
        {
            hint = "当前选择：" + displayController.SelectedId + "号 " +
                   AnimalCatalog.GetName(displayController.SelectedId);
        }
        else if (displayController.CurrentPage == DisplayPage.Race)
        {
            hint = "比赛进行中  第 " + displayController.CurrentRound + " 局";
        }
        else if (displayController.CurrentPage == DisplayPage.WinReady)
        {
            hint = "按开始键，将 WIN 收入 CREDIT";
        }
        else if (displayController.CurrentPage == DisplayPage.WinCollecting)
        {
            hint = "正在收分，再按开始键快速完成";
        }

        if (!string.IsNullOrEmpty(hint))
        {
            GUI.Label(new Rect(395f, 80f, 490f, 36f), hint, hintStyle);
        }
    }

    private void DrawActiveOverlay()
    {
        if (settingsDisplay != null && settingsDisplay.IsVisible)
        {
            // SettingsDisplay renders the recovered 640x480 cabinet artwork itself.
            return;
        }

        if (faultDisplay != null && faultDisplay.IsVisible)
        {
            DrawOverlay(
                "设备故障",
                "游戏已停止",
                faultDisplay.Detail,
                new Color(0.34f, 0.07f, 0.07f, 0.96f),
                true);
            return;
        }

        if (ShouldSuppressPostRaceOverlay(
                displayController.CurrentPage,
                animationController != null &&
                animationController.IsFinishSceneLocked))
        {
            return;
        }

        if (payoutDisplay != null && payoutDisplay.IsVisible)
        {
            DrawOverlay(
                payoutDisplay.Headline,
                displayController.PayoutComplete ? "处理完成" : "正在处理",
                payoutDisplay.Detail,
                new Color(0.05f, 0.24f, 0.22f, 0.94f),
                false);
            return;
        }

        if (resultDisplay != null && resultDisplay.IsVisible)
        {
            DrawOverlay(
                resultDisplay.Headline,
                displayController.ResultStatus == "WIN" ? "恭喜中奖" : "本局未中奖",
                resultDisplay.Detail,
                new Color(0.16f, 0.17f, 0.05f, 0.94f),
                false);
            return;
        }

        if (winCollectPresenter != null && winCollectPresenter.IsVisible)
        {
            DrawOverlay(
                winCollectPresenter.Headline,
                winCollectPresenter.Subtitle,
                winCollectPresenter.Detail,
                new Color(0.10f, 0.23f, 0.18f, 0.94f),
                false);
            return;
        }

        if (displayController.CurrentPage == DisplayPage.Loading)
        {
            DrawOverlay(
                "正在连接 H743",
                "等待状态同步",
                displayController.DeviceInfo,
                new Color(0.04f, 0.12f, 0.16f, 0.94f),
                false);
        }
    }

    private void DrawOverlay(
        string title,
        string subtitle,
        string body,
        Color background,
        bool fullScreen)
    {
        Rect rect = fullScreen
            ? new Rect(0f, 0f, 1280f, 720f)
            : new Rect(340f, 150f, 600f, 380f);
        DrawColorRect(rect, background);
        DrawColorRect(new Rect(rect.x, rect.y, 8f, rect.height), new Color(0.42f, 0.86f, 0.72f));

        float contentX = rect.x + 54f;
        float contentY = rect.y + 48f;
        GUI.Label(new Rect(contentX, contentY, rect.width - 108f, 64f), title, overlayTitleStyle);
        GUI.Label(new Rect(contentX, contentY + 70f, rect.width - 108f, 42f), subtitle, hintStyle);
        GUI.Label(new Rect(contentX, contentY + 126f, rect.width - 108f, rect.height - 174f), body, overlayBodyStyle);
    }

    private void EnsureStyles()
    {
        if (topLabelStyle != null)
        {
            return;
        }

        whiteTexture = CreateSolidTexture(Color.white);
        darkTexture = CreateSolidTexture(new Color(0.04f, 0.06f, 0.07f, 0.96f));
        panelTexture = CreateSolidTexture(new Color(0.08f, 0.11f, 0.13f, 0.98f));
        selectedTexture = CreateSolidTexture(new Color(0.06f, 0.27f, 0.32f, 1f));
        circleTexture = CreateCircleTexture(64);

        topLabelStyle = CreateLabel(37, TextAnchor.MiddleLeft, Color.white);
        topLabelStyle.font = arcadeFont != null ? arcadeFont : uiFont;
        topValueStyle = CreateLabel(43, TextAnchor.MiddleLeft, Color.white);
        topValueStyle.font = arcadeFont != null ? arcadeFont : uiFont;
        laneLabelStyle = CreateLabel(17, TextAnchor.MiddleLeft, new Color(0.08f, 0.12f, 0.14f));
        animalLabelStyle = CreateLabel(18, TextAnchor.MiddleCenter, Color.white);
        slotTitleStyle = CreateLabel(17, TextAnchor.MiddleLeft, new Color(0.74f, 0.81f, 0.84f));
        slotValueStyle = CreateLabel(25, TextAnchor.MiddleLeft, Color.white);
        slotOddsStyle = CreateLabel(29, TextAnchor.MiddleRight, new Color(1f, 0.95f, 0.98f));
        slotOddsStyle.font = arcadeFont != null ? arcadeFont : uiFont;
        slotBetStyle = CreateLabel(39, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.18f));
        slotBetStyle.font = arcadeFont != null ? arcadeFont : uiFont;
        hintStyle = CreateLabel(21, TextAnchor.MiddleCenter, new Color(0.54f, 0.92f, 0.82f));
        overlayTitleStyle = CreateLabel(42, TextAnchor.MiddleCenter, Color.white);
        overlayBodyStyle = CreateLabel(25, TextAnchor.UpperCenter, new Color(0.93f, 0.96f, 0.97f));
        overlayBodyStyle.wordWrap = true;
        stateStyle = CreateLabel(18, TextAnchor.MiddleRight, new Color(0.48f, 0.86f, 0.75f));

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        selectedPanelStyle = new GUIStyle(GUI.skin.box);
        selectedPanelStyle.normal.background = selectedTexture;
    }

    private GUIStyle CreateLabel(int size, TextAnchor alignment, Color color)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = size,
            alignment = alignment,
            wordWrap = false
        };
        style.normal.textColor = color;
        return style;
    }

    private void DrawColorRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = old;
    }

    private static void DrawOutlinedLabel(
        Rect rect,
        string text,
        GUIStyle style,
        Color outlineColor,
        float thickness)
    {
        Color fillColor = style.normal.textColor;
        style.normal.textColor = outlineColor;
        GUI.Label(new Rect(rect.x - thickness, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - thickness, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - thickness, rect.y + thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y + thickness, rect.width, rect.height), text, style);
        style.normal.textColor = fillColor;
        GUI.Label(rect, text, style);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateCircleTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        foreach(Texture2D texture in clippedHistoryPortraits) DestroyTexture(texture);
        foreach(Texture2D texture in clippedBottomPortraits) DestroyTexture(texture);
        DestroyTexture(whiteTexture);
        DestroyTexture(circleTexture);
        DestroyTexture(darkTexture);
        DestroyTexture(panelTexture);
        DestroyTexture(selectedTexture);
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }
}
