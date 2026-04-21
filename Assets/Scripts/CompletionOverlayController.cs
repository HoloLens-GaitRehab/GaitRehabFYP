using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CompletionOverlayController
{
    public struct Settings
    {
        public bool autoShowCompletionOverlay;
        public float completionOverlayDistance;
        public float completionOverlayVerticalOffset;
        public Vector2 completionOverlaySize;
        public float completionOverlayScale;
        public Color completionOverlayBackgroundColor;
        public Color completionOverlayAccentColor;
        public Color completionOverlayTextColor;
        public int completionTitleFontSize;
        public int completionBodyFontSize;
        public int completionTitleMinFontSize;
        public int completionBodyMinFontSize;
        public float completionFadeSeconds;
        public float completionEntryScale;
        public bool autoHideCompletionOverlay;
        public float completionOverlayAutoHideSeconds;
    }

    private GameObject completionOverlayCanvas;
    private CanvasGroup completionOverlayCanvasGroup;
    private Text completionOverlayTitle;
    private Text completionOverlayBody;
    private bool completionOverlayShown;
    private bool overlayTargetVisible;
    private float overlayVisibility;
    private float baseScale = 0.0013f;
    private float overlayAutoHideAtTime = -1f;
    private bool overlayDismissedForCurrentSession;

    public void ResetForNewSession()
    {
        completionOverlayShown = false;
        overlayTargetVisible = false;
        overlayVisibility = 0f;
        overlayAutoHideAtTime = -1f;
        overlayDismissedForCurrentSession = false;
        Hide();
    }

    public void Ensure(Transform cameraTransform, Settings settings)
    {
        if (completionOverlayCanvas != null)
            return;

        completionOverlayCanvas = new GameObject("SessionCompletionCanvas");
        Canvas overlayCanvas = completionOverlayCanvas.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.WorldSpace;
        completionOverlayCanvas.AddComponent<CanvasScaler>();
        completionOverlayCanvas.AddComponent<GraphicRaycaster>();
        completionOverlayCanvasGroup = completionOverlayCanvas.AddComponent<CanvasGroup>();
        completionOverlayCanvasGroup.alpha = 0f;

        RectTransform canvasRect = completionOverlayCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = settings.completionOverlaySize;

        GameObject completionOverlayPanel = new GameObject("SessionCompletionPanel");
        completionOverlayPanel.transform.SetParent(completionOverlayCanvas.transform, false);

        Image panelImage = completionOverlayPanel.AddComponent<Image>();
        panelImage.color = settings.completionOverlayBackgroundColor;
        completionOverlayPanel.AddComponent<RectMask2D>();

        RectTransform panelRect = completionOverlayPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = settings.completionOverlaySize;
        panelRect.anchoredPosition = Vector2.zero;

        GameObject accentObj = new GameObject("TopAccent");
        accentObj.transform.SetParent(completionOverlayPanel.transform, false);
        Image accentImage = accentObj.AddComponent<Image>();
        accentImage.color = settings.completionOverlayAccentColor;
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.sizeDelta = new Vector2(settings.completionOverlaySize.x * 0.86f, 12f);
        accentRect.anchoredPosition = new Vector2(0f, (settings.completionOverlaySize.y * 0.5f) - 28f);

        GameObject titleObj = new GameObject("CompletionTitle");
        titleObj.transform.SetParent(completionOverlayPanel.transform, false);
        completionOverlayTitle = titleObj.AddComponent<Text>();
        completionOverlayTitle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        completionOverlayTitle.fontSize = settings.completionTitleFontSize;
        completionOverlayTitle.fontStyle = FontStyle.Bold;
        completionOverlayTitle.color = settings.completionOverlayTextColor;
        completionOverlayTitle.alignment = TextAnchor.MiddleCenter;
        completionOverlayTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        completionOverlayTitle.verticalOverflow = VerticalWrapMode.Truncate;
        completionOverlayTitle.resizeTextForBestFit = true;
        completionOverlayTitle.resizeTextMinSize = Mathf.Max(14, settings.completionTitleMinFontSize);
        completionOverlayTitle.resizeTextMaxSize = Mathf.Max(completionOverlayTitle.resizeTextMinSize, settings.completionTitleFontSize);

        RectTransform titleRect = completionOverlayTitle.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(settings.completionOverlaySize.x - 80f, 90f);
        titleRect.anchoredPosition = new Vector2(0f, (settings.completionOverlaySize.y * 0.5f) - 88f);

        GameObject bodyObj = new GameObject("CompletionBody");
        bodyObj.transform.SetParent(completionOverlayPanel.transform, false);
        completionOverlayBody = bodyObj.AddComponent<Text>();
        completionOverlayBody.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        completionOverlayBody.fontSize = settings.completionBodyFontSize;
        completionOverlayBody.color = settings.completionOverlayTextColor;
        completionOverlayBody.alignment = TextAnchor.UpperLeft;
        completionOverlayBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        completionOverlayBody.verticalOverflow = VerticalWrapMode.Truncate;
        completionOverlayBody.lineSpacing = 0.92f;
        completionOverlayBody.resizeTextForBestFit = true;
        completionOverlayBody.resizeTextMinSize = Mathf.Max(12, settings.completionBodyMinFontSize);
        completionOverlayBody.resizeTextMaxSize = Mathf.Max(completionOverlayBody.resizeTextMinSize, settings.completionBodyFontSize);

        RectTransform bodyRect = completionOverlayBody.GetComponent<RectTransform>();
        bodyRect.sizeDelta = new Vector2(settings.completionOverlaySize.x - 100f, settings.completionOverlaySize.y - 180f);
        bodyRect.anchoredPosition = new Vector2(0f, -34f);

        completionOverlayCanvas.SetActive(false);
        UpdatePose(cameraTransform, settings);
    }

    public void UpdatePose(Transform cameraTransform, Settings settings)
    {
        if (completionOverlayCanvas == null || cameraTransform == null)
            return;

        completionOverlayCanvas.transform.SetParent(cameraTransform, false);
        baseScale = Mathf.Clamp(settings.completionOverlayScale, 0.0007f, 0.003f);
        completionOverlayCanvas.transform.localPosition = new Vector3(
            0f,
            settings.completionOverlayVerticalOffset,
            Mathf.Max(2.2f, settings.completionOverlayDistance));
        completionOverlayCanvas.transform.localRotation = Quaternion.identity;
        ApplyAnimatedScale(settings);
    }

    public void UpdateState(WaypointSystemManager waypointManager, Transform cameraTransform, Settings settings)
    {
        bool shouldShow = false;
        string statsText = null;

        if (settings.autoShowCompletionOverlay && waypointManager != null)
        {
            bool eligible = !waypointManager.IsSessionActive && waypointManager.IsSessionCompleted;
            if (eligible)
            {
                statsText = waypointManager.GetStatsText();
                shouldShow = !overlayDismissedForCurrentSession && !string.IsNullOrWhiteSpace(statsText);
            }
        }

        if (shouldShow)
        {
            Ensure(cameraTransform, settings);
            if (!completionOverlayShown)
            {
                ApplyText(statsText, settings.completionOverlayAccentColor);
                completionOverlayShown = true;

                if (settings.autoHideCompletionOverlay)
                {
                    overlayAutoHideAtTime = Time.time + Mathf.Max(1f, settings.completionOverlayAutoHideSeconds);
                }
                else
                {
                    overlayAutoHideAtTime = -1f;
                }
            }

            if (settings.autoHideCompletionOverlay && overlayAutoHideAtTime > 0f && Time.time >= overlayAutoHideAtTime)
            {
                shouldShow = false;
                completionOverlayShown = false;
                overlayDismissedForCurrentSession = true;
                overlayAutoHideAtTime = -1f;
            }
        }
        else
        {
            completionOverlayShown = false;
        }

        overlayTargetVisible = shouldShow;
        Animate(settings);
    }

    public void Hide()
    {
        overlayTargetVisible = false;
        overlayVisibility = 0f;
        overlayAutoHideAtTime = -1f;

        if (completionOverlayCanvasGroup != null)
            completionOverlayCanvasGroup.alpha = 0f;

        if (completionOverlayCanvas != null && completionOverlayCanvas.activeSelf)
            completionOverlayCanvas.SetActive(false);
    }

    private void Animate(Settings settings)
    {
        if (completionOverlayCanvas == null)
            return;

        if (overlayTargetVisible && !completionOverlayCanvas.activeSelf)
        {
            completionOverlayCanvas.SetActive(true);
        }

        float fadeDuration = Mathf.Max(0.05f, settings.completionFadeSeconds);
        float delta = Time.deltaTime / fadeDuration;
        overlayVisibility = Mathf.MoveTowards(overlayVisibility, overlayTargetVisible ? 1f : 0f, delta);

        float eased = overlayVisibility * overlayVisibility * (3f - (2f * overlayVisibility));
        if (completionOverlayCanvasGroup != null)
            completionOverlayCanvasGroup.alpha = eased;

        ApplyAnimatedScale(settings);

        if (!overlayTargetVisible && overlayVisibility <= 0.001f && completionOverlayCanvas.activeSelf)
        {
            completionOverlayCanvas.SetActive(false);
        }
    }

    private void ApplyAnimatedScale(Settings settings)
    {
        if (completionOverlayCanvas == null)
            return;

        float eased = overlayVisibility * overlayVisibility * (3f - (2f * overlayVisibility));
        float entryScale = Mathf.Clamp(settings.completionEntryScale, 0.75f, 1f);
        float scaleFactor = Mathf.Lerp(entryScale, 1f, eased);
        completionOverlayCanvas.transform.localScale = Vector3.one * (baseScale * scaleFactor);
    }

    private void ApplyText(string statsText, Color accentColor)
    {
        if (completionOverlayTitle == null || completionOverlayBody == null)
            return;

        string[] lines = statsText.Split('\n');
        if (lines.Length == 0)
        {
            completionOverlayTitle.text = "Session Summary";
            completionOverlayBody.text = statsText;
            return;
        }

        completionOverlayTitle.text = lines[0];

        string accentHex = ColorUtility.ToHtmlStringRGB(accentColor);
        StringBuilder bodyBuilder = new StringBuilder();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex > 0)
            {
                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex + 1).Trim();
                bodyBuilder.Append("<color=#");
                bodyBuilder.Append(accentHex);
                bodyBuilder.Append("><b>");
                bodyBuilder.Append(key);
                bodyBuilder.Append(":</b></color> ");
                bodyBuilder.Append(value);
            }
            else
            {
                bodyBuilder.Append(line.Trim());
            }

            if (i < lines.Length - 1)
            {
                bodyBuilder.Append("\n");
            }
        }

        completionOverlayBody.text = bodyBuilder.ToString();
    }
}
