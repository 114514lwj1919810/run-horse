using UnityEngine;

// One immutable course per round. Extra time adds ground, not playback speed.
public sealed class RaceCourseLayout
{
    public const float BaseSeconds = 15f;
    public const float StartScreenX = 561f;
    public const float FinishScreenX = 110f;
    public const float CameraSpeed = (RaceCourseGeometry.CourseLength - StartScreenX + FinishScreenX) / BaseSeconds;
    public static readonly RaceCourseLayout Default = new RaceCourseLayout(BaseSeconds);

    public readonly float Duration, BeforeSeconds, ObstacleSeconds, AfterSeconds;
    public readonly float StartWorldX, InitialCameraLeft, ObstacleWorldX, CourseLength, ProgressSpan;
    public readonly Rect PondWorldBounds;
    public float ObstacleWindowEnd { get { return Duration - AfterSeconds; } }

    public RaceCourseLayout(float seconds)
    {
        Duration = Mathf.Max(0.25f, seconds);
        ObstacleSeconds = Mathf.Min(4f, Duration * 0.65f);
        BeforeSeconds = (Duration - ObstacleSeconds) * (5f / 11f);
        AfterSeconds = (Duration - ObstacleSeconds) * (6f / 11f);
        InitialCameraLeft = RaceCourseGeometry.FinishWorldX - FinishScreenX + CameraSpeed * Duration;
        StartWorldX = InitialCameraLeft + StartScreenX;
        CourseLength = StartWorldX - RaceCourseGeometry.FinishWorldX;
        ProgressSpan = StartWorldX - RaceCourseGeometry.EndWorldX;
        float propTime = ObstacleWindowEnd - Mathf.Min(1.5f, ObstacleSeconds * 0.5f);
        ObstacleWorldX = GetCameraLeft(propTime) + 320f;
        PondWorldBounds = new Rect(ObstacleWorldX - 208f, 103f, 416f, 312f);
    }

    public float GetCameraLeft(float elapsed)
    {
        return InitialCameraLeft - CameraSpeed * Mathf.Clamp(elapsed, 0f, Duration);
    }

    public float ToWorldX(float position)
    {
        return Mathf.Lerp(StartWorldX, RaceCourseGeometry.EndWorldX, Mathf.Clamp01(position));
    }

    public float ToPosition01(float worldX)
    {
        return Mathf.InverseLerp(StartWorldX, RaceCourseGeometry.EndWorldX, worldX);
    }

    public float GetStartMarkerWorldX() { return StartWorldX - 75f; }

    public static float GetStartOffset(int animalId)
    {
        // Camel's long neck places its nose farther left of the sprite centre.
        // Match the photographed nose alignment without shrinking its body.
        return animalId == 6 ? 20f : 0f;
    }

    public static float GetFinishPackScreenX(int animalId)
    {
        // Rectified finish-photo centres in ROM canvas coordinates. These are
        // also the production losing-racer destinations, not screenshot overrides.
        switch (animalId)
        {
            case 1: return 238f;
            case 2: return 300f;
            case 3: return 326f;
            case 4: return 302f;
            case 5: return 302f;
            default: return 306f;
        }
    }

    public static float GetHalfWidth(int animalId)
    {
        switch (animalId)
        {
            case 3: return 95f;
            case 6: return 117.5f;
            default: return 110f;
        }
    }
}
