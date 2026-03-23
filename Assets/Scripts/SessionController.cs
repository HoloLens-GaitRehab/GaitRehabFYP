using UnityEngine;

public class SessionController
{
    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsPaused { get; private set; }
    public string FinalSessionStats { get; private set; } = "";
    public float SessionStartTime { get; private set; }
    public float TotalDistanceTraveled { get; private set; }
    public Vector3 LastCameraPosition { get; private set; }
    private float pauseStartTime;
    private float totalPausedDuration;

    public void Begin(Vector3 initialCameraPosition, float currentTime)
    {
        IsActive = true;
        IsCompleted = false;
        IsPaused = false;
        FinalSessionStats = "";
        SessionStartTime = currentTime;
        TotalDistanceTraveled = 0f;
        LastCameraPosition = initialCameraPosition;
        pauseStartTime = 0f;
        totalPausedDuration = 0f;
    }

    public void UpdateDistance(Vector3 currentCameraPosition)
    {
        if (!IsActive || IsPaused)
            return;

        float frameDistance = Vector3.Distance(currentCameraPosition, LastCameraPosition);
        TotalDistanceTraveled += frameDistance;
        LastCameraPosition = currentCameraPosition;
    }

    public bool Pause(float currentTime)
    {
        if (!IsActive || IsPaused)
            return false;

        IsPaused = true;
        pauseStartTime = currentTime;
        return true;
    }

    public bool Resume(float currentTime, Vector3 currentCameraPosition)
    {
        if (!IsActive || !IsPaused)
            return false;

        totalPausedDuration += Mathf.Max(0f, currentTime - pauseStartTime);
        IsPaused = false;
        LastCameraPosition = currentCameraPosition;
        return true;
    }

    public float GetEffectiveElapsedTime(float currentTime)
    {
        if (!IsActive && !IsCompleted)
            return 0f;

        float activePauseDuration = IsPaused ? Mathf.Max(0f, currentTime - pauseStartTime) : 0f;
        float elapsed = currentTime - SessionStartTime - totalPausedDuration - activePauseDuration;
        return Mathf.Max(0f, elapsed);
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

    public void Complete(float currentTime, float offCoursePercent, float offCourseSeconds, string completionTitle = "Session complete!")
    {
        if (!IsActive)
            return;

        if (IsPaused)
        {
            totalPausedDuration += Mathf.Max(0f, currentTime - pauseStartTime);
        }

        IsActive = false;
        IsCompleted = true;
        IsPaused = false;

        float elapsed = GetEffectiveElapsedTime(currentTime);
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);

        FinalSessionStats = string.Format(
            "{0}\nDistance: {1:F1}m\nOff-course: {2:F0}% ({3:F1}s)\nTime: {4:00}:{5:00}",
            completionTitle,
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

        if (IsPaused)
            return "Session paused";

        if (!IsActive)
            return "Session not started";

        return "";
    }
}
