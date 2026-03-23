using UnityEngine;

public class StraightPathGuide
{
    public struct Settings
    {
        public Color straightGuideLineColor;
        public float straightGuideLineAlpha;
        public float straightGuideLineWidth;
        public float straightGuideLineLength;
        public float straightGuideLineFloorOffset;

        public bool enableLineEndMarker;
        public Color lineEndMarkerColor;
        public float lineEndMarkerHeight;
        public float lineEndMarkerWidth;

        public bool enableConditionalRails;
        public float railSpacing;
        public float railWidth;
        public float railBaseAlpha;
        public float railMaxAlpha;
        public bool railsFollowOffCourseBoundary;
    }

    public Vector3 LineStart { get; private set; }
    public Vector3 LineEnd { get; private set; }
    public bool HasPath { get; private set; }

    private GameObject straightGuideLineObject;
    private LineRenderer straightGuideLine;
    private GameObject leftRailObject;
    private LineRenderer leftRailLine;
    private GameObject rightRailObject;
    private LineRenderer rightRailLine;
    private GameObject lineEndMarkerObject;
    private LineRenderer lineEndMarkerLine;

    public void StartOrUpdate(
        Transform playerCamera,
        Settings settings,
        float assumedEyeHeight,
        float offCourseTolerance,
        float driftArrowShowBuffer,
        float severity)
    {
        if (playerCamera == null)
            return;

        EnsureMainLine(settings);

        Color lineTint = GetStraightGuideLineTint(settings);
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

        float floorY = playerCamera.position.y - assumedEyeHeight + settings.straightGuideLineFloorOffset;
        Vector3 start = new Vector3(playerCamera.position.x, floorY, playerCamera.position.z);
        Vector3 end = start + forward * settings.straightGuideLineLength;

        LineStart = start;
        LineEnd = end;
        HasPath = true;

        straightGuideLine.SetPosition(0, start);
        straightGuideLine.SetPosition(1, end);

        UpdateEndMarker(settings);

        SetupConditionalRails(settings);
        UpdateRailGeometry(settings, offCourseTolerance, driftArrowShowBuffer);
        UpdateRailBrightening(true, playerCamera, offCourseTolerance, driftArrowShowBuffer, severity, settings);
    }

    public void UpdateRailBrightening(
        bool sessionActive,
        Transform playerCamera,
        float offCourseTolerance,
        float driftArrowShowBuffer,
        float severity,
        Settings settings)
    {
        if (!settings.enableConditionalRails || leftRailLine == null || rightRailLine == null)
            return;

        float triggerDistance = Mathf.Max(0.1f, offCourseTolerance) + Mathf.Max(0f, driftArrowShowBuffer);
        float currentDistance = HasPath && playerCamera != null
            ? Mathf.Abs(OffCourseTracker.SignedDistanceToInfiniteLineXZ(playerCamera.position, LineStart, LineEnd))
            : 0f;

        bool shouldShowRails = sessionActive && currentDistance > triggerDistance;
        if (leftRailObject != null)
        {
            leftRailObject.SetActive(shouldShowRails);
        }
        if (rightRailObject != null)
        {
            rightRailObject.SetActive(shouldShowRails);
        }

        if (!shouldShowRails)
            return;

        float alpha = Mathf.Lerp(Mathf.Clamp01(settings.railBaseAlpha), Mathf.Clamp01(settings.railMaxAlpha), severity);

        Color railTint = settings.straightGuideLineColor;
        railTint.a = alpha;

        leftRailLine.startColor = railTint;
        leftRailLine.endColor = railTint;
        rightRailLine.startColor = railTint;
        rightRailLine.endColor = railTint;

        if (leftRailLine.material != null)
        {
            leftRailLine.material.color = railTint;
        }
        if (rightRailLine.material != null)
        {
            rightRailLine.material.color = railTint;
        }
    }

    public void SetRailsEnabled(bool enabled)
    {
        if (leftRailObject != null)
        {
            leftRailObject.SetActive(enabled);
        }
        if (rightRailObject != null)
        {
            rightRailObject.SetActive(enabled);
        }
    }

    private void EnsureMainLine(Settings settings)
    {
        if (straightGuideLineObject != null)
            return;

        straightGuideLineObject = new GameObject("StraightGuideLine");
        straightGuideLine = straightGuideLineObject.AddComponent<LineRenderer>();
        straightGuideLine.useWorldSpace = true;
        straightGuideLine.positionCount = 2;
        straightGuideLine.startWidth = settings.straightGuideLineWidth;
        straightGuideLine.endWidth = settings.straightGuideLineWidth;
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
            straightGuideLine.material.color = GetStraightGuideLineTint(settings);
        }
    }

    private Color GetStraightGuideLineTint(Settings settings)
    {
        Color tint = settings.straightGuideLineColor;
        tint.a = Mathf.Clamp01(settings.straightGuideLineAlpha);
        return tint;
    }

    private void SetupConditionalRails(Settings settings)
    {
        if (!settings.enableConditionalRails)
        {
            if (leftRailObject != null) leftRailObject.SetActive(false);
            if (rightRailObject != null) rightRailObject.SetActive(false);
            return;
        }

        if (leftRailObject == null)
        {
            leftRailObject = new GameObject("LeftGuideRail");
            leftRailLine = leftRailObject.AddComponent<LineRenderer>();
            ConfigureGuideLineRenderer(leftRailLine, settings.railWidth);
        }

        if (rightRailObject == null)
        {
            rightRailObject = new GameObject("RightGuideRail");
            rightRailLine = rightRailObject.AddComponent<LineRenderer>();
            ConfigureGuideLineRenderer(rightRailLine, settings.railWidth);
        }

        leftRailObject.SetActive(true);
        rightRailObject.SetActive(true);
    }

    private void UpdateEndMarker(Settings settings)
    {
        if (!settings.enableLineEndMarker)
        {
            if (lineEndMarkerObject != null)
            {
                lineEndMarkerObject.SetActive(false);
            }
            return;
        }

        if (!HasPath)
            return;

        if (lineEndMarkerObject == null)
        {
            lineEndMarkerObject = new GameObject("LineEndMarker");
            lineEndMarkerLine = lineEndMarkerObject.AddComponent<LineRenderer>();
            lineEndMarkerLine.useWorldSpace = true;
            lineEndMarkerLine.positionCount = 2;
            lineEndMarkerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineEndMarkerLine.receiveShadows = false;

            Shader markerShader = Shader.Find("Sprites/Default");
            if (markerShader == null)
            {
                markerShader = Shader.Find("Unlit/Color");
            }
            if (markerShader != null)
            {
                lineEndMarkerLine.material = new Material(markerShader);
            }
        }

        if (lineEndMarkerObject != null)
        {
            lineEndMarkerObject.SetActive(true);
        }

        float markerWidth = Mathf.Max(0.015f, settings.lineEndMarkerWidth);
        float markerHeight = Mathf.Max(0.3f, settings.lineEndMarkerHeight);

        Vector3 markerStart = LineEnd;
        Vector3 markerTop = LineEnd + Vector3.up * markerHeight;

        lineEndMarkerLine.startWidth = markerWidth;
        lineEndMarkerLine.endWidth = markerWidth * 0.8f;
        lineEndMarkerLine.SetPosition(0, markerStart);
        lineEndMarkerLine.SetPosition(1, markerTop);

        Color markerTint = settings.lineEndMarkerColor;
        lineEndMarkerLine.startColor = markerTint;
        lineEndMarkerLine.endColor = markerTint;

        if (lineEndMarkerLine.material != null)
        {
            lineEndMarkerLine.material.color = markerTint;
        }
    }

    private void ConfigureGuideLineRenderer(LineRenderer line, float width)
    {
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Color");
        }
        if (lineShader != null)
        {
            line.material = new Material(lineShader);
        }
    }

    private void UpdateRailGeometry(Settings settings, float offCourseTolerance, float driftArrowShowBuffer)
    {
        if (!settings.enableConditionalRails || !HasPath || leftRailLine == null || rightRailLine == null)
            return;

        Vector3 pathDir = LineEnd - LineStart;
        pathDir.y = 0f;
        if (pathDir.sqrMagnitude < 0.0001f)
            return;

        pathDir.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, pathDir).normalized;
        float offset = Mathf.Max(0.05f, settings.railSpacing * 0.5f);
        if (settings.railsFollowOffCourseBoundary)
        {
            float boundaryOffset = Mathf.Max(0.05f, Mathf.Max(0.1f, offCourseTolerance) + Mathf.Max(0f, driftArrowShowBuffer));
            offset = Mathf.Max(offset, boundaryOffset);
        }

        Vector3 leftStart = LineStart - right * offset;
        Vector3 leftEnd = LineEnd - right * offset;
        Vector3 rightStart = LineStart + right * offset;
        Vector3 rightEnd = LineEnd + right * offset;

        leftRailLine.startWidth = settings.railWidth;
        leftRailLine.endWidth = settings.railWidth;
        rightRailLine.startWidth = settings.railWidth;
        rightRailLine.endWidth = settings.railWidth;

        leftRailLine.SetPosition(0, leftStart);
        leftRailLine.SetPosition(1, leftEnd);
        rightRailLine.SetPosition(0, rightStart);
        rightRailLine.SetPosition(1, rightEnd);
    }
}
