using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine.Windows.Speech;

public class StatsUiToggle : MonoBehaviour
{
    [Header("References")]
    public WaypointSystemManager waypointManager;

    [Header("UI Layout")]
    public float uiDistance = 1.0f;
    public Vector3 uiOffset = new Vector3(0.35f, -0.25f, 0f); // bottom-right of view
    public Vector2 panelSize = new Vector2(300f, 200f);
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
    public Vector3 startButtonOffset = new Vector3(0f, -0.05f, 0f);
    public bool hideStartButtonAfterStart = true;

    private GameObject canvasObj;
    private GameObject panelObj;
    private Text panelText;
    private Button toggleButton;
    private Button metronomeButton;
    private Text metronomeButtonText;
    private Button startSessionFallbackButton;
    private GameObject mrtkStatsButton;
    private GameObject mrtkMetronomeButton;
    private GameObject startSessionButton;
    private Transform cameraTransform;
    private Vector3 followVelocity;
    private Vector3 worldOffset;
    private Quaternion fixedRotation;
    private KeywordRecognizer keywordRecognizer;

    void Start()
    {
        if (waypointManager == null)
        {
            waypointManager = FindObjectOfType<WaypointSystemManager>();
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        CreateUi();
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
        panelText.fontSize = 28;
        panelText.color = Color.white;
        panelText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = panelText.GetComponent<RectTransform>();
        textRect.sizeDelta = panelSize;
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
            interactable.OnClick.AddListener(OnStartSessionPressed);
        }

        SetMrtkButtonLabel(startSessionButton, "Start");
        UpdateStartSessionButtonPose();
    }

    void CreateFallbackStartButton()
    {
        if (startSessionFallbackButton != null || canvasObj == null)
            return;

        GameObject buttonObj = new GameObject("StartSessionButtonFallback");
        buttonObj.transform.SetParent(canvasObj.transform, false);

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

        startSessionFallbackButton.onClick.AddListener(OnStartSessionPressed);
    }

    void UpdateStartSessionButtonPose()
    {
        if (startSessionButton == null || cameraTransform == null || !startSessionButton.activeSelf)
            return;

        Vector3 targetPos = cameraTransform.position
                            + cameraTransform.forward * startButtonDistance
                            + cameraTransform.right * startButtonOffset.x
                            + cameraTransform.up * startButtonOffset.y;
        startSessionButton.transform.position = targetPos;

        Vector3 toCamera = cameraTransform.position - targetPos;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f)
        {
            toCamera = -cameraTransform.forward;
        }
        startSessionButton.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    void OnStartSessionPressed()
    {
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
            startSessionFallbackButton.gameObject.SetActive(false);
        }
    }

    void SetupVoiceCommands()
    {
        string[] keywords = { "show stats", "hide stats", "metronome", "metronome on", "metronome off" };

        keywordRecognizer = new KeywordRecognizer(keywords);
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string command = args.text.ToLower();

        switch (command)
        {
            case "show stats":
                if (panelObj != null)
                {
                    panelObj.SetActive(true);
                }
                break;

            case "hide stats":
                if (panelObj != null)
                {
                    panelObj.SetActive(false);
                }
                break;

            case "metronome":
                ToggleMetronome();
                break;

            case "metronome on":
                if (waypointManager != null)
                {
                    waypointManager.ToggleMetronome(true);
                }
                break;

            case "metronome off":
                if (waypointManager != null)
                {
                    waypointManager.ToggleMetronome(false);
                }
                break;
        }
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
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
