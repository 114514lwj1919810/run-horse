using System;
using UnityEngine;

public enum RaceWinStory { EarlyLead, Chase, FastObstacle, MidRaceSurge, CleanJump, QuickRecovery, RollingSurge }

public sealed class RacePresentationRequest
{
    public readonly int WinnerId, SceneIndex;
    public readonly float MotionSeconds;
    public readonly uint Seed;

    public RacePresentationRequest(int winnerId, int sceneIndex, float motionSeconds, uint seed)
    {
        if (winnerId < 1 || winnerId > 6) throw new ArgumentOutOfRangeException("winnerId");
        if (sceneIndex < -1 || sceneIndex > 2) throw new ArgumentOutOfRangeException("sceneIndex");
        if (float.IsNaN(motionSeconds) || float.IsInfinity(motionSeconds) || motionSeconds < 0.25f)
            throw new ArgumentOutOfRangeException("motionSeconds");
        WinnerId = winnerId; SceneIndex = sceneIndex; MotionSeconds = motionSeconds; Seed = seed;
    }
}

public sealed class RacePerformanceProfile
{
    public float ApproachBias, JumpPreference, RecoveryPreference, SurgeStrength;
    public float BallAccelerationStart, BallAccelerationStrength;
    public float LaunchRampFraction = 0.12f, LaunchSpeedFactor = 0.5f;
    public float PaceVariationScale = 1f;
    // Two long, smooth pace waves let racers trade places without abrupt bursts.
    // The phases are seeded per racer, so a replay remains deterministic.
    public float PaceWavePhase, PaceSecondaryPhase, PaceWaveStrength;
    public bool ClockCamera;
    public int SurgeKnot;
    public bool? FallsOverride;
}

// Pure presentation: no serial port, page state, clock or GameObject references.
// The hardware's winner is an input. A seed selects an entire replayable race.
public sealed class RacePresentationPlan
{
    private readonly RaceObstacleTimeline[] racers = new RaceObstacleTimeline[6];
    public RacePresentationRequest Request { get; private set; }
    public RaceWinStory Story { get; private set; }
    public RaceCourseLayout Course { get; private set; }

    public RacePresentationPlan(RacePresentationRequest request)
    {
        if (request == null) throw new ArgumentNullException("request");
        Request = request;
        Course = new RaceCourseLayout(request.MotionSeconds);
        var random = new System.Random(unchecked((int)request.Seed));
        int storyIndex = random.Next(request.SceneIndex == 0 ? 6 : (request.SceneIndex == 2 ? 5 : 4));
        Story = request.SceneIndex == 2 && storyIndex == 4 ? RaceWinStory.RollingSurge : (RaceWinStory)storyIndex;
        for (int id = 1; id <= 6; id++)
        {
            for (int attempt = 0; attempt < 160; attempt++)
            {
            var profile = new RacePerformanceProfile
            {
                ClockCamera = true,
                PaceVariationScale = Mathf.Min(1f, 15f / request.MotionSeconds),
                ApproachBias = Range(random, -0.060f, 0.060f),
                JumpPreference = Range(random, -0.6f, 0.6f),
                RecoveryPreference = Range(random, -0.6f, 0.6f),
                SurgeKnot = random.Next(2, 6), SurgeStrength = Range(random, 0.16f, 0.34f),
                PaceWavePhase = Range(random, 0f, Mathf.PI * 2f),
                PaceSecondaryPhase = Range(random, 0f, Mathf.PI * 2f),
                PaceWaveStrength = Range(random, 0.12f, 0.17f),
                BallAccelerationStart = Range(random, 0.12f, 0.34f),
                BallAccelerationStrength = Range(random, 0.22f, 0.40f),
                LaunchRampFraction = Range(random, 0.10f, 0.16f),
                LaunchSpeedFactor = Range(random, 0.42f, 0.50f)
            };
            if (id == request.WinnerId) ApplyStory(profile, Story);
            float finish = RaceCourseGeometry.ToWorldX(GetFinishPosition(id, request.WinnerId, request.Seed, 0));
            uint motionSeed = unchecked(request.Seed + (uint)attempt * 2654435761U);
            var candidate = new RaceObstacleTimeline(request.MotionSeconds < 5f ? -1 : request.SceneIndex, id, motionSeed, 0,
                request.MotionSeconds, finish, profile, Course, request.Seed);
            if (request.MotionSeconds < 5f || FitsCamera(candidate, id))
            {
                racers[id - 1] = candidate;
                break;
            }
            }
            if (racers[id - 1] == null)
                throw new InvalidOperationException("Cannot fit racer to clock camera: scene=" + request.SceneIndex +
                    " id=" + id + " seed=" + request.Seed + " seconds=" + request.MotionSeconds);
        }
    }

    private bool FitsCamera(RaceObstacleTimeline timeline, int id)
    {
        float halfWidth = RaceCourseLayout.GetHalfWidth(id);
        float min = halfWidth + 12f;
        // The photographed start position intentionally clips the animals' tails
        // at the right edge. Do not reject every plan at t=0 for that layout.
        float max = Mathf.Max(640f + 20f - halfWidth - 4f,
            RaceCourseLayout.StartScreenX + halfWidth * 0.5f - 4f);
        float previous = 0f;
        float[] boundaries = timeline.GetPhaseBoundaries();
        for (int phase = 0; phase <= boundaries.Length; phase++)
        {
            float end = phase == boundaries.Length ? Course.Duration : boundaries[phase];
            for (int sample = 0; sample <= 48; sample++)
            {
                float t = Mathf.Lerp(previous, end, sample / 48f);
                float screenX = timeline.Sample(t).Foot.x - Course.GetCameraLeft(t);
                if (screenX < min || screenX > max) return false;
            }
            previous = end;
        }
        return true;
    }

    public RaceObstaclePose Sample(int animalId, float elapsedSeconds)
    {
        return GetTimeline(animalId).Sample(elapsedSeconds);
    }

    public RaceObstacleTimeline GetTimeline(int animalId)
    {
        if (animalId < 1 || animalId > 6) throw new ArgumentOutOfRangeException("animalId");
        return racers[animalId - 1];
    }

    public bool HasWinnerTouchedLine(float elapsedSeconds)
    {
        return Sample(Request.WinnerId, elapsedSeconds).Foot.x <=
            RaceCourseGeometry.GetFinishContactWorldX(Request.WinnerId) + 0.001f;
    }

    public static uint CreateSeed(uint sequence, uint round)
    {
        unchecked { return sequence * 73856093U ^ round * 19349663U; }
    }

    public static float GetFinishPosition(int animalId, int winnerId, uint seed, uint round)
    {
        float contact = RaceCourseGeometry.ToPosition01(RaceCourseGeometry.GetFinishContactWorldX(animalId));
        if (animalId == winnerId) return contact;
        // Keep the five losing racers visibly staggered on the final straight.
        // Their order is seeded per round, so they do not collapse into the
        // same vertical line and the same animal is not always last.
        int[] losers = new int[5];
        int count = 0;
        for (int id = 1; id <= 6; id++)
            if (id != winnerId) losers[count++] = id;
        var random = new System.Random(unchecked((int)(seed ^ (round * 2246822519U) ^
            ((uint)winnerId * 3266489917U))));
        for (int i = losers.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int swap = losers[i]; losers[i] = losers[j]; losers[j] = swap;
        }
        int slot = 0;
        while (slot < losers.Length && losers[slot] != animalId) slot++;
        float[] finishSlots = { 270f, 325f, 380f, 440f, 510f };
        float finishWorld = RaceCourseGeometry.FinishWorldX - RaceCourseLayout.FinishScreenX +
            finishSlots[Mathf.Clamp(slot, 0, finishSlots.Length - 1)];
        return RaceCourseGeometry.ToPosition01(finishWorld);
    }

    private static void ApplyStory(RacePerformanceProfile profile, RaceWinStory story)
    {
        switch (story)
        {
            case RaceWinStory.EarlyLead:
                profile.ApproachBias = -0.060f;
                profile.SurgeKnot = 2; profile.SurgeStrength = 0.55f;
                profile.PaceWavePhase = 0f; profile.PaceWaveStrength = 0.17f;
                break;
            case RaceWinStory.Chase:
                profile.ApproachBias = 0.055f;
                profile.LaunchRampFraction = 0.24f; profile.LaunchSpeedFactor = 0.35f;
                break;
            case RaceWinStory.FastObstacle:
                profile.ApproachBias = 0.025f;
                profile.JumpPreference = 0.95f; profile.RecoveryPreference = 0.9f;
                break;
            case RaceWinStory.MidRaceSurge:
                profile.ApproachBias = 0.015f; profile.SurgeStrength = 0.45f;
                break;
            case RaceWinStory.CleanJump:
                profile.ApproachBias = 0.045f; profile.FallsOverride = false;
                break;
            case RaceWinStory.QuickRecovery:
                profile.FallsOverride = true; profile.RecoveryPreference = 0.95f;
                break;
            case RaceWinStory.RollingSurge:
                profile.ApproachBias = 0.035f; profile.RecoveryPreference = 0.7f;
                profile.BallAccelerationStrength = 0.45f;
                break;
        }
    }

    // Compatibility preview for callers without an active round; live playback
    // always uses the complete seeded plan, including obstacle interactions.
    public static float EvaluatePreviewPosition(int animalId, int winnerId, float progress)
    {
        float p = Mathf.Clamp01(progress);
        float contact = RaceCourseGeometry.ToPosition01(RaceCourseGeometry.GetFinishContactWorldX(animalId));
        float target = animalId == winnerId ? contact : contact -
            (165f + ((animalId * 7) % 4) * 19.8f) / RaceCourseGeometry.ProgressSpan;
        float variation = Mathf.Sin(p * Mathf.PI * 2f + animalId * 0.83f) * Mathf.Sin(p * Mathf.PI) * 0.018f;
        return Mathf.Clamp01(target * Mathf.Clamp01(p + variation));
    }

    private static float Range(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
