using System;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartSessionControlController
{
    public struct Settings
    {
        public bool showStartSessionButton;
        public bool useMrtkButtons;
        public GameObject mrtkButtonPrefab;
        public Vector3 mrtkButtonScale;
        public Vector2 buttonSize;
        public bool useDwellStart;
        public float startDwellSeconds;
        public float startGazeAngleThreshold;
        public float startGazeMaxDistance;
        public float dwellBarWidth;
        public float dwellBarHeight;
        public float dwellBarDepth;
        public float dwellBarVerticalOffset;
        public Color dwellBarBackgroundColor;
        public Color dwellBarFillColor;
        public float startButtonDistance;
        public Vector3 startButtonOffset;
        public bool followStartButtonVerticalGaze;
        public float startButtonMinHeightAboveEyes;
        public bool hideStartButtonAfterStart;
        public bool showStartupInstructionCard;
        public string startupInstructionText;
        public float startupInstructionDistance;
        public float startupInstructionVerticalOffset;
        public Vector2 startupInstructionSize;
        public float startupInstructionScale;
        public Color startupInstructionBackgroundColor;
        public Color startupInstructionTextColor;
        public int startupInstructionFontSize;
        public bool hideStartupInstructionAfterStart;
    }

    private Button startSessionFallbackButton;
    private GameObject startSessionFallbackCanvas;
    private GameObject startSessionButton;
    private GameObject startDwellBarRoot;
    private Transform startDwellBarFill;
    private Renderer startDwellBarBgRenderer;
    private Renderer startDwellBarFillRenderer;
    private GameObject startupInstructionCanvas;
    private Text startupInstructionLabel;
    private bool hasDismissedStartupInstruction;
    private float startDwellTimer;
    private bool startTriggered;
    private bool waitingForNextSession;

    public void EnsureCreated(Transform cameraTransform, Settings settings, Action onStartSessionPressed)
    {
        if (!settings.showStartSessionButton || cameraTransform == null)
            return;

        if (startSessionButton == null && startSessionFallbackButton == null)
        {
            TryCreateStartSessionButton(cameraTransform, settings, onStartSessionPressed);
        }

        EnsureStartupInstructionCard(cameraTransform, settings);
    }

    public void Update(Transform cameraTransform, WaypointSystemManager waypointManager, Settings settings, Action onStartSessionPressed)
    {
        if (cameraTransform == null)
            return;

        UpdateStartButtonSessionCycle(waypointManager, settings, cameraTransform, onStartSessionPressed);
        UpdateStartDwellActivation(cameraTransform, settings, onStartSessionPressed);
        UpdateStartupInstructionCard(waypointManager, settings);
    }

    public void LateUpdatePose(Transform cameraTransform, Settings settings, Action onStartSessionPressed)
    {
        if (cameraTransform == null)
            return;

        EnsureCreated(cameraTransform, settings, onStartSessionPressed);
        UpdateStartSessionButtonPose(cameraTransform, settings);
        UpdateStartupInstructionPose(cameraTransform, settings);
    }

    private void EnsureStartupInstructionCard(Transform cameraTransform, Settings settings)
    {
        if (!settings.showStartupInstructionCard || cameraTransform == null || startupInstructionCanvas != null)
            return;

        startupInstructionCanvas = new GameObject("StartupInstructionCanvas");
        Canvas canvas = startupInstructionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        startupInstructionCanvas.AddComponent<CanvasScaler>();
        startupInstructionCanvas.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = startupInstructionCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = settings.startupInstructionSize;

        GameObject panelObj = new GameObject("StartupInstructionPanel");
        panelObj.transform.SetParent(startupInstructionCanvas.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = settings.startupInstructionBackgroundColor;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = settings.startupInstructionSize;
        panelRect.anchoredPosition = Vector2.zero;

        GameObject textObj = new GameObject("StartupInstructionText");
        textObj.transform.SetParent(panelObj.transform, false);
        startupInstructionLabel = textObj.AddComponent<Text>();
        startupInstructionLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        startupInstructionLabel.fontSize = Mathf.Max(16, settings.startupInstructionFontSize);
        startupInstructionLabel.color = settings.startupInstructionTextColor;
        startupInstructionLabel.alignment = TextAnchor.MiddleCenter;
        startupInstructionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        startupInstructionLabel.verticalOverflow = VerticalWrapMode.Overflow;
        startupInstructionLabel.text = string.IsNullOrWhiteSpace(settings.startupInstructionText)
            ? "Look where you want the session path to begin, then look up at the Start button to begin."
            : settings.startupInstructionText;

        RectTransform textRect = startupInstructionLabel.GetComponent<RectTransform>();
        textRect.sizeDelta = settings.startupInstructionSize - new Vector2(36f, 24f);
        textRect.anchoredPosition = Vector2.zero;

        startupInstructionCanvas.transform.SetParent(null, true);
        UpdateStartupInstructionPose(cameraTransform, settings);
    }

    private void UpdateStartupInstructionCard(WaypointSystemManager waypointManager, Settings settings)
    {
        if (startupInstructionCanvas == null)
            return;

        if (!settings.showStartupInstructionCard)
        {
            startupInstructionCanvas.SetActive(false);
            return;
        }

        if (settings.hideStartupInstructionAfterStart && waypointManager != null && waypointManager.IsSessionActive)
            hasDismissedStartupInstruction = true;

        bool shouldShow = !hasDismissedStartupInstruction;
        if (shouldShow && waypointManager != null && waypointManager.IsSessionActive)
            shouldShow = false;

        startupInstructionCanvas.SetActive(shouldShow);

        if (shouldShow && startupInstructionLabel != null)
        {
            startupInstructionLabel.text = string.IsNullOrWhiteSpace(settings.startupInstructionText)
                ? "Look where you want the session path to begin, then look up at the Start button to begin."
                : settings.startupInstructionText;
        }
    }

    private void UpdateStartupInstructionPose(Transform cameraTransform, Settings settings)
    {
        if (startupInstructionCanvas == null || cameraTransform == null)
            return;

        if (startupInstructionCanvas.transform.parent != null)
            startupInstructionCanvas.transform.SetParent(null, true);

        Vector3 horizontalForward = cameraTransform.forward;
        horizontalForward.y = 0f;
        if (horizontalForward.sqrMagnitude < 0.0001f)
        {
            horizontalForward = cameraTransform.parent != null ? cameraTransform.parent.forward : Vector3.forward;
            horizontalForward.y = 0f;
        }
        horizontalForward.Normalize();

        RectTransform rect = startupInstructionCanvas.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = settings.startupInstructionSize;

        Vector3 targetPos = cameraTransform.position
            + horizontalForward * Mathf.Max(1.8f, settings.startupInstructionDistance)
            + Vector3.up * Mathf.Min(settings.startupInstructionVerticalOffset, -0.32f);

        Vector3 toCamera = cameraTransform.position - targetPos;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f)
            toCamera = -horizontalForward;

        startupInstructionCanvas.transform.position = targetPos;
        startupInstructionCanvas.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        startupInstructionCanvas.transform.localScale = Vector3.one * Mathf.Clamp(settings.startupInstructionScale, 0.0007f, 0.0035f);
    }

    private void TryCreateStartSessionButton(Transform cameraTransform, Settings settings, Action onStartSessionPressed)
    {
        if (!settings.showStartSessionButton || cameraTransform == null)
            return;

        if (settings.useMrtkButtons && settings.mrtkButtonPrefab != null)
        {
            CreateMrtkStartSessionButton(cameraTransform, settings, onStartSessionPressed);
            return;
        }

        CreateFallbackStartButton(cameraTransform, settings, onStartSessionPressed);
    }

    private void CreateMrtkStartSessionButton(Transform cameraTransform, Settings settings, Action onStartSessionPressed)
    {
        if (startSessionButton != null)
            return;

        startSessionButton = UnityEngine.Object.Instantiate(settings.mrtkButtonPrefab);
        startSessionButton.name = "StartSessionButton";
        startSessionButton.transform.localScale = settings.mrtkButtonScale;

        Interactable interactable = startSessionButton.GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.OnClick.RemoveAllListeners();
            if (!settings.useDwellStart)
            {
                interactable.OnClick.AddListener(() => TriggerStart(settings, onStartSessionPressed));
            }
        }

        SetButtonLabel(startSessionButton, settings.useDwellStart ? "Look & Hold" : "Start");
        UpdateStartSessionButtonPose(cameraTransform, settings);
    }

    private void CreateFallbackStartButton(Transform cameraTransform, Settings settings, Action onStartSessionPressed)
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
        buttonRect.sizeDelta = settings.buttonSize;
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
        labelRect.sizeDelta = settings.buttonSize;
        labelRect.anchoredPosition = Vector2.zero;

        if (!settings.useDwellStart)
        {
            startSessionFallbackButton.onClick.AddListener(() => TriggerStart(settings, onStartSessionPressed));
        }

        UpdateStartSessionButtonPose(cameraTransform, settings);
    }

    private void EnsureStartDwellBar(Settings settings)
    {
        if (!settings.useDwellStart || startDwellBarRoot != null)
            return;

        startDwellBarRoot = new GameObject("StartDwellProgressBar");

        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bgObj.name = "DwellBarBackground";
        UnityEngine.Object.Destroy(bgObj.GetComponent<Collider>());
        bgObj.transform.SetParent(startDwellBarRoot.transform, false);
        bgObj.transform.localScale = new Vector3(settings.dwellBarWidth, settings.dwellBarHeight, settings.dwellBarDepth);
        startDwellBarBgRenderer = bgObj.GetComponent<Renderer>();

        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fillObj.name = "DwellBarFill";
        UnityEngine.Object.Destroy(fillObj.GetComponent<Collider>());
        fillObj.transform.SetParent(startDwellBarRoot.transform, false);
        startDwellBarFill = fillObj.transform;
        startDwellBarFill.localScale = new Vector3(0.0001f, settings.dwellBarHeight * 0.8f, settings.dwellBarDepth * 0.8f);
        startDwellBarFill.localPosition = new Vector3(-settings.dwellBarWidth * 0.5f, 0f, -0.001f);
        startDwellBarFillRenderer = fillObj.GetComponent<Renderer>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            if (startDwellBarBgRenderer != null)
            {
                startDwellBarBgRenderer.material = new Material(shader);
                startDwellBarBgRenderer.material.color = settings.dwellBarBackgroundColor;
            }
            if (startDwellBarFillRenderer != null)
            {
                startDwellBarFillRenderer.material = new Material(shader);
                startDwellBarFillRenderer.material.color = settings.dwellBarFillColor;
            }
        }

        startDwellBarRoot.SetActive(false);
    }

    private Transform GetActiveStartTarget()
    {
        if (startSessionButton != null && startSessionButton.activeSelf)
            return startSessionButton.transform;

        if (startSessionFallbackCanvas != null && startSessionFallbackCanvas.activeSelf)
            return startSessionFallbackCanvas.transform;

        return null;
    }

    private void UpdateStartDwellActivation(Transform cameraTransform, Settings settings, Action onStartSessionPressed)
    {
        if (!settings.useDwellStart || startTriggered)
            return;

        Transform target = GetActiveStartTarget();
        EnsureStartDwellBar(settings);

        if (target == null)
        {
            startDwellTimer = 0f;
            if (startDwellBarRoot != null)
                startDwellBarRoot.SetActive(false);
            return;
        }

        Vector3 toTarget = target.position - cameraTransform.position;
        float distance = toTarget.magnitude;
        float angle = distance > 0.001f ? Vector3.Angle(cameraTransform.forward, toTarget.normalized) : 180f;
        bool isGazing = angle <= Mathf.Max(1f, settings.startGazeAngleThreshold) && distance <= Mathf.Max(0.2f, settings.startGazeMaxDistance);

        if (isGazing)
            startDwellTimer += Time.deltaTime;
        else
            startDwellTimer = Mathf.Max(0f, startDwellTimer - Time.deltaTime * 2f);

        float progress = Mathf.Clamp01(startDwellTimer / Mathf.Max(0.2f, settings.startDwellSeconds));
        UpdateDwellBarVisual(target, settings, progress);

        if (progress >= 1f)
        {
            TriggerStart(settings, onStartSessionPressed);
        }
    }

    private void UpdateStartButtonSessionCycle(
        WaypointSystemManager waypointManager,
        Settings settings,
        Transform cameraTransform,
        Action onStartSessionPressed)
    {
        if (!settings.showStartSessionButton || waypointManager == null)
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
                TryCreateStartSessionButton(cameraTransform, settings, onStartSessionPressed);
            }

            if (startSessionButton != null)
            {
                startSessionButton.SetActive(true);
                SetButtonLabel(startSessionButton, settings.useDwellStart ? "Look & Hold" : "Start");
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
                startDwellBarRoot.SetActive(false);

            waitingForNextSession = false;
        }
    }

    private void UpdateDwellBarVisual(Transform target, Settings settings, float progress)
    {
        if (startDwellBarRoot == null || startDwellBarFill == null)
            return;

        startDwellBarRoot.SetActive(progress > 0.001f || target != null);
        startDwellBarRoot.transform.position = target.position + target.up * settings.dwellBarVerticalOffset;
        startDwellBarRoot.transform.rotation = target.rotation;

        float fillWidth = Mathf.Max(0.0001f, settings.dwellBarWidth * progress);
        startDwellBarFill.localScale = new Vector3(fillWidth, settings.dwellBarHeight * 0.8f, settings.dwellBarDepth * 0.8f);
        startDwellBarFill.localPosition = new Vector3((-settings.dwellBarWidth * 0.5f) + (fillWidth * 0.5f), 0f, -0.001f);
    }

    private void UpdateStartSessionButtonPose(Transform cameraTransform, Settings settings)
    {
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
            + horizontalForward * settings.startButtonDistance
            + horizontalRight * settings.startButtonOffset.x
            + horizontalForward * settings.startButtonOffset.z;

        float verticalOffset = settings.startButtonOffset.y;
        if (settings.followStartButtonVerticalGaze)
            verticalOffset += cameraTransform.forward.y * settings.startButtonDistance;
        else
            verticalOffset = Mathf.Max(verticalOffset, settings.startButtonMinHeightAboveEyes);

        targetPos.y = cameraTransform.position.y + verticalOffset;

        Vector3 toCamera = cameraTransform.position - targetPos;
        if (!settings.followStartButtonVerticalGaze)
            toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.001f)
            toCamera = -cameraTransform.forward;

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

    private void TriggerStart(Settings settings, Action onStartSessionPressed)
    {
        startTriggered = true;
        onStartSessionPressed?.Invoke();

        if (settings.hideStartupInstructionAfterStart)
        {
            hasDismissedStartupInstruction = true;
            if (startupInstructionCanvas != null)
                startupInstructionCanvas.SetActive(false);
        }

        if (settings.hideStartButtonAfterStart && startSessionButton != null)
        {
            startSessionButton.SetActive(false);
        }

        if (settings.hideStartButtonAfterStart && startSessionFallbackButton != null)
        {
            if (startSessionFallbackCanvas != null)
                startSessionFallbackCanvas.SetActive(false);
            else
                startSessionFallbackButton.gameObject.SetActive(false);
        }

        if (startDwellBarRoot != null)
            startDwellBarRoot.SetActive(false);
    }

    private static void SetButtonLabel(GameObject buttonObj, string label)
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
            textMesh.text = label;
    }
}
