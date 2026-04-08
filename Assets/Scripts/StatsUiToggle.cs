using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

public class StatsUiToggle : MonoBehaviour
{
    [Header("References")]
    public WaypointSystemManager waypointManager;

    [Header("UI Layout")]
    public float uiDistance = 1.0f;
    public Vector3 uiOffset = new Vector3(0.35f, -0.25f, 0f); // bottom-right of view
    public Vector2 panelSize = new Vector2(340f, 260f);
    public Vector2 buttonSize = new Vector2(180f, 60f);
    public float followSpeed = 8f;
    public float rotateSpeed = 10f;
    public float followAngleThreshold = 10f; // degrees
    public float followDistanceThreshold = 0.15f; // meters
    public bool followPositionOnly = true;

    [Header("MRTK Buttons")]
    public bool useMrtkButtons = true;
    public GameObject mrtkButtonPrefab;
    public Vector3 statsButtonLocalPos = new Vector3(0f, -0.07f, 0f);
    public Vector3 metronomeButtonLocalPos = new Vector3(0f, -0.16f, 0f);
    public Vector3 mrtkButtonScale = new Vector3(0.06f, 0.06f, 0.06f);

    [Header("Start Session Button")]
    public bool showStartSessionButton = true;
    public float startButtonDistance = 1.0f;
    public Vector3 startButtonOffset = new Vector3(0f, 0.2f, 0f);
    public bool followStartButtonVerticalGaze = false;
    public float startButtonMinHeightAboveEyes = 0.4f;
    public bool hideStartButtonAfterStart = true;

    [Header("Hands-Free Start (Dwell)")]
    public bool useDwellStart = true;
    public float startDwellSeconds = 1.5f;
    public float startGazeAngleThreshold = 8f;
    public float startGazeMaxDistance = 2.5f;
    public float dwellBarWidth = 0.16f;
    public float dwellBarHeight = 0.015f;
    public float dwellBarDepth = 0.005f;
    public float dwellBarVerticalOffset = -0.07f;
    public Color dwellBarBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
    public Color dwellBarFillColor = new Color(0.2f, 0.9f, 0.2f, 0.95f);

    [Header("Hands-Free Session Controls")]
    public bool useDwellSessionControls = true;
    [Range(0.5f, 3f)] public float sessionControlDwellSeconds = 1.1f;
    [Range(1f, 5f)] public float sessionEndDwellSeconds = 2.4f;
    [Range(8f, 60f)] public float sessionControlLookUpAngle = 25f;
    [Range(0f, 2f)] public float sessionControlCooldownSeconds = 0.8f;
    public bool requirePauseBeforeEnd = true;
    public float sessionControlBarDistance = 0.9f;
    public float sessionControlBarVerticalOffset = 0.12f;
    public float sessionControlBarWidth = 0.16f;
    public float sessionControlBarHeight = 0.014f;
    public float sessionControlBarDepth = 0.005f;
    public Color sessionControlBarBackgroundColor = new Color(0f, 0f, 0f, 0.78f);
    public Color sessionControlBarPauseColor = new Color(1f, 0.8f, 0.2f, 0.95f);
    public Color sessionControlBarResumeColor = new Color(0.2f, 0.9f, 0.35f, 0.95f);
    public Color sessionControlBarEndColor = new Color(1f, 0.3f, 0.3f, 0.95f);

    [Header("Completion Overlay")]
    public bool autoShowCompletionOverlay = true;
    public float completionOverlayDistance = 3.3f;
    public float completionOverlayVerticalOffset = 0.02f;
    public Vector2 completionOverlaySize = new Vector2(760f, 440f);
    public float completionOverlayScale = 0.00135f;
    public Color completionOverlayBackgroundColor = new Color(0.03f, 0.07f, 0.12f, 0.9f);
    public Color completionOverlayAccentColor = new Color(0.16f, 0.86f, 0.66f, 0.95f);
    public Color completionOverlayTextColor = new Color(0.94f, 0.97f, 1f, 1f);
    public int completionTitleFontSize = 46;
    public int completionBodyFontSize = 34;
    public int completionTitleMinFontSize = 24;
    public int completionBodyMinFontSize = 18;

    private GameObject canvasObj;
    private GameObject panelObj;
    private Text panelText;
    private Button toggleButton;
    private Button metronomeButton;
    private Text metronomeButtonText;
    private Button startSessionFallbackButton;
    private GameObject startSessionFallbackCanvas;
    private GameObject mrtkStatsButton;
    private GameObject mrtkMetronomeButton;
    private GameObject startSessionButton;
    private GameObject startDwellBarRoot;
    private Transform startDwellBarFill;
    private Renderer startDwellBarBgRenderer;
    private Renderer startDwellBarFillRenderer;
    private float startDwellTimer;
    private bool startTriggered;
    private bool waitingForNextSession;
    private CompletionOverlayController completionOverlayController = new CompletionOverlayController();
    private SessionDwellControlController sessionDwellControlController = new SessionDwellControlController();
    private VoiceCommandController voiceCommandController = new VoiceCommandController();
    private Transform cameraTransform;
    private Vector3 followVelocity;
    private Vector3 worldOffset;
    private Quaternion fixedRotation;

    void Start()
    {
        if (waypointManager == null)
        {
            waypointManager = FindObjectOfType<WaypointSystemManager>();
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        CreateUi();
        EnsureCompletionOverlay();
        TryCreateStartSessionButton();
        SetupVoiceCommands();
    }

    void CreateUi()
    {
        // Canvas
        canvasObj = new GameObject("StatsUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Place UI in front of camera (world space) and smooth follow in LateUpdate
        if (cameraTransform != null)
        {
            canvasObj.transform.position = GetTargetPosition();
            canvasObj.transform.rotation = GetTargetRotation();
            canvasObj.transform.localScale = Vector3.one * 0.0022f; // scale for world-space UI
        }

        if (cameraTransform != null)
        {
            worldOffset = canvasObj.transform.position - cameraTransform.position;
            fixedRotation = canvasObj.transform.rotation;
        }

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 300);

        // Panel
        panelObj = new GameObject("StatsPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = new Vector2(0f, 60f);

        // Panel text
        GameObject textObj = new GameObject("StatsText");
        textObj.transform.SetParent(panelObj.transform, false);
        panelText = textObj.AddComponent<Text>();
        panelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        panelText.fontSize = 24;
        panelText.color = Color.white;
        panelText.alignment = TextAnchor.MiddleCenter;
        panelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        panelText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform textRect = panelText.GetComponent<RectTransform>();
        textRect.sizeDelta = panelSize - new Vector2(20f, 20f);
        textRect.anchoredPosition = Vector2.zero;

        if (useMrtkButtons && mrtkButtonPrefab != null)
        {
            mrtkStatsButton = CreateMrtkButton("StatsButton", statsButtonLocalPos, "Stats", TogglePanel);
            mrtkMetronomeButton = CreateMrtkButton("MetronomeButton", metronomeButtonLocalPos, "Metronome: Off", ToggleMetronome);
        }
        else
        {
            // Unity UI fallback
            GameObject buttonObj = new GameObject("StatsToggleButton");
            buttonObj.transform.SetParent(canvasObj.transform, false);
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0f, 0.6f, 0.1f, 0.9f);
            toggleButton = buttonObj.AddComponent<Button>();
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = buttonSize;
            buttonRect.anchoredPosition = new Vector2(0f, -40f);

            // Button label
            GameObject btnTextObj = new GameObject("ButtonText");
            btnTextObj.transform.SetParent(buttonObj.transform, false);
            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 30;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = "Stats";
            RectTransform btnTextRect = btnText.GetComponent<RectTransform>();
            btnTextRect.sizeDelta = buttonSize;
            btnTextRect.anchoredPosition = Vector2.zero;

            toggleButton.onClick.AddListener(TogglePanel);

            // Metronome toggle button
            GameObject metroBtnObj = new GameObject("MetronomeToggleButton");
            metroBtnObj.transform.SetParent(canvasObj.transform, false);
            Image metroBtnImage = metroBtnObj.AddComponent<Image>();
            metroBtnImage.color = new Color(0.1f, 0.3f, 0.8f, 0.9f);
            metronomeButton = metroBtnObj.AddComponent<Button>();
            RectTransform metroBtnRect = metroBtnObj.GetComponent<RectTransform>();
            metroBtnRect.sizeDelta = buttonSize;
            metroBtnRect.anchoredPosition = new Vector2(0f, -95f);

            // Metronome button label
            GameObject metroTextObj = new GameObject("MetronomeButtonText");
            metroTextObj.transform.SetParent(metroBtnObj.transform, false);
            metronomeButtonText = metroTextObj.AddComponent<Text>();
            metronomeButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            metronomeButtonText.fontSize = 26;
            metronomeButtonText.color = Color.white;
            metronomeButtonText.alignment = TextAnchor.MiddleCenter;
            metronomeButtonText.text = "Metronome: Off";
            RectTransform metroTextRect = metronomeButtonText.GetComponent<RectTransform>();
            metroTextRect.sizeDelta = buttonSize;
            metroTextRect.anchoredPosition = Vector2.zero;

            metronomeButton.onClick.AddListener(ToggleMetronome);
        }

        // Start hidden
        panelObj.SetActive(false);
    }

    void Update()
    {
        UpdateStartButtonSessionCycle();
        UpdateStartDwellActivation();
        UpdateSessionDwellControls();
        UpdateCompletionOverlayState();

        if (panelObj == null || panelText == null)
            return;

        if (panelObj.activeSelf && waypointManager != null)
        {
            panelText.text = waypointManager.GetStatsText();
        }

        if (metronomeButtonText != null && waypointManager != null)
        {
            metronomeButtonText.text = waypointManager.enableMetronome
                ? "Metronome: On"
                : "Metronome: Off";
        }

        if (mrtkMetronomeButton != null && waypointManager != null)
        {
            SetMrtkButtonLabel(mrtkMetronomeButton, waypointManager.enableMetronome ? "Metronome: On" : "Metronome: Off");
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        if (startSessionButton == null && startSessionFallbackButton == null)
        {
            TryCreateStartSessionButton();
        }

        if (canvasObj == null || cameraTransform == null)
            return;

        UpdateCompletionOverlayPose();

        UpdateStartSessionButtonPose();

        Vector3 targetPos = followPositionOnly ? cameraTransform.position + worldOffset : GetTargetPosition();
        Quaternion targetRot = followPositionOnly ? fixedRotation : GetTargetRotation();

        float angleToTarget = Vector3.Angle(cameraTransform.forward, targetPos - cameraTransform.position);
        float distanceToTarget = Vector3.Distance(canvasObj.transform.position, targetPos);

        // Only move the UI if it's outside a small deadzone
        if (angleToTarget > followAngleThreshold || distanceToTarget > followDistanceThreshold)
        {
            canvasObj.transform.position = Vector3.SmoothDamp(
                canvasObj.transform.position,
                targetPos,
                ref followVelocity,
                1f / Mathf.Max(0.01f, followSpeed)
            );

            if (!followPositionOnly)
            {
                canvasObj.transform.rotation = Quaternion.Slerp(
                    canvasObj.transform.rotation,
                    targetRot,
                    Time.deltaTime * rotateSpeed
                );
            }
        }
    }

    Vector3 GetTargetPosition()
    {
        return cameraTransform.position
               + cameraTransform.forward * uiDistance
               + cameraTransform.right * uiOffset.x
               + cameraTransform.up * uiOffset.y
               + cameraTransform.forward * uiOffset.z;
    }

    Quaternion GetTargetRotation()
    {
        Vector3 toCamera = cameraTransform.position - canvasObj.transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f)
        {
            toCamera = -cameraTransform.forward;
        }
        return Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    CompletionOverlayController.Settings GetCompletionOverlaySettings()
    {
        return new CompletionOverlayController.Settings
        {
            autoShowCompletionOverlay = autoShowCompletionOverlay,
            completionOverlayDistance = completionOverlayDistance,
            completionOverlayVerticalOffset = completionOverlayVerticalOffset,
            completionOverlaySize = completionOverlaySize,
            completionOverlayScale = completionOverlayScale,
            completionOverlayBackgroundColor = completionOverlayBackgroundColor,
            completionOverlayAccentColor = completionOverlayAccentColor,
            completionOverlayTextColor = completionOverlayTextColor,
            completionTitleFontSize = completionTitleFontSize,
            completionBodyFontSize = completionBodyFontSize,
            completionTitleMinFontSize = completionTitleMinFontSize,
            completionBodyMinFontSize = completionBodyMinFontSize
        };
    }

    SessionDwellControlController.Settings GetSessionDwellControlSettings()
    {
        return new SessionDwellControlController.Settings
        {
            useDwellSessionControls = useDwellSessionControls,
            sessionControlDwellSeconds = sessionControlDwellSeconds,
            sessionEndDwellSeconds = sessionEndDwellSeconds,
            sessionControlLookUpAngle = sessionControlLookUpAngle,
            sessionControlCooldownSeconds = sessionControlCooldownSeconds,
            requirePauseBeforeEnd = requirePauseBeforeEnd,
            sessionControlBarDistance = sessionControlBarDistance,
            sessionControlBarVerticalOffset = sessionControlBarVerticalOffset,
            sessionControlBarWidth = sessionControlBarWidth,
            sessionControlBarHeight = sessionControlBarHeight,
            sessionControlBarDepth = sessionControlBarDepth,
            sessionControlBarBackgroundColor = sessionControlBarBackgroundColor,
            sessionControlBarPauseColor = sessionControlBarPauseColor,
            sessionControlBarResumeColor = sessionControlBarResumeColor,
            sessionControlBarEndColor = sessionControlBarEndColor
        };
    }

    void TogglePanel()
    {
        if (panelObj == null)
            return;

        panelObj.SetActive(!panelObj.activeSelf);
    }

    void ToggleMetronome()
    {
        if (waypointManager == null)
            return;

        bool newState = !waypointManager.enableMetronome;
        waypointManager.ToggleMetronome(newState);
    }

    void TryCreateStartSessionButton()
    {
        if (!showStartSessionButton || cameraTransform == null)
            return;

        if (useMrtkButtons && mrtkButtonPrefab != null)
        {
            CreateMrtkStartSessionButton();
            return;
        }

        CreateFallbackStartButton();
    }

    void CreateMrtkStartSessionButton()
    {
        if (startSessionButton != null)
            return;

        startSessionButton = Instantiate(mrtkButtonPrefab);
        startSessionButton.name = "StartSessionButton";
        startSessionButton.transform.localScale = mrtkButtonScale;

        Interactable interactable = startSessionButton.GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.OnClick.RemoveAllListeners();
            if (!useDwellStart)
            {
                interactable.OnClick.AddListener(OnStartSessionPressed);
            }
        }

        SetMrtkButtonLabel(startSessionButton, useDwellStart ? "Look & Hold" : "Start");
        UpdateStartSessionButtonPose();
    }

    void CreateFallbackStartButton()
    {
        if (startSessionFallbackButton != null || cameraTransform == null)
            return;

        startSessionFallbackCanvas = new GameObject("StartSessionCanvas");
        Canvas fallbackCanvas = startSessionFallbackCanvas.AddComponent<Canvas>();
        fallbackCanvas.renderMode = RenderMode.WorldSpace;
        startSessionFallbackCanvas.AddComponent<CanvasScaler>();
        startSessionFallbackCanvas.AddComponent<GraphicRaycaster>();
        startSessionFallbackCanvas.transform.localScale = Vector3.one * 0.0022f;

        RectTransform canvasRect = startSessionFallbackCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300f, 180f);

        GameObject buttonObj = new GameObject("StartSessionButtonFallback");
        buttonObj.transform.SetParent(startSessionFallbackCanvas.transform, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.7f, 0.2f, 0.95f);

        startSessionFallbackButton = buttonObj.AddComponent<Button>();
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = buttonSize;
        buttonRect.anchoredPosition = new Vector2(0f, 20f);

        GameObject labelObj = new GameObject("StartText");
        labelObj.transform.SetParent(buttonObj.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 30;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "Start";
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.sizeDelta = buttonSize;
        labelRect.anchoredPosition = Vector2.zero;

        if (!useDwellStart)
        {
            startSessionFallbackButton.onClick.AddListener(OnStartSessionPressed);
        }
        UpdateStartSessionButtonPose();
    }

    void EnsureStartDwellBar()
    {
        if (!useDwellStart || startDwellBarRoot != null)
            return;

        startDwellBarRoot = new GameObject("StartDwellProgressBar");

        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bgObj.name = "DwellBarBackground";
        Destroy(bgObj.GetComponent<Collider>());
        bgObj.transform.SetParent(startDwellBarRoot.transform, false);
        bgObj.transform.localScale = new Vector3(dwellBarWidth, dwellBarHeight, dwellBarDepth);
        startDwellBarBgRenderer = bgObj.GetComponent<Renderer>();

        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fillObj.name = "DwellBarFill";
        Destroy(fillObj.GetComponent<Collider>());
        fillObj.transform.SetParent(startDwellBarRoot.transform, false);
        startDwellBarFill = fillObj.transform;
        startDwellBarFill.localScale = new Vector3(0.0001f, dwellBarHeight * 0.8f, dwellBarDepth * 0.8f);
        startDwellBarFill.localPosition = new Vector3(-dwellBarWidth * 0.5f, 0f, -0.001f);
        startDwellBarFillRenderer = fillObj.GetComponent<Renderer>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader != null)
        {
            if (startDwellBarBgRenderer != null)
            {
                startDwellBarBgRenderer.material = new Material(shader);
                startDwellBarBgRenderer.material.color = dwellBarBackgroundColor;
            }
            if (startDwellBarFillRenderer != null)
            {
                startDwellBarFillRenderer.material = new Material(shader);
                startDwellBarFillRenderer.material.color = dwellBarFillColor;
            }
        }

        startDwellBarRoot.SetActive(false);
    }

    Transform GetActiveStartTarget()
    {
        if (startSessionButton != null && startSessionButton.activeSelf)
            return startSessionButton.transform;

        if (startSessionFallbackCanvas != null && startSessionFallbackCanvas.activeSelf)
            return startSessionFallbackCanvas.transform;

        return null;
    }

    void UpdateStartDwellActivation()
    {
        if (!useDwellStart || startTriggered)
            return;

        if (cameraTransform == null)
            return;

        Transform target = GetActiveStartTarget();
        EnsureStartDwellBar();

        if (target == null)
        {
            startDwellTimer = 0f;
            if (startDwellBarRoot != null)
            {
                startDwellBarRoot.SetActive(false);
            }
            return;
        }

        Vector3 toTarget = target.position - cameraTransform.position;
        float distance = toTarget.magnitude;
        float angle = distance > 0.001f ? Vector3.Angle(cameraTransform.forward, toTarget.normalized) : 180f;
        bool isGazing = angle <= Mathf.Max(1f, startGazeAngleThreshold) && distance <= Mathf.Max(0.2f, startGazeMaxDistance);

        if (isGazing)
        {
            startDwellTimer += Time.deltaTime;
        }
        else
        {
            startDwellTimer = Mathf.Max(0f, startDwellTimer - Time.deltaTime * 2f);
        }

        float progress = Mathf.Clamp01(startDwellTimer / Mathf.Max(0.2f, startDwellSeconds));
        UpdateDwellBarVisual(target, progress);

        if (progress >= 1f)
        {
            startTriggered = true;
            OnStartSessionPressed();
        }
    }

    void UpdateStartButtonSessionCycle()
    {
        if (!showStartSessionButton || waypointManager == null)
            return;

        if (waypointManager.IsSessionActive)
        {
            waitingForNextSession = true;
            return;
        }

        if (waypointManager.IsSessionCompleted && waitingForNextSession)
        {
            if (startSessionButton == null && startSessionFallbackButton == null)
            {
                TryCreateStartSessionButton();
            }

            if (startSessionButton != null)
            {
                startSessionButton.SetActive(true);
                SetMrtkButtonLabel(startSessionButton, useDwellStart ? "Look & Hold" : "Start");
            }

            if (startSessionFallbackCanvas != null)
            {
                startSessionFallbackCanvas.SetActive(true);
            }
            else if (startSessionFallbackButton != null)
            {
                startSessionFallbackButton.gameObject.SetActive(true);
            }

            startTriggered = false;
            startDwellTimer = 0f;
            if (startDwellBarRoot != null)
            {
                startDwellBarRoot.SetActive(false);
            }

            waitingForNextSession = false;
        }
    }

    void UpdateDwellBarVisual(Transform target, float progress)
    {
        if (startDwellBarRoot == null || startDwellBarFill == null)
            return;

        startDwellBarRoot.SetActive(progress > 0.001f || target != null);
        startDwellBarRoot.transform.position = target.position + target.up * dwellBarVerticalOffset;
        startDwellBarRoot.transform.rotation = target.rotation;

        float fillWidth = Mathf.Max(0.0001f, dwellBarWidth * progress);
        startDwellBarFill.localScale = new Vector3(fillWidth, dwellBarHeight * 0.8f, dwellBarDepth * 0.8f);
        startDwellBarFill.localPosition = new Vector3((-dwellBarWidth * 0.5f) + (fillWidth * 0.5f), 0f, -0.001f);
    }

    void EnsureSessionControlDwellBar()
    {
        // Moved to SessionDwellControlController.
    }

    void HideSessionControlDwellBar()
    {
        // Moved to SessionDwellControlController.
    }

    void UpdateSessionControlDwellBar(float progress, string actionLabel, Color fillColor)
    {
        // Moved to SessionDwellControlController.
    }

    void UpdateSessionDwellControls()
    {
        sessionDwellControlController.Update(waypointManager, cameraTransform, GetSessionDwellControlSettings());
    }

    void UpdateStartSessionButtonPose()
    {
        if (cameraTransform == null)
            return;

        Vector3 horizontalForward = cameraTransform.forward;
        horizontalForward.y = 0f;
        if (horizontalForward.sqrMagnitude < 0.0001f)
        {
            horizontalForward = cameraTransform.parent != null ? cameraTransform.parent.forward : Vector3.forward;
            horizontalForward.y = 0f;
        }
        horizontalForward.Normalize();

        Vector3 horizontalRight = Vector3.Cross(Vector3.up, horizontalForward).normalized;

        Vector3 targetPos = cameraTransform.position
                            + horizontalForward * startButtonDistance
                            + horizontalRight * startButtonOffset.x
                            + horizontalForward * startButtonOffset.z;

        float verticalOffset = startButtonOffset.y;
        if (followStartButtonVerticalGaze)
        {
            verticalOffset += cameraTransform.forward.y * startButtonDistance;
        }
        else
        {
            verticalOffset = Mathf.Max(verticalOffset, startButtonMinHeightAboveEyes);
        }
        targetPos.y = cameraTransform.position.y + verticalOffset;

        Vector3 toCamera = cameraTransform.position - targetPos;
        if (!followStartButtonVerticalGaze)
        {
            toCamera.y = 0f;
        }
        if (toCamera.sqrMagnitude < 0.001f)
        {
            toCamera = -cameraTransform.forward;
        }

        Quaternion targetRot = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);

        if (startSessionButton != null && startSessionButton.activeSelf)
        {
            startSessionButton.transform.position = targetPos;
            startSessionButton.transform.rotation = targetRot;
        }

        if (startSessionFallbackCanvas != null && startSessionFallbackCanvas.activeSelf)
        {
            startSessionFallbackCanvas.transform.position = targetPos;
            startSessionFallbackCanvas.transform.rotation = targetRot;
        }
    }

    void OnStartSessionPressed()
    {
        startTriggered = true;
        completionOverlayController.ResetForNewSession();

        if (waypointManager != null)
        {
            waypointManager.StartSessionFromButton();
        }

        if (hideStartButtonAfterStart && startSessionButton != null)
        {
            startSessionButton.SetActive(false);
        }

        if (hideStartButtonAfterStart && startSessionFallbackButton != null)
        {
            if (startSessionFallbackCanvas != null)
            {
                startSessionFallbackCanvas.SetActive(false);
            }
            else
            {
                startSessionFallbackButton.gameObject.SetActive(false);
            }
        }

        if (startDwellBarRoot != null)
        {
            startDwellBarRoot.SetActive(false);
        }
    }

    void EnsureCompletionOverlay()
    {
        completionOverlayController.Ensure(cameraTransform, GetCompletionOverlaySettings());
    }

    void UpdateCompletionOverlayPose()
    {
        completionOverlayController.UpdatePose(cameraTransform, GetCompletionOverlaySettings());
    }

    void UpdateCompletionOverlayState()
    {
        completionOverlayController.UpdateState(waypointManager, cameraTransform, GetCompletionOverlaySettings());
    }

    void HideCompletionOverlay()
    {
        completionOverlayController.Hide();
    }

    void ApplyCompletionOverlayText(string statsText)
    {
        // Kept only for backward compatibility while this class is being decomposed.
    }

    void SetupVoiceCommands()
    {
        voiceCommandController.Initialize(
            onShowStats: () =>
            {
                if (panelObj != null)
                    panelObj.SetActive(true);
            },
            onHideStats: () =>
            {
                if (panelObj != null)
                    panelObj.SetActive(false);
            },
            onToggleMetronome: ToggleMetronome,
            onMetronomeOn: () =>
            {
                if (waypointManager != null)
                    waypointManager.ToggleMetronome(true);
            },
            onMetronomeOff: () =>
            {
                if (waypointManager != null)
                    waypointManager.ToggleMetronome(false);
            },
            onPause: () =>
            {
                if (waypointManager != null)
                    waypointManager.PauseSession();
            },
            onResume: () =>
            {
                if (waypointManager != null)
                    waypointManager.ResumeSession();
            },
            onEndSession: () =>
            {
                if (waypointManager != null)
                    waypointManager.EndSessionEarly();
            });
    }

    void OnDestroy()
    {
        voiceCommandController.Dispose();
    }

    GameObject CreateMrtkButton(string name, Vector3 localPos, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = Instantiate(mrtkButtonPrefab, canvasObj.transform);
        buttonObj.name = name;
        buttonObj.transform.localPosition = localPos;
        buttonObj.transform.localRotation = Quaternion.identity;
        buttonObj.transform.localScale = mrtkButtonScale;

        Interactable interactable = buttonObj.GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.OnClick.RemoveAllListeners();
            interactable.OnClick.AddListener(onClick);
        }

        SetMrtkButtonLabel(buttonObj, label);
        return buttonObj;
    }

    void SetMrtkButtonLabel(GameObject buttonObj, string label)
    {
        if (buttonObj == null)
            return;

        TMP_Text tmp = buttonObj.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = label;
            return;
        }

        TextMesh textMesh = buttonObj.GetComponentInChildren<TextMesh>(true);
        if (textMesh != null)
        {
            textMesh.text = label;
        }
    }
}
