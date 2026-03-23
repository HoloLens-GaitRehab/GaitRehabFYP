using UnityEngine;

public class OffCourseTracker
{
    public float CurrentOffCoursePercent { get; private set; }
    public float OffCourseTimeSeconds { get; private set; }
    public float CurrentLateralDistance { get; private set; }
    public float CurrentSignedLateralDistance { get; private set; }

    public void Reset()
    {
        CurrentOffCoursePercent = 0f;
        OffCourseTimeSeconds = 0f;
        CurrentLateralDistance = 0f;
        CurrentSignedLateralDistance = 0f;
    }

    public void Update(
        float deltaTime,
        bool trackOffCourse,
        float offCourseTolerance,
        float effectiveElapsedTime,
        Vector3 playerPosition,
        Vector3 lineStart,
        Vector3 lineEnd)
    {
        if (deltaTime <= 0f)
            return;

        CurrentSignedLateralDistance = SignedDistanceToInfiniteLineXZ(playerPosition, lineStart, lineEnd);
        CurrentLateralDistance = Mathf.Abs(CurrentSignedLateralDistance);

        if (trackOffCourse)
        {
            float clampedTolerance = Mathf.Max(0.1f, offCourseTolerance);
            if (CurrentLateralDistance > clampedTolerance)
            {
                OffCourseTimeSeconds += deltaTime;
            }

            float elapsedTime = Mathf.Max(0.001f, effectiveElapsedTime);
            CurrentOffCoursePercent = (OffCourseTimeSeconds / elapsedTime) * 100f;
        }
        else
        {
            CurrentOffCoursePercent = 0f;
        }
    }

    public float GetSeverity(float offCourseTolerance, float redShiftDistance)
    {
        float clampedTolerance = Mathf.Max(0.1f, offCourseTolerance);
        float clampedRedShiftDistance = Mathf.Max(clampedTolerance + 0.05f, redShiftDistance);
        float excess = Mathf.Max(0f, CurrentLateralDistance - clampedTolerance);
        return Mathf.Clamp01(excess / (clampedRedShiftDistance - clampedTolerance));
    }

    public static float DistanceToInfiniteLineXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a = new Vector2(lineStart.x, lineStart.z);
        Vector2 b = new Vector2(lineEnd.x, lineEnd.z);
        Vector2 ab = b - a;
        float abSqr = ab.sqrMagnitude;

        if (abSqr < 0.0001f)
        {
            return Vector2.Distance(p, a);
        }

        float t = Vector2.Dot(p - a, ab) / abSqr;
        Vector2 nearest = a + ab * t;
        return Vector2.Distance(p, nearest);
    }

    public static float SignedDistanceToInfiniteLineXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a = new Vector2(lineStart.x, lineStart.z);
        Vector2 b = new Vector2(lineEnd.x, lineEnd.z);
        Vector2 ab = b - a;
        float abMagnitude = ab.magnitude;
        if (abMagnitude < 0.0001f)
        {
            return 0f;
        }

        Vector2 dir = ab / abMagnitude;
        Vector2 rightNormal = new Vector2(dir.y, -dir.x);
        return Vector2.Dot(p - a, rightNormal);
    }
}
