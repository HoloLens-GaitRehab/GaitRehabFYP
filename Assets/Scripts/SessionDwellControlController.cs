using UnityEngine;

public class SessionDwellControlController
{
    public struct Settings
    {
        public bool useDwellSessionControls;
        public float sessionControlDwellSeconds;
        public float sessionEndDwellSeconds;
        public float sessionControlLookUpAngle;
        public float sessionControlCooldownSeconds;
        public bool requirePauseBeforeEnd;
        public float sessionControlBarDistance;
        public float sessionControlBarVerticalOffset;
        public float sessionControlBarWidth;
        public float sessionControlBarHeight;
        public float sessionControlBarDepth;
        public Color sessionControlBarBackgroundColor;
        public Color sessionControlBarPauseColor;
        public Color sessionControlBarResumeColor;
        public Color sessionControlBarEndColor;
    }

    private float sessionControlHoldTimer;
    private bool sessionControlWasHolding;
    private float sessionControlCooldownUntil;
    private GameObject sessionControlBarRoot;
    private Transform sessionControlBarFill;
    private Renderer sessionControlBarBgRenderer;
    private Renderer sessionControlBarFillRenderer;
    private TextMesh sessionControlActionLabel;

    public void Update(WaypointSystemManager waypointManager, Transform cameraTransform, Settings settings)
    {
        if (!settings.useDwellSessionControls || waypointManager == null || cameraTransform == null)
        {
            sessionControlHoldTimer = 0f;
            sessionControlWasHolding = false;
            HideBar();
            return;
        }

        if (!waypointManager.IsSessionActive)
        {
            sessionControlHoldTimer = 0f;
            sessionControlWasHolding = false;
            HideBar();
            return;
        }

        if (Time.time < sessionControlCooldownUntil)
        {
            sessionControlHoldTimer = 0f;
            sessionControlWasHolding = false;
            HideBar();
            return;
        }

        float lookUpThresholdDot = Mathf.Sin(Mathf.Deg2Rad * Mathf.Clamp(settings.sessionControlLookUpAngle, 8f, 60f));
        float lookUpDot = Vector3.Dot(cameraTransform.forward.normalized, Vector3.up);
        bool isHoldingControl = lookUpDot >= lookUpThresholdDot;

        if (isHoldingControl)
        {
            sessionControlHoldTimer += Time.deltaTime;
            sessionControlWasHolding = true;

            float shortThreshold = Mathf.Max(0.25f, settings.sessionControlDwellSeconds);
            float longThreshold = Mathf.Max(shortThreshold + 0.2f, settings.sessionEndDwellSeconds);
            bool paused = waypointManager.IsSessionPaused;
            bool canEnd = !settings.requirePauseBeforeEnd || paused;

            string actionLabel;
            Color actionColor;
            float actionProgress;

            if (paused)
            {
                if (canEnd && sessionControlHoldTimer > shortThreshold)
                {
                    actionLabel = "Hold to End";
                    actionColor = settings.sessionControlBarEndColor;
                    float endStageDuration = Mathf.Max(0.2f, longThreshold - shortThreshold);
                    actionProgress = (sessionControlHoldTimer - shortThreshold) / endStageDuration;
                }
                else
                {
                    actionLabel = canEnd ? "Release to Resume" : "Resume";
                    actionColor = settings.sessionControlBarResumeColor;
                    actionProgress = sessionControlHoldTimer / shortThreshold;
                }
            }
            else
            {
                actionLabel = "Pause";
                actionColor = settings.sessionControlBarPauseColor;
                actionProgress = sessionControlHoldTimer / shortThreshold;
            }

            UpdateBar(cameraTransform, settings, actionProgress, actionLabel, actionColor);
            return;
        }

        if (!sessionControlWasHolding)
            return;

        float heldSeconds = sessionControlHoldTimer;
        sessionControlHoldTimer = 0f;
        sessionControlWasHolding = false;
        HideBar();

        if (heldSeconds >= settings.sessionEndDwellSeconds && (!settings.requirePauseBeforeEnd || waypointManager.IsSessionPaused))
        {
            waypointManager.EndSessionEarly();
            sessionControlCooldownUntil = Time.time + settings.sessionControlCooldownSeconds;
            return;
        }

        if (heldSeconds >= settings.sessionControlDwellSeconds)
        {
            if (waypointManager.IsSessionPaused)
                waypointManager.ResumeSession();
            else
                waypointManager.PauseSession();

            sessionControlCooldownUntil = Time.time + settings.sessionControlCooldownSeconds;
        }
    }

    private void EnsureBar(Settings settings)
    {
        if (sessionControlBarRoot != null)
            return;

        sessionControlBarRoot = new GameObject("SessionControlProgressBar");

        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bgObj.name = "SessionControlBarBackground";
        Object.Destroy(bgObj.GetComponent<Collider>());
        bgObj.transform.SetParent(sessionControlBarRoot.transform, false);
        bgObj.transform.localScale = new Vector3(settings.sessionControlBarWidth, settings.sessionControlBarHeight, settings.sessionControlBarDepth);
        sessionControlBarBgRenderer = bgObj.GetComponent<Renderer>();

        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fillObj.name = "SessionControlBarFill";
        Object.Destroy(fillObj.GetComponent<Collider>());
        fillObj.transform.SetParent(sessionControlBarRoot.transform, false);
        sessionControlBarFill = fillObj.transform;
        sessionControlBarFill.localScale = new Vector3(0.0001f, settings.sessionControlBarHeight * 0.8f, settings.sessionControlBarDepth * 0.8f);
        sessionControlBarFill.localPosition = new Vector3(-settings.sessionControlBarWidth * 0.5f, 0f, -0.001f);
        sessionControlBarFillRenderer = fillObj.GetComponent<Renderer>();

        GameObject labelObj = new GameObject("SessionControlActionLabel");
        labelObj.transform.SetParent(sessionControlBarRoot.transform, false);
        sessionControlActionLabel = labelObj.AddComponent<TextMesh>();
        sessionControlActionLabel.fontSize = 48;
        sessionControlActionLabel.characterSize = 0.007f;
        sessionControlActionLabel.alignment = TextAlignment.Center;
        sessionControlActionLabel.anchor = TextAnchor.LowerCenter;
        sessionControlActionLabel.color = Color.white;
        sessionControlActionLabel.text = "";
        labelObj.transform.localPosition = new Vector3(0f, settings.sessionControlBarHeight * 1.5f, -0.001f);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            if (sessionControlBarBgRenderer != null)
            {
                sessionControlBarBgRenderer.material = new Material(shader);
                sessionControlBarBgRenderer.material.color = settings.sessionControlBarBackgroundColor;
            }
            if (sessionControlBarFillRenderer != null)
            {
                sessionControlBarFillRenderer.material = new Material(shader);
                sessionControlBarFillRenderer.material.color = settings.sessionControlBarPauseColor;
            }
        }

        sessionControlBarRoot.SetActive(false);
    }

    private void HideBar()
    {
        if (sessionControlBarRoot != null)
            sessionControlBarRoot.SetActive(false);
    }

    private void UpdateBar(Transform cameraTransform, Settings settings, float progress, string actionLabel, Color fillColor)
    {
        EnsureBar(settings);
        if (sessionControlBarRoot == null || sessionControlBarFill == null || cameraTransform == null)
            return;

        sessionControlBarRoot.SetActive(true);

        Vector3 targetPos = cameraTransform.position
            + cameraTransform.forward * Mathf.Max(0.2f, settings.sessionControlBarDistance)
            + cameraTransform.up * settings.sessionControlBarVerticalOffset;

        Vector3 toCamera = cameraTransform.position - targetPos;
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = -cameraTransform.forward;

        sessionControlBarRoot.transform.position = targetPos;
        sessionControlBarRoot.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);

        float clampedProgress = Mathf.Clamp01(progress);
        float fillWidth = Mathf.Max(0.0001f, settings.sessionControlBarWidth * clampedProgress);
        sessionControlBarFill.localScale = new Vector3(fillWidth, settings.sessionControlBarHeight * 0.8f, settings.sessionControlBarDepth * 0.8f);
        sessionControlBarFill.localPosition = new Vector3((-settings.sessionControlBarWidth * 0.5f) + (fillWidth * 0.5f), 0f, -0.001f);

        if (sessionControlBarFillRenderer != null)
            sessionControlBarFillRenderer.material.color = fillColor;

        if (sessionControlActionLabel != null)
            sessionControlActionLabel.text = actionLabel;
    }
}
