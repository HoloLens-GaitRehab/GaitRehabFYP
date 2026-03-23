using UnityEngine;

public class MetronomeController
{
    public struct Settings
    {
        public GameObject metronomePrefab;
        public float metronomeBPM;
        public float metronomeSpeedMultiplier;
        public Color metronomeColor;
        public float metronomeHeightOffset;
        public bool showMetronomeArc;
        public Color metronomeArcColor;
        public float metronomeArcWidth;
        public int metronomeArcSegments;
        public AudioClip metronomeTickClip;
        public float metronomeTickVolume;
        public bool useGeneratedTickFallback;
        public float generatedTickFrequencyHz;
        public float generatedTickDurationSeconds;
    }

    private GameObject metronomeObject;
    private Renderer metronomeRenderer;
    private AudioSource metronomeAudioSource;
    private AudioClip generatedMetronomeTickClip;
    private float generatedTickFrequencyCached = -1f;
    private float generatedTickDurationCached = -1f;
    private float metronomeBeatTime;
    private float previousMetronomeAngle;
    private bool hasPreviousMetronomeAngle;
    private float lastMetronomeTickTime = -10f;
    private GameObject metronomeArcObject;
    private LineRenderer metronomeArcLine;

    public void StartOrEnable(MonoBehaviour owner, Settings settings)
    {
        if (metronomeObject == null)
        {
            SetupMetronome(owner, settings);
        }
        else
        {
            metronomeObject.SetActive(true);
            if (metronomeArcObject != null)
            {
                metronomeArcObject.SetActive(settings.showMetronomeArc);
            }
        }

        hasPreviousMetronomeAngle = false;
        lastMetronomeTickTime = -10f;

        float effectiveBpm = Mathf.Max(1f, settings.metronomeBPM * Mathf.Max(0.1f, settings.metronomeSpeedMultiplier));
        metronomeBeatTime = 60f / effectiveBpm;
    }

    public void SetEnabled(bool enabled, bool showArc)
    {
        if (metronomeObject != null)
        {
            metronomeObject.SetActive(enabled);
        }

        if (metronomeArcObject != null)
        {
            metronomeArcObject.SetActive(enabled && showArc);
        }
    }

    public void Update(Transform playerCamera, Settings settings, float offCourseSeverity)
    {
        if (playerCamera == null || metronomeObject == null)
            return;

        // Get center position along full gaze direction (horizontal + vertical)
        Vector3 forward = playerCamera.forward.normalized;
        Vector3 centerPos = playerCamera.position + forward * 1.5f;
        centerPos += playerCamera.up * settings.metronomeHeightOffset;

        // Calculate pendulum swing on semicircular arc
        float beatProgress = (Time.time % metronomeBeatTime) / metronomeBeatTime;
        beatProgress = (beatProgress + 0.5f) % 1f;
        float angle = Mathf.Sin(beatProgress * Mathf.PI * 2f) * 45f;
        TryPlayMetronomeCenterTick(settings, angle);

        float angleRad = angle * Mathf.Deg2Rad;
        float arcRadius = 0.3f;
        Vector3 right = playerCamera.right.normalized;

        if (settings.showMetronomeArc)
        {
            if (metronomeArcLine == null)
            {
                SetupMetronomeArc(settings);
            }
            if (metronomeArcLine != null)
            {
                UpdateMetronomeArc(centerPos, right, arcRadius, settings);
            }
        }
        else if (metronomeArcObject != null)
        {
            metronomeArcObject.SetActive(false);
        }

        float xOffset = Mathf.Sin(angleRad) * arcRadius;
        float yOffset = Mathf.Cos(angleRad) * arcRadius;

        Vector3 metronomePos = centerPos + (right * xOffset);
        metronomePos.y = centerPos.y + yOffset;
        metronomeObject.transform.position = metronomePos;

        if (metronomeRenderer != null)
        {
            Color currentMetronomeColor = Color.Lerp(Color.white, Color.red, offCourseSeverity);
            metronomeRenderer.material.color = currentMetronomeColor;

            if (settings.showMetronomeArc && metronomeArcLine != null)
            {
                float targetAlpha = Mathf.Clamp01(settings.metronomeArcColor.a);
                Color baseArcColor = new Color(Color.white.r, Color.white.g, Color.white.b, targetAlpha);
                Color offCourseArcColor = new Color(Color.red.r, Color.red.g, Color.red.b, targetAlpha);
                Color currentArcColor = Color.Lerp(baseArcColor, offCourseArcColor, offCourseSeverity);

                metronomeArcLine.startColor = currentArcColor;
                metronomeArcLine.endColor = currentArcColor;
                if (metronomeArcLine.material != null)
                {
                    metronomeArcLine.material.color = currentArcColor;
                }
            }

            float intensity = Mathf.Abs(angle) > 35f ? 3f : 1.5f;
            metronomeRenderer.material.SetColor("_EmissionColor", currentMetronomeColor * intensity);
        }
    }

    private void SetupMetronome(MonoBehaviour owner, Settings settings)
    {
        if (settings.metronomePrefab != null)
        {
            metronomeObject = Object.Instantiate(settings.metronomePrefab, Vector3.zero, Quaternion.identity);
            metronomeObject.transform.localScale = Vector3.one * 0.02f;
        }
        else
        {
            metronomeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            metronomeObject.transform.localScale = Vector3.one * 0.02f;
            Object.Destroy(metronomeObject.GetComponent<Collider>());
        }

        metronomeObject.name = "Metronome";
        metronomeRenderer = metronomeObject.GetComponent<Renderer>();

        if (metronomeAudioSource == null)
        {
            metronomeAudioSource = owner.GetComponent<AudioSource>();
            if (metronomeAudioSource == null)
            {
                metronomeAudioSource = owner.gameObject.AddComponent<AudioSource>();
            }
        }
        metronomeAudioSource.playOnAwake = false;
        metronomeAudioSource.spatialBlend = 0f;
        metronomeAudioSource.loop = false;

        if (metronomeRenderer != null)
        {
            metronomeRenderer.material.color = settings.metronomeColor;
            metronomeRenderer.material.EnableKeyword("_EMISSION");
        }

        if (settings.showMetronomeArc)
        {
            SetupMetronomeArc(settings);
        }

        float effectiveBpm = Mathf.Max(1f, settings.metronomeBPM * Mathf.Max(0.1f, settings.metronomeSpeedMultiplier));
        metronomeBeatTime = 60f / effectiveBpm;
        Debug.Log("Visual metronome enabled at " + effectiveBpm + " BPM");
    }

    private void SetupMetronomeArc(Settings settings)
    {
        if (metronomeArcObject == null)
        {
            metronomeArcObject = new GameObject("MetronomeArc");
            metronomeArcLine = metronomeArcObject.AddComponent<LineRenderer>();
        }

        metronomeArcObject.SetActive(true);
        metronomeArcLine.useWorldSpace = true;
        metronomeArcLine.positionCount = Mathf.Max(2, settings.metronomeArcSegments + 1);
        metronomeArcLine.startWidth = settings.metronomeArcWidth;
        metronomeArcLine.endWidth = settings.metronomeArcWidth;

        Shader arcShader = Shader.Find("Sprites/Default");
        if (arcShader == null)
        {
            arcShader = Shader.Find("Unlit/Color");
        }
        if (arcShader != null)
        {
            if (metronomeArcLine.material == null)
            {
                metronomeArcLine.material = new Material(arcShader);
            }
            metronomeArcLine.material.color = settings.metronomeArcColor;
        }
        metronomeArcLine.startColor = settings.metronomeArcColor;
        metronomeArcLine.endColor = settings.metronomeArcColor;
    }

    private void UpdateMetronomeArc(Vector3 centerPos, Vector3 right, float arcRadius, Settings settings)
    {
        int segments = Mathf.Max(2, settings.metronomeArcSegments);
        if (metronomeArcLine.positionCount != segments + 1)
        {
            metronomeArcLine.positionCount = segments + 1;
        }

        float startAngle = -45f;
        float endAngle = 45f;
        float step = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + step * i;
            float angleRad = angle * Mathf.Deg2Rad;
            float xOffset = Mathf.Sin(angleRad) * arcRadius;
            float yOffset = Mathf.Cos(angleRad) * arcRadius;
            Vector3 pos = centerPos + (right * xOffset);
            pos.y = centerPos.y + yOffset;
            metronomeArcLine.SetPosition(i, pos);
        }
    }

    private void TryPlayMetronomeCenterTick(Settings settings, float currentAngle)
    {
        AudioClip tickClip = GetMetronomeTickClip(settings);
        if (tickClip == null || metronomeAudioSource == null)
        {
            previousMetronomeAngle = currentAngle;
            hasPreviousMetronomeAngle = true;
            return;
        }

        if (!hasPreviousMetronomeAngle)
        {
            previousMetronomeAngle = currentAngle;
            hasPreviousMetronomeAngle = true;
            return;
        }

        bool crossedCenter = (previousMetronomeAngle < 0f && currentAngle >= 0f)
                             || (previousMetronomeAngle > 0f && currentAngle <= 0f);

        float minTickInterval = Mathf.Max(0.08f, metronomeBeatTime * 0.2f);
        bool canTick = Time.time - lastMetronomeTickTime >= minTickInterval;

        if (crossedCenter && canTick)
        {
            metronomeAudioSource.PlayOneShot(tickClip, settings.metronomeTickVolume);
            lastMetronomeTickTime = Time.time;
        }

        previousMetronomeAngle = currentAngle;
    }

    private AudioClip GetMetronomeTickClip(Settings settings)
    {
        if (settings.metronomeTickClip != null)
        {
            return settings.metronomeTickClip;
        }

        if (!settings.useGeneratedTickFallback)
        {
            return null;
        }

        float safeFrequency = Mathf.Clamp(settings.generatedTickFrequencyHz, 300f, 5000f);
        float safeDuration = Mathf.Clamp(settings.generatedTickDurationSeconds, 0.01f, 0.2f);

        if (generatedMetronomeTickClip == null
            || !Mathf.Approximately(generatedTickFrequencyCached, safeFrequency)
            || !Mathf.Approximately(generatedTickDurationCached, safeDuration))
        {
            generatedTickFrequencyCached = safeFrequency;
            generatedTickDurationCached = safeDuration;
            generatedMetronomeTickClip = BuildGeneratedTickClip(safeFrequency, safeDuration);
        }

        return generatedMetronomeTickClip;
    }

    private AudioClip BuildGeneratedTickClip(float frequencyHz, float durationSeconds)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("GeneratedMetronomeTick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
