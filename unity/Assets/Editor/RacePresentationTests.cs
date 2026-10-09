using System;
using System.Collections.Generic;
using UnityEngine;

public static class RacePresentationTests
{
    private static int checks;

    public static void RunDramaProbe()
    {
        float spreadTotal = 0f, peakSpread = 0f, peakAcceleration = 0f;
        int samples = 0, leaderChanges = 0, races = 0;
        string peakAccelerationContext = string.Empty;
        for (int scene = 0; scene < 3; scene++)
        for (int winner = 1; winner <= 6; winner++)
        for (uint seed = 1; seed <= 12; seed++)
        {
            var plan = new RacePresentationPlan(new RacePresentationRequest(winner, scene, 15f,
                RacePresentationPlan.CreateSeed(seed + 800, seed)));
            int previousLeader = 0;
            float[] previousSpeeds = new float[6];
            RaceObstaclePhase[] previousPhases = new RaceObstaclePhase[6];
            for (int tick = 0; tick <= 150; tick++)
            {
                float time = tick * 0.1f;
                float minimumNose = float.MaxValue, maximumNose = float.MinValue;
                int leader = 0;
                for (int id = 1; id <= 6; id++)
                {
                    RaceObstaclePose pose = plan.Sample(id, time);
                    float nose = pose.Foot.x - RaceCourseGeometry.GetLeadingEdgeOffset(id);
                    if (nose < minimumNose) { minimumNose = nose; leader = id; }
                    if (nose > maximumNose) maximumNose = nose;
                    if (tick > 0 && pose.Phase == RaceObstaclePhase.Running &&
                        previousPhases[id - 1] == RaceObstaclePhase.Running)
                    {
                        float acceleration = Mathf.Abs(pose.ForwardSpeed - previousSpeeds[id - 1]) / 0.1f;
                        if (acceleration > peakAcceleration)
                        {
                            peakAcceleration = acceleration;
                            peakAccelerationContext = " scene=" + scene + " winner=" + winner +
                                " seed=" + seed + " id=" + id + " time=" + time.ToString("F1") +
                                " speed=" + previousSpeeds[id - 1].ToString("F1") + "->" +
                                pose.ForwardSpeed.ToString("F1");
                        }
                    }
                    previousSpeeds[id - 1] = pose.ForwardSpeed;
                    previousPhases[id - 1] = pose.Phase;
                }
                if (time >= 1.5f && time <= 13.5f)
                {
                    float spread = maximumNose - minimumNose;
                    spreadTotal += spread;
                    peakSpread = Mathf.Max(peakSpread, spread);
                    samples++;
                    if (previousLeader != 0 && leader != previousLeader) leaderChanges++;
                    previousLeader = leader;
                }
            }
            races++;
        }
        Debug.Log("RACE_DRAMA_PROBE races=" + races + " averageSpread=" +
            (spreadTotal / Mathf.Max(1, samples)).ToString("F1") + " peakSpread=" +
            peakSpread.ToString("F1") + " leaderChanges=" + leaderChanges +
            " peakAcceleration=" + peakAcceleration.ToString("F1") + peakAccelerationContext);
    }

    public static void RunBatch()
    {
        checks = 0;
        var sceneLeaders = new[] { new HashSet<int>(), new HashSet<int>(), new HashSet<int>() };
        for (int scene = 0; scene < 3; scene++)
        for (int winner = 1; winner <= 6; winner++)
        {
            int comebacks = 0, earlyLeads = 0, fallWins = 0, clearWins = 0;
            var stories = new HashSet<RaceWinStory>();
            var firstAtObstacle = new HashSet<int>();
            var ballSpeedProfiles = new HashSet<int>();
            for (uint seed = 1; seed <= 32; seed++)
            {
                var request = new RacePresentationRequest(winner, scene, 15f,
                    RacePresentationPlan.CreateSeed(seed + 50, seed));
                var plan = new RacePresentationPlan(request);
                var replay = new RacePresentationPlan(request);
                stories.Add(plan.Story);
                int earlyRank = Rank(plan, winner, 2.7f);
                if (earlyRank >= 4) comebacks++;
                if (earlyRank == 1) earlyLeads++;
                if (plan.GetTimeline(winner).Falls) fallWins++; else clearWins++;
                float earliest = float.MaxValue;
                int first = 0;
                for (int id = 1; id <= 6; id++)
                {
                    var timeline = plan.GetTimeline(id);
                    float approachAverage = (timeline.Sample(0f).Foot.x -
                        timeline.Sample(timeline.InteractionStart).Foot.x) / timeline.InteractionStart;
                    float exitAverage = (timeline.Sample(timeline.InteractionEnd).Foot.x -
                        timeline.Sample(timeline.Duration).Foot.x) / (timeline.Duration - timeline.InteractionEnd);
                    Check(exitAverage >= RaceCourseLayout.CameraSpeed * 0.5f && exitAverage <= RaceCourseLayout.CameraSpeed * 1.6f,
                        "post-obstacle running pace outside camera-compatible range scene=" + scene +
                        " winner=" + winner + " id=" + id + " seed=" + seed +
                        " pace=" + exitAverage + " interactionEnd=" + timeline.InteractionEnd);
                    float launchRatio = timeline.Sample(0f).ForwardSpeed / approachAverage;
                    Check(launchRatio >= 0.349f && launchRatio <= 0.501f,
                        "launch speed should ease in below half cruise speed");
                    if (scene == 2)
                    {
                        float rideSpan = timeline.RideEnd - timeline.RideStart;
                        var slow = timeline.Sample(timeline.RideStart + rideSpan * 0.20f);
                        var fast = timeline.Sample(timeline.RideStart + rideSpan * 0.75f);
                        Check(fast.ForwardSpeed > slow.ForwardSpeed * 1.15f, "ball does not accelerate while riding");
                        ballSpeedProfiles.Add(Mathf.RoundToInt(fast.ForwardSpeed * 10f));
                        float approachEntry = timeline.Sample(timeline.InteractionStart - 0.001f).ForwardSpeed;
                        float previousRideSpeed = timeline.Sample(timeline.RideStart).ForwardSpeed;
                        Check(Mathf.Abs(previousRideSpeed - approachEntry) < 0.1f,
                            "mounting ball brakes below approaching running speed");
                        float rideAverage = (timeline.Sample(timeline.RideStart).Foot.x -
                            timeline.Sample(timeline.RideEnd).Foot.x) / rideSpan;
                        Check(rideAverage > approachEntry * 1.10f, "riding is slower than running despite its internal acceleration");
                        float priorBallX = timeline.Sample(timeline.RideStart).BallWorldX;
                        for (int step = 1; step <= 100; step++)
                        {
                            float t = timeline.RideStart + rideSpan * step / 101f;
                            var ride = timeline.Sample(t);
                            Check(Mathf.Abs(ride.Foot.x - ride.BallWorldX) < 0.001f &&
                                Mathf.Abs(ride.Foot.y - RaceObstacleTimeline.GetBallSurfaceY(id)) < 0.001f,
                                "acceleration separates rider from ball");
                            Check(ride.BallWorldX <= priorBallX, "accelerating ball reverses");
                            Check(ride.ForwardSpeed >= previousRideSpeed - 0.001f &&
                                ride.ForwardSpeed >= approachEntry - 0.1f, "ball ride decelerates");
                            priorBallX = ride.BallWorldX;
                            previousRideSpeed = ride.ForwardSpeed;
                        }
                    }
                    if (timeline.InteractionStart < earliest) { earliest = timeline.InteractionStart; first = id; }
                    float previousX = plan.Sample(id, 0f).Foot.x;
                    float previousRunDistance = 0f;
                    for (int tick = 0; tick <= 375; tick++)
                    {
                        float time = Mathf.Min(15f, tick * 0.04f);
                        RaceObstaclePose pose = plan.Sample(id, time);
                        RaceObstaclePose again = replay.Sample(id, time);
                        Check(pose.Foot == again.Foot && pose.SourceFrame == again.SourceFrame &&
                            pose.BallWorldX == again.BallWorldX && pose.ForwardSpeed == again.ForwardSpeed,
                            "seeded replay differs");
                        Check(pose.Foot.x <= previousX + 0.001f, "race moves backwards");
                        Check(pose.RunDistance >= previousRunDistance - 0.001f, "gait clock reverses");
                        Check(pose.ForwardSpeed >= 0f, "negative speed");
                        float screenX = pose.Foot.x - plan.Course.GetCameraLeft(time);
                        float halfWidth = RaceCourseLayout.GetHalfWidth(id);
                        Check(screenX - halfWidth >= 0f && screenX + halfWidth <= AllowedRightEdge(id),
                            "racer outside clock camera bounds scene=" + scene + " winner=" + winner +
                            " id=" + id + " seed=" + seed + " time=" + time + " x=" + screenX +
                            " half=" + halfWidth + " allowed=" + AllowedRightEdge(id));
                        if (pose.Phase == RaceObstaclePhase.Running)
                            Check(pose.ForwardSpeed < 750f, "implausible running catch-up speed");
                        if (time > 14f)
                            Check(pose.Phase == RaceObstaclePhase.Running && pose.ForwardSpeed >
                                (timeline.Sample(0f).Foot.x - timeline.Sample(15f).Foot.x) / 15f * 0.30f,
                                "racer slows/stops before contact scene=" + scene + " winner=" + winner +
                                " id=" + id + " seed=" + seed + " time=" + time + " phase=" + pose.Phase +
                                " speed=" + pose.ForwardSpeed);
                        float contact = RaceCourseGeometry.GetFinishContactWorldX(id);
                        if (id != winner) Check(pose.Foot.x > contact, "non-winner touches line");
                        else if (time < 14.99f) Check(pose.Foot.x > contact, "winner finishes early");
                        previousX = pose.Foot.x;
                        previousRunDistance = pose.RunDistance;
                    }
                    foreach (float boundary in timeline.GetPhaseBoundaries())
                    {
                        var left = timeline.Sample(boundary - 0.0001f);
                        var right = timeline.Sample(boundary + 0.0001f);
                        Check(Vector2.Distance(left.Foot, right.Foot) < 2f, "phase position discontinuity");
                        Check(Mathf.Abs(left.ForwardSpeed - right.ForwardSpeed) < 1f,
                            "phase speed discontinuity");
                    }
                    var saved = plan.Sample(id, 2f);
                    plan.Sample(id, 11f);
                    Check(saved.Foot == plan.Sample(id, 2f).Foot, "sampling mutates playback");
                }
                var loserFinishSlots = new List<float>();
                for (int id = 1; id <= 6; id++)
                    if (id != winner)
                        loserFinishSlots.Add(plan.Sample(id, 15f).Foot.x - plan.Course.GetCameraLeft(15f));
                loserFinishSlots.Sort();
                for (int i = 1; i < loserFinishSlots.Count; i++)
                    Check(loserFinishSlots[i] - loserFinishSlots[i - 1] >= 54.9f,
                        "losing racers collapse into one finish column");
                Check(loserFinishSlots[loserFinishSlots.Count - 1] - loserFinishSlots[0] >= 239.9f,
                    "front-to-back finish spread is too small");
                firstAtObstacle.Add(first);
                sceneLeaders[scene].Add(first);
                Check(plan.HasWinnerTouchedLine(15f), "no contact at configured end");
                Check(!plan.HasWinnerTouchedLine(14.999f), "premature contact");
                Check(Mathf.Abs(plan.Sample(winner, 15f).Foot.x -
                    RaceCourseGeometry.GetFinishContactWorldX(winner)) < 0.002f, "winner contact offset");
            }
            Check(comebacks > 0 && earlyLeads > 0, "winner always follows same early order scene=" + scene +
                " winner=" + winner + " comeback=" + comebacks + " earlyLead=" + earlyLeads);
            // Matched ball-mount speeds constrain arrival order; do not demand
            // large artificial timing offsets merely to spread the first arrivals.
            Check(firstAtObstacle.Count >= (scene == 2 ? 2 : 4), "same animal always leads at obstacle scene=" + scene +
                " winner=" + winner + " count=" + firstAtObstacle.Count);
            Check(stories.Count == (scene == 0 ? 6 : (scene == 2 ? 5 : 4)), "story variation missing");
            if (scene == 2) Check(ballSpeedProfiles.Count > 50, "ball acceleration not randomized");
            if (scene == 0) Check(fallWins > 0 && clearWins > 0, "barrel always grants winner same outcome");
            Debug.Log("PRESENTATION_VARIATION scene=" + scene + " winner=" + winner +
                " comeback=" + comebacks + " earlyLead=" + earlyLeads + " fall=" + fallWins + " clear=" + clearWins);
        }
        for (int scene = 0; scene < 3; scene++)
            Check(sceneLeaders[scene].Count == 6, "some animals can never lead at the obstacle");
        foreach (float seconds in new[] { 0.25f, 3.5f, 8.5f, 28.5f, 58.5f })
        for (int scene = -1; scene < 3; scene++)
        for (int winner = 1; winner <= 6; winner++)
        {
            var plan = new RacePresentationPlan(new RacePresentationRequest(winner, scene, seconds, 319U));
            Check(plan.HasWinnerTouchedLine(seconds), "duration mismatch");
            Check(!plan.HasWinnerTouchedLine(seconds * 0.999f), "duration ends too early");
        }
        TestClockCamera();
        TestPcSettings();
        Debug.Log("RACE_PRESENTATION_TESTS_PASSED checks=" + checks);
    }

    private static void TestClockCamera()
    {
        var baseline = new RaceCourseLayout(15f);
        Check(baseline.BeforeSeconds == 5f && baseline.AfterSeconds == 6f && baseline.ObstacleSeconds == 4f,
            "15s segment mapping must be 5/4/6");
        foreach (float seconds in new[] { 5f, 10f, 15f, 20f, 30f, 60f })
        for (int scene = 0; scene < 3; scene++)
        for (int winner = 1; winner <= 6; winner++)
        {
            var plan = new RacePresentationPlan(new RacePresentationRequest(winner, scene, seconds, 533U));
            var course = plan.Course;
            Check(Mathf.Abs(course.CourseLength - baseline.CourseLength -
                RaceCourseLayout.CameraSpeed * (seconds - 15f)) < 0.002f, "duration must add length at fixed speed");
            Check(Mathf.Abs((course.GetCameraLeft(0) - course.GetCameraLeft(seconds)) / seconds -
                RaceCourseLayout.CameraSpeed) < 0.002f, "camera speed changes with duration");
            for (int i = 0; i <= 600; i++)
            {
                float t = seconds * i / 600f;
                Check(Mathf.Abs(course.GetCameraLeft(t) - (course.InitialCameraLeft - RaceCourseLayout.CameraSpeed * t)) < 0.002f,
                    "camera is not linear");
                for (int id = 1; id <= 6; id++)
                {
                    float x = plan.Sample(id, t).Foot.x - course.GetCameraLeft(t);
                    float half = RaceCourseLayout.GetHalfWidth(id);
                    Check(x - half >= -0.01f && x + half <= AllowedRightEdge(id) + 0.01f,
                        "duration-specific racer clipping seconds=" + seconds + " scene=" + scene + " id=" + id);
                }
            }
            Check(plan.HasWinnerTouchedLine(seconds), "scaled course misses configured finish time");
        }
        for (int i = 0; i <= 100; i++)
        {
            float p = i / 100f;
            Check(AnimalRaceUI.GetCameraLeft(p, 0f) == AnimalRaceUI.GetCameraLeft(p, 1f),
                "camera still follows an animal");
        }
    }

    private static void TestPcSettings()
    {
        var root = new GameObject("PcRaceSettingsTest");
        root.SetActive(false);
        try
        {
            var display = root.AddComponent<DisplayStateController>();
            var simulator = root.AddComponent<PcStandaloneSimulator>();
            simulator.DisplayController = display;
            Check(display.ProcessRawLine("SHOW_BETTING:seq=1,round=0,credit=600,total_bet=0,selected=1," +
                "bets=0|0|0|0|0|0,odds=24|24|8|6|3|3"), "settings test betting state");
            Check(simulator.RaceDurationSeconds == 15, "PC default is not 15s");
            simulator.OpenSettings();
            Check(simulator.SettingsOpen, "PC cannot open settings without hardware");
            Check(!simulator.SetRaceDurationSeconds(0) && !simulator.SetRaceDurationSeconds(61), "invalid duration accepted");
            Check(simulator.SetRaceDurationSeconds(30), "PC duration cannot change");
            simulator.CancelSettings();
            simulator.PlaceBet();
            simulator.StartOrCollect();
            Check(display.RaceDurationMilliseconds == 30000, "configured duration not sent to race");
            Check(!simulator.SetRaceDurationSeconds(15), "active round duration changed in place");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static float AllowedRightEdge(int id)
    {
        // The production photograph-aligned layout intentionally clips tails at the start.
        return Mathf.Max(660f, RaceCourseLayout.StartScreenX + RaceCourseLayout.GetHalfWidth(id) * 1.5f);
    }

    private static int Rank(RacePresentationPlan plan, int id, float time)
    {
        int rank = 1;
        float nose = plan.Sample(id, time).Foot.x - RaceCourseGeometry.GetLeadingEdgeOffset(id);
        for (int other = 1; other <= 6; other++)
            if (other != id && plan.Sample(other, time).Foot.x - RaceCourseGeometry.GetLeadingEdgeOffset(other) < nose) rank++;
        return rank;
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException("Race presentation: " + message);
    }
}
