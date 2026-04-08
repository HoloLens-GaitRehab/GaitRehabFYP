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
    }

    private GameObject completionOverlayCanvas;
    private Text completionOverlayTitle;
    private Text completionOverlayBody;
    private bool completionOverlayShown;

    public void ResetForNewSession()
    {
        completionOverlayShown = false;
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
        completionOverlayCanvas.transform.localScale = Vector3.one * Mathf.Clamp(settings.completionOverlayScale, 0.0007f, 0.003f);
        completionOverlayCanvas.transform.localPosition = new Vector3(
            0f,
            settings.completionOverlayVerticalOffset,
            Mathf.Max(2.2f, settings.completionOverlayDistance));
        completionOverlayCanvas.transform.localRotation = Quaternion.identity;
    }

    public void UpdateState(WaypointSystemManager waypointManager, Transform cameraTransform, Settings settings)
    {
        if (!settings.autoShowCompletionOverlay || waypointManager == null)
        {
            completionOverlayShown = false;
            Hide();
            return;
        }

        if (waypointManager.IsSessionActive)
        {
            completionOverlayShown = false;
            Hide();
            return;
        }

        if (!waypointManager.IsSessionCompleted)
        {
            completionOverlayShown = false;
            Hide();
            return;
        }

        if (completionOverlayShown)
            return;

        string statsText = waypointManager.GetStatsText();
        if (string.IsNullOrWhiteSpace(statsText))
            return;

        Ensure(cameraTransform, settings);
        ApplyText(statsText, settings.completionOverlayAccentColor);

        completionOverlayCanvas.SetActive(true);
        completionOverlayShown = true;
    }

    public void Hide()
    {
        if (completionOverlayCanvas != null && completionOverlayCanvas.activeSelf)
        {
            completionOverlayCanvas.SetActive(false);
        }
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
