using UnityEngine;

public class WaypointSystemManager : MonoBehaviour
{
    [Header("Session Flow")]
    public bool requireManualStart = true;
    public float lineEndReachDistance = 0.6f;
    public float lineEndProgressPadding = 0.2f;
    
    [Header("Stats Display (Optional)")]
    public TextMesh statsText;
    public bool autoCreateStatsText = true;
    public float statsDistanceFromCamera = 2.0f;
    public float statsHeightOffset = -0.15f;
    public float statsTextScale = 0.012f;
    public bool hideStatsText = false;
    
    [Header("Visual Metronome")]
    public bool enableMetronome = false;
    public GameObject metronomePrefab;  // Assign a small sphere/cube
    public float metronomeBPM = 40f;    // Beats per minute (slower default - adjust to your preference)
    [Range(0.25f, 1.5f)] public float metronomeSpeedMultiplier = 0.75f;
    public Color metronomeColor = Color.white;
    public float metronomeHeightOffset = -0.3f; // Lower than gaze (meters)
    public bool showMetronomeArc = true;
    public Color metronomeArcColor = new Color(1f, 1f, 0f, 0.5f);
    public float metronomeArcWidth = 0.01f;
    public int metronomeArcSegments = 24;
    public float metronomeRedShiftDistance = 1.0f;
    public AudioClip metronomeTickClip;
    [Range(0f, 1f)] public float metronomeTickVolume = 0.2f;
    public bool useGeneratedTickFallback = true;
    public float generatedTickFrequencyHz = 1300f;
    public float generatedTickDurationSeconds = 0.03f;

    [Header("Straight Guide Line")]
    public Color straightGuideLineColor = Color.green;
    [Range(0.05f, 1f)] public float straightGuideLineAlpha = 0.65f;
    public float straightGuideLineWidth = 0.08f;
    public float straightGuideLineLength = 15f;
    public float straightGuideLineFloorOffset = 0.02f;
    public float assumedEyeHeight = 1.6f;

    [Header("Conditional Rails")]
    public bool enableConditionalRails = true;
    public float railSpacing = 0.45f;
    public float railWidth = 0.05f;
    [Range(0.02f, 0.6f)] public float railBaseAlpha = 0.12f;
    [Range(0.1f, 1f)] public float railMaxAlpha = 0.9f;
    public bool railsFollowOffCourseBoundary = true;

    [Header("Off-course Tracking")]
    public bool trackOffCourse = true;
    public float offCourseTolerance = 0.25f;

    [Header("Drift Direction Arrow")]
    public bool enableDriftDirectionArrow = true;
    public float driftArrowShowBuffer = 0.03f;
    public float driftArrowDistance = 1.1f;
    public float driftArrowVerticalOffset = -0.06f;
    public float driftArrowScale = 0.02f;
    public Color driftArrowBaseColor = Color.white;
    
    private Transform playerCamera;
    private float sessionStartTime;
    private bool sessionActive = false;
    private bool sessionCompleted = false;
    private string finalSessionStats = "";
    private float totalDistanceTraveled = 0f;
    private Vector3 lastCameraPosition;
    private MetronomeController metronomeController = new MetronomeController();
    private StraightPathGuide straightPathGuide = new StraightPathGuide();
    private OffCourseTracker offCourseTracker = new OffCourseTracker();
    private DriftArrowController driftArrowController = new DriftArrowController();

    public float CurrentOffCoursePercent { get; private set; }
    
    void Start()
    {
        // Get the player camera (MRTK's Main Camera)
        playerCamera = Camera.main?.transform;
        if (playerCamera == null)
        {
            Debug.LogError("WaypointSystemManager: Main Camera not found!");
            return;
        }

        if (statsText == null && autoCreateStatsText)
        {
            CreateStatsText();
        }

        if (statsText != null)
        {
            statsText.gameObject.SetActive(!hideStatsText);
        }
        
        if (!requireManualStart)
        {
            Invoke("StartSession", 1f);
        }
    }
    
    void StartSession()
    {
        if (sessionActive)
            return;

        sessionActive = true;
        sessionCompleted = false;
        finalSessionStats = "";
        sessionStartTime = Time.time;
        totalDistanceTraveled = 0f;
        offCourseTracker.Reset();
        CurrentOffCoursePercent = 0f;
        lastCameraPosition = playerCamera.position;

        // Always spawn/show metronome when session starts
        enableMetronome = true;
        metronomeController.StartOrEnable(this, GetMetronomeSettings());

        straightPathGuide.StartOrUpdate(
            playerCamera,
            GetStraightPathGuideSettings(),
            assumedEyeHeight,
            offCourseTolerance,
            driftArrowShowBuffer,
            0f
        );
        driftArrowController.StartOrEnable(GetDriftArrowSettings());
        if (statsText != null)
        {
            statsText.text = "";
        }
        Debug.Log("Straight-line session started.");
    }

    public void StartSessionFromButton()
    {
        StartSession();
    }

    void CreateStatsText()
    {
        GameObject statsObj = new GameObject("StatsText");
        statsText = statsObj.AddComponent<TextMesh>();
        statsText.fontSize = 48;
        statsText.color = Color.white;
        statsText.alignment = TextAlignment.Center;
        statsText.anchor = TextAnchor.MiddleCenter;
        statsObj.transform.localScale = Vector3.one * statsTextScale;

        if (playerCamera != null)
        {
            statsObj.transform.SetParent(playerCamera, false);
            statsObj.transform.localPosition = new Vector3(0f, statsHeightOffset, statsDistanceFromCamera);
            statsObj.transform.localRotation = Quaternion.identity;
        }
    }
    
    void Update()
    {
        if (!sessionActive || playerCamera == null)
            return;
        
        // Track distance traveled
        float frameDistance = Vector3.Distance(playerCamera.position, lastCameraPosition);
        totalDistanceTraveled += frameDistance;
        lastCameraPosition = playerCamera.position;
        
        // Update metronome
        if (enableMetronome)
        {
            metronomeController.Update(playerCamera, GetMetronomeSettings(), GetCurrentOffCourseSeverity());
        }

        UpdateOffCourse(Time.deltaTime);
        straightPathGuide.UpdateRailBrightening(
            sessionActive,
            playerCamera,
            offCourseTolerance,
            driftArrowShowBuffer,
            GetCurrentOffCourseSeverity()
        );
        driftArrowController.Update(
            GetDriftArrowSettings(),
            sessionActive,
            straightPathGuide.HasPath,
            playerCamera,
            straightPathGuide.LineStart,
            straightPathGuide.LineEnd,
            GetCurrentOffCourseSeverity()
        );

        if (HasReachedLineEnd())
        {
            CompleteSession();
            return;
        }
        
        UpdateStats();
    }
    
    void UpdateStats()
    {
        if (statsText == null || !sessionActive)
            return;

        // Intentionally blank during session to reduce visual clutter.
        statsText.text = "";
    }

    public string GetStatsText()
    {
        if (sessionCompleted)
        {
            return finalSessionStats;
        }

        if (!sessionActive)
        {
            return "Session not started";
        }

        return "";
    }

    void UpdateOffCourse(float deltaTime)
    {
        if (!straightPathGuide.HasPath || deltaTime <= 0f || playerCamera == null)
            return;

        offCourseTracker.Update(
            deltaTime,
            trackOffCourse,
            offCourseTolerance,
            sessionStartTime,
            playerCamera.position,
            straightPathGuide.LineStart,
            straightPathGuide.LineEnd,
            Time.time
        );

        CurrentOffCoursePercent = offCourseTracker.CurrentOffCoursePercent;
    }

    bool HasReachedLineEnd()
    {
        if (!straightPathGuide.HasPath || playerCamera == null)
            return false;

        Vector2 start = new Vector2(straightPathGuide.LineStart.x, straightPathGuide.LineStart.z);
        Vector2 end = new Vector2(straightPathGuide.LineEnd.x, straightPathGuide.LineEnd.z);
        Vector2 current = new Vector2(playerCamera.position.x, playerCamera.position.z);
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

    void CompleteSession()
    {
        if (!sessionActive)
            return;

        sessionActive = false;
        sessionCompleted = true;

        float elapsed = Mathf.Max(0f, Time.time - sessionStartTime);
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);

        finalSessionStats = string.Format(
            "Session complete!\nDistance: {0:F1}m\nOff-course: {1:F0}% ({2:F1}s)\nTime: {3:00}:{4:00}",
            totalDistanceTraveled,
            CurrentOffCoursePercent,
            offCourseTracker.OffCourseTimeSeconds,
            minutes,
            seconds
        );

        if (statsText != null)
        {
            statsText.text = finalSessionStats;
        }

        metronomeController.SetEnabled(false, showMetronomeArc);
        driftArrowController.SetEnabled(false);
        straightPathGuide.SetRailsEnabled(false);

        Debug.Log("Session completed at end of straight line.");
    }
    
    float GetCurrentOffCourseSeverity()
    {
        if (!trackOffCourse || !straightPathGuide.HasPath || playerCamera == null)
            return 0f;

        return offCourseTracker.GetSeverity(offCourseTolerance, metronomeRedShiftDistance);
    }

    MetronomeController.Settings GetMetronomeSettings()
    {
        return new MetronomeController.Settings
        {
            metronomePrefab = metronomePrefab,
            metronomeBPM = metronomeBPM,
            metronomeSpeedMultiplier = metronomeSpeedMultiplier,
            metronomeColor = metronomeColor,
            metronomeHeightOffset = metronomeHeightOffset,
            showMetronomeArc = showMetronomeArc,
            metronomeArcColor = metronomeArcColor,
            metronomeArcWidth = metronomeArcWidth,
            metronomeArcSegments = metronomeArcSegments,
            metronomeTickClip = metronomeTickClip,
            metronomeTickVolume = metronomeTickVolume,
            useGeneratedTickFallback = useGeneratedTickFallback,
            generatedTickFrequencyHz = generatedTickFrequencyHz,
            generatedTickDurationSeconds = generatedTickDurationSeconds
        };
    }

    DriftArrowController.Settings GetDriftArrowSettings()
    {
        return new DriftArrowController.Settings
        {
            enableDriftDirectionArrow = enableDriftDirectionArrow,
            driftArrowShowBuffer = driftArrowShowBuffer,
            driftArrowDistance = driftArrowDistance,
            driftArrowVerticalOffset = driftArrowVerticalOffset,
            driftArrowScale = driftArrowScale,
            driftArrowBaseColor = driftArrowBaseColor,
            offCourseTolerance = offCourseTolerance
        };
    }

    StraightPathGuide.Settings GetStraightPathGuideSettings()
    {
        return new StraightPathGuide.Settings
        {
            straightGuideLineColor = straightGuideLineColor,
            straightGuideLineAlpha = straightGuideLineAlpha,
            straightGuideLineWidth = straightGuideLineWidth,
            straightGuideLineLength = straightGuideLineLength,
            straightGuideLineFloorOffset = straightGuideLineFloorOffset,
            enableConditionalRails = enableConditionalRails,
            railSpacing = railSpacing,
            railWidth = railWidth,
            railBaseAlpha = railBaseAlpha,
            railMaxAlpha = railMaxAlpha,
            railsFollowOffCourseBoundary = railsFollowOffCourseBoundary
        };
    }

    public void ToggleMetronome(bool enabled)
    {
        enableMetronome = enabled;

        if (!enabled)
        {
            metronomeController.SetEnabled(false, showMetronomeArc);
            return;
        }

        if (sessionActive)
        {
            metronomeController.StartOrEnable(this, GetMetronomeSettings());
        }
    }
}
