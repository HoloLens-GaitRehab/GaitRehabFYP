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
    private GameObject metronomeObject;
    private Renderer metronomeRenderer;
    private float metronomeBeatTime;
    private GameObject metronomeArcObject;
    private LineRenderer metronomeArcLine;
    private GameObject straightGuideLineObject;
    private LineRenderer straightGuideLine;
    private GameObject leftRailObject;
    private LineRenderer leftRailLine;
    private GameObject rightRailObject;
    private LineRenderer rightRailLine;
    private Vector3 straightLineStart;
    private Vector3 straightLineEnd;
    private bool hasStraightLinePath = false;
    private float offCourseTimeSeconds;
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
        CurrentOffCoursePercent = 0f;
        offCourseTimeSeconds = 0f;
        lastCameraPosition = playerCamera.position;

        // Always spawn/show metronome when session starts
        enableMetronome = true;
        if (metronomeObject == null)
        {
            SetupMetronome();
        }
        else
        {
            metronomeObject.SetActive(true);
            if (metronomeArcObject != null)
            {
                metronomeArcObject.SetActive(showMetronomeArc);
            }
        }

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
        if (enableMetronome && metronomeObject != null)
        {
            UpdateMetronome();
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
            ? Mathf.Abs(SignedDistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd))
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
        if (!trackOffCourse || !hasStraightLinePath || deltaTime <= 0f || playerCamera == null)
            return;

        float clampedTolerance = Mathf.Max(0.1f, offCourseTolerance);

        float lateralDistance = DistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd);
        bool isOffCourse = lateralDistance > clampedTolerance;
        if (isOffCourse)
        {
            offCourseTimeSeconds += deltaTime;
        }

        float elapsedTime = Mathf.Max(0.001f, Time.time - sessionStartTime);
        if (elapsedTime > 0f)
        {
            CurrentOffCoursePercent = (offCourseTimeSeconds / elapsedTime) * 100f;
        }
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
        float signedLateralDistance = SignedDistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd);
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

    float DistanceToInfiniteLineXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
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

    float SignedDistanceToInfiniteLineXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
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
            offCourseTimeSeconds,
            minutes,
            seconds
        );

        if (statsText != null)
        {
            statsText.text = finalSessionStats;
        }

        if (metronomeObject != null)
        {
            metronomeObject.SetActive(false);
        }
        if (metronomeArcObject != null)
        {
            metronomeArcObject.SetActive(false);
        }
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
    
    void SetupMetronome()
    {
        // Create metronome visual in front of player at floor level
        if (metronomePrefab != null)
        {
            metronomeObject = Instantiate(metronomePrefab, Vector3.zero, Quaternion.identity);
            metronomeObject.transform.localScale = Vector3.one * 0.02f; // Ensure it's very small (finger-tip sized)
        }
        else
        {
            // Create default sphere
            metronomeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            metronomeObject.transform.localScale = Vector3.one * 0.02f;  // Small 2cm sphere (tip of a finger)
            Destroy(metronomeObject.GetComponent<Collider>());
        }
        
        metronomeObject.name = "Metronome";
        metronomeRenderer = metronomeObject.GetComponent<Renderer>();
        
        if (metronomeRenderer != null)
        {
            metronomeRenderer.material.color = metronomeColor;
            metronomeRenderer.material.EnableKeyword("_EMISSION");
        }

        if (showMetronomeArc)
        {
            SetupMetronomeArc();
        }
        
        float effectiveBpm = Mathf.Max(1f, metronomeBPM * Mathf.Max(0.1f, metronomeSpeedMultiplier));
        metronomeBeatTime = 60f / effectiveBpm;
        Debug.Log("Visual metronome enabled at " + effectiveBpm + " BPM");
    }
    
    void UpdateMetronome()
    {
        // Get center position along full gaze direction (horizontal + vertical)
        Vector3 forward = playerCamera.forward.normalized;
        Vector3 centerPos = playerCamera.position + forward * 1.5f;
        centerPos += playerCamera.up * metronomeHeightOffset;
        
        // Calculate pendulum swing on semicircular arc (slower swing)
        float beatProgress = (Time.time % metronomeBeatTime) / metronomeBeatTime;
        beatProgress = (beatProgress + 0.5f) % 1f; // invert phase (opposite direction)
        float angle = Mathf.Sin(beatProgress * Mathf.PI * 2f) * 45f;  // ±45 degrees swing (reduced from 60)
        
        // Convert angle to radians
        float angleRad = angle * Mathf.Deg2Rad;
        
        // Arc radius (smaller for eye level)
        float arcRadius = 0.3f;
        
        // Get right vector for side-to-side movement relative to current gaze orientation
        Vector3 right = playerCamera.right.normalized;

        if (showMetronomeArc && metronomeArcLine != null)
        {
            UpdateMetronomeArc(centerPos, right, arcRadius);
        }
        
        // Calculate position on arc (x = sin, y = +cos centered at gaze height)
        float xOffset = Mathf.Sin(angleRad) * arcRadius;
        float yOffset = Mathf.Cos(angleRad) * arcRadius;  // Centered at gaze height
        
        // Apply arc position
        Vector3 metronomePos = centerPos + (right * xOffset);
        metronomePos.y = centerPos.y + yOffset;
        metronomeObject.transform.position = metronomePos;
        
        if (metronomeRenderer != null)
        {
            float colorSeverity = GetCurrentOffCourseSeverity();
            Color currentMetronomeColor = Color.Lerp(Color.white, Color.red, colorSeverity);
            metronomeRenderer.material.color = currentMetronomeColor;

            if (showMetronomeArc && metronomeArcLine != null)
            {
                float targetAlpha = Mathf.Clamp01(metronomeArcColor.a);
                Color baseArcColor = new Color(Color.white.r, Color.white.g, Color.white.b, targetAlpha);
                Color offCourseArcColor = new Color(Color.red.r, Color.red.g, Color.red.b, targetAlpha);
                Color currentArcColor = Color.Lerp(baseArcColor, offCourseArcColor, colorSeverity);

                metronomeArcLine.startColor = currentArcColor;
                metronomeArcLine.endColor = currentArcColor;
                if (metronomeArcLine.material != null)
                {
                    metronomeArcLine.material.color = currentArcColor;
                }
            }

            // Brighten at the extremes of the swing
            float intensity = Mathf.Abs(angle) > 35f ? 3f : 1.5f;
            metronomeRenderer.material.SetColor("_EmissionColor", currentMetronomeColor * intensity);
        }
    }

    float GetCurrentOffCourseSeverity()
    {
        if (!trackOffCourse || !hasStraightLinePath || playerCamera == null)
            return 0f;

        float lateralDistance = DistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd);
        float clampedTolerance = Mathf.Max(0.1f, offCourseTolerance);
        float redShiftDistance = Mathf.Max(clampedTolerance + 0.05f, metronomeRedShiftDistance);
        float excess = Mathf.Max(0f, lateralDistance - clampedTolerance);
        return Mathf.Clamp01(excess / (redShiftDistance - clampedTolerance));
    }

    void SetupMetronomeArc()
    {
        metronomeArcObject = new GameObject("MetronomeArc");
        metronomeArcLine = metronomeArcObject.AddComponent<LineRenderer>();
        metronomeArcLine.useWorldSpace = true;
        metronomeArcLine.positionCount = metronomeArcSegments + 1;
        metronomeArcLine.startWidth = metronomeArcWidth;
        metronomeArcLine.endWidth = metronomeArcWidth;
        Shader arcShader = Shader.Find("Sprites/Default");
        if (arcShader == null)
        {
            arcShader = Shader.Find("Unlit/Color");
        }
        if (arcShader != null)
        {
            metronomeArcLine.material = new Material(arcShader);
            metronomeArcLine.material.color = metronomeArcColor;
        }
        metronomeArcLine.startColor = metronomeArcColor;
        metronomeArcLine.endColor = metronomeArcColor;
    }

    void UpdateMetronomeArc(Vector3 centerPos, Vector3 right, float arcRadius)
    {
        float startAngle = -45f;
        float endAngle = 45f;
        float step = (endAngle - startAngle) / metronomeArcSegments;

        for (int i = 0; i <= metronomeArcSegments; i++)
        {
            float angle = startAngle + step * i;
            float angleRad = angle * Mathf.Deg2Rad;
            float xOffset = Mathf.Sin(angleRad) * arcRadius;
            float yOffset = Mathf.Cos(angleRad) * arcRadius;
            Vector3 pos = centerPos + (right * xOffset);
            pos.y = centerPos.y + yOffset;
            metronomeArcLine.SetPosition(i, pos);
        }
    }
    
    public void ToggleMetronome(bool enabled)
    {
        enableMetronome = enabled;
        
        if (metronomeObject != null)
        {
            metronomeObject.SetActive(enabled);
        }
        if (metronomeArcObject != null)
        {
            metronomeArcObject.SetActive(enabled && showMetronomeArc);
        }
        else if (enabled && sessionActive)
        {
            SetupMetronome();
        }
    }
}
