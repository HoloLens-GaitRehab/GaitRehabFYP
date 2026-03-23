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
    private GameObject straightGuideLineObject;
    private LineRenderer straightGuideLine;
    private GameObject leftRailObject;
    private LineRenderer leftRailLine;
    private GameObject rightRailObject;
    private LineRenderer rightRailLine;
    private Vector3 straightLineStart;
    private Vector3 straightLineEnd;
    private bool hasStraightLinePath = false;
    private OffCourseTracker offCourseTracker = new OffCourseTracker();
    private GameObject driftArrowObject;
    private TextMesh driftArrowText;
    private Renderer driftArrowRenderer;

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

        SpawnStraightGuideLine();
        SetupDriftDirectionArrow();
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
        UpdateRailBrightening();
        UpdateDriftDirectionArrow();

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

    void SpawnStraightGuideLine()
    {
        if (playerCamera == null)
            return;

        if (straightGuideLineObject == null)
        {
            straightGuideLineObject = new GameObject("StraightGuideLine");
            straightGuideLine = straightGuideLineObject.AddComponent<LineRenderer>();
            straightGuideLine.useWorldSpace = true;
            straightGuideLine.positionCount = 2;
            straightGuideLine.startWidth = straightGuideLineWidth;
            straightGuideLine.endWidth = straightGuideLineWidth;
            straightGuideLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            straightGuideLine.receiveShadows = false;

            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null)
            {
                lineShader = Shader.Find("Unlit/Color");
            }
            if (lineShader != null)
            {
                straightGuideLine.material = new Material(lineShader);
                straightGuideLine.material.color = GetStraightGuideLineTint();
            }
        }

        Color lineTint = GetStraightGuideLineTint();
        straightGuideLine.startColor = lineTint;
        straightGuideLine.endColor = lineTint;
        if (straightGuideLine.material != null)
        {
            straightGuideLine.material.color = lineTint;
        }

        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = playerCamera.parent != null ? playerCamera.parent.forward : Vector3.forward;
            forward.y = 0f;
        }
        forward.Normalize();

        float floorY = playerCamera.position.y - assumedEyeHeight + straightGuideLineFloorOffset;
        Vector3 start = new Vector3(playerCamera.position.x, floorY, playerCamera.position.z);
        Vector3 end = start + forward * straightGuideLineLength;

        straightLineStart = start;
        straightLineEnd = end;
        hasStraightLinePath = true;

        straightGuideLine.SetPosition(0, start);
        straightGuideLine.SetPosition(1, end);

        SetupConditionalRails();
        UpdateRailGeometry();
        UpdateRailBrightening();
    }

    Color GetStraightGuideLineTint()
    {
        Color tint = straightGuideLineColor;
        tint.a = Mathf.Clamp01(straightGuideLineAlpha);
        return tint;
    }

    void SetupConditionalRails()
    {
        if (!enableConditionalRails)
        {
            if (leftRailObject != null) leftRailObject.SetActive(false);
            if (rightRailObject != null) rightRailObject.SetActive(false);
            return;
        }

        if (leftRailObject == null)
        {
            leftRailObject = new GameObject("LeftGuideRail");
            leftRailLine = leftRailObject.AddComponent<LineRenderer>();
            ConfigureGuideLineRenderer(leftRailLine, railWidth);
        }

        if (rightRailObject == null)
        {
            rightRailObject = new GameObject("RightGuideRail");
            rightRailLine = rightRailObject.AddComponent<LineRenderer>();
            ConfigureGuideLineRenderer(rightRailLine, railWidth);
        }

        leftRailObject.SetActive(true);
        rightRailObject.SetActive(true);
    }

    void ConfigureGuideLineRenderer(LineRenderer line, float width)
    {
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Color");
        }
        if (lineShader != null)
        {
            line.material = new Material(lineShader);
        }
    }

    void UpdateRailGeometry()
    {
        if (!enableConditionalRails || !hasStraightLinePath || leftRailLine == null || rightRailLine == null)
            return;

        Vector3 pathDir = straightLineEnd - straightLineStart;
        pathDir.y = 0f;
        if (pathDir.sqrMagnitude < 0.0001f)
            return;

        pathDir.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, pathDir).normalized;
        float offset = Mathf.Max(0.05f, railSpacing * 0.5f);
        if (railsFollowOffCourseBoundary)
        {
            float boundaryOffset = Mathf.Max(0.05f, Mathf.Max(0.1f, offCourseTolerance) + Mathf.Max(0f, driftArrowShowBuffer));
            offset = Mathf.Max(offset, boundaryOffset);
        }

        Vector3 leftStart = straightLineStart - right * offset;
        Vector3 leftEnd = straightLineEnd - right * offset;
        Vector3 rightStart = straightLineStart + right * offset;
        Vector3 rightEnd = straightLineEnd + right * offset;

        leftRailLine.startWidth = railWidth;
        leftRailLine.endWidth = railWidth;
        rightRailLine.startWidth = railWidth;
        rightRailLine.endWidth = railWidth;

        leftRailLine.SetPosition(0, leftStart);
        leftRailLine.SetPosition(1, leftEnd);
        rightRailLine.SetPosition(0, rightStart);
        rightRailLine.SetPosition(1, rightEnd);
    }

    void UpdateRailBrightening()
    {
        if (!enableConditionalRails || leftRailLine == null || rightRailLine == null)
            return;

        float triggerDistance = Mathf.Max(0.1f, offCourseTolerance) + Mathf.Max(0f, driftArrowShowBuffer);
        float currentDistance = hasStraightLinePath && playerCamera != null
            ? Mathf.Abs(OffCourseTracker.SignedDistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd))
            : 0f;

        bool shouldShowRails = sessionActive && currentDistance > triggerDistance;
        if (leftRailObject != null)
        {
            leftRailObject.SetActive(shouldShowRails);
        }
        if (rightRailObject != null)
        {
            rightRailObject.SetActive(shouldShowRails);
        }

        if (!shouldShowRails)
            return;

        float severity = GetCurrentOffCourseSeverity();
        float alpha = Mathf.Lerp(Mathf.Clamp01(railBaseAlpha), Mathf.Clamp01(railMaxAlpha), severity);

        Color railTint = straightGuideLineColor;
        railTint.a = alpha;

        leftRailLine.startColor = railTint;
        leftRailLine.endColor = railTint;
        rightRailLine.startColor = railTint;
        rightRailLine.endColor = railTint;

        if (leftRailLine.material != null)
        {
            leftRailLine.material.color = railTint;
        }
        if (rightRailLine.material != null)
        {
            rightRailLine.material.color = railTint;
        }
    }

    void UpdateOffCourse(float deltaTime)
    {
        if (!hasStraightLinePath || deltaTime <= 0f || playerCamera == null)
            return;

        offCourseTracker.Update(
            deltaTime,
            trackOffCourse,
            offCourseTolerance,
            sessionStartTime,
            playerCamera.position,
            straightLineStart,
            straightLineEnd,
            Time.time
        );

        CurrentOffCoursePercent = offCourseTracker.CurrentOffCoursePercent;
    }

    void SetupDriftDirectionArrow()
    {
        if (!enableDriftDirectionArrow)
            return;

        if (driftArrowObject == null)
        {
            driftArrowObject = new GameObject("DriftDirectionArrow");
            driftArrowText = driftArrowObject.AddComponent<TextMesh>();
            driftArrowText.text = "←";
            driftArrowText.fontSize = 96;
            driftArrowText.anchor = TextAnchor.MiddleCenter;
            driftArrowText.alignment = TextAlignment.Center;
            driftArrowText.color = driftArrowBaseColor;

            driftArrowRenderer = driftArrowObject.GetComponent<Renderer>();
        }

        driftArrowObject.transform.localScale = Vector3.one * Mathf.Max(0.005f, driftArrowScale);
        driftArrowObject.SetActive(false);
    }

    void UpdateDriftDirectionArrow()
    {
        if (driftArrowObject == null)
        {
            SetupDriftDirectionArrow();
        }

        if (!enableDriftDirectionArrow || !sessionActive || !hasStraightLinePath || playerCamera == null || driftArrowObject == null || driftArrowText == null)
        {
            if (driftArrowObject != null)
            {
                driftArrowObject.SetActive(false);
            }
            return;
        }

        float tolerance = Mathf.Max(0.1f, offCourseTolerance) + Mathf.Max(0f, driftArrowShowBuffer);
        float signedLateralDistance = OffCourseTracker.SignedDistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd);
        float absDistance = Mathf.Abs(signedLateralDistance);

        if (absDistance <= tolerance)
        {
            driftArrowObject.SetActive(false);
            return;
        }

        // Reversed mapping: positive signed distance now shows right arrow.
        driftArrowText.text = signedLateralDistance > 0f ? "→" : "←";

        float severity = GetCurrentOffCourseSeverity();
        Color arrowColor = Color.Lerp(driftArrowBaseColor, Color.red, severity);
        driftArrowText.color = arrowColor;
        if (driftArrowRenderer != null)
        {
            driftArrowRenderer.material.color = arrowColor;
        }

        Vector3 arrowPos = playerCamera.position
                           + playerCamera.forward * driftArrowDistance
                           + playerCamera.up * driftArrowVerticalOffset;
        driftArrowObject.transform.position = arrowPos;

        Vector3 toCamera = playerCamera.position - arrowPos;
        if (toCamera.sqrMagnitude < 0.0001f)
        {
            toCamera = -playerCamera.forward;
        }
        driftArrowObject.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        driftArrowObject.SetActive(true);
    }

    bool HasReachedLineEnd()
    {
        if (!hasStraightLinePath || playerCamera == null)
            return false;

        Vector2 start = new Vector2(straightLineStart.x, straightLineStart.z);
        Vector2 end = new Vector2(straightLineEnd.x, straightLineEnd.z);
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
        if (driftArrowObject != null)
        {
            driftArrowObject.SetActive(false);
        }
        if (leftRailObject != null)
        {
            leftRailObject.SetActive(false);
        }
        if (rightRailObject != null)
        {
            rightRailObject.SetActive(false);
        }

        Debug.Log("Session completed at end of straight line.");
    }
    
    float GetCurrentOffCourseSeverity()
    {
        if (!trackOffCourse || !hasStraightLinePath || playerCamera == null)
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
