using System;
using System.Collections.Generic;
using UnityEngine;

public enum RaceObstaclePhase { Running, Jumping, LeafRest, Fallen, BallRide }

public struct RaceObstaclePose
{
    public Vector2 Foot;
    public RaceObstaclePhase Phase;
    public int SourceFrame;
    public float BallWorldX;
    public float BarrelWorldX;
    public int BallFrame;
    public float ForwardSpeed;
    public float RunDistance;
}

// A seeded, time-based course keeps sprite poses, feet and props on one trajectory.
// Sampling it never advances state, so OnGUI repaint frequency cannot change a race.
public sealed class RaceObstacleTimeline
{
    public const float BallRideSeconds = 2.5f;
    public const float PropWorldX = RaceCourseGeometry.ObstacleWorldX;
    public const float LaneHeight = RaceCourseGeometry.LaneHeight;
    public const float MinimumPropHeight = LaneHeight * 1.15f;
    public const float PropBottomGap = 6f;
    public const float BallContactInset = 3f;
    private static readonly float[] BallStartOffsets = { 11f, -21f, 29f, -7f, 19f, -29f };
    public static readonly Rect PondWorldBounds = new Rect(PropWorldX - 208f, 103f, 416f, 312f);
    private static readonly float[] LeafX = { 334f, 208f, 72f };
    private static readonly float[,] LeafY =
    {
        { 29f, 34f, 28f }, { 77f, 81f, 75f }, { 123f, 128f, 123f },
        { 170f, 176f, 169f }, { 215f, 222f, 215f }, { 262f, 269f, 261f }
    };

    private struct Segment
    {
        public float Start, Duration, Arc, RunDistanceBefore;
        public Vector2 From, To;
        public RaceObstaclePhase Phase;
        public int Frame;
        public RaceMotionCurve Motion;
    }

    private readonly List<Segment> segments = new List<Segment>();
    private readonly int scene;
    private readonly float duration;
    private readonly float initialBallX;
    private readonly float barrelWorldX;
    private readonly float ballFrameDistance;
    private float rideStart = float.MaxValue;
    private float rideEnd = float.MaxValue;
    private float finalBallX = PropWorldX;
    public float InteractionStart { get; private set; }
    public float InteractionEnd { get; private set; }
    public bool Falls { get; private set; }
    public float Duration { get { return duration; } }
    public float RideStart { get { return rideStart; } }
    public float RideEnd { get { return rideEnd; } }

    public static float GetPropHeight(int animalId)
    {
        return MinimumPropHeight * Mathf.Lerp(1.15f, 1f, (Mathf.Clamp(animalId, 1, 6) - 1) / 5f);
    }

    public static float GetPropBottom(int animalId)
    {
        return 104f + Mathf.Clamp(animalId, 1, 6) * LaneHeight - PropBottomGap;
    }

    public static float GetBarrelHeight(int animalId)
    {
        return GetPropHeight(animalId) * 1.15f * 1.10f;
    }

    public static float GetInitialBallWorldX(int animalId)
    {
        return PropWorldX + BallStartOffsets[Mathf.Clamp(animalId, 1, 6) - 1];
    }

    public static Rect GetBallWorldBounds(int animalId, float centerX)
    {
        float scale = GetPropHeight(animalId) / 72f;
        return new Rect(centerX - 48f * scale, GetPropBottom(animalId) - 80f * scale,
            96f * scale, 80f * scale);
    }

    // A small overlap closes the bilinear-filtered outline seam at the curved crown.
    public static float GetBallSurfaceY(int animalId)
    {
        return GetBallWorldBounds(animalId, 0f).y + BallContactInset;
    }
    public static float GetBallRadius(int animalId) { return GetPropHeight(animalId) * 0.5f; }

    public static float GetInitialBarrelWorldX(int animalId, uint sequence, uint round)
    {
        var random = new System.Random(unchecked((int)(sequence * 157U + round * 281U + 137U)));
        int[] slots = { -2, 2, 0, -1, 3, 1 };
        for (int i = slots.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int swap = slots[i]; slots[i] = slots[j]; slots[j] = swap;
        }
        int lane = Mathf.Clamp(animalId, 1, 6) - 1;
        return PropWorldX + (slots[lane] - 0.5f) * 10f + Range(random, -2f, 2f);
    }

    public static Rect GetBarrelWorldBounds(int animalId, float centerX)
    {
        float scale = GetBarrelHeight(animalId) / 97f;
        return new Rect(centerX - 64f * scale, GetPropBottom(animalId) - 100f * scale,
            128f * scale, 104f * scale);
    }

    public static float GetBarrelHalfWidth(int animalId) { return GetBarrelWorldBounds(animalId, 0f).width * 0.5f; }

    public static Vector2 GetLeafFoot(int animalId, int columnFromRight)
    {
        int lane = Mathf.Clamp(animalId - 1, 0, 5);
        int column = Mathf.Clamp(columnFromRight, 0, 2);
        return new Vector2(PondWorldBounds.x + LeafX[column],
            PondWorldBounds.y + LeafY[lane, column]);
    }

    public RaceObstacleTimeline(int sceneIndex, int animalId, uint sequence, uint round,
        float totalSeconds, float finishX, RacePerformanceProfile profile = null,
        RaceCourseLayout course = null, uint? layoutSeed = null)
    {
        scene = Mathf.Clamp(sceneIndex, -1, 2);
        duration = Mathf.Max(0.25f, totalSeconds);
        float propShift = course == null ? 0f : course.ObstacleWorldX - PropWorldX;
        float startWorldX = course == null ? RaceCourseGeometry.StartWorldX : course.StartWorldX;
        startWorldX += RaceCourseLayout.GetStartOffset(animalId);
        Rect pond = course == null ? PondWorldBounds : course.PondWorldBounds;
        initialBallX = GetInitialBallWorldX(animalId) + propShift;
        ballFrameDistance = GetPropHeight(animalId) * 0.22f;
        barrelWorldX = GetInitialBarrelWorldX(animalId, layoutSeed ?? sequence, round) + propShift;
        finalBallX = initialBallX;
        var random = new System.Random(unchecked((int)(sequence * 31U + round * 131U + (uint)animalId * 997U)));
        float ground = RaceCourseGeometry.GetLaneFootBaseline(animalId);
        float jumpPreference = profile == null ? 0f : profile.JumpPreference;
        float recoveryPreference = profile == null ? 0f : profile.RecoveryPreference;
        var actions = new List<Segment>();
        float interactionStartDelay = 0f;
        Vector2 begin, exit;
        if (scene < 0)
        {
            begin = new Vector2(Mathf.Lerp(startWorldX, finishX, 0.5f), ground);
            exit = begin;
        }
        else if (scene == 1)
        {
            begin = new Vector2(pond.xMax, ground);
            Vector2 from = begin;
            for (int leaf = 0; leaf < 3; leaf++)
            {
                Vector2 landing = GetLeafFoot(animalId, leaf);
                landing.x += propShift;
                Add(actions, from, landing, ActionSeconds(random, 0.20f, 0.28f, jumpPreference),
                    RaceObstaclePhase.Jumping, Pick(random, 5), Range(random, 20f, 30f));
                Add(actions, landing, landing, ActionSeconds(random, 0.40f, 0.65f, recoveryPreference),
                    RaceObstaclePhase.LeafRest, Pick(random, 9), 0f);
                from = landing;
            }
            exit = new Vector2(pond.x - 55f, ground);
            Add(actions, from, exit, ActionSeconds(random, 0.24f, 0.32f, jumpPreference),
                RaceObstaclePhase.Jumping, Pick(random, 5), 28f);
        }
        else if (scene == 0)
        {
            float barrelHalfWidth = GetBarrelHalfWidth(animalId);
            begin = new Vector2(barrelWorldX + barrelHalfWidth + Range(random, 45f, 85f), ground);
            Falls = random.NextDouble() < 0.5;
            if (profile != null && profile.FallsOverride.HasValue) Falls = profile.FallsOverride.Value;
            // Both outcomes clear the left edge. A stumble lands beyond the barrel,
            // instead of pinning the fallen sprite underneath its center.
            exit = new Vector2(barrelWorldX - barrelHalfWidth -
                (Falls ? Range(random, 37f, 72f) : Range(random, 80f, 115f)), ground);
            float clearance = ground - (GetPropBottom(animalId) - GetBarrelHeight(animalId)) + 8f;
            float barrelArc = Mathf.Max(clearance,
                animalId == 1 ? 43f : (animalId == 2 ? 58f : Range(random, 65f, 88f)));
            Add(actions, begin, exit, ActionSeconds(random, 0.34f, 0.45f, jumpPreference),
                RaceObstaclePhase.Jumping, Pick(random, 5), barrelArc);
            if (Falls)
                Add(actions, exit, exit, ActionSeconds(random, 0.50f, 1.00f, recoveryPreference),
                    RaceObstaclePhase.Fallen, Pick(random, 7), 0f);
        }
        else
        {
            begin = new Vector2(initialBallX + GetBallRadius(animalId), ground);
            Vector2 top = new Vector2(initialBallX, GetBallSurfaceY(animalId));
            Add(actions, begin, top, ActionSeconds(random, 0.19f, 0.25f, jumpPreference),
                RaceObstaclePhase.Jumping, Pick(random, 5), 18f);
            // A 2.5-second ride should cover more ground, not turn into a slow
            // crawl while the clock camera keeps moving at its normal speed.
            float desiredBallTravel = Range(random, 260f, 330f);
            float maximumBallTravel = Mathf.Max(90f, initialBallX - finishX - 108f);
            finalBallX = initialBallX - Mathf.Min(desiredBallTravel, maximumBallTravel);
            Vector2 rideFinish = new Vector2(finalBallX, top.y);
            float originalRideSeconds = ActionSeconds(random, 1.1f, 1.6f, recoveryPreference);
            interactionStartDelay = BallRideSeconds - originalRideSeconds;
            Add(actions, top, rideFinish, BallRideSeconds,
                RaceObstaclePhase.BallRide, Pick(random, 9), 0f);
            exit = new Vector2(finalBallX - 72f, ground);
            Add(actions, rideFinish, exit, ActionSeconds(random, 0.20f, 0.28f, jumpPreference),
                RaceObstaclePhase.Jumping, Pick(random, 5), 18f);
        }

        float approachDistance = startWorldX - begin.x;
        float exitDistance = exit.x - finishX;
        // Allocate by actual distance. A wider seeded bias makes the field fan
        // out around the obstacle and regroup later; the motion curves still
        // change speed gradually, so this does not create a sudden launch.
        float exitSpeedRatio = Mathf.Clamp(1.06f + Range(random, -0.035f, 0.035f)
            + (profile == null ? 0f : profile.ApproachBias * 4.2f), 0.91f, 1.31f);
        RacePerformanceProfile ballProfile = profile;
        float ballEntrySpeed = 0f, ballExitSpeed = 0f, groundExitSpeed = 0f;
        float plannedInteractionEnd = course == null ? 0f : Mathf.Clamp(course.ObstacleWindowEnd - 0.25f
            + (profile == null ? 0f : profile.ApproachBias * 9f) + Range(random, -0.30f, 0.30f),
            duration * 0.31f, duration * 0.88f);
        if (scene == 2)
        {
            if (ballProfile == null)
                ballProfile = new RacePerformanceProfile
                {
                    BallAccelerationStart = Range(random, 0.12f, 0.34f),
                    BallAccelerationStrength = Range(random, 0.22f, 0.40f)
                };
            float gain = Mathf.Clamp(ballProfile.BallAccelerationStrength, 0.15f, 0.50f);
            float rideMean = RaceMotionCurve.GetRideMeanFactor(ballProfile.BallAccelerationStart, gain);
            float dismountMean = (1f + gain + exitSpeedRatio) * 0.5f;
            float mountDistance = actions[0].From.x - actions[0].To.x;
            float rideDistance = actions[1].From.x - actions[1].To.x;
            float dismountDistance = actions[2].From.x - actions[2].To.x;
            // Keep the visible ball ride at a deliberate 2.5 seconds. The
            // acceleration curve integrates to rideDistance, so derive its
            // entry speed from the fixed ride duration rather than stretching
            // the curve after it has been built.
            ballEntrySpeed = rideDistance / (BallRideSeconds * rideMean);
            if (course != null)
            {
                groundExitSpeed = exitDistance / (duration - plannedInteractionEnd);
                exitSpeedRatio = groundExitSpeed / ballEntrySpeed;
                dismountMean = (1f + gain + exitSpeedRatio) * 0.5f;
            }
            ballExitSpeed = ballEntrySpeed * (1f + gain);
            groundExitSpeed = ballEntrySpeed * exitSpeedRatio;
            SetDuration(actions, 0, Mathf.Min(actions[0].Duration, mountDistance / ballEntrySpeed));
            SetDuration(actions, 1, BallRideSeconds);
            SetDuration(actions, 2, Mathf.Min(actions[2].Duration,
                dismountDistance / (ballEntrySpeed * dismountMean)));
        }

        float actionDuration = 0f;
        foreach (Segment action in actions) actionDuration += action.Duration;
        // Only abnormally short race settings compress the dwell/jump durations.
        float actionScale = actionDuration > 0f ? Mathf.Min(1f, duration * 0.82f / actionDuration) : 1f;
        if (scene == 2 && duration >= 5f) actionScale = 1f;
        actionDuration *= actionScale;
        float runDuration = duration - actionDuration;
        float approachShare = approachDistance * exitSpeedRatio /
            (approachDistance * exitSpeedRatio + exitDistance);
        InteractionStart = runDuration * approachShare;
        if (course != null) InteractionStart = Mathf.Clamp(plannedInteractionEnd - actionDuration,
            duration * 0.04f, runDuration - duration * 0.04f);
        if (course != null && duration >= 8f && interactionStartDelay > 0f)
            InteractionStart = Mathf.Clamp(InteractionStart + interactionStartDelay,
                duration * 0.04f, runDuration - duration * 0.04f);
        if (course != null && duration >= 5f)
        {
            float halfWidth = RaceCourseLayout.GetHalfWidth(animalId);
            float earliestEntry = (halfWidth + 32f + course.InitialCameraLeft - begin.x) /
                RaceCourseLayout.CameraSpeed;
            float latestEntry = (640f + 20f - halfWidth - 14f
                + course.InitialCameraLeft - begin.x) / RaceCourseLayout.CameraSpeed;
            InteractionStart = Mathf.Clamp(InteractionStart, earliestEntry, latestEntry);
        }
        if (profile != null && profile.ClockCamera)
            profile.LaunchRampFraction = Mathf.Min(0.24f, Range(random, 0.72f, 0.90f) / InteractionStart);
        Add(segments, new Vector2(startWorldX, ground), begin, InteractionStart,
            RaceObstaclePhase.Running, 0, 0f);
        float time = InteractionStart;
        foreach (Segment original in actions)
        {
            Segment action = original;
            action.Start = time;
            action.Duration *= actionScale;
            segments.Add(action);
            if (action.Phase == RaceObstaclePhase.BallRide)
            {
                rideStart = time;
                rideEnd = time + action.Duration;
            }
            time += action.Duration;
        }
        InteractionEnd = time;
        Add(segments, exit, new Vector2(finishX, ground), duration - time,
            RaceObstaclePhase.Running, 0, 0f);
        Segment last = segments[segments.Count - 1];
        last.Start = time;
        segments[segments.Count - 1] = last;
        float runDistance = 0f;
        for (int i = 0; i < segments.Count; i++)
        {
            Segment paced = segments[i];
            float average = AverageSpeed(paced);
            float launchFactor = profile == null ? 0.5f : Mathf.Clamp(profile.LaunchSpeedFactor, 0.35f, 0.5f);
            float startSpeed = i == 0 ? average * launchFactor : Mathf.Min(average, AverageSpeed(segments[i - 1]));
            float endSpeed = i == segments.Count - 1 ? average : Mathf.Min(average, AverageSpeed(segments[i + 1]));
            if (scene == 2)
            {
                if (i == 0) endSpeed = ballEntrySpeed;
                if (i == 1) { startSpeed = ballEntrySpeed; endSpeed = ballEntrySpeed; }
                if (i == 2) { startSpeed = ballEntrySpeed; endSpeed = ballExitSpeed; }
                if (i == 3) { startSpeed = ballExitSpeed; endSpeed = groundExitSpeed; }
                if (i == 4) startSpeed = groundExitSpeed;
            }
            paced.RunDistanceBefore = runDistance;
            paced.Motion = new RaceMotionCurve(paced.Duration, paced.From.x - paced.To.x,
                startSpeed, endSpeed, paced.Phase == RaceObstaclePhase.Running, random,
                paced.Phase == RaceObstaclePhase.BallRide ? ballProfile : profile,
                paced.Phase == RaceObstaclePhase.BallRide, i == 0);
            if (paced.Phase == RaceObstaclePhase.Running) runDistance += paced.From.x - paced.To.x;
            segments[i] = paced;
        }
    }

    public RaceObstaclePose Sample(float elapsed)
    {
        float time = Mathf.Clamp(elapsed, 0f, duration);
        Segment segment = segments[segments.Count - 1];
        foreach (Segment candidate in segments)
        {
            if (time < candidate.Start + candidate.Duration)
            {
                segment = candidate;
                break;
            }
        }
        float p = Mathf.Clamp01((time - segment.Start) / Mathf.Max(0.0001f, segment.Duration));
        float localTime = Mathf.Clamp(time - segment.Start, 0f, segment.Duration);
        float distance = segment.Motion.SampleDistance(localTime);
        Vector2 foot = new Vector2(segment.From.x - distance, Mathf.Lerp(segment.From.y, segment.To.y, p));
        foot.y -= Mathf.Sin(p * Mathf.PI) * segment.Arc;
        int frame = segment.Frame;
        if (segment.Phase == RaceObstaclePhase.BallRide)
            frame = 9 + ((Mathf.FloorToInt((time - rideStart) * 7f) + segment.Frame - 9) & 1);
        float ballX = time <= rideStart ? initialBallX : finalBallX;
        if (segment.Phase == RaceObstaclePhase.BallRide) ballX = foot.x;
        return new RaceObstaclePose
        {
            Foot = foot, Phase = segment.Phase, SourceFrame = frame,
            BarrelWorldX = barrelWorldX,
            BallWorldX = scene == 2 ? ballX : PropWorldX,
            BallFrame = scene == 2 && time > rideStart
                ? Mathf.FloorToInt((initialBallX - ballX) / ballFrameDistance) & 1 : 0,
            ForwardSpeed = segment.Motion.SampleSpeed(localTime),
            RunDistance = segment.RunDistanceBefore + (segment.Phase == RaceObstaclePhase.Running ? distance : 0f)
        };
    }

    public float[] GetPhaseBoundaries()
    {
        float[] result = new float[segments.Count - 1];
        for (int i = 1; i < segments.Count; i++) result[i - 1] = segments[i].Start;
        return result;
    }

    private static float AverageSpeed(Segment segment)
    {
        return (segment.From.x - segment.To.x) / segment.Duration;
    }

    private static void SetDuration(List<Segment> actions, int index, float seconds)
    {
        Segment action = actions[index];
        action.Duration = seconds;
        actions[index] = action;
    }

    private static float ActionSeconds(System.Random random, float min, float max, float preference)
    {
        float unit = (float)random.NextDouble();
        unit = Mathf.Lerp(unit, preference >= 0f ? 0f : 1f, Mathf.Abs(preference));
        return Mathf.Lerp(min, max, unit);
    }

    private static void Add(List<Segment> target, Vector2 from, Vector2 to, float seconds,
        RaceObstaclePhase phase, int frame, float arc)
    {
        target.Add(new Segment { From = from, To = to, Duration = seconds,
            Phase = phase, Frame = frame, Arc = arc });
    }
    private static int Pick(System.Random random, int first) { return first + random.Next(2); }
    private static float Range(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
