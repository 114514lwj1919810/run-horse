using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class VisualV32Tests
{
    [Test]
    public void CatalogUsesTheApprovedSixAnimalOrder()
    {
        Assert.AreEqual("狮子", AnimalCatalog.GetName(1));
        Assert.AreEqual("大象", AnimalCatalog.GetName(2));
        Assert.AreEqual("猩猩", AnimalCatalog.GetName(3));
        Assert.AreEqual("斑马", AnimalCatalog.GetName(4));
        Assert.AreEqual("长颈鹿", AnimalCatalog.GetName(5));
        Assert.AreEqual("骆驼", AnimalCatalog.GetName(6));
        Assert.AreEqual("Animal_06_Camel", AnimalCatalog.GetResourceName(6));
    }

    [Test]
    public void ThreeSceneNamesAreStable()
    {
        Assert.AreEqual("沙漠", AnimalRaceUI.GetSceneName(0));
        Assert.AreEqual("丛林", AnimalRaceUI.GetSceneName(1));
        Assert.AreEqual("城堡", AnimalRaceUI.GetSceneName(2));
    }

    [Test]
    public void ResultHoldIsCappedAtOnePointFiveSeconds()
    {
        Assert.AreEqual(1.5f, ResultDisplay.GetPresentationDurationSeconds(3000), 0.0001f);
        Assert.AreEqual(0.9f, ResultDisplay.GetPresentationDurationSeconds(900), 0.0001f);
    }

    [Test]
    public void WinCollectionTransfersTenPointsPerSecondWithoutFollowingFastDeviceUpdates()
    {
        int win;
        int credit;

        WinCollectPresenter.EvaluateVisualTransfer(80, 10, 1f, out win, out credit);
        Assert.AreEqual(70, win);
        Assert.AreEqual(20, credit);

        WinCollectPresenter.EvaluateVisualTransfer(80, 10, 8f, out win, out credit);
        Assert.AreEqual(0, win);
        Assert.AreEqual(90, credit);

        WinCollectPresenter.ApplyTimerDrivenVisualValues(
            60, 30, out win, out credit);
        Assert.AreEqual(60, win);
        Assert.AreEqual(30, credit);
        Assert.AreEqual(10f,
            DisplayStateController.WinCollectPointsPerSecond, 0.0001f);
        Assert.AreEqual(2.4f,
            DisplayStateController.GetWinCollectVisualDurationSeconds(24), 0.0001f);
        Assert.AreEqual(12f,
            DisplayStateController.GetWinCollectVisualDurationSeconds(120), 0.0001f);
        Assert.AreEqual(0.75f,
            DisplayStateController.WinCollectFastDoneHoldSeconds, 0.0001f);
    }

    [Test]
    public void SceneChangesAfterRoundInsteadOfWhenRaceStarts()
    {
        Assert.AreEqual(0, AnimalRaceUI.GetSceneIndexForState(
            0U, DisplayPage.Betting, DisplayPhase.Betting));
        Assert.AreEqual(0, AnimalRaceUI.GetSceneIndexForState(
            1U, DisplayPage.Race, DisplayPhase.WaitRaceOver));
        Assert.AreEqual(0, AnimalRaceUI.GetSceneIndexForState(
            1U, DisplayPage.Result, DisplayPhase.WaitResultOver));
        Assert.AreEqual(1, AnimalRaceUI.GetSceneIndexForState(
            1U, DisplayPage.Betting, DisplayPhase.Betting));
        Assert.AreEqual(1, AnimalRaceUI.GetSceneIndexForState(
            2U, DisplayPage.Race, DisplayPhase.WaitRaceOver));
    }

    [Test]
    public void PcSimulatorStartsBeforeRoundOneAndAdvancesOnlyWhenRaceStarts()
    {
        Assert.AreEqual(0U, PcStandaloneSimulator.InitialBettingRound);
        Assert.AreEqual(1U, PcStandaloneSimulator.GetNextRaceRound(0U));
        Assert.AreEqual(2U, PcStandaloneSimulator.GetNextRaceRound(1U));
    }

    [Test]
    public void SettingOverlayKeepsTheSceneUnderItsResumePhase()
    {
        Assert.AreEqual(1, AnimalRaceUI.GetSceneIndexForState(
            1U, DisplayPage.Setting, DisplayPhase.Betting));
        Assert.AreEqual(1, AnimalRaceUI.GetSceneIndexForState(
            2U, DisplayPage.Setting, DisplayPhase.WaitRaceOver));
    }

    [Test]
    public void FixedWorldPropsEnterFromLeftAndLeaveToRight()
    {
        float beforeView = AnimalRaceUI.GetObstacleScreenX(0f, 0f);
        float duringPass = AnimalRaceUI.GetObstacleScreenX(0.46f, 0.46f);
        float afterPass = AnimalRaceUI.GetObstacleScreenX(1f, 1f);

        Assert.Less(beforeView, 0f);
        Assert.That(duringPass, Is.InRange(0f, 640f));
        Assert.Greater(afterPass, 640f);
        Assert.AreEqual(RaceCourseLayout.FinishScreenX, AnimalRaceUI.GetFinishLineX(1f), 0.001f);
    }

    [Test]
    public void StartMarkerIsVisibleBeforeTheRaceAndFinishStartsOffscreen()
    {
        Assert.That(AnimalRaceUI.GetStartLineX(0f), Is.InRange(450f, 500f));
        Assert.Less(AnimalRaceUI.GetFinishLineX(0f), 0f);
    }

    [Test]
    public void RaceWorldCoordinatesMoveOnlyTheAnimalsAndCamera()
    {
        Assert.Greater(AnimalRaceUI.RacePositionToWorldX(0f),
            AnimalRaceUI.RacePositionToWorldX(1f));
        Assert.Greater(AnimalRaceUI.GetCameraLeft(0f, 0f),
            AnimalRaceUI.GetCameraLeft(1f, 1f));
    }

    [Test]
    public void RacersUseThePaintedBackgroundTrackBaselines()
    {
        Assert.AreEqual(new Vector2(640f, 480f), AnimalRaceUI.GetLogicalCanvasSize());
        Assert.AreEqual(144f, AnimalRaceUI.GetLaneFootBaseline(1));
        Assert.AreEqual(374f, AnimalRaceUI.GetLaneFootBaseline(6));
        for (int animalId = 2; animalId <= AnimalCatalog.Count; animalId++)
        {
            float spacing = AnimalRaceUI.GetLaneFootBaseline(animalId) -
                            AnimalRaceUI.GetLaneFootBaseline(animalId - 1);
            Assert.That(spacing, Is.InRange(46f, 48f));
        }
    }

    [Test]
    public void RacersUseArcadeScaleAndFullCanvasPortraits()
    {
        Assert.AreEqual(new Vector2(220f, 141f), AnimalRaceUI.GetRacerBoundsSize(1));
        Assert.AreEqual(new Vector2(220f, 217f), AnimalRaceUI.GetRacerBoundsSize(5));
        Assert.AreEqual(0f, AnimalRaceUI.GetRacerBottomPadding(1));
        Assert.AreEqual(0f, AnimalRaceUI.GetRacerBottomPadding(5));
        Assert.AreEqual(1f, AnimalRaceUI.GetRacerHorizontalScale(4));
        Assert.AreEqual(1f, AnimalRaceUI.GetRacerHorizontalScale(5));
        Assert.AreEqual(1f, AnimalRaceUI.GetRacerHorizontalScale(6));
    }

    [Test]
    public void TrackNumbersFollowTheirPaintedLanesAndUseBothFlashFrames()
    {
        Assert.AreEqual(-90f, AnimalRaceUI.GetTrackNumberRotationDegrees());
        for (int laneId = 1; laneId <= AnimalCatalog.Count; laneId++)
        {
            Rect bounds = AnimalRaceUI.GetTrackNumberBounds(500f, laneId);
            int displayedNumber = AnimalCatalog.Count + 1 - laneId;
            Assert.AreEqual(447f, bounds.x);
            Assert.AreEqual(112f + (laneId - 1) * 44.5f,
                bounds.y);
            Assert.AreEqual(new Rect(0f, 1f - displayedNumber / 6f,
                    0.5f, 1f / 6f),
                AnimalRaceUI.GetTrackNumberUv(laneId, false));
            Assert.AreEqual(new Rect(0.5f, 1f - displayedNumber / 6f,
                    0.5f, 1f / 6f),
                AnimalRaceUI.GetTrackNumberUv(laneId, true));
        }

        Assert.IsFalse(AnimalRaceUI.ShouldHighlightTrackNumber(4, 4, 0f));
        Assert.IsTrue(AnimalRaceUI.ShouldHighlightTrackNumber(4, 4, 0.30f));
        Assert.IsFalse(AnimalRaceUI.ShouldHighlightTrackNumber(3, 4, 0.30f));
        Assert.AreEqual(4, AnimalRaceUI.GetBlinkingTrackNumber(
            DisplayPage.Betting, 4, 2, false, false));
        Assert.AreEqual(0, AnimalRaceUI.GetBlinkingTrackNumber(
            DisplayPage.Race, 4, 2, true, false));
        Assert.AreEqual(2, AnimalRaceUI.GetBlinkingTrackNumber(
            DisplayPage.Race, 4, 2, true, true));
        Assert.AreEqual(0, AnimalRaceUI.GetBlinkingTrackNumber(
            DisplayPage.Race, 4, 2, false, true));
        float lionContact = AnimalRaceUI.GetFinishContactPosition01(1);
        Assert.IsFalse(AnimalRaceUI.HasWinnerReachedFinish(1, lionContact - 0.001f));
        Assert.IsTrue(AnimalRaceUI.HasWinnerReachedFinish(1, lionContact));
    }

    [Test]
    public void SettingsValuesChangeImmediatelyForEachLeftOrRightPress()
    {
        Assert.AreEqual(14, SettingsDisplay.GetDirectAdjustedValue(15, -1, 5, 60));
        Assert.AreEqual(16, SettingsDisplay.GetDirectAdjustedValue(15, 1, 5, 60));
        Assert.AreEqual(5, SettingsDisplay.GetDirectAdjustedValue(5, -1, 5, 60));
        Assert.AreEqual(60, SettingsDisplay.GetDirectAdjustedValue(60, 1, 5, 60));
    }

    [Test]
    public void SettingsCoverEveryPageVisibleInTheRecoveredVideo()
    {
        Assert.AreEqual(11, SettingsDisplay.RecoveredVideoPageCount);
    }

    [Test]
    public void RecoveredSystemPasswordOpensTheProtectedSettingsPage()
    {
        Assert.IsTrue(SettingsDisplay.IsDefaultSystemPassword(
            new[] { 3, 9, 4, 2, 5, 1, 0, 0 }));
        Assert.IsFalse(SettingsDisplay.IsDefaultSystemPassword(
            new[] { 3, 9, 4, 2, 5, 1, 0, 1 }));
        Assert.IsFalse(SettingsDisplay.CanAdvancePasswordCursor(false));
        Assert.IsTrue(SettingsDisplay.CanAdvancePasswordCursor(true));
    }

    [Test]
    public void HistoryUsesNeutralOvalFramesInsteadOfBottomMarqueeFrames()
    {
        Rect first = AnimalRaceUI.GetHistoryIconBounds(0);
        Rect fifth = AnimalRaceUI.GetHistoryIconBounds(4);
        Rect sixth = AnimalRaceUI.GetHistoryIconBounds(5);
        Rect last = AnimalRaceUI.GetHistoryIconBounds(9);
        Assert.AreEqual(new Rect(10f + 8f * 122f / 120f, 106f + 4f * 270f / 272f,
            47f * 122f / 120f, 47f * 270f / 272f), first);
        Assert.AreEqual(new Rect(10f + 8f * 122f / 120f, 106f + 220f * 270f / 272f,
            47f * 122f / 120f, 47f * 270f / 272f), fifth);
        Assert.AreEqual(new Rect(10f + 65f * 122f / 120f, 106f + 4f * 270f / 272f,
            47f * 122f / 120f, 47f * 270f / 272f), sixth);
        Assert.AreEqual(new Rect(10f + 65f * 122f / 120f, 106f + 220f * 270f / 272f,
            47f * 122f / 120f, 47f * 270f / 272f), last);
        Assert.AreEqual(new Color(0.46f, 0.46f, 0.44f, 1f),
            AnimalRaceUI.GetHistoryFrameColor());
        Assert.AreEqual(-160f, AnimalRaceUI.GetHistoryPanelOffsetX(0f), 0.0001f);
        Assert.AreEqual(0f, AnimalRaceUI.GetHistoryPanelOffsetX(1f), 0.0001f);
        Assert.AreEqual(new Rect(0f, 470f, 640f, 10f),
            AnimalRaceUI.GetSceneBottomExtensionBounds());
    }

    [Test]
    public void WinnerHistoryShiftsUpAndStoresTheLatestWinnerInSlotTen()
    {
        var root = new GameObject("WinnerHistoryTest");
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;
            int[] winners = { 2, 5, 1 };

            for (int index = 0; index < winners.Length; index++)
            {
                uint round = (uint)(index + 1);
                uint raceSequence = (uint)(index * 2 + 1);
                uint resultSequence = raceSequence + 1;
                Assert.IsTrue(display.ProcessRawLine(
                    "SHOW_RACE:seq=" + raceSequence + ",round=" + round +
                    ",winner=" + winners[index] + ",duration_ms=15000," +
                    "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3"));
                Assert.IsTrue(display.ProcessRawLine(
                    "SHOW_RESULT:seq=" + resultSequence + ",round=" + round +
                    ",result=LOSE,winner=" + winners[index] + ",odds=3," +
                    "win_total=0,win_remaining=0,credit=60,duration_ms=1500"));
                Assert.IsTrue(display.CompleteRacePresentation(round));
            }

            int[] history = display.WinnerHistory;
            Assert.AreEqual(0, history[6]);
            Assert.AreEqual(2, history[7]);
            Assert.AreEqual(5, history[8]);
            Assert.AreEqual(1, history[9]);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BottomPortraitsFillTheArcadeMarqueeWithoutCoveringItsOuterFrame()
    {
        Rect first = AnimalRaceUI.GetBottomPortraitBounds(0);
        Rect last = AnimalRaceUI.GetBottomPortraitBounds(5);
        Assert.AreEqual(new Rect(15f, 379f, 640f / 6f - 34f, 82f), first);
        Assert.AreEqual(new Rect(10f + 5f * 640f / 6f, 382f,
            640f / 6f - 20f, 71f), last);
        Assert.AreEqual(461f, first.yMax);
        Assert.AreEqual(453f, last.yMax);

        Assert.AreEqual(451f, AnimalRaceUI.GetBottomPortraitBounds(2).yMax);
        Assert.AreEqual(450f, AnimalRaceUI.GetBottomPortraitBounds(3).yMax);
        Assert.AreEqual(448f, AnimalRaceUI.GetBottomPortraitBounds(4).yMax);

        for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
        {
            Rect highResolutionUv = AnimalRaceUI.GetHighResolutionPortraitUv(animalId);
            Assert.Greater(highResolutionUv.width, 0f);
            Assert.Greater(highResolutionUv.height, 0f);
            Assert.LessOrEqual(highResolutionUv.xMax, 1f);
            Assert.LessOrEqual(highResolutionUv.yMax, 1.001f);
        }

        for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
        {
            Rect uv = AnimalRaceUI.GetRecoveredPortraitUv(animalId);
            Assert.Less(uv.width, 0.14f,
                "Portrait is still too narrow because its crop includes empty padding: " + animalId);
            Assert.That(uv.height, Is.InRange(0.67f, 0.86f));
        }
    }

    [Test]
    public void SelectedAvatarFrameAlternatesBetweenRecoveredTopAndBottomRows()
    {
        Rect white = new Rect(0f, 97f / 200f,
            107f / 640f, -97f / 200f);
        Rect colored = new Rect(0f, 194f / 200f,
            107f / 640f, -97f / 200f);
        Assert.AreEqual(white,
            AnimalRaceUI.GetRecoveredAvatarFrameUv(0, true, 0f));
        Assert.AreEqual(colored,
            AnimalRaceUI.GetRecoveredAvatarFrameUv(0, true, 0.30f));
        Assert.AreEqual(white,
            AnimalRaceUI.GetRecoveredAvatarFrameUv(0, false, 0.30f));
        Assert.AreEqual(0f, white.y + white.height, 0.0001f);
        Assert.AreEqual(97f / 200f, colored.y + colored.height, 0.0001f);

        Rect whiteBounds = AnimalRaceUI.GetRecoveredAvatarFrameBounds(0, true, 0f);
        Rect coloredBounds = AnimalRaceUI.GetRecoveredAvatarFrameBounds(0, true, 0.30f);
        Assert.AreEqual(380f, whiteBounds.y);
        Assert.AreEqual(97f, whiteBounds.height);
        Assert.AreEqual(whiteBounds, coloredBounds);
        Assert.AreEqual(new Rect(534f, 380f, 106f, 97f),
            AnimalRaceUI.GetRecoveredAvatarFrameBounds(5, false, 0.30f));
    }

    [Test]
    public void SelectedAnimalRunsInPlaceOnlyOnTheBettingPage()
    {
        Assert.IsTrue(AnimalRaceUI.ShouldAnimateAnimal(DisplayPage.Betting, 4, 4));
        Assert.IsFalse(AnimalRaceUI.ShouldAnimateAnimal(DisplayPage.Betting, 4, 3));
        Assert.IsTrue(AnimalRaceUI.ShouldAnimateAnimal(DisplayPage.Race, 4, 3));
        Assert.IsFalse(AnimalRaceUI.ShouldAnimateAnimal(DisplayPage.Idle, 4, 4));
        Assert.AreEqual(13f * AnimalRaceUI.SelectedRunSpeedScale,
            AnimalRaceUI.GetAnimalAnimationFrameRate(DisplayPage.Betting, 4, 4), 0.001f);
        Assert.AreEqual(13f,
            AnimalRaceUI.GetAnimalAnimationFrameRate(DisplayPage.Race, 4, 4), 0.001f);
        Assert.AreEqual(11f,
            AnimalRaceUI.GetAnimalAnimationFrameRate(DisplayPage.Betting, 4, 3), 0.001f);
        Assert.AreEqual(13f * AnimalRaceUI.WinnerRunSpeedScale,
            AnimalRaceUI.GetAnimalAnimationFrameRate(
                DisplayPage.Result, 4, 4, true, 4), 0.001f);
        Assert.AreEqual(13f,
            AnimalRaceUI.GetAnimalAnimationFrameRate(
                DisplayPage.Race, 4, 4, false, 4), 0.001f);
    }

    [Test]
    public void FinishCameraFreezesLosersAndKeepsWinnerRunning()
    {
        Assert.IsTrue(AnimalRaceUI.ShouldAnimateAnimal(
            DisplayPage.Race, 0, 4, true, 4));
        Assert.IsFalse(AnimalRaceUI.ShouldAnimateAnimal(
            DisplayPage.Race, 0, 3, true, 4));
        Assert.IsTrue(AnimalRaceUI.ShouldAnimateAnimal(
            DisplayPage.WinReady, 0, 4, true, 4));
        Assert.IsFalse(AnimalRaceUI.ShouldAnimateAnimal(
            DisplayPage.WinCollecting, 0, 3, true, 4));
        Assert.That(AnimalRaceUI.GetFrozenFinishFrame(3), Is.InRange(0, 3));
    }

    [Test]
    public void ResultAndCollectionPanelsStayHiddenOverLockedFinishScene()
    {
        Assert.IsTrue(AnimalRaceUI.ShouldSuppressPostRaceOverlay(
            DisplayPage.Result, true));
        Assert.IsTrue(AnimalRaceUI.ShouldSuppressPostRaceOverlay(
            DisplayPage.WinReady, true));
        Assert.IsTrue(AnimalRaceUI.ShouldSuppressPostRaceOverlay(
            DisplayPage.WinCollecting, true));
        Assert.IsFalse(AnimalRaceUI.ShouldSuppressPostRaceOverlay(
            DisplayPage.WinReady, false));
        Assert.IsFalse(AnimalRaceUI.ShouldSuppressPostRaceOverlay(
            DisplayPage.Setting, true));
    }

    [Test]
    public void FinishCameraClosesAndReopensBeforeRankStartsBlinking()
    {
        Assert.AreEqual(new Vector2(320f, 205f),
            AnimalRaceUI.GetFinishShutterCenter());
        Assert.AreEqual(1f, AnimalRaceUI.GetFinishShutterOpen01(0f), 0.0001f);
        Assert.AreEqual(0f,
            AnimalRaceUI.GetFinishShutterOpen01(0.45f), 0.0001f);
        Assert.AreEqual(1f,
            AnimalRaceUI.GetFinishShutterOpen01(0.88f), 0.0001f);

        Assert.IsTrue(AnimalRaceUI.ShouldHighlightFinishTrackNumber(
            4, 4, 0.20f, 0f));
        Assert.IsFalse(AnimalRaceUI.ShouldHighlightFinishTrackNumber(
            3, 4, 0.20f, 0.30f));
        Assert.IsFalse(AnimalRaceUI.ShouldHighlightFinishTrackNumber(
            4, 4, 1.00f, 0f));
        Assert.IsTrue(AnimalRaceUI.ShouldHighlightFinishTrackNumber(
            4, 4, 1.00f, 0.30f));
    }

    [Test]
    public void RecoveredRacerContactUsesPaintedNoseInsteadOfTransparentBounds()
    {
        Assert.AreEqual(84f, AnimalRaceUI.GetRacerVisibleLeadingEdgeOffset(1));
        Assert.AreEqual(94f, AnimalRaceUI.GetRacerVisibleLeadingEdgeOffset(4));
        Assert.Less(AnimalRaceUI.GetRacerVisibleLeadingEdgeOffset(4),
            AnimalRaceUI.GetRacerBoundsSize(4).x * 0.5f);
    }

    [Test]
    public void EveryRacerKeepsMovingUntilTheContactFrame()
    {
        for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
        {
            float atNinety = AnimalRaceAnimationController.EvaluatePosition(
                animalId, 4, 0.90f);
            float atNinetyFive = AnimalRaceAnimationController.EvaluatePosition(
                animalId, 4, 0.95f);
            float atFinish = AnimalRaceAnimationController.EvaluatePosition(
                animalId, 4, 1f);
            Assert.Greater(atNinetyFive, atNinety,
                "Racer paused before the finish: " + animalId);
            Assert.Greater(atFinish, atNinetyFive,
                "Racer did not keep moving into the finish frame: " + animalId);
        }

        Assert.AreEqual(AnimalRaceUI.GetFinishContactPosition01(4),
            AnimalRaceAnimationController.EvaluatePosition(4, 4, 1f), 0.0001f);
    }

    [Test]
    public void BetBoxUvDoesNotSampleTheAdjacentRedSegment()
    {
        Rect normal = AnimalRaceUI.GetRecoveredBetBoxUv(false);
        Rect selected = AnimalRaceUI.GetRecoveredBetBoxUv(true);
        Assert.AreEqual(new Rect(1.5f / 384f, 0.5f / 32f,
            91f / 384f, 31f / 32f), normal);
        Assert.AreEqual(new Rect(97.5f / 384f, 0.5f / 32f,
            91f / 384f, 31f / 32f), selected);
        Assert.Less(selected.xMax, 190f / 384f);
    }

    [Test]
    public void EnclosedPureBlackSpriteHolesAreRemovedButTinyDetailsRemain()
    {
        Assert.IsTrue(RecoveredOriginalAssetProcessor.ShouldRemoveEnclosedBlackRegion(20));
        Assert.IsTrue(RecoveredOriginalAssetProcessor.ShouldRemoveEnclosedBlackRegion(2000));
        Assert.IsFalse(RecoveredOriginalAssetProcessor.ShouldRemoveEnclosedBlackRegion(19));
    }

    [Test]
    public void ColoredSixHasNoBlackArcInsideItsHole()
    {
        string path = Path.Combine(Application.dataPath,
            "Art/RecoveredOriginal/Processed/ui_finish_ranks.png");
        Assert.IsTrue(File.Exists(path));

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));
        Assert.AreEqual(0, texture.GetPixel(63, 18).a,
            "The colored six still contains the recovered black inner arc.");
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void EachLanePropHasBeforeContactAndPassedStates()
    {
        Assert.AreEqual(0, AnimalRaceUI.GetObstaclePhase(RaceCourseGeometry.ObstacleWorldX + 150f));
        Assert.AreEqual(1, AnimalRaceUI.GetObstaclePhase(RaceCourseGeometry.ObstacleWorldX));
        Assert.AreEqual(2, AnimalRaceUI.GetObstaclePhase(RaceCourseGeometry.ObstacleWorldX - 150f));
    }

    [Test]
    public void FirstVisualContactEndsRaceAndImmediatelyStartsFinishCamera()
    {
        GameObject owner = new GameObject("RaceAnimationTest");
        AnimalRaceAnimationController controller = owner.AddComponent<AnimalRaceAnimationController>();
        controller.BeginRace(10U, 1U, 4, 15000);

        controller.Evaluate(0.99f);
        Assert.IsTrue(controller.IsRunning);
        float winnerContact = AnimalRaceUI.GetFinishContactPosition01(4);
        Assert.Less(controller.GetPosition01(4), winnerContact);

        controller.Evaluate(1f);

        int completedCount = 0;
        controller.RaceCompleted += delegate { completedCount++; };
        Assert.IsTrue(controller.TryFinishWhenWinnerCrosses());
        Assert.IsFalse(controller.IsRunning);
        Assert.IsTrue(controller.IsFinishHoldActive);
        Assert.AreEqual(1, completedCount,
            "The logical race must end on the exact visual contact frame.");
        Assert.AreEqual(1.5f, AnimalRaceAnimationController.FinishHoldSeconds,
            0.0001f);

        int finishers = 0;
        for (int animalId = 1; animalId <= AnimalCatalog.Count; animalId++)
        {
            float contact = AnimalRaceUI.GetFinishContactPosition01(animalId);
            if (controller.GetPosition01(animalId) >= contact)
            {
                finishers++;
            }
            else
            {
                // Use painted-nose world distance; a normalized 0.09 gap changed
                // meaning when the duration-scaled course replaced the old course.
                Assert.Greater(
                    controller.GetWorldX(animalId) - RaceCourseGeometry.GetLeadingEdgeOffset(animalId),
                    RaceCourseGeometry.FinishWorldX + 32f,
                    "A losing animal still visually overlaps the finish line: " + animalId);
            }
        }

        Assert.AreEqual(1, finishers);
        Assert.GreaterOrEqual(controller.GetPosition01(4), winnerContact);
        Assert.IsFalse(controller.TryCompleteFinishHold(1.49f));
        Assert.IsFalse(controller.IsRunning);
        Assert.IsTrue(controller.TryCompleteFinishHold(1.5f));
        Assert.IsFalse(controller.IsRunning);
        Assert.IsFalse(controller.IsFinishHoldActive);
        Assert.IsTrue(controller.IsFinishSceneLocked,
            "The finish scene must remain locked until the next round starts.");
        Assert.AreEqual(1, completedCount,
            "Completing the camera presentation must not end the race twice.");
        controller.ResetToStart();
        Assert.IsFalse(controller.IsFinishSceneLocked);
        Object.DestroyImmediate(owner);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void PhotoAlignedStartFitsEveryAnimalAndCanReachTheFinish(int scene)
    {
        for (int winner = 1; winner <= 6; winner++)
        {
            var plan = new RacePresentationPlan(new RacePresentationRequest(winner, scene, 15f,
                RacePresentationPlan.CreateSeed((uint)(100 + winner), 9U)));
            for (int animal = 1; animal <= 6; animal++)
            {
                Assert.AreEqual(RaceCourseLayout.StartScreenX + RaceCourseLayout.GetStartOffset(animal),
                    plan.Sample(animal,0f).Foot.x - plan.Course.GetCameraLeft(0f), 0.01f);
                Assert.Less(plan.Sample(animal,15f).Foot.x, plan.Sample(animal,0f).Foot.x);
                if (animal != winner)
                    Assert.AreEqual(RaceCourseLayout.GetFinishPackScreenX(animal),
                        plan.Sample(animal,15f).Foot.x - plan.Course.GetCameraLeft(15f), 0.01f);
            }
            Assert.IsTrue(plan.HasWinnerTouchedLine(15f));
        }
    }

    [Test]
    public void BuiltSceneContainsRecoveredOriginalArtworkAndPcSimulator()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        AnimalRaceUI ui = Object.FindObjectOfType<AnimalRaceUI>();
        Assert.IsNotNull(ui);
        Assert.AreEqual(6, ui.animalRunSheets.Length);
        Assert.AreEqual(6, ui.animalIdleSheets.Length);
        Assert.AreEqual(0, ui.animalRunFrames.Length);
        Assert.AreEqual(0, ui.animalFallFrames.Length);
        Assert.AreEqual(0, ui.animalJumpFrames.Length);
        Assert.AreEqual(0, ui.animalBallFrames.Length);
        Assert.AreEqual(3, ui.originalSceneTopStrips.Length);
        Assert.AreEqual(3, ui.originalSceneTrackTiles.Length);
        Assert.IsNotNull(ui.originalAvatarFrameStrip);
        Assert.IsNotNull(ui.originalPortraitStrip);
        Assert.IsNotNull(ui.originalBetBoxStrip);
        Assert.IsNotNull(ui.originalWinLabel);
        Assert.IsNotNull(ui.originalBonusLabel);
        Assert.IsNotNull(ui.originalCreditLabel);
        Assert.IsNotNull(ui.originalTopDigits);
        Assert.IsNotNull(ui.scoreFont);
        Assert.IsNotNull(ui.originalBetDigits);
        Assert.IsNotNull(ui.originalPlaqueDigits);
        Assert.IsNotNull(ui.originalHistoryPanel);
        Assert.IsNotNull(ui.originalOddsGlyphs);
        Assert.IsNotNull(ui.originalFinishLine);
        Assert.IsNotNull(ui.originalFinishRanks);
        Assert.IsNotNull(ui.hayWallTexture);
        Assert.IsNotNull(ui.lilyPadPoolTexture);
        Assert.IsNotNull(ui.beachBallStackTexture);
        Assert.IsNotNull(Object.FindObjectOfType<PcStandaloneSimulator>());
    }
}
