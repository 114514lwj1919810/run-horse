using System;
using UnityEngine;

// Integrate smoothly varying, nonnegative speeds instead of correcting position
// near the finish. Shared endpoint speeds make adjacent actions continuous.
public sealed class RaceMotionCurve
{
    public const float RideRampFraction = 0.34f;
    private readonly float[] times;
    private readonly float[] speeds;
    private readonly float[] distances;
    private readonly float length;

    public RaceMotionCurve(float seconds, float distance, float startSpeed, float endSpeed,
        bool running, System.Random random, RacePerformanceProfile profile, bool acceleratingRide = false,
        bool easingLaunch = false)
    {
        length = Mathf.Max(0f, distance);
        float[] fractions = running
            ? new[] { 0f, 0.20f, 0.36f, 0.52f, 0.68f, 0.84f, 1f }
            : new[] { 0f, 0.5f, 1f };
        if (running && easingLaunch)
        {
            float ramp = profile == null ? 0.12f : Mathf.Clamp(profile.LaunchRampFraction,
                profile.ClockCamera ? 0.005f : 0.10f, 0.24f);
            fractions = new[] { 0f, ramp, Mathf.Lerp(ramp, 0.46f, 0.5f),
                0.52f, 0.68f, 0.84f, 1f };
        }
        if (acceleratingRide)
        {
            float onset = profile == null ? Mathf.Lerp(0.12f, 0.34f, (float)random.NextDouble())
                : profile.BallAccelerationStart;
            onset = Mathf.Clamp(onset, 0.12f, 0.34f);
            fractions = new[] { 0f, onset, onset + RideRampFraction, 1f };
        }
        times = new float[fractions.Length];
        speeds = new float[fractions.Length];
        distances = new float[fractions.Length];
        for (int i = 0; i < times.Length; i++) times[i] = fractions[i] * seconds;
        if (length <= 0f) return;
        float average = length / seconds;
        speeds[0] = Mathf.Max(0f, startSpeed);
        speeds[speeds.Length - 1] = Mathf.Max(0f, endSpeed);
        if (acceleratingRide)
        {
            // Keep entry speed, accelerate once, then carry that speed off the ball.
            // The timeline solves ride duration from this curve's integrated speed.
            speeds[1] = startSpeed;
            speeds[2] = endSpeed;
            IntegrateDistances();
            if (Mathf.Abs(distances[distances.Length - 1] - length) > 0.02f)
                throw new ArgumentException("Ride duration does not match its entry/exit speeds.");
            return;
        }
        for (int i = 1; i < speeds.Length - 1; i++)
        {
            float factor = 1f;
            if (running)
            {
                float f = fractions[i];
                float primary = profile == null ? 0.13f : profile.PaceWaveStrength;
                float phase = profile == null ? 0f : profile.PaceWavePhase;
                float secondaryPhase = profile == null ? 1.7f : profile.PaceSecondaryPhase;
                // One broad rise/fall produces visible chases. A smaller second
                // wave avoids a predictable single overtake, while cosine
                // interpolation keeps acceleration gradual between knots.
                factor += primary * Mathf.Sin(phase + f * Mathf.PI * 2f);
                factor += 0.040f * Mathf.Sin(secondaryPhase + f * Mathf.PI * 4f);
                factor += Mathf.Lerp(-0.018f, 0.018f, (float)random.NextDouble());
                if (i == 1) factor -= 0.055f;
                if (profile != null && i == Mathf.Max(2, profile.SurgeKnot))
                    factor += Mathf.Min(0.10f, profile.SurgeStrength * 0.22f);
                if (profile != null)
                    factor = 1f + (factor - 1f) * profile.PaceVariationScale;
                factor = Mathf.Clamp(factor, 0.78f, 1.22f);
            }
            speeds[i] = average * factor;
        }
        float fixedArea = speeds[0] * (times[1] - times[0]) * 0.5f
            + speeds[speeds.Length - 1] * (times[times.Length - 1] - times[times.Length - 2]) * 0.5f;
        float interiorArea = 0f;
        for (int i = 1; i < speeds.Length - 1; i++)
            interiorArea += speeds[i] * (times[i + 1] - times[i - 1]) * 0.5f;
        float scale = (length - fixedArea) / interiorArea;
        if (scale <= 0f) throw new ArgumentException("Endpoint speeds exceed segment distance.");
        for (int i = 1; i < speeds.Length - 1; i++) speeds[i] *= scale;
        IntegrateDistances();
    }

    public static float GetRideMeanFactor(float onset, float gain)
    {
        return 1f + gain * (1f - Mathf.Clamp(onset, 0.12f, 0.34f) - RideRampFraction * 0.5f);
    }

    private void IntegrateDistances()
    {
        for (int i = 1; i < times.Length; i++)
            distances[i] = distances[i - 1] + (speeds[i - 1] + speeds[i]) * 0.5f * (times[i] - times[i - 1]);
    }

    public float SampleDistance(float elapsed)
    {
        if (elapsed <= 0f || length == 0f) return 0f;
        if (elapsed >= times[times.Length - 1]) return length;
        int i = FindInterval(elapsed);
        float span = times[i + 1] - times[i];
        float p = (elapsed - times[i]) / span;
        float integral = p * 0.5f - Mathf.Sin(Mathf.PI * p) / (2f * Mathf.PI);
        return distances[i] + span * (speeds[i] * p + (speeds[i + 1] - speeds[i]) * integral);
    }

    public float SampleSpeed(float elapsed)
    {
        if (length == 0f) return 0f;
        if (elapsed <= 0f) return speeds[0];
        if (elapsed >= times[times.Length - 1]) return speeds[speeds.Length - 1];
        int i = FindInterval(elapsed);
        float p = (elapsed - times[i]) / (times[i + 1] - times[i]);
        return Mathf.Lerp(speeds[i], speeds[i + 1], (1f - Mathf.Cos(p * Mathf.PI)) * 0.5f);
    }

    private int FindInterval(float elapsed)
    {
        for (int i = 0; i < times.Length - 1; i++) if (elapsed < times[i + 1]) return i;
        return times.Length - 2;
    }
}
