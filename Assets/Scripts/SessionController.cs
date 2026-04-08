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

    public void Complete(
        float currentTime,
        bool trackOffCourse,
        float offCoursePercent,
        float offCourseSeconds,
        float maxLateralDistance,
        float averageLateralDistance,
        string completionTitle = "Session complete!")
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
        float avgSpeedMps = elapsed > 0.001f ? TotalDistanceTraveled / elapsed : 0f;
        float paceSecondsPerMeter = TotalDistanceTraveled > 0.001f ? elapsed / TotalDistanceTraveled : 0f;
        float onCoursePercent = Mathf.Clamp(100f - offCoursePercent, 0f, 100f);

        string offCourseSummary = trackOffCourse
            ? string.Format("Off-course: {0:F0}% ({1:F1}s)", offCoursePercent, offCourseSeconds)
            : "Off-course: N/A";

        string onCourseSummary = trackOffCourse
            ? string.Format("On-course: {0:F0}%", onCoursePercent)
            : "On-course: N/A";

        string driftSummary = trackOffCourse
            ? string.Format("Drift avg/max: {0:F2}m / {1:F2}m", averageLateralDistance, maxLateralDistance)
            : "Drift avg/max: N/A";

        FinalSessionStats = string.Format(
            "{0}\nDistance: {1:F1}m\nTime: {2:00}:{3:00}\nSpeed avg: {4:F2} m/s\nPace: {5:F1} s/m\n{6}\n{7}\n{8}",
            completionTitle,
            TotalDistanceTraveled,
            minutes,
            seconds,
            avgSpeedMps,
            paceSecondsPerMeter,
            onCourseSummary,
            offCourseSummary,
            driftSummary
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
