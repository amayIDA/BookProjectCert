using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    public enum CardOrigin
    {
        /// <summary>A thin line the width (or height) of the card draws out from the centre, then the card opens from the line.</summary>
        Line,

        /// <summary>The card grows out of another UI object, taking its position and size first.</summary>
        FromObject
    }

    public enum CardLineAxis
    {
        /// <summary>The line is as wide as the card and the card opens upwards and downwards.</summary>
        Horizontal,

        /// <summary>The line is as tall as the card and the card opens to the left and to the right.</summary>
        Vertical
    }

    /// <summary>
    /// Card reveal. The width of a sprite becomes a line, the line grows out from the centre in both directions
    /// and opens into the full sprite or card. It can also grow out of another object. Works with any Image, RawImage or Text,
    /// children of the card are revealed together with it.
    /// </summary>
    [AddComponentMenu("Motion Studio/Motion Card")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MotionCard : MotionBehaviour
    {
        [Header("Reveal")]
        public CardOrigin origin = CardOrigin.Line;
        public CardLineAxis lineAxis = CardLineAxis.Horizontal;

        [Header("Line")]
        [Tooltip("Draw the line out from the centre first. Off starts with the full line already there.")]
        public bool drawLineFirst = true;
        [Min(0.01f)] public float lineDrawDuration = 0.5f;
        [Tooltip("Seconds the line rests before the card opens.")]
        [Min(0f)] public float lineHold = 0.1f;
        [Min(0.5f)] public float lineThickness = 3f;
        public MotionEase lineEase = MotionEase.AppleOut;

        [Tooltip("The color of the line over the whole reveal, left to right. Change it to make the line shift color.")]
        public Gradient lineColors = DefaultLine();
        public bool lineGlow = true;
        [Min(1f)] public float lineGlowSize = 22f;
        [Tooltip("Fade the line away while the card opens.")]
        public bool fadeLine = true;

        [Header("Open")]
        [Min(0.01f)] public float expandDuration = 0.8f;
        public MotionEase expandEase = MotionEase.AppleInOut;

        [Header("From object")]
        [Tooltip("The card starts as this object (position and size) and grows to its own size.")]
        public RectTransform fromObject;
        [Min(0.01f)] public float fromObjectDuration = 0.9f;
        public MotionEase fromObjectEase = MotionEase.AppleInOut;
        [Tooltip("Fade the source object out while the card grows out of it.")]
        public bool fadeSource = true;

        [Header("Fade")]
        [Tooltip("Also fade the whole card (and its children) in while it opens.")]
        public bool fade;
        [Range(0f, 1f)] public float fadeFrom = 0f;
        [Tooltip("Seconds the fade takes. 0 uses the whole reveal time.")]
        [Min(0f)] public float fadeDuration = 0f;
        public MotionEase fadeEase = MotionEase.CubicOut;

        [Header("Color")]
        [Tooltip("Turn on to move the color of the card from the initial to the final color while it opens.")]
        public bool changeColor;
        public Color colorFrom = Color.white;
        public Color colorTo = Color.white;
        [Tooltip("Seconds the color change takes. 0 uses the whole reveal time.")]
        [Min(0f)] public float colorDuration = 0f;
        public MotionEase colorEase = MotionEase.SineInOut;

        [Header("Content")]
        [Tooltip("Reveal children together with the card by clipping them to the same window.")]
        public bool clipChildren = true;

        Graphic[] graphics;
        MotionClipEffect[] clips;
        RectMask2D rectMask;
        RectTransform rect;
        Image lineImage;
        Image glowImage;
        CanvasGroup sourceGroup;
        CanvasGroup cardGroup;
        float restAlpha = 1f;
        bool fadeTouched;
        Color restColor = Color.white;
        bool colorTouched;
        static Sprite whiteSprite;
        static Sprite softSprite;

        static Gradient DefaultLine()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 1f, 1f), 0f), new GradientColorKey(new Color(0.93f, 0.84f, 0.6f), 0.6f), new GradientColorKey(new Color(1f, 1f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        // ---------- setup ----------

        public override void Prepare()
        {
            if (rect == null)
                rect = (RectTransform)transform;

            if (graphics == null)
            {
                Graphic own = GetComponent<Graphic>();
                graphics = own != null ? new[] { own } : new Graphic[0];
                clips = new MotionClipEffect[graphics.Length];
                for (int i = 0; i < graphics.Length; i++)
                {
                    clips[i] = graphics[i].GetComponent<MotionClipEffect>();
                    if (clips[i] == null)
                        clips[i] = graphics[i].gameObject.AddComponent<MotionClipEffect>();
                }
            }

            if (clipChildren && rectMask == null)
            {
                rectMask = GetComponent<RectMask2D>();
                if (rectMask == null)
                    rectMask = gameObject.AddComponent<RectMask2D>();
            }

            if (!clipChildren && rectMask != null)
                rectMask.padding = Vector4.zero;
        }

        void Awake()
        {
            Prepare();
        }

        static Sprite WhiteSprite()
        {
            if (whiteSprite != null)
                return whiteSprite;
            Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            Color[] c = new Color[16];
            for (int i = 0; i < c.Length; i++)
                c[i] = Color.white;
            t.SetPixels(c);
            t.Apply();
            whiteSprite = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteSprite;
        }

        // a vertical soft gradient (bright in the middle) for the glow around the line
        static Sprite SoftSprite()
        {
            if (softSprite != null)
                return softSprite;
            Texture2D t = new Texture2D(4, 64, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            Color[] c = new Color[4 * 64];
            for (int y = 0; y < 64; y++)
            {
                float d = Mathf.Abs((y + 0.5f) / 64f - 0.5f) * 2f;
                float a = Mathf.Pow(1f - d, 2.2f);
                for (int x = 0; x < 4; x++)
                    c[y * 4 + x] = new Color(1f, 1f, 1f, a);
            }

            t.SetPixels(c);
            t.Apply();
            softSprite = Sprite.Create(t, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
            softSprite.hideFlags = HideFlags.HideAndDontSave;
            return softSprite;
        }

        Image EnsureLine(string childName, Sprite sprite)
        {
            Transform existing = transform.Find(childName);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                GameObject go = new GameObject(childName, typeof(RectTransform), typeof(Image));
                go.hideFlags = HideFlags.HideAndDontSave;
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                image = go.GetComponent<Image>();
                image.raycastTarget = false;
                LayoutElement ignore = go.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.transform.SetAsLastSibling();
            return image;
        }

        void OnDestroy()
        {
            DestroyHelper(lineImage);
            DestroyHelper(glowImage);
        }

        static void DestroyHelper(Image image)
        {
            if (image == null)
                return;
            if (Application.isPlaying)
                Destroy(image.gameObject);
            else
                DestroyImmediate(image.gameObject);
        }

        // ---------- timing ----------

        public override float ContentDuration
        {
            get
            {
                if (origin == CardOrigin.FromObject)
                    return fromObjectDuration;
                return (drawLineFirst ? lineDrawDuration + lineHold : 0.08f) + expandDuration;
            }
        }

        // ---------- evaluation ----------

        protected override void EvaluateContent(float time)
        {
            Prepare();
            Rect r = rect.rect;
            if (r.width < 0.5f || r.height < 0.5f)
                return;

            Vector2 min;
            Vector2 max;
            float colorProgress;
            float lineAlpha;
            float lineLength;

            if (origin == CardOrigin.FromObject && fromObject != null)
            {
                float p = Mathf.Clamp01(time / Mathf.Max(0.0001f, fromObjectDuration));
                float e = MotionEasing.Evaluate(fromObjectEase, p);
                Rect source = SourceRectNormalized(r);
                min = Vector2.LerpUnclamped(source.min, Vector2.zero, e);
                max = Vector2.LerpUnclamped(source.max, Vector2.one, e);
                colorProgress = p;
                lineAlpha = 0f;
                lineLength = 0f;
                if (time <= 0f)
                    min = max = new Vector2(source.center.x, source.center.y);
                SetSourceAlpha(fadeSource ? 1f - Mathf.Clamp01(p * 3f) : 1f);
            }
            else
            {
                float lineStart = 0f;
                float openStart = drawLineFirst ? lineDrawDuration + lineHold : 0.08f;
                float drawP = drawLineFirst ? MotionEasing.Evaluate(lineEase, MotionEasing.Remap01(time, lineStart, lineDrawDuration)) : 1f;
                float openP = MotionEasing.Evaluate(expandEase, MotionEasing.Remap01(time, openStart, expandDuration));
                float lineIn = drawLineFirst ? 1f : Mathf.Clamp01(time / 0.08f);

                bool horizontal = lineAxis == CardLineAxis.Horizontal;
                float lineSpan = horizontal ? r.width : r.height;
                float crossSize = horizontal ? r.height : r.width;
                float thick = Mathf.Min(lineThickness, crossSize) / Mathf.Max(1f, crossSize);

                float half = 0.5f * drawP;
                float cross = Mathf.LerpUnclamped(thick * 0.5f, 0.5f, openP);

                if (time <= 0f && drawLineFirst)
                    half = 0f;
                if (time <= 0f && !drawLineFirst)
                    cross = 0f;

                if (horizontal)
                {
                    min = new Vector2(0.5f - half, 0.5f - cross);
                    max = new Vector2(0.5f + half, 0.5f + cross);
                }
                else
                {
                    min = new Vector2(0.5f - cross, 0.5f - half);
                    max = new Vector2(0.5f + cross, 0.5f + half);
                }

                colorProgress = Mathf.Clamp01(time / Mathf.Max(0.0001f, ContentDuration));
                float fade = fadeLine ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(openP * 1.15f)) : 1f;
                lineAlpha = lineIn * fade;
                lineLength = lineSpan * half * 2f;
                if (time <= 0f && drawLineFirst)
                    lineAlpha = 0f;
            }

            ApplyWindow(min, max, r);
            UpdateLine(min, max, r, colorProgress, lineAlpha, lineLength);
            ApplyFadeAndColor(time);
        }

        void ApplyFadeAndColor(float time)
        {
            float total = Mathf.Max(0.0001f, ContentDuration);
            if (fade)
            {
                if (cardGroup == null)
                {
                    cardGroup = GetComponent<CanvasGroup>();
                    if (cardGroup == null)
                        cardGroup = gameObject.AddComponent<CanvasGroup>();
                }

                if (!fadeTouched)
                {
                    restAlpha = cardGroup.alpha;
                    fadeTouched = true;
                }

                float fadeTime = fadeDuration > 0f ? fadeDuration : total;
                float e = MotionEasing.Evaluate(fadeEase, Mathf.Clamp01(time / fadeTime));
                cardGroup.alpha = time <= 0f ? fadeFrom * restAlpha : Mathf.Lerp(fadeFrom * restAlpha, restAlpha, e);
            }

            if (changeColor && graphics != null && graphics.Length > 0 && graphics[0] != null)
            {
                if (!colorTouched)
                {
                    restColor = graphics[0].color;
                    colorTouched = true;
                }

                float colorTime = colorDuration > 0f ? colorDuration : total;
                float e = MotionEasing.Evaluate(colorEase, Mathf.Clamp01(time / colorTime));
                graphics[0].color = Color.LerpUnclamped(colorFrom, colorTo, e);
            }
        }

        Rect SourceRectNormalized(Rect cardRect)
        {
            Vector3[] corners = new Vector3[4];
            fromObject.GetWorldCorners(corners);
            Vector2 a = rect.InverseTransformPoint(corners[0]);
            Vector2 b = rect.InverseTransformPoint(corners[2]);
            float x0 = (Mathf.Min(a.x, b.x) - cardRect.xMin) / cardRect.width;
            float x1 = (Mathf.Max(a.x, b.x) - cardRect.xMin) / cardRect.width;
            float y0 = (Mathf.Min(a.y, b.y) - cardRect.yMin) / cardRect.height;
            float y1 = (Mathf.Max(a.y, b.y) - cardRect.yMin) / cardRect.height;
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        void SetSourceAlpha(float alpha)
        {
            if (fromObject == null)
                return;
            if (sourceGroup == null)
            {
                sourceGroup = fromObject.GetComponent<CanvasGroup>();
                if (sourceGroup == null)
                    sourceGroup = fromObject.gameObject.AddComponent<CanvasGroup>();
            }

            sourceGroup.alpha = alpha;
        }

        void ApplyWindow(Vector2 min, Vector2 max, Rect r)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    clips[i].SetWindow(min, max);
            }

            if (rectMask != null && clipChildren)
            {
                float x0 = Mathf.Lerp(r.xMin, r.xMax, min.x);
                float x1 = Mathf.Lerp(r.xMin, r.xMax, max.x);
                float y0 = Mathf.Lerp(r.yMin, r.yMax, min.y);
                float y1 = Mathf.Lerp(r.yMin, r.yMax, max.y);
                rectMask.padding = new Vector4(x0 - r.xMin, y0 - r.yMin, r.xMax - x1, r.yMax - y1);
            }
        }

        void UpdateLine(Vector2 min, Vector2 max, Rect r, float colorProgress, float alpha, float length)
        {
            bool show = origin == CardOrigin.Line && alpha > 0.001f;
            if (lineImage == null && show)
                lineImage = EnsureLine("~MotionLine", WhiteSprite());
            if (glowImage == null && show && lineGlow)
                glowImage = EnsureLine("~MotionLineGlow", SoftSprite());

            if (lineImage != null)
            {
                lineImage.gameObject.SetActive(show);
                if (show)
                {
                    Color c = lineColors.Evaluate(colorProgress);
                    c.a *= alpha;
                    lineImage.color = c;
                    bool horizontal = lineAxis == CardLineAxis.Horizontal;
                    Vector2 centre = new Vector2(Mathf.Lerp(r.xMin, r.xMax, 0.5f), Mathf.Lerp(r.yMin, r.yMax, 0.5f));
                    PlaceLine(lineImage.rectTransform, centre, horizontal, length, lineThickness);

                    if (glowImage != null)
                    {
                        glowImage.gameObject.SetActive(lineGlow);
                        Color g = c;
                        g.a *= 0.55f;
                        glowImage.color = g;
                        PlaceLine(glowImage.rectTransform, centre, horizontal, length, lineGlowSize);
                    }
                }
                else if (glowImage != null)
                    glowImage.gameObject.SetActive(false);
            }
        }

        static void PlaceLine(RectTransform line, Vector2 centre, bool horizontal, float length, float thickness)
        {
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = centre;
            // the glow sprite is a vertical gradient, so a vertical line is a horizontal one turned by 90 degrees
            line.sizeDelta = new Vector2(Mathf.Max(0f, length), thickness);
            line.localRotation = horizontal ? Quaternion.identity : Quaternion.Euler(0f, 0f, 90f);
        }

        public override void ApplyRestState()
        {
            Prepare();
            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null)
                        clips[i].ClearWindow();
                }
            }

            if (rectMask != null)
                rectMask.padding = Vector4.zero;
            if (lineImage != null)
                lineImage.gameObject.SetActive(false);
            if (glowImage != null)
                glowImage.gameObject.SetActive(false);
            if (sourceGroup != null)
                sourceGroup.alpha = 1f;
            if (fadeTouched && cardGroup != null)
            {
                cardGroup.alpha = restAlpha;
                fadeTouched = false;
            }

            if (colorTouched && graphics != null && graphics.Length > 0 && graphics[0] != null)
            {
                graphics[0].color = restColor;
                colorTouched = false;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
