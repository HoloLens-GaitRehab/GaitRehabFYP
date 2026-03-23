using UnityEngine;

public class SessionController
{
    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public string FinalSessionStats { get; private set; } = "";
    public float SessionStartTime { get; private set; }
    public float TotalDistanceTraveled { get; private set; }
    public Vector3 LastCameraPosition { get; private set; }

    public void Begin(Vector3 initialCameraPosition, float currentTime)
    {
        IsActive = true;
        IsCompleted = false;
        FinalSessionStats = "";
        SessionStartTime = currentTime;
        TotalDistanceTraveled = 0f;
        LastCameraPosition = initialCameraPosition;
    }

    public void UpdateDistance(Vector3 currentCameraPosition)
    {
        if (!IsActive)
            return;

        float frameDistance = Vector3.Distance(currentCameraPosition, LastCameraPosition);
        TotalDistanceTraveled += frameDistance;
        LastCameraPosition = currentCameraPosition;
    }

    public bool HasReachedLineEnd(
        bool hasPath,
        Vector3 cameraPosition,
        Vector3 lineStart,
        Vector3 lineEnd,
        float lineEndReachDistance,
        float lineEndProgressPadding)
    {
        if (!hasPath)
            return false;

        Vector2 start = new Vector2(lineStart.x, lineStart.z);
        Vector2 end = new Vector2(lineEnd.x, lineEnd.z);
        Vector2 current = new Vector2(cameraPosition.x, cameraPosition.z);
        Vector2 path = end - start;

        float pathLength = path.magnitude;
        if (pathLength < 0.001f)
            return false;

        Vector2 pathDir = path / pathLength;
        float alongDistance = Vector2.Dot(current - start, pathDir);
        float distanceToEnd = Vector2.Distance(current, end);

        bool nearEnd = distanceToEnd <= Mathf.Max(0.1f, lineEndReachDistance);
        bool passedEnd = alongDistance >= pathLength - Mathf.Max(0f, lineEndProgressPadding);

        return nearEnd || passedEnd;
    }

    public void Complete(float currentTime, float offCoursePercent, float offCourseSeconds)
    {
        if (!IsActive)
            return;

        IsActive = false;
        IsCompleted = true;

        float elapsed = Mathf.Max(0f, currentTime - SessionStartTime);
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);

        FinalSessionStats = string.Format(
            "Session complete!\nDistance: {0:F1}m\nOff-course: {1:F0}% ({2:F1}s)\nTime: {3:00}:{4:00}",
            TotalDistanceTraveled,
            offCoursePercent,
            offCourseSeconds,
            minutes,
            seconds
        );
    }

    public string GetStatsText()
    {
        if (IsCompleted)
            return FinalSessionStats;

        if (!IsActive)
            return "Session not started";

        return "";
    }
}
