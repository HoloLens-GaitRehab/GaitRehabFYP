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

    [Header("Room Adaptation")]
    public bool capLineLengthForSmallSpaces = true;
    [Range(2f, 12f)] public float maxLineLengthForSmallSpaces = 4.5f;

    [Header("Line End Marker")]
    public bool enableLineEndMarker = true;
    public Color lineEndMarkerColor = new Color(1f, 0.95f, 0.2f, 0.95f);
    public float lineEndMarkerHeight = 1.35f;
    public float lineEndMarkerWidth = 0.055f;

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
    private SessionController sessionController = new SessionController();
    private MetronomeController metronomeController = new MetronomeController();
    private StraightPathGuide straightPathGuide = new StraightPathGuide();
    private OffCourseTracker offCourseTracker = new OffCourseTracker();
    private DriftArrowController driftArrowController = new DriftArrowController();
    private string sessionStatsStatusLine = "";

    public float CurrentOffCoursePercent { get; private set; }
    public bool IsSessionActive => sessionController.IsActive;
    public bool IsSessionCompleted => sessionController.IsCompleted;
    public bool IsSessionPaused => sessionController.IsPaused;
    public string LastSessionCompletionTitle => sessionController.LastCompletionTitle;
    public float LastSessionElapsedSeconds => sessionController.LastElapsedSeconds;
    public float LastSessionDistanceMeters => sessionController.TotalDistanceTraveled;
    public float LastSessionAverageSpeedMps => sessionController.LastAverageSpeedMps;
    public float LastSessionPaceSecondsPerMeter => sessionController.LastPaceSecondsPerMeter;
    public float LastSessionOnCoursePercent => sessionController.LastOnCoursePercent;
    public float LastSessionOffCoursePercent => sessionController.LastOffCoursePercent;
    public float LastSessionOffCourseSeconds => sessionController.LastOffCourseSeconds;
    public float LastSessionDriftAverageMeters => sessionController.LastAverageLateralDistance;
    public float LastSessionDriftMaxMeters => sessionController.LastMaxLateralDistance;
    
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
        if (sessionController.IsActive)
            return;

        sessionController.Begin(playerCamera.position, Time.time);
        sessionStatsStatusLine = "";
        offCourseTracker.Reset();
        CurrentOffCoursePercent = 0f;

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
        if (!sessionController.IsActive || playerCamera == null)
            return;
        
        sessionController.UpdateDistance(playerCamera.position);

        if (sessionController.IsPaused)
        {
            UpdateStats();
            return;
        }
        
        // Update metronome
        if (enableMetronome)
        {
            metronomeController.Update(playerCamera, GetMetronomeSettings(), GetCurrentOffCourseSeverity());
        }

        UpdateOffCourse(Time.deltaTime);
        straightPathGuide.UpdateRailBrightening(
            sessionController.IsActive,
            playerCamera,
            offCourseTolerance,
            driftArrowShowBuffer,
            GetCurrentOffCourseSeverity(),
            GetStraightPathGuideSettings()
        );
        driftArrowController.Update(
            GetDriftArrowSettings(),
            sessionController.IsActive,
            straightPathGuide.HasPath,
            playerCamera,
            straightPathGuide.LineStart,
            straightPathGuide.LineEnd,
            GetCurrentOffCourseSeverity()
        );

        if (sessionController.HasReachedLineEnd(
            straightPathGuide.HasPath,
            playerCamera.position,
            straightPathGuide.LineStart,
            straightPathGuide.LineEnd,
            lineEndReachDistance,
            lineEndProgressPadding))
        {
            CompleteSession();
            return;
        }
        
        UpdateStats();
    }
    
    void UpdateStats()
    {
        if (statsText == null || !sessionController.IsActive)
            return;

        // Intentionally blank during session to reduce visual clutter.
        statsText.text = "";
    }

    public string GetStatsText()
    {
        string stats = sessionController.GetStatsText();

        if (!sessionController.IsCompleted || string.IsNullOrWhiteSpace(sessionStatsStatusLine))
            return stats;

        if (string.IsNullOrWhiteSpace(stats))
            return sessionStatsStatusLine;

        return stats + "\n" + sessionStatsStatusLine;
    }

    public void SetSessionStatsStatusLine(string statusLine)
    {
        sessionStatsStatusLine = statusLine ?? "";

        if (statsText != null && sessionController.IsCompleted)
        {
            statsText.text = GetStatsText();
        }
    }

    void UpdateOffCourse(float deltaTime)
    {
        if (!straightPathGuide.HasPath || deltaTime <= 0f || playerCamera == null)
            return;

        offCourseTracker.Update(
            deltaTime,
            trackOffCourse,
            offCourseTolerance,
            sessionController.GetEffectiveElapsedTime(Time.time),
            playerCamera.position,
            straightPathGuide.LineStart,
            straightPathGuide.LineEnd
        );

        CurrentOffCoursePercent = offCourseTracker.CurrentOffCoursePercent;
    }

    void CompleteSession()
    {
        CompleteSession("Session complete!");
    }

    void CompleteSession(string completionTitle)
    {
        if (!sessionController.IsActive)
            return;

        sessionController.Complete(
            Time.time,
            trackOffCourse,
            CurrentOffCoursePercent,
            offCourseTracker.OffCourseTimeSeconds,
            offCourseTracker.MaxLateralDistance,
            offCourseTracker.AverageLateralDistance,
            completionTitle
        );

        if (statsText != null)
        {
            statsText.text = GetStatsText();
        }

        metronomeController.SetEnabled(false, showMetronomeArc);
        driftArrowController.SetEnabled(false);
        straightPathGuide.SetRailsEnabled(false);

        Debug.Log("Session completed at end of straight line.");
    }

    public void PauseSession()
    {
        if (!sessionController.Pause(Time.time))
            return;

        metronomeController.SetEnabled(false, showMetronomeArc);
        driftArrowController.SetEnabled(false);
        straightPathGuide.SetRailsEnabled(false);

        Debug.Log("Session paused.");
    }

    public void ResumeSession()
    {
        if (playerCamera == null)
            return;

        if (!sessionController.Resume(Time.time, playerCamera.position))
            return;

        metronomeController.SetEnabled(enableMetronome, showMetronomeArc);
        driftArrowController.SetEnabled(enableDriftDirectionArrow);

        Debug.Log("Session resumed.");
    }

    public void EndSessionEarly()
    {
        CompleteSession("Session ended early.");
    }

    public void ForceCompleteForTesting()
    {
        if (!sessionController.IsActive)
        {
            Debug.LogWarning("ForceCompleteForTesting ignored because no session is active.");
            return;
        }

        CompleteSession("Session complete! (test)");
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
        float effectiveLineLength = Mathf.Max(1f, straightGuideLineLength);
        if (capLineLengthForSmallSpaces)
        {
            effectiveLineLength = Mathf.Min(effectiveLineLength, Mathf.Clamp(maxLineLengthForSmallSpaces, 2f, 12f));
        }

        return new StraightPathGuide.Settings
        {
            straightGuideLineColor = straightGuideLineColor,
            straightGuideLineAlpha = straightGuideLineAlpha,
            straightGuideLineWidth = straightGuideLineWidth,
            straightGuideLineLength = effectiveLineLength,
            straightGuideLineFloorOffset = straightGuideLineFloorOffset,
            enableLineEndMarker = enableLineEndMarker,
            lineEndMarkerColor = lineEndMarkerColor,
            lineEndMarkerHeight = lineEndMarkerHeight,
            lineEndMarkerWidth = lineEndMarkerWidth,
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

        if (sessionController.IsActive && !sessionController.IsPaused)
        {
            metronomeController.StartOrEnable(this, GetMetronomeSettings());
        }
    }
}
