using UnityEngine;

// Shared by camera, racer movement and prop placement so changing the course
// length cannot leave the finish trigger on an older coordinate scale.
public static class RaceCourseGeometry
{
    public const float FinishWorldX = 300f;
    public const float ReferenceCourseLength = 1400f;
    public const float CourseLength = 2600f;
    public const float PackVariationScale = ReferenceCourseLength / CourseLength;
    public const float StartWorldX = FinishWorldX + CourseLength;
    public const float EndWorldX = 50f;
    public const float ProgressSpan = StartWorldX - EndWorldX;
    public const float StartMarkerWorldX = StartWorldX - 75f;
    public const float ObstacleWorldX = FinishWorldX - RaceCourseLayout.FinishScreenX
        + RaceCourseLayout.CameraSpeed * 7.5f + 320f;
    public const float InitialCameraLeft = StartWorldX - RaceCourseLayout.StartScreenX;
    public const float WorldMaxX = StartWorldX + 200f;
    public const float LaneHeight = 46f;
    public const float GroundFootInset = 4f;
    // Painted noses, not the bounds of padded sprite cells.
    private static readonly float[] LeadingEdgeOffsets = { 84f, 86f, 77f, 94f, 98f, 107f };

    public static float GetLaneLowerEdgeY(int animalId)
    {
        return 102f + Mathf.Clamp(animalId, 1, 6) * LaneHeight;
    }

    public static float GetLaneFootBaseline(int animalId)
    {
        return GetLaneLowerEdgeY(animalId) - GroundFootInset;
    }

    public static float GetLeadingEdgeOffset(int animalId)
    {
        return LeadingEdgeOffsets[Mathf.Clamp(animalId - 1, 0, 5)];
    }

    public static float GetFinishContactWorldX(int animalId)
    {
        return FinishWorldX + GetLeadingEdgeOffset(animalId);
    }

    public static float ToWorldX(float position)
    {
        return Mathf.Lerp(StartWorldX, EndWorldX, Mathf.Clamp01(position));
    }

    public static float ToPosition01(float worldX)
    {
        return Mathf.InverseLerp(StartWorldX, EndWorldX, worldX);
    }
}
