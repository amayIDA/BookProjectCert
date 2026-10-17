using UnityEngine;

namespace MotionStudio
{
    /// <summary>
    /// Animates a <see cref="MotionGlowGraphic"/>: the light grows to the size you set, comes into focus, brightens, and can keep
    /// moving afterwards (scrolling gradient, color cycling, breathing, slow spin).
    /// </summary>
    [AddComponentMenu("Motion Studio/Motion Glow")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MotionGlowGraphic))]
    public class MotionGlow : MotionBehaviour
    {
        [Header("Grow in")]
        [Min(0.01f)] public float duration = 1.6f;
        public MotionEase ease = MotionEase.AppleOut;

        [Tooltip("Size when the light starts, as a part of the size you set in the scene. 0.2 means a fifth.")]
        [Range(0f, 2f)] public float growFrom = 0.2f;

        [Tooltip("Opacity when the light starts.")]
        [Range(0f, 1f)] public float fadeFrom = 0f;

        [Tooltip("Seconds the fade takes. 0 uses 70 percent of Duration.")]
        [Min(0f)] public float fadeDuration = 0f;
        public MotionEase fadeEase = MotionEase.CubicOut;

        [Header("Tint color (on top of the gradient)")]
        [Tooltip("Turn on to move the tint of the light from the initial to the final color while it grows.")]
        public bool changeColor;
        public Color colorFrom = Color.white;
        public Color colorTo = Color.white;
        [Tooltip("Seconds the color change takes. 0 uses Duration.")]
        [Min(0f)] public float colorDuration = 0f;
        public MotionEase colorEase = MotionEase.SineInOut;

        [Tooltip("Brightness when the light starts, as a part of the final brightness.")]
        [Range(0f, 2f)] public float brightnessFrom = 0f;

        [Tooltip("The light comes into focus: it starts this much blurrier than the final blur.")]
        [Range(0f, 1f)] public float extraBlurFrom = 0.4f;

        [Header("Colors over time")]
        public bool cycleColors;
        public Gradient palette = DefaultPalette();
        [Min(0.5f)] public float cycleSeconds = 8f;

        [Header("Keeps moving after it arrived")]
        [Tooltip("Degrees per second.")]
        public float spin;
        [Range(0f, 0.5f)] public float breathe = 0f;
        [Min(0.5f)] public float breatheSeconds = 5f;
        [Min(0f)] public float drift = 0f;
        [Min(0.5f)] public float driftSeconds = 11f;

        [Header("Designed state (recorded from the scene)")]
        public bool recorded;
        public Vector3 restScale = Vector3.one;
        public Vector2 restPosition;
        public float restRotation;
        public float restAlpha = 1f;
        public float restBrightness = 1.4f;
        public float restBlur = 0.35f;
        public Color restTint = Color.white;

        MotionGlowGraphic glow;
        RectTransform rect;
        bool arrived;

        static Gradient DefaultPalette()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.35f, 0.62f, 1f), 0f),
                    new GradientColorKey(new Color(0.72f, 0.45f, 1f), 0.35f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.75f), 0.7f),
                    new GradientColorKey(new Color(0.35f, 0.62f, 1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        void Cache()
        {
            if (glow == null)
                glow = GetComponent<MotionGlowGraphic>();
            if (rect == null)
                rect = (RectTransform)transform;
        }

        public override void Prepare()
        {
            Cache();
            if (!recorded)
                Record();
        }

        [ContextMenu("Record Rest Pose")]
        public void Record()
        {
            Cache();
            restScale = rect.localScale;
            restPosition = rect.anchoredPosition;
            restRotation = rect.localEulerAngles.z;
            restAlpha = glow.color.a;
            restBrightness = glow.brightness;
            restBlur = glow.blur;
            restTint = glow.color;
            recorded = true;
        }

        void Awake()
        {
            Prepare();
        }

        public override float ContentDuration
        {
            get { return duration; }
        }

        protected override void EvaluateContent(float time)
        {
            Prepare();
            float p = Mathf.Clamp01(time / Mathf.Max(0.0001f, duration));
            float e = MotionEasing.Evaluate(ease, p);

            float s = Mathf.LerpUnclamped(growFrom, 1f, e);
            rect.localScale = new Vector3(restScale.x * s, restScale.y * s, restScale.z);
            rect.localRotation = Quaternion.Euler(0f, 0f, restRotation);
            rect.anchoredPosition = restPosition;

            float fadeTime = fadeDuration > 0f ? fadeDuration : duration * 0.7f;
            float fadeValue = time <= 0f ? fadeFrom : Mathf.Lerp(fadeFrom, 1f, MotionEasing.Evaluate(fadeEase, Mathf.Clamp01(time / Mathf.Max(0.0001f, fadeTime))));
            Color c = glow.color;
            if (changeColor)
            {
                float colorTime = colorDuration > 0f ? colorDuration : duration;
                Color tint = Color.LerpUnclamped(colorFrom, colorTo, MotionEasing.Evaluate(colorEase, Mathf.Clamp01(time / Mathf.Max(0.0001f, colorTime))));
                c.r = tint.r;
                c.g = tint.g;
                c.b = tint.b;
            }

            c.a = restAlpha * fadeValue;
            glow.color = c;
            glow.brightness = restBrightness * Mathf.Lerp(brightnessFrom, 1f, MotionEasing.Evaluate(MotionEase.CubicOut, p));
            glow.blur = Mathf.Clamp01(restBlur + extraBlurFrom * (1f - MotionEasing.Evaluate(MotionEase.CubicOut, p)));

            arrived = time >= duration;
            glow.PushProperties();
            glow.SetVerticesDirty();
        }

        public override void ApplyRestState()
        {
            Prepare();
            arrived = false;
            rect.localScale = restScale;
            rect.anchoredPosition = restPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, restRotation);
            Color c = restTint;
            c.a = restAlpha;
            glow.color = c;
            glow.brightness = restBrightness;
            glow.blur = restBlur;
            glow.PushProperties();
            glow.SetVerticesDirty();
        }

        void Update()
        {
            if (!Application.isPlaying || !arrived || glow == null)
                return;

            float t = Time.unscaledTime;
            if (cycleColors)
            {
                float u = Mathf.Repeat(t / cycleSeconds, 1f);
                glow.colorA = palette.Evaluate(u);
                glow.colorB = palette.Evaluate(Mathf.Repeat(u + 0.5f, 1f));
                glow.PushProperties();
            }

            if (Mathf.Abs(spin) > 0.001f)
                rect.localRotation = Quaternion.Euler(0f, 0f, restRotation + t * spin);

            if (breathe > 0f)
            {
                float k = 1f + Mathf.Sin(t / breatheSeconds * Mathf.PI * 2f) * breathe;
                rect.localScale = new Vector3(restScale.x * k, restScale.y * k, restScale.z);
            }

            if (drift > 0f)
            {
                float phase = t / driftSeconds * Mathf.PI * 2f;
                rect.anchoredPosition = restPosition + new Vector2(Mathf.Sin(phase) * drift, Mathf.Cos(phase * 0.8f) * drift * 0.6f);
            }
        }
    }
}
