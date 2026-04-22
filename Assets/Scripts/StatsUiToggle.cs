using System;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

public class StatsUiToggle : MonoBehaviour
{
    [Header("References")]
    public WaypointSystemManager waypointManager;
    public RehabUiTheme uiTheme;

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
    public Color statsPanelBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public Color statsPanelTextColor = Color.white;
    public int statsPanelFontSize = 24;

    [Header("Stats Panel Behavior")]
    public bool autoHideStatsPanel = true;
    [Range(1f, 60f)] public float statsPanelAutoHideSeconds = 10f;

    [Header("MRTK Buttons")]
    public bool useMrtkButtons = true;
    public GameObject mrtkButtonPrefab;
    public Vector3 statsButtonLocalPos = new Vector3(0f, -0.07f, 0f);
    public Vector3 metronomeButtonLocalPos = new Vector3(0f, -0.16f, 0f);
    public Vector3 testFinishButtonLocalPos = new Vector3(0f, -0.25f, 0f);
    public Vector3 mrtkButtonScale = new Vector3(0.06f, 0.06f, 0.06f);
    public Color statsButtonFallbackColor = new Color(0f, 0.6f, 0.1f, 0.9f);
    public Color metronomeButtonFallbackColor = new Color(0.1f, 0.3f, 0.8f, 0.9f);
    public Color testFinishButtonFallbackColor = new Color(0.8f, 0.35f, 0.15f, 0.9f);

    [Header("Session Test Tools")]
    public bool showForceCompleteTestButton = true;

    [Header("Start Session Button")]
    public bool showStartSessionButton = true;
    public float startButtonDistance = 1.0f;
    public Vector3 startButtonOffset = new Vector3(0f, 0.2f, 0f);
    public bool followStartButtonVerticalGaze = false;
    public float startButtonMinHeightAboveEyes = 0.4f;
    public bool hideStartButtonAfterStart = true;

    [Header("Startup Instruction Card")]
    public bool showStartupInstructionCard = true;
    [TextArea(2, 4)]
    public string startupInstructionText = "Look where you want the session path to begin, then look up at the Start button to begin.";
    public float startupInstructionDistance = 2.05f;
    public float startupInstructionVerticalOffset = -0.34f;
    public Vector2 startupInstructionSize = new Vector2(520f, 150f);
    public float startupInstructionScale = 0.0017f;
    public Color startupInstructionBackgroundColor = new Color(0f, 0f, 0f, 0.68f);
    public Color startupInstructionTextColor = new Color(0.97f, 0.99f, 1f, 1f);
    public int startupInstructionFontSize = 28;
    public bool hideStartupInstructionAfterStart = true;

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
    [Range(0.05f, 1.2f)] public float completionOverlayFadeSeconds = 0.28f;
    [Range(0.75f, 1f)] public float completionOverlayEntryScale = 0.93f;
    public bool autoHideCompletionOverlay = true;
    [Range(1f, 30f)] public float completionOverlayAutoHideSeconds = 8f;

    [Header("Startup CSV Test")]
    public bool writeStartupSampleCsvOnLaunch = false;
    public string startupSampleCsvFileName = "startup_usb_test.csv";
    public string startupPicturesFolderName = "GaitRehabFYP_CSV";
    public bool writeUsbVisibleCsvCopyOnLaunch = false;

    [Header("Session Metrics CSV")]
    public bool writeSessionMetricsCsvOnSessionComplete = true;
    public string sessionMetricsCsvFileName = "session_metrics.csv";
    public bool writeSampleSessionMetricsOnLaunch = true;
    [Range(3, 60)] public int sampleSessionMetricsRowCount = 12;

    [Header("Backend Session CSV Upload")]
    public bool uploadSessionMetricsCsvToBackend = false;
    [Tooltip("Use your PC LAN IP, not localhost. Example: http://192.168.1.20:4000/api/sessions/upload")]
    public string backendSessionCsvUploadUrl = "http://127.0.0.1:4000/api/sessions/upload";
    [Range(3f, 120f)] public float backendSessionCsvUploadTimeoutSeconds = 20f;
    public bool uploadSampleSessionMetricsCsvOnLaunch = false;

    private GameObject canvasObj;
    private GameObject panelObj;
    private Text panelText;
    private Button toggleButton;
    private Button metronomeButton;
    private Button testFinishButton;
    private Text metronomeButtonText;
    private GameObject mrtkStatsButton;
    private GameObject mrtkMetronomeButton;
    private GameObject mrtkTestFinishButton;
    private CompletionOverlayController completionOverlayController = new CompletionOverlayController();
    private SessionDwellControlController sessionDwellControlController = new SessionDwellControlController();
    private StartSessionControlController startSessionControlController = new StartSessionControlController();
    private StartupCsvController startupCsvController = new StartupCsvController();
    private SessionMetricsCsvController sessionMetricsCsvController = new SessionMetricsCsvController();
    private SessionCsvUploadController sessionCsvUploadController = new SessionCsvUploadController();
    private CsvExportService csvExportService = new CsvExportService();
    private VoiceCommandController voiceCommandController = new VoiceCommandController();
    private UiFollowController uiFollowController = new UiFollowController();
    private Transform cameraTransform;
    private float statsPanelHideAtTime = -1f;

    void Start()
    {
        if (waypointManager == null)
        {
            waypointManager = FindObjectOfType<WaypointSystemManager>();
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        ApplyThemeIfPresent();

        CreateUi();
        EnsureCompletionOverlay();
        startSessionControlController.EnsureCreated(cameraTransform, GetStartSessionControlSettings(), OnStartSessionPressed);
        SetupVoiceCommands();
        WriteStartupSampleCsv();
        WriteSampleSessionMetricsCsv();
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
            canvasObj.transform.position = uiFollowController.GetTargetPosition(cameraTransform, uiDistance, uiOffset);
            canvasObj.transform.rotation = uiFollowController.GetTargetRotation(cameraTransform, canvasObj.transform.position);
            canvasObj.transform.localScale = Vector3.one * 0.0022f; // scale for world-space UI
            uiFollowController.InitializeOffsets(cameraTransform, canvasObj.transform);
        }

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 300);

        // Panel
        panelObj = new GameObject("StatsPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = statsPanelBackgroundColor;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = new Vector2(0f, 60f);

        // Panel text
        GameObject textObj = new GameObject("StatsText");
        textObj.transform.SetParent(panelObj.transform, false);
        panelText = textObj.AddComponent<Text>();
        panelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        panelText.fontSize = statsPanelFontSize;
        panelText.color = statsPanelTextColor;
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

            if (showForceCompleteTestButton)
            {
                mrtkTestFinishButton = CreateMrtkButton("TestFinishButton", testFinishButtonLocalPos, "Finish Test", TriggerTestSessionCompletion);
            }
        }
        else
        {
            // Unity UI fallback
            GameObject buttonObj = new GameObject("StatsToggleButton");
            buttonObj.transform.SetParent(canvasObj.transform, false);
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = statsButtonFallbackColor;
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
            metroBtnImage.color = metronomeButtonFallbackColor;
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

            if (showForceCompleteTestButton)
            {
                GameObject finishBtnObj = new GameObject("TestFinishButton");
                finishBtnObj.transform.SetParent(canvasObj.transform, false);
                Image finishBtnImage = finishBtnObj.AddComponent<Image>();
                finishBtnImage.color = testFinishButtonFallbackColor;
                testFinishButton = finishBtnObj.AddComponent<Button>();
                RectTransform finishBtnRect = finishBtnObj.GetComponent<RectTransform>();
                finishBtnRect.sizeDelta = buttonSize;
                finishBtnRect.anchoredPosition = new Vector2(0f, -150f);

                GameObject finishTextObj = new GameObject("TestFinishButtonText");
                finishTextObj.transform.SetParent(finishBtnObj.transform, false);
                Text finishText = finishTextObj.AddComponent<Text>();
                finishText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                finishText.fontSize = 24;
                finishText.color = Color.white;
                finishText.alignment = TextAnchor.MiddleCenter;
                finishText.text = "Finish Test";
                RectTransform finishTextRect = finishText.GetComponent<RectTransform>();
                finishTextRect.sizeDelta = buttonSize;
                finishTextRect.anchoredPosition = Vector2.zero;

                testFinishButton.onClick.AddListener(TriggerTestSessionCompletion);
            }
        }

        // Start hidden
        panelObj.SetActive(false);
    }

    void Update()
    {
        startSessionControlController.Update(cameraTransform, waypointManager, GetStartSessionControlSettings(), OnStartSessionPressed);
        UpdateSessionDwellControls();
        UpdateCompletionOverlayState();
        UpdateSessionMetricsCsvLogging();

        if (panelObj == null || panelText == null)
            return;

        if (panelObj.activeSelf && waypointManager != null)
        {
            panelText.text = waypointManager.GetStatsText();
        }

        if (panelObj.activeSelf && autoHideStatsPanel && statsPanelHideAtTime > 0f && Time.time >= statsPanelHideAtTime)
        {
            HideStatsPanel();
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

        if (canvasObj == null || cameraTransform == null)
            return;

        UpdateCompletionOverlayPose();
        startSessionControlController.LateUpdatePose(cameraTransform, GetStartSessionControlSettings(), OnStartSessionPressed);

        uiFollowController.UpdateFollow(
            cameraTransform,
            canvasObj.transform,
            followPositionOnly,
            uiDistance,
            uiOffset,
            followAngleThreshold,
            followDistanceThreshold,
            followSpeed,
            rotateSpeed);
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
            completionBodyMinFontSize = completionBodyMinFontSize,
            completionFadeSeconds = completionOverlayFadeSeconds,
            completionEntryScale = completionOverlayEntryScale,
            autoHideCompletionOverlay = autoHideCompletionOverlay,
            completionOverlayAutoHideSeconds = completionOverlayAutoHideSeconds
        };
    }

    void ApplyThemeIfPresent()
    {
        if (uiTheme == null)
            return;

        statsPanelBackgroundColor = uiTheme.statsPanelBackgroundColor;
        statsPanelTextColor = uiTheme.statsPanelTextColor;
        statsPanelFontSize = uiTheme.statsPanelFontSize;

        statsButtonFallbackColor = uiTheme.statsButtonColor;
        metronomeButtonFallbackColor = uiTheme.metronomeButtonColor;

        completionOverlayBackgroundColor = uiTheme.completionBackgroundColor;
        completionOverlayAccentColor = uiTheme.completionAccentColor;
        completionOverlayTextColor = uiTheme.completionTextColor;
        completionOverlayDistance = uiTheme.completionDistance;
        completionOverlayScale = uiTheme.completionScale;
        completionOverlaySize = uiTheme.completionSize;
        completionTitleFontSize = uiTheme.completionTitleFontSize;
        completionBodyFontSize = uiTheme.completionBodyFontSize;
        completionTitleMinFontSize = uiTheme.completionTitleMinFontSize;
        completionBodyMinFontSize = uiTheme.completionBodyMinFontSize;
        completionOverlayFadeSeconds = uiTheme.completionFadeSeconds;
        completionOverlayEntryScale = uiTheme.completionEntryScale;

        dwellBarBackgroundColor = uiTheme.startDwellBackgroundColor;
        dwellBarFillColor = uiTheme.startDwellFillColor;
        sessionControlBarBackgroundColor = uiTheme.sessionDwellBackgroundColor;
        sessionControlBarPauseColor = uiTheme.sessionDwellPauseColor;
        sessionControlBarResumeColor = uiTheme.sessionDwellResumeColor;
        sessionControlBarEndColor = uiTheme.sessionDwellEndColor;
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

    StartSessionControlController.Settings GetStartSessionControlSettings()
    {
        return new StartSessionControlController.Settings
        {
            showStartSessionButton = showStartSessionButton,
            useMrtkButtons = useMrtkButtons,
            mrtkButtonPrefab = mrtkButtonPrefab,
            mrtkButtonScale = mrtkButtonScale,
            buttonSize = buttonSize,
            useDwellStart = useDwellStart,
            startDwellSeconds = startDwellSeconds,
            startGazeAngleThreshold = startGazeAngleThreshold,
            startGazeMaxDistance = startGazeMaxDistance,
            dwellBarWidth = dwellBarWidth,
            dwellBarHeight = dwellBarHeight,
            dwellBarDepth = dwellBarDepth,
            dwellBarVerticalOffset = dwellBarVerticalOffset,
            dwellBarBackgroundColor = dwellBarBackgroundColor,
            dwellBarFillColor = dwellBarFillColor,
            startButtonDistance = startButtonDistance,
            startButtonOffset = startButtonOffset,
            followStartButtonVerticalGaze = followStartButtonVerticalGaze,
            startButtonMinHeightAboveEyes = startButtonMinHeightAboveEyes,
            hideStartButtonAfterStart = hideStartButtonAfterStart,
            showStartupInstructionCard = showStartupInstructionCard,
            startupInstructionText = startupInstructionText,
            startupInstructionDistance = startupInstructionDistance,
            startupInstructionVerticalOffset = startupInstructionVerticalOffset,
            startupInstructionSize = startupInstructionSize,
            startupInstructionScale = startupInstructionScale,
            startupInstructionBackgroundColor = startupInstructionBackgroundColor,
            startupInstructionTextColor = startupInstructionTextColor,
            startupInstructionFontSize = startupInstructionFontSize,
            hideStartupInstructionAfterStart = hideStartupInstructionAfterStart
        };
    }

    StartupCsvController.Settings GetStartupCsvSettings()
    {
        return new StartupCsvController.Settings
        {
            writeOnLaunch = writeStartupSampleCsvOnLaunch,
            fileName = startupSampleCsvFileName
        };
    }

    SessionMetricsCsvController.Settings GetSessionMetricsCsvSettings()
    {
        return new SessionMetricsCsvController.Settings
        {
            writeOnSessionComplete = writeSessionMetricsCsvOnSessionComplete,
            fileName = sessionMetricsCsvFileName,
            writeSampleDataOnLaunch = writeSampleSessionMetricsOnLaunch,
            sampleRowCount = sampleSessionMetricsRowCount
        };
    }

    SessionCsvUploadController.Settings GetSessionCsvUploadSettings()
    {
        return new SessionCsvUploadController.Settings
        {
            uploadEnabled = uploadSessionMetricsCsvToBackend,
            uploadUrl = backendSessionCsvUploadUrl,
            timeoutSeconds = backendSessionCsvUploadTimeoutSeconds
        };
    }

    void TogglePanel()
    {
        if (panelObj == null)
            return;

        if (panelObj.activeSelf)
        {
            HideStatsPanel();
        }
        else
        {
            ShowStatsPanel();
        }
    }

    void ShowStatsPanel()
    {
        if (panelObj == null)
            return;

        panelObj.SetActive(true);
        if (autoHideStatsPanel)
        {
            statsPanelHideAtTime = Time.time + Mathf.Max(1f, statsPanelAutoHideSeconds);
        }
        else
        {
            statsPanelHideAtTime = -1f;
        }
    }

    void HideStatsPanel()
    {
        if (panelObj == null)
            return;

        panelObj.SetActive(false);
        statsPanelHideAtTime = -1f;
    }

    void ToggleMetronome()
    {
        if (waypointManager == null)
            return;

        bool newState = !waypointManager.enableMetronome;
        waypointManager.ToggleMetronome(newState);
    }

    void TriggerTestSessionCompletion()
    {
        if (waypointManager == null)
            return;

        waypointManager.ForceCompleteForTesting();
    }

    void UpdateSessionDwellControls()
    {
        sessionDwellControlController.Update(waypointManager, cameraTransform, GetSessionDwellControlSettings());
    }

    void OnStartSessionPressed()
    {
        completionOverlayController.ResetForNewSession();
        if (waypointManager != null)
        {
            waypointManager.SetSessionStatsStatusLine("");
        }

        if (waypointManager != null)
        {
            waypointManager.StartSessionFromButton();
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

    void SetupVoiceCommands()
    {
        voiceCommandController.Initialize(
            onShowStats: () =>
            {
                ShowStatsPanel();
            },
            onHideStats: () =>
            {
                HideStatsPanel();
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
            },
            onFinishTest: () =>
            {
                TriggerTestSessionCompletion();
            });
    }

    void OnDestroy()
    {
        voiceCommandController.Dispose();
    }

    void UpdateSessionMetricsCsvLogging()
    {
        sessionMetricsCsvController.Update(
            waypointManager,
            GetSessionMetricsCsvSettings(),
            csvExportService,
            startupPicturesFolderName,
            writeUsbVisibleCsvCopyOnLaunch,
            Application.persistentDataPath,
            OnSessionMetricsCsvWritten);
    }

    void WriteStartupSampleCsv()
    {
        startupCsvController.WriteStartupSampleCsv(
            GetStartupCsvSettings(),
            csvExportService,
            startupPicturesFolderName,
            writeUsbVisibleCsvCopyOnLaunch,
            Application.persistentDataPath);
    }

    void WriteSampleSessionMetricsCsv()
    {
        Action<string, string, string> onSampleRowWritten = null;
        if (uploadSampleSessionMetricsCsvOnLaunch)
        {
            onSampleRowWritten = OnSessionMetricsCsvWritten;
        }

        sessionMetricsCsvController.WriteSampleDatasetOnLaunch(
            GetSessionMetricsCsvSettings(),
            csvExportService,
            startupPicturesFolderName,
            writeUsbVisibleCsvCopyOnLaunch,
            Application.persistentDataPath,
            onSampleRowWritten);
    }

    void OnSessionMetricsCsvWritten(string fileName, string header, string row)
    {
        SessionCsvUploadController.Settings uploadSettings = GetSessionCsvUploadSettings();
        if (waypointManager != null && waypointManager.IsSessionCompleted)
        {
            if (!uploadSettings.uploadEnabled)
            {
                waypointManager.SetSessionStatsStatusLine("CSV upload: disabled");
                return;
            }

            waypointManager.SetSessionStatsStatusLine("CSV upload: sending...");
        }

        sessionCsvUploadController.TryUploadCsvRow(
            this,
            uploadSettings,
            fileName,
            header,
            row,
            "Session metrics",
            (success, detail) =>
            {
                if (waypointManager == null || !waypointManager.IsSessionCompleted)
                    return;

                if (success)
                {
                    waypointManager.SetSessionStatsStatusLine("CSV upload: success");
                }
                else
                {
                    string suffix = string.IsNullOrWhiteSpace(detail) ? "" : " (" + detail + ")";
                    waypointManager.SetSessionStatsStatusLine("CSV upload: failed" + suffix);
                }
            });
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
