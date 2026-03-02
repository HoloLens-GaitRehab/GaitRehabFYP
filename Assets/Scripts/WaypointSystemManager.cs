using UnityEngine;

public class WaypointSystemManager : MonoBehaviour
{
    [Header("Session Flow")]
    public bool requireManualStart = true;
    
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
    public Color metronomeColor = Color.yellow;
    public float metronomeHeightOffset = -0.3f; // Lower than gaze (meters)
    public bool showMetronomeArc = true;
    public Color metronomeArcColor = new Color(1f, 1f, 0f, 0.5f);
    public float metronomeArcWidth = 0.01f;
    public int metronomeArcSegments = 24;

    [Header("Straight Guide Line")]
    public Color straightGuideLineColor = Color.green;
    [Range(0.05f, 1f)] public float straightGuideLineAlpha = 0.65f;
    public float straightGuideLineWidth = 0.08f;
    public float straightGuideLineLength = 15f;
    public float straightGuideLineFloorOffset = 0.02f;
    public float assumedEyeHeight = 1.6f;
    
    private Transform playerCamera;
    private float sessionStartTime;
    private bool sessionActive = false;
    private float totalDistanceTraveled = 0f;
    private Vector3 lastCameraPosition;
    private GameObject metronomeObject;
    private Renderer metronomeRenderer;
    private float metronomeBeatTime;
    private GameObject metronomeArcObject;
    private LineRenderer metronomeArcLine;
    private GameObject straightGuideLineObject;
    private LineRenderer straightGuideLine;
    
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
        sessionStartTime = Time.time;
        totalDistanceTraveled = 0f;
        lastCameraPosition = playerCamera.position;
        
        // Setup metronome if enabled
        if (enableMetronome)
        {
            SetupMetronome();
        }

        SpawnStraightGuideLine();
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
        
        UpdateStats();
    }
    
    void UpdateStats()
    {
        if (statsText == null || !sessionActive)
            return;
        
        float elapsed = Time.time - sessionStartTime;
        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);
        
        statsText.text = string.Format(
            "Distance: {0:F1}m\nTime: {1:00}:{2:00}",
            totalDistanceTraveled,
            minutes,
            seconds
        );
    }

    public string GetStatsText()
    {
        if (!sessionActive)
        {
            return "Session not started";
        }

        float elapsed = Time.time - sessionStartTime;
        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);

        return string.Format(
            "Distance: {0:F1}m\nTime: {1:00}:{2:00}",
            totalDistanceTraveled,
            minutes,
            seconds
        );
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

        straightGuideLine.SetPosition(0, start);
        straightGuideLine.SetPosition(1, end);
    }

    Color GetStraightGuideLineTint()
    {
        Color tint = straightGuideLineColor;
        tint.a = Mathf.Clamp01(straightGuideLineAlpha);
        return tint;
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
            // Brighten at the extremes of the swing
            float intensity = Mathf.Abs(angle) > 35f ? 3f : 1.5f;
            metronomeRenderer.material.SetColor("_EmissionColor", metronomeColor * intensity);
        }
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
