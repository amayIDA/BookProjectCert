using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    /// <summary>
    /// Brings any UI object (image, text, panel with children) in: fade, slide from a side, scale, rotate and color change.
    /// For text it can do the same word by word, character by character or line by line.
    /// The position you place the object at in the scene is recorded and is where it ends up.
    /// </summary>
    [AddComponentMenu("Motion Studio/Motion Item")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MotionItem : MotionBehaviour
    {
        [Header("Timing")]
        [Min(0.01f)] public float duration = 0.8f;
        public MotionEase ease = MotionEase.AppleOut;

        [Header("Fade")]
        public bool fade = true;
        [Range(0f, 1f)] public float fadeFrom = 0f;

        [Tooltip("Seconds the fade takes. 0 uses three quarters of Duration. For text pieces it is the fade time of every piece.")]
        [Min(0f)] public float fadeDuration = 0f;
        public MotionEase fadeEase = MotionEase.CubicOut;

        [Header("Slide in from")]
        public MotionDirection direction = MotionDirection.None;

        [Tooltip("How far away the object starts, in canvas units.")]
        public float distance = 80f;

        [Tooltip("Used when Direction is Custom. The object starts at this offset from where it rests.")]
        public Vector2 customOffset = new Vector2(0f, -60f);

        [Header("Scale and rotate")]
        public bool scale;
        [Range(0f, 2f)] public float scaleFrom = 0.9f;
        public bool rotate;
        public float rotateFrom = -8f;

        [Header("Color change")]
        [Tooltip("Turn on to move from the initial color to the final color while the item arrives.")]
        public bool changeColor;
        [Tooltip("Initial color (at the start of the animation).")]
        public Color colorFrom = new Color(1f, 1f, 1f, 1f);
        [Tooltip("Final color (when the animation is done).")]
        public Color colorTo = new Color(1f, 1f, 1f, 1f);
        [Tooltip("Seconds the color change takes. 0 uses Duration.")]
        [Min(0f)] public float colorDuration = 0f;
        public MotionEase colorEase = MotionEase.SineInOut;

        [Header("Text pieces")]
        [Tooltip("Whole animates the text as one block. The other modes animate the pieces one after another.")]
        public TextSplit split = TextSplit.Whole;
        public TextOrder splitOrder = TextOrder.Forward;

        [Tooltip("Seconds between the start of one piece and the next.")]
        [Min(0.005f)] public float pieceStagger = 0.07f;

        [Tooltip("Seconds one piece takes to arrive. Leave at 0 to use Duration.")]
        [Min(0f)] public float pieceDuration = 0f;

        [Header("After arriving")]
        [Tooltip("Gently cycle the color over time once the item has arrived.")]
        public bool loopColor;
        public Gradient loopGradient = DefaultGradient();
        [Min(0.2f)] public float loopSeconds = 6f;

        [Tooltip("Slow floating movement once the item has arrived. 0 is off.")]
        [Min(0f)] public float floatAmount = 0f;
        [Min(0.2f)] public float floatSeconds = 5f;

        [Header("Designed state (recorded from the scene)")]
        public bool recorded;
        public Vector2 restPosition;
        public Vector3 restScale = Vector3.one;
        public float restRotation;
        public float restAlpha = 1f;
        public Color restColor = Color.white;

        RectTransform rect;
        CanvasGroup group;
        Graphic graphic;
        IMotionTextAdapter adapter;
        bool adapterTried;
        float arrivedAt = -1f;
        int[] ranks;
        int ranksCount = -1;
        TextOrder ranksOrder;
        bool shown;

        static Gradient DefaultGradient()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 1f, 1f), 0f), new GradientColorKey(new Color(0.93f, 0.84f, 0.6f), 0.5f), new GradientColorKey(new Color(1f, 1f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        // ---------- setup ----------

        void Cache()
        {
            if (rect == null)
                rect = (RectTransform)transform;
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            if (graphic == null)
                graphic = GetComponent<Graphic>();
            if (!adapterTried && graphic != null && split != TextSplit.Whole)
            {
                adapter = MotionTextAdapters.Create(graphic);
                adapterTried = true;
            }
        }

        public override void Prepare()
        {
            Cache();
            if (!recorded)
                Record();
        }

        /// <summary>Remember the current position, scale, rotation, opacity and color as the final state of the animation.</summary>
        [ContextMenu("Record Rest Pose")]
        public void Record()
        {
            Cache();
            if (adapter != null)
                adapter.Restore();
            restPosition = rect.anchoredPosition;
            restScale = rect.localScale;
            restRotation = rect.localEulerAngles.z;
            restAlpha = group.alpha;
            restColor = graphic != null ? graphic.color : Color.white;
            if (changeColor)
                colorTo = restColor;
            recorded = true;
        }

        void Reset()
        {
            Cache();
            Record();
            colorTo = restColor;
            colorFrom = restColor;
        }

        void Awake()
        {
            Cache();
            if (!recorded)
                Record();
        }

        // ---------- timing ----------

        float PieceDuration
        {
            get { return pieceDuration > 0f ? pieceDuration : duration; }
        }

        bool UsesPieces
        {
            get { return split != TextSplit.Whole && GetComponent<Graphic>() != null; }
        }

        int PieceCount()
        {
            if (!UsesPieces)
                return 0;
            Cache();
            if (adapter == null)
            {
                adapter = MotionTextAdapters.Create(graphic);
                adapterTried = true;
            }

            return adapter != null ? adapter.UnitCount(split) : 0;
        }

        public override float ContentDuration
        {
            get
            {
                int count = PieceCount();
                if (count > 0)
                    return (count - 1) * pieceStagger + PieceDuration;
                return duration;
            }
        }

        // ---------- evaluation ----------

        protected override void EvaluateContent(float time)
        {
            Cache();
            if (!recorded)
                Record();

            shown = time > 0f;
            arrivedAt = time >= ContentDuration ? 0f : -1f;

            if (UsesPieces && PieceCount() > 0)
            {
                EvaluatePieces(time);
                return;
            }

            if (adapter != null)
                adapter.Restore();

            float p = Mathf.Clamp01(time / Mathf.Max(0.0001f, duration));
            float e = MotionEasing.Evaluate(ease, p);

            float alpha = restAlpha;
            float fadeTime = fadeDuration > 0f ? fadeDuration : duration * 0.75f;
            if (fade)
                alpha = Mathf.Lerp(fadeFrom * restAlpha, restAlpha, MotionEasing.Evaluate(fadeEase, Mathf.Clamp01(time / Mathf.Max(0.0001f, fadeTime))));
            group.alpha = time <= 0f && fade ? fadeFrom * restAlpha : alpha;

            Vector2 offset = Vector2.zero;
            if (direction != MotionDirection.None)
            {
                Vector2 dir = MotionEasing.DirectionVector(direction, customOffset);
                offset = direction == MotionDirection.Custom ? customOffset * (1f - e) : dir * distance * (1f - e);
            }

            rect.anchoredPosition = restPosition + offset;

            float s = scale ? Mathf.LerpUnclamped(scaleFrom, 1f, e) : 1f;
            rect.localScale = new Vector3(restScale.x * s, restScale.y * s, restScale.z);

            float r = rotate ? Mathf.LerpUnclamped(rotateFrom, 0f, e) : 0f;
            rect.localRotation = Quaternion.Euler(0f, 0f, restRotation + r);

            if (changeColor && graphic != null)
            {
                float colorTime = colorDuration > 0f ? colorDuration : duration;
                float ce = MotionEasing.Evaluate(colorEase, Mathf.Clamp01(time / Mathf.Max(0.0001f, colorTime)));
                graphic.color = Color.LerpUnclamped(colorFrom, colorTo, ce);
            }
        }

        void EvaluatePieces(float time)
        {
            if (adapter.NeedsBegin(split))
                adapter.Begin(split);

            group.alpha = restAlpha;
            rect.anchoredPosition = restPosition;
            rect.localScale = restScale;
            rect.localRotation = Quaternion.Euler(0f, 0f, restRotation);

            int count = adapter.UnitCount(split);
            BuildRanks(count);
            float unitDuration = Mathf.Max(0.0001f, PieceDuration);
            Vector2 dir = MotionEasing.DirectionVector(direction, customOffset);
            bool transforms = adapter.SupportsTransform;

            for (int i = 0; i < count; i++)
            {
                float local = time - ranks[i] * pieceStagger;
                float p = Mathf.Clamp01(local / unitDuration);
                float e = MotionEasing.Evaluate(ease, p);

                TextUnitState state = new TextUnitState();
                float unitFade = fadeDuration > 0f ? fadeDuration : unitDuration * 0.75f;
                state.alpha = fade ? Mathf.Lerp(fadeFrom, 1f, MotionEasing.Evaluate(fadeEase, Mathf.Clamp01(local / Mathf.Max(0.0001f, unitFade)))) : 1f;
                if (local <= 0f && fade)
                    state.alpha = fadeFrom;
                if (!fade && local <= 0f)
                    state.alpha = 0f;

                if (transforms)
                {
                    state.offset = direction == MotionDirection.None ? Vector2.zero : (direction == MotionDirection.Custom ? customOffset * (1f - e) : dir * distance * (1f - e));
                    state.scale = scale ? Mathf.LerpUnclamped(scaleFrom, 1f, e) : 1f;
                }
                else
                    state.scale = 1f;

                if (changeColor)
                {
                    float unitColor = colorDuration > 0f ? colorDuration : unitDuration;
                    state.useColor = true;
                    state.color = Color.LerpUnclamped(colorFrom, colorTo, MotionEasing.Evaluate(colorEase, Mathf.Clamp01(local / Mathf.Max(0.0001f, unitColor))));
                }

                adapter.SetUnit(i, state);
            }

            adapter.Commit();
        }

        void BuildRanks(int count)
        {
            if (ranks != null && ranksCount == count && ranksOrder == splitOrder)
                return;

            ranksCount = count;
            ranksOrder = splitOrder;
            ranks = new int[count];
            switch (splitOrder)
            {
                case TextOrder.Reverse:
                    for (int i = 0; i < count; i++)
                        ranks[i] = count - 1 - i;
                    break;
                case TextOrder.FromCenter:
                    FillCenter(count, false);
                    break;
                case TextOrder.ToCenter:
                    FillCenter(count, true);
                    break;
                case TextOrder.Random:
                {
                    List<int> numbers = new List<int>();
                    for (int i = 0; i < count; i++)
                        numbers.Add(i);
                    System.Random random = new System.Random(count * 7919 + 13);
                    for (int i = numbers.Count - 1; i > 0; i--)
                    {
                        int j = random.Next(i + 1);
                        int tmp = numbers[i];
                        numbers[i] = numbers[j];
                        numbers[j] = tmp;
                    }

                    for (int i = 0; i < count; i++)
                        ranks[i] = numbers[i];
                    break;
                }
                default:
                    for (int i = 0; i < count; i++)
                        ranks[i] = i;
                    break;
            }
        }

        void FillCenter(int count, bool inward)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < count; i++)
                indexes.Add(i);
            float centre = (count - 1) * 0.5f;
            indexes.Sort((a, b) =>
            {
                int c = Mathf.Abs(a - centre).CompareTo(Mathf.Abs(b - centre));
                return inward ? -c : c;
            });
            for (int r = 0; r < indexes.Count; r++)
                ranks[indexes[r]] = r;
        }

        public override void ApplyRestState()
        {
            Cache();
            if (!recorded)
                return;

            if (adapter != null)
                adapter.Restore();
            group.alpha = restAlpha;
            rect.anchoredPosition = restPosition;
            rect.localScale = restScale;
            rect.localRotation = Quaternion.Euler(0f, 0f, restRotation);
            if (graphic != null)
                graphic.color = restColor;
        }

        // ---------- life after arriving ----------

        void Update()
        {
            if (!Application.isPlaying || arrivedAt < 0f)
                return;

            if (loopColor && graphic != null && split == TextSplit.Whole)
                graphic.color = loopGradient.Evaluate(Mathf.Repeat(Time.unscaledTime / loopSeconds, 1f));

            if (floatAmount > 0f)
            {
                float phase = Time.unscaledTime / floatSeconds * Mathf.PI * 2f;
                rect.anchoredPosition = restPosition + new Vector2(Mathf.Sin(phase * 0.5f) * floatAmount * 0.35f, Mathf.Sin(phase) * floatAmount);
            }
        }
    }
}
