using UnityEngine;

[CreateAssetMenu(fileName = "RehabUiTheme", menuName = "GaitRehab/UI Theme")]
public class RehabUiTheme : ScriptableObject
{
    [Header("Stats Panel")]
    public Color statsPanelBackgroundColor = new Color(0.02f, 0.08f, 0.12f, 0.74f);
    public Color statsPanelTextColor = new Color(0.9f, 0.96f, 1f, 1f);
    public int statsPanelFontSize = 24;

    [Header("Fallback Buttons")]
    public Color statsButtonColor = new Color(0.06f, 0.58f, 0.46f, 0.95f);
    public Color metronomeButtonColor = new Color(0.09f, 0.35f, 0.78f, 0.95f);

    [Header("Completion Overlay")]
    public Color completionBackgroundColor = new Color(0.03f, 0.07f, 0.12f, 0.92f);
    public Color completionAccentColor = new Color(0.2f, 0.84f, 0.72f, 0.95f);
    public Color completionTextColor = new Color(0.93f, 0.97f, 1f, 1f);
    public float completionDistance = 3.3f;
    public float completionScale = 0.00135f;
    public Vector2 completionSize = new Vector2(760f, 440f);
    public int completionTitleFontSize = 46;
    public int completionBodyFontSize = 34;
    public int completionTitleMinFontSize = 24;
    public int completionBodyMinFontSize = 18;
    public float completionFadeSeconds = 0.28f;
    public float completionEntryScale = 0.93f;

    [Header("Dwell Bars")]
    public Color startDwellBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
    public Color startDwellFillColor = new Color(0.2f, 0.9f, 0.2f, 0.95f);
    public Color sessionDwellBackgroundColor = new Color(0f, 0f, 0f, 0.78f);
    public Color sessionDwellPauseColor = new Color(1f, 0.8f, 0.2f, 0.95f);
    public Color sessionDwellResumeColor = new Color(0.2f, 0.9f, 0.35f, 0.95f);
    public Color sessionDwellEndColor = new Color(1f, 0.3f, 0.3f, 0.95f);
}
