using UnityEngine;

public class DriftArrowController
{
    public struct Settings
    {
        public bool enableDriftDirectionArrow;
        public float driftArrowShowBuffer;
        public float driftArrowDistance;
        public float driftArrowVerticalOffset;
        public float driftArrowScale;
        public Color driftArrowBaseColor;
        public float offCourseTolerance;
    }

    private GameObject driftArrowObject;
    private TextMesh driftArrowText;
    private Renderer driftArrowRenderer;

    public void StartOrEnable(Settings settings)
    {
        if (!settings.enableDriftDirectionArrow)
        {
            SetEnabled(false);
            return;
        }

        EnsureArrowObject(settings);
        if (driftArrowObject != null)
        {
            driftArrowObject.transform.localScale = Vector3.one * Mathf.Max(0.005f, settings.driftArrowScale);
            driftArrowObject.SetActive(false);
        }
    }

    public void Update(
        Settings settings,
        bool sessionActive,
        bool hasStraightLinePath,
        Transform playerCamera,
        Vector3 straightLineStart,
        Vector3 straightLineEnd,
        float severity)
    {
        if (driftArrowObject == null && settings.enableDriftDirectionArrow)
        {
            EnsureArrowObject(settings);
        }

        if (!settings.enableDriftDirectionArrow || !sessionActive || !hasStraightLinePath || playerCamera == null || driftArrowObject == null || driftArrowText == null)
        {
            SetEnabled(false);
            return;
        }

        float tolerance = Mathf.Max(0.1f, settings.offCourseTolerance) + Mathf.Max(0f, settings.driftArrowShowBuffer);
        float signedLateralDistance = OffCourseTracker.SignedDistanceToInfiniteLineXZ(playerCamera.position, straightLineStart, straightLineEnd);
        float absDistance = Mathf.Abs(signedLateralDistance);

        if (absDistance <= tolerance)
        {
            driftArrowObject.SetActive(false);
            return;
        }

        // Keep current behavior: positive signed distance shows right arrow.
        driftArrowText.text = signedLateralDistance > 0f ? "→" : "←";

        Color arrowColor = Color.Lerp(settings.driftArrowBaseColor, Color.red, severity);
        driftArrowText.color = arrowColor;
        if (driftArrowRenderer != null)
        {
            driftArrowRenderer.material.color = arrowColor;
        }

        Vector3 arrowPos = playerCamera.position
                           + playerCamera.forward * settings.driftArrowDistance
                           + playerCamera.up * settings.driftArrowVerticalOffset;
        driftArrowObject.transform.position = arrowPos;

        Vector3 toCamera = playerCamera.position - arrowPos;
        if (toCamera.sqrMagnitude < 0.0001f)
        {
            toCamera = -playerCamera.forward;
        }
        driftArrowObject.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        driftArrowObject.SetActive(true);
    }

    public void SetEnabled(bool enabled)
    {
        if (driftArrowObject != null)
        {
            driftArrowObject.SetActive(enabled);
        }
    }

    private void EnsureArrowObject(Settings settings)
    {
        if (driftArrowObject != null)
            return;

        driftArrowObject = new GameObject("DriftDirectionArrow");
        driftArrowText = driftArrowObject.AddComponent<TextMesh>();
        driftArrowText.text = "←";
        driftArrowText.fontSize = 96;
        driftArrowText.anchor = TextAnchor.MiddleCenter;
        driftArrowText.alignment = TextAlignment.Center;
        driftArrowText.color = settings.driftArrowBaseColor;

        driftArrowRenderer = driftArrowObject.GetComponent<Renderer>();
    }
}
