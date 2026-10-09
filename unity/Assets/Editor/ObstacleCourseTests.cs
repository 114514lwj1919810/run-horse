using System;
using UnityEditor;
using UnityEngine;

public static class ObstacleCourseTests
{
    private static int checks;
    public static void BuildValidatedPc()
    {
        RunBatch();
        RK3128Build.ConfigureAndBuildPc();
    }
    public static void RunBatch()
    {
        checks = 0;
        ProtocolV31Tests.RunBatch();
        RacePresentationTests.RunBatch();
        TestLayoutAndPace();
        TestScoreDisplay();
        TestLongCourse();
        TestGroundAndBallContact();
        bool sawFall = false, sawClear = false;
        bool[] seenFrames = new bool[11];
        for (int scene = 0; scene < 3; scene++)
        for (int id = 1; id <= 6; id++)
        for (uint seed = 1; seed <= 16; seed++)
        {
            const float seconds = 13.5f;
            float finish = AnimalRaceUI.RacePositionToWorldX(
                AnimalRaceAnimationController.GetCourseFinishPosition(id, 1, seed, seed + 4));
            var course = new RaceObstacleTimeline(scene, id, seed, seed + 4, seconds, finish);
            if (scene == 0) { sawFall |= course.Falls; sawClear |= !course.Falls; }
            Check(Mathf.Abs(course.Sample(0).Foot.x - RaceCourseGeometry.StartWorldX) < 0.001f, "start");
            Check(Mathf.Abs(course.Sample(seconds).Foot.x - finish) < 0.001f, "finish");
            float lastX = RaceCourseGeometry.StartWorldX, lastBallX = scene == 2
                ? RaceObstacleTimeline.GetInitialBallWorldX(id) : RaceObstacleTimeline.PropWorldX;
            bool[] leaves = new bool[3];
            float[] leafHolds = new float[3];
            float fallHold = 0f;
            float jumpHold = 0f;
            for (float t = 0; t <= seconds; t += 0.01f)
            {
                RaceObstaclePose pose = course.Sample(t);
                Check(pose.Foot.x <= lastX + 0.01f, "racer moved backwards");
                Check(pose.BallWorldX <= lastBallX + 0.01f, "ball moved backwards");
                lastX = pose.Foot.x; lastBallX = pose.BallWorldX;
                if (pose.SourceFrame > 0) seenFrames[pose.SourceFrame] = true;
                if (pose.Phase == RaceObstaclePhase.Jumping)
                {
                    jumpHold += 0.01f;
                    Check(pose.SourceFrame == 5 || pose.SourceFrame == 6, "jump frame");
                }
                if (pose.Phase == RaceObstaclePhase.Fallen)
                {
                    fallHold += 0.01f;
                    Check(pose.SourceFrame == 7 || pose.SourceFrame == 8, "fall frame");
                    float fallClearance = pose.BarrelWorldX - RaceObstacleTimeline.GetBarrelHalfWidth(id) - pose.Foot.x;
                    Check(fallClearance >= 36.99f && fallClearance <= 72.01f,
                        "fall landing must shift only eight units right while staying left of barrel");
                }
                if (pose.Phase == RaceObstaclePhase.LeafRest)
                {
                    Check(pose.SourceFrame == 9 || pose.SourceFrame == 10, "rest frame");
                    bool onLeaf = false;
                    for (int leaf = 0; leaf < 3; leaf++)
                        if (Vector2.Distance(pose.Foot, RaceObstacleTimeline.GetLeafFoot(id, leaf)) < 0.001f)
                            { leaves[leaf] = true; leafHolds[leaf] += 0.01f; onLeaf = true; }
                    Check(onLeaf, "missed lily pad");
                }
                if (pose.Phase == RaceObstaclePhase.BallRide)
                {
                    Check(Mathf.Abs(pose.Foot.x - pose.BallWorldX) < 0.001f, "not centered on ball");
                    Check(Mathf.Abs(pose.Foot.y - RaceObstacleTimeline.GetBallSurfaceY(id)) < 0.001f,
                        "feet inside ball");
                    Check(pose.SourceFrame == 9 || pose.SourceFrame == 10, "ball pose");
                }
            }
            if (scene == 1) for (int leaf = 0; leaf < 3; leaf++)
            {
                Check(leaves[leaf], "skipped leaf");
                Check(leafHolds[leaf] >= 0.38f && leafHolds[leaf] <= 0.67f, "leaf rest duration");
            }
            if (course.Falls) Check(fallHold >= 0.48f && fallHold <= 1.02f, "stun duration");
            // Ball dismount duration now preserves speed continuity instead of
            // forcing the old fixed-time jump to brake or surge on landing.
            Check(jumpHold <= (scene == 1 ? 1.19f : (scene == 0 ? 0.47f : 0.65f)),
                "jump too slow scene=" + scene + " id=" + id + " seconds=" + jumpHold);
            if (scene == 0 && !course.Falls)
                Check(course.Sample(course.InteractionEnd).Foot.x <
                    course.Sample(0).BarrelWorldX - RaceObstacleTimeline.GetBarrelHalfWidth(id), "clear jump landing");
            if (scene == 2)
                Check(Mathf.Abs(course.RideEnd - course.RideStart - RaceObstacleTimeline.BallRideSeconds) < 0.01f,
                    "ball ride must last 2.5 seconds");
            Check(course.Sample(0).BarrelWorldX == course.Sample(seconds).BarrelWorldX,
                "barrel moved during race");
            RaceObstaclePose after = course.Sample(course.InteractionEnd + 0.05f);
            RaceObstaclePose final = course.Sample(seconds);
            Check(after.BallFrame == final.BallFrame && Mathf.Abs(after.BallWorldX - final.BallWorldX) < 0.001f,
                "ball did not stop after dismount");
        }
        Check(sawFall && sawClear, "barrel outcome variation missing");
        for (int frame = 5; frame <= 10; frame++) Check(seenFrames[frame], "unused source frame " + frame);
        TestController();
        Debug.Log("OBSTACLE_COURSE_TESTS_PASSED checks=" + checks);
    }

    private static void TestLayoutAndPace()
    {
        float lastHeight = float.MaxValue;
        for (int id = 1; id <= 6; id++)
        {
            float height = RaceObstacleTimeline.GetPropHeight(id);
            Check(height < lastHeight, "props not descending in size");
            Check(height >= 54.05f && height <= 62.2f, "prop not enlarged by one step");
            Check(Mathf.Abs(RaceObstacleTimeline.GetBarrelHeight(id) - height * 1.15f * 1.10f) < 0.001f,
                "barrel not enlarged independently of ball");
            lastHeight = height;
            Rect ball = RaceObstacleTimeline.GetBallWorldBounds(id, RaceObstacleTimeline.GetInitialBallWorldX(id));
            Check(Mathf.Abs(ball.yMax - (104f + id * 47f - 6f)) < 0.001f, "ball bottom gap");
            Rect barrel = RaceObstacleTimeline.GetBarrelWorldBounds(id,
                RaceObstacleTimeline.GetInitialBarrelWorldX(id, 0U, 0U));
            Check(Mathf.Abs(barrel.y + barrel.height * (100f / 104f)
                - RaceObstacleTimeline.GetPropBottom(id)) < 0.001f, "barrel bottom gap");
            Rect odds = AnimalRaceUI.GetRecoveredOddsBounds(id - 1, 24);
            Check(odds.width == 72f && odds.height == 32f, "odds not double sized");
            Check(Mathf.Abs(odds.xMax - (id * 640f / 6f - 5f)) < 0.001f, "odds right alignment");
            Check(odds.yMax == 444f, "odds score-box gap");
        }
        var startPositions = new System.Collections.Generic.HashSet<float>();
        var secondPlaceIds = new System.Collections.Generic.HashSet<int>();
        var firstToPropIds = new System.Collections.Generic.HashSet<int>();
        for (int id = 1; id <= 6; id++) startPositions.Add(RaceObstacleTimeline.GetInitialBallWorldX(id));
        Check(startPositions.Count == 6, "balls not staggered");
        Check(Mathf.Abs(RaceObstacleTimeline.GetPropHeight(6) - 47f * 1.15f) < 0.001f,
            "ball size changed unexpectedly");
        Check(Mathf.Abs(RaceObstacleTimeline.GetBarrelHeight(6) - RaceObstacleTimeline.GetPropHeight(1) * 1.10f) < 0.001f,
            "barrels must grow ten percent from the current size");
        float ballMin = float.MaxValue, ballMax = float.MinValue;
        foreach (float x in startPositions) { ballMin = Mathf.Min(ballMin, x); ballMax = Mathf.Max(ballMax, x); }
        Check(ballMax - ballMin <= 60f, "balls scattered too widely");
        var barrelLayouts = new System.Collections.Generic.HashSet<float>();
        for (uint round = 1; round <= 24; round++)
        {
            var barrels = new System.Collections.Generic.HashSet<float>();
            for (int id = 1; id <= 6; id++)
            {
                float x = RaceObstacleTimeline.GetInitialBarrelWorldX(id, round + 50, round);
                barrels.Add(x);
                Check(x == RaceObstacleTimeline.GetInitialBarrelWorldX(id, round + 50, round),
                    "barrel layout not deterministic");
            }
            Check(barrels.Count == 6, "barrels not staggered");
            float barrelMin = float.MaxValue, barrelMax = float.MinValue;
            foreach (float x in barrels) { barrelMin = Mathf.Min(barrelMin, x); barrelMax = Mathf.Max(barrelMax, x); }
            Check(barrelMax - barrelMin <= 54f, "barrels scattered too widely");
            barrelLayouts.Add(RaceObstacleTimeline.GetInitialBarrelWorldX(1, round + 50, round));
            var presentation = new RacePresentationPlan(new RacePresentationRequest(1, 1, 13.5f,
                RacePresentationPlan.CreateSeed(round + 50, round)));
            float bestPosition = 0f, firstTime = float.MaxValue, minX = RaceCourseGeometry.StartWorldX, maxX = 0f;
            int second = 0, first = 0;
            for (int id = 1; id <= 6; id++)
            {
                float target = AnimalRaceAnimationController.GetCourseFinishPosition(id, 1, round + 50, round);
                if (id != 1 && target > bestPosition) { bestPosition = target; second = id; }
                var course = presentation.GetTimeline(id);
                if (course.InteractionStart < firstTime) { firstTime = course.InteractionStart; first = id; }
                float x = course.Sample(2f).Foot.x;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            }
            Check(maxX - minX > 10f, "pack speeds too similar in round " + round + ": " + (maxX - minX));
            secondPlaceIds.Add(second); firstToPropIds.Add(first);
        }
        Check(secondPlaceIds.Count >= 4, "loser order not varied");
        Check(firstToPropIds.Count >= 4, "approach order not varied");
        Check(barrelLayouts.Count > 8, "barrel layout not randomized between rounds");
    }

    private static void TestLongCourse()
    {
        Check(RaceCourseGeometry.CourseLength == 2600f, "course must be slightly shorter than 2800 units");
        Check(Mathf.Abs(RaceCourseGeometry.ObstacleWorldX - RaceCourseLayout.Default.ObstacleWorldX) < 0.001f,
            "default prop and runtime course positions disagree");
        Check(AnimalRaceUI.GetFinishLineX(0f) < -640f, "finish not far enough from start");
        Check(AnimalRaceUI.GetStartLineX(0f) >= 440f && AnimalRaceUI.GetStartLineX(0f) <= 500f,
            "starting lineup shifted on screen");
        Check(RaceObstacleTimeline.PondWorldBounds.width == 416f &&
            RaceObstacleTimeline.PondWorldBounds.height == 312f, "pond art must not stretch with track length");
        float lastCamera = AnimalRaceUI.GetCameraLeft(0f, 0f);
        for (int i = 0; i <= 1000; i++)
        {
            float p = i / 1000f;
            float camera = AnimalRaceUI.GetCameraLeft(p, p);
            Check(camera <= lastCamera + 0.001f, "camera reversed during forward run");
            Check(lastCamera - camera < 10f, "camera jumped during follow transition");
            Check(Mathf.Abs(RaceCourseGeometry.ToPosition01(RaceCourseGeometry.ToWorldX(p)) - p) < 0.00001f,
                "world/progress coordinate mismatch");
            lastCamera = camera;
        }
        for (int scene = 0; scene < 3; scene++)
        for (int id = 1; id <= 6; id++)
        {
            float target = AnimalRaceUI.RacePositionToWorldX(
                AnimalRaceAnimationController.GetCourseFinishPosition(id, id, 33, 9));
            var course = new RaceObstacleTimeline(scene, id, 33, 9, 13.5f, target);
            Check(course.Duration == 13.5f, "shorter course changed race duration");
            float previousSpeed = (2800f - AnimalRaceUI.GetRacerVisibleLeadingEdgeOffset(id)) / course.Duration;
            float speedRatio = ((RaceCourseGeometry.StartWorldX - target) / course.Duration) / previousSpeed;
            Check(speedRatio > 0.92f && speedRatio < 0.94f, "average speed must decrease slightly, not drastically");
            Check(RaceCourseGeometry.StartWorldX - course.Sample(2f).Foot.x > 230f,
                "initial run unexpectedly stalled");
            Check(Mathf.Abs(course.Sample(course.Duration).Foot.x -
                AnimalRaceUI.GetRacerVisibleLeadingEdgeOffset(id) - RaceCourseGeometry.FinishWorldX) < 0.001f,
                "finish contact moved away from painted line");
        }
    }

    private static void TestGroundAndBallContact()
    {
        const string root = "Assets/Art/RecoveredOriginal/Processed/";
        string[] names = { "lion", "elephant", "gorilla", "zebra", "giraffe", "camel" };
        Texture2D balls = LoadPixels(root + "prop_balls.png");
        try
        {
            for (int id = 1; id <= 6; id++)
            {
                float edge = AnimalRaceUI.GetLaneLowerEdgeY(id);
                Check(edge == 104f + 47f * id, "lane edge not aligned with background");
                Check(AnimalRaceUI.GetLaneFootBaseline(id) == edge - 4f, "feet not near lane bottom");
                Vector2 size = AnimalRaceUI.GetRacerBoundsSize(id);
                Rect bounds = new Rect(-size.x * 0.5f, edge - 4f - size.y, size.x, size.y);
                Check(AnimalRaceUI.GetRacerGroundShadowBounds(bounds, id).yMax == edge,
                    "shadow not touching lane edge");
                Texture2D run = LoadPixels(root + "Animals/" + names[id - 1] + "_run.png");
                Texture2D idle = LoadPixels(root + "Animals/" + names[id - 1] + "_idle.png");
                Texture2D actions = LoadPixels(root + "Animals/" + names[id - 1] + "_actions.png");
                try
                {
                    CheckSpriteBottom(run, 4, bounds, edge);
                    CheckSpriteBottom(idle, 1, bounds, edge);
                    float scale = Mathf.Min(size.x / (run.width / 4f), size.y / run.height);
                    int width = actions.width / 2;
                    for (int frame = 0; frame < 2; frame++)
                    for (int ballFrame = 0; ballFrame < 2; ballFrame++)
                    {
                        Rect ball = RaceObstacleTimeline.GetBallWorldBounds(id, 0f);
                        float minimumGap = float.MaxValue;
                        // The lowest painted row is six pixels above the atlas cell edge.
                        for (int x = 0; x < width; x++)
                        {
                            if (actions.GetPixel(frame * width + x, 6).a < 0.5f) continue;
                            float footX = (x + 0.5f - width * 0.5f) * scale;
                            int ballX = Mathf.FloorToInt((footX - ball.x) / ball.width * 96f);
                            if (ballX < 0 || ballX >= 96) continue;
                            for (int y = 0; y < 72; y++)
                            {
                                if (balls.GetPixel(ballFrame * 96 + ballX, balls.height - 1 - y).a < 0.5f) continue;
                                float paintedTop = ball.y + y / 80f * ball.height;
                                minimumGap = Mathf.Min(minimumGap, paintedTop - RaceObstacleTimeline.GetBallSurfaceY(id));
                                break;
                            }
                        }
                        Check(minimumGap <= -0.5f && minimumGap >= -3.1f,
                            "visible foot/ball contact gap: " + id + "/" + frame + " = " + minimumGap);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(run);
                    UnityEngine.Object.DestroyImmediate(idle);
                    UnityEngine.Object.DestroyImmediate(actions);
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(balls); }
    }

    private static Texture2D LoadPixels(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(System.IO.File.ReadAllBytes(path)))
            throw new InvalidOperationException("Cannot inspect " + path);
        return texture;
    }

    private static void CheckSpriteBottom(Texture2D texture, int frameCount, Rect bounds, float edge)
    {
        int width = texture.width / frameCount;
        for (int frame = 0; frame < frameCount; frame++)
        {
            int lowest = -1;
            for (int y = 0; y < texture.height && lowest < 0; y++)
                for (int x = 0; x < width; x++)
                    if (texture.GetPixel(frame * width + x, y).a >= 0.5f) { lowest = y; break; }
            float targetBottom = edge - (frameCount == 4 && frame == 3 ? 7f : 0f);
            Rect target = AnimalRaceUI.GetGroundedRacerBounds(bounds, width, texture.height, targetBottom);
            float paintedBottom = target.y + (texture.height - lowest) * target.height / texture.height;
            Check(Mathf.Abs(paintedBottom - targetBottom) < 0.001f,
                "transparent padding leaves sprite above ground");
        }
    }

    private static void TestScoreDisplay()
    {
        foreach (int value in new[] { 0, 10, 100, 590, 600, 700, 240, 1234567, int.MaxValue })
        {
            string digits = AnimalRaceUI.FormatScore(value);
            Check(digits == value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "score units changed");
            for (int metric = 0; metric < 3; metric++)
            {
                Rect area = AnimalRaceUI.GetTopValueBounds(metric);
                for (int i = 0; i < digits.Length; i++)
                {
                    Rect glyph = AnimalRaceUI.GetTopDigitBounds(area, digits.Length, i);
                    Check(glyph.xMin >= area.xMin - 0.001f && glyph.xMax <= area.xMax + 0.001f &&
                        glyph.yMin >= area.yMin && glyph.yMax <= area.yMax + 0.001f && glyph.xMax < 640f,
                        "score digit clipped");
                }
            }
        }
        var root = new GameObject("ScoreUnitsTest");
        root.SetActive(false);
        try
        {
            var serial = root.AddComponent<AndroidSerialService>();
            var display = root.AddComponent<DisplayStateController>();
            display.SerialService = serial;
            var simulator = root.AddComponent<PcStandaloneSimulator>();
            simulator.DisplayController = display;
            Check(display.ProcessRawLine("SHOW_BETTING:seq=1,round=0,credit=600,total_bet=0,selected=1," +
                "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3"), "initial score state");
            simulator.AddCredit();
            Check(display.Balance == 700, "deposit 100 must add 100");
            simulator.PlaceBet();
            Check(display.Balance == 690 && display.Bets[0] == 10, "bet 10 must subtract 10");
            Check(display.ProcessRawLine("SHOW_WIN_READY:seq=2000,round=0,win=240,credit=690"), "win state");
            Check(display.WinPoints == 240 && display.Balance == 690, "win displayed in different units");
            int win, credit;
            WinCollectPresenter.EvaluateVisualTransfer(240, 690, 1f, out win, out credit);
            Check(win == 230 && credit == 700, "collect must transfer actual points one for one");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void TestController()
    {
        for (int scene = 0; scene < 3; scene++)
        for (int winner = 1; winner <= 6; winner++)
        {
            var root = new GameObject("ObstacleCourseTest");
            try
            {
                var controller = root.AddComponent<AnimalRaceAnimationController>();
                controller.ObstacleSceneIndexOverride = scene;
                controller.BeginRace(100, 1, winner, 15000);
                controller.Evaluate(0.999f);
                Check(!controller.TryFinishWhenWinnerCrosses(), "early finish");
                controller.Evaluate(1f);
                Check(controller.TryFinishWhenWinnerCrosses(), "finish not immediate");
                Check(controller.IsFinishSceneLocked, "finish not locked");
                Check(!controller.TryFinishWhenWinnerCrosses(), "duplicate finish");
                for (int id = 1; id <= 6; id++)
                    if (id != winner)
                    {
                        Check(!AnimalRaceUI.HasWinnerReachedFinish(id,
                            controller.GetPosition01(id)), "loser at line");
                        float gap = AnimalRaceUI.RacePositionToWorldX(controller.GetPosition01(id)) -
                            RaceCourseGeometry.FinishWorldX - AnimalRaceUI.GetRacerVisibleLeadingEdgeOffset(id);
                        Check(gap >= 165f && gap < 240f, "longer course also stretched losing margins");
                    }
                controller.ResetToStart();
                Check(!controller.HasObstacleCourse, "course not reset");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException("Obstacle course: " + message);
    }
}
