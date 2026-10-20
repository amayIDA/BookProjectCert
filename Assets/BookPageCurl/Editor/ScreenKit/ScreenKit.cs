using System.Collections.Generic;
using System.Text.RegularExpressions;
using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenKit
{
    /// <summary>Fonts and colours shared by every screen. Change them here and all screens follow.</summary>
    public class Style
    {
        public TMP_FontAsset light = Load("Inter_28pt-Light SDF");
        public TMP_FontAsset regular = Load("Inter_28pt-Regular SDF");
        public TMP_FontAsset semi = Load("Inter_24pt-SemiBold SDF");
        public TMP_FontAsset bold = Load("Inter_28pt-Bold SDF");
        /// <summary>The inside of every box. Solid, so the artwork behind the card never shows through.</summary>
        public Color fill = new Color(0f, 0f, 0f, 1f);
        public Color white = new Color(0.93f, 0.95f, 0.98f, 1f);
        public Color mute = new Color(0.78f, 0.82f, 0.9f, 1f);
        public Color dim = new Color(0.55f, 0.6f, 0.72f, 1f);
        public Color accent = new Color(0.55f, 0.62f, 1f, 1f);
        public Color line = new Color(0.74f, 0.78f, 0.9f, 0.85f);
        /// <summary>Line width in canvas units. Thinner lines disappear when the canvas is scaled down to a screen.</summary>
        public float lineWidth = 3f;

        static TMP_FontAsset Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/" + name + ".asset");
        }
    }

    /// <summary>
    /// The timing of the reference animation, in seconds from the moment the screen is shown.
    /// Every screen uses these beats so they all feel like one product.
    /// </summary>
    public static class Beat
    {
        public const float EyebrowRule = 0f;
        public const float Eyebrow = 0.7f;
        public const float Headline = 0.72f;
        public const float Note = 1.5f;
        public const float CardLine = 1.0f;
        public const float CardOpen = 2.2f;
        public const float CardDone = 3.3f;
        public const float Glow = 2.6f;
        public const float HeaderRule = 2.9f;
        public const float Content = 3.0f;
        public const float FooterGap = 0.4f;

        /// <summary>The Footer prefab was authored to start at this time.</summary>
        public const float FooterBase = 5.0f;
    }

    /// <summary>One column of content that plays top to bottom. Add the text in reading order and the lane keeps the clock.</summary>
    public class Lane
    {
        public float time;

        public Lane(float start)
        {
            time = start;
        }

        /// <summary>Returns when the next piece should start and moves the clock on by roughly its reading time.</summary>
        public float Take(int words, float stagger)
        {
            float start = time;
            time += Mathf.Max(0.25f, words * stagger * 0.85f + 0.12f);
            return start;
        }
    }

    /// <summary>The motion presets. They only add Motion Studio components and set their start time.</summary>
    public static class Fx
    {
        public static MotionItem Words(GameObject go, MotionDirection dir, float distance, float at, float stagger = 0.055f, float piece = 0.5f, bool blur = true)
        {
            MotionItem it = Item(go);
            it.split = TextSplit.Words; it.splitOrder = TextOrder.Forward; it.direction = dir; it.distance = distance; it.delay = at;
            it.pieceStagger = stagger; it.pieceDuration = piece; it.duration = piece;
            it.fade = true; it.fadeFrom = 0f; it.fadeDuration = blur ? 0.28f : 0.4f; it.fadeEase = MotionEase.CubicOut; it.ease = MotionEase.AppleOut;
            it.scale = false; it.rotate = false; it.changeColor = false;
            it.blur = blur; it.blurAmount = 1f; it.blurDuration = 0.6f; it.blurEase = MotionEase.CubicOut;
            return it;
        }

        /// <summary>The headline: word by word from the right, soft at first, blue settling to white.</summary>
        public static MotionItem Headline(GameObject go, float at)
        {
            MotionItem it = Words(go, MotionDirection.Right, 230f, at, 0.1f, 0.75f, true);
            it.fadeDuration = 0.4f; it.blurDuration = 0.75f;
            it.changeColor = true; it.colorFrom = new Color(0.4f, 0.52f, 1f, 1f); it.colorTo = Color.white; it.colorDuration = 1.1f; it.colorEase = MotionEase.SineOut;
            return it;
        }

        public static MotionItem Fade(GameObject go, MotionDirection dir, float distance, float at, float duration = 0.55f)
        {
            MotionItem it = Item(go);
            it.split = TextSplit.Whole; it.direction = dir; it.distance = distance; it.delay = at; it.duration = duration;
            it.fade = true; it.fadeFrom = 0f; it.fadeDuration = 0.4f; it.fadeEase = MotionEase.CubicOut; it.ease = MotionEase.AppleOut;
            it.scale = false; it.rotate = false; it.changeColor = false; it.blur = false;
            return it;
        }

        /// <summary>The small label that follows the rule, sliding in from the right.</summary>
        public static MotionItem Eyebrow(GameObject go, float at)
        {
            MotionItem it = Fade(go, MotionDirection.Right, 60f, at, 0.6f);
            it.fadeDuration = 0.45f;
            return it;
        }

        /// <summary>An icon turning and growing into place.</summary>
        public static MotionItem Icon(GameObject go, float at)
        {
            MotionItem it = Fade(go, MotionDirection.None, 0f, at, 0.7f);
            it.scale = true; it.scaleFrom = 0.5f; it.rotate = true; it.rotateFrom = -60f;
            return it;
        }

        public static AxisReveal Sweep(GameObject go, float at)
        {
            AxisReveal a = Reveal(go);
            a.squeezeFromRight = false; a.sweepFromRight = true; a.sweepStart = 0.27f; a.sweepLeadTime = 0.65f; a.sweepTailStart = 0.35f; a.duration = 0.8f; a.delay = at;
            return a;
        }

        public static AxisReveal WipeLeft(GameObject go, float at, float duration = 0.9f)
        {
            AxisReveal a = Reveal(go);
            a.fromLeft = true; a.fromBottom = false; a.duration = duration; a.delay = at;
            return a;
        }

        /// <summary>Opens from the middle out to both sides.</summary>
        public static AxisReveal WipeOut(GameObject go, float at, float duration = 1f)
        {
            AxisReveal a = Reveal(go);
            a.fromCenterX = true; a.fromLeft = false; a.fromBottom = false; a.duration = duration; a.delay = at;
            return a;
        }

        public static AxisReveal WipeUp(GameObject go, float at, float duration = 0.7f)
        {
            AxisReveal a = Reveal(go);
            a.axis = AxisReveal.Axis.Vertical; a.fromBottom = true; a.fromLeft = false; a.duration = duration; a.delay = at;
            return a;
        }

        /// <summary>A line sweeps across the card, rests, then the box opens up and down.</summary>
        public static CardLineToBox CardFrame(GameObject go, float at, float originY = 0.5f)
        {
            CardLineToBox c = go.GetComponent<CardLineToBox>() ?? go.AddComponent<CardLineToBox>();
            c.drawFromLeft = true; c.lineDuration = 0.85f; c.holdDuration = 0.35f; c.openDuration = 0.95f; c.lowerLag = 0.15f;
            c.originY = originY; c.stripLag = 0.35f; c.tintLine = true; c.lineTint = new Color(0.4f, 0.5f, 1f, 1f); c.lineThickness = 3f; c.delay = at;
            return c;
        }

        public static MotionItem Glow(GameObject go, float at)
        {
            MotionItem it = Fade(go, MotionDirection.None, 0f, at, 1.3f);
            it.fadeDuration = 1.3f; it.fadeEase = MotionEase.SineOut;
            return it;
        }

        static MotionItem Item(GameObject go)
        {
            MotionItem it = go.GetComponent<MotionItem>();
            return it != null ? it : go.AddComponent<MotionItem>();
        }

        static AxisReveal Reveal(GameObject go)
        {
            AxisReveal a = go.GetComponent<AxisReveal>();
            return a != null ? a : go.AddComponent<AxisReveal>();
        }
    }

    /// <summary>
    /// Builds one screen on a page and animates it with the reference choreography.
    /// Positions are fractions (0..1) of the parent, so a screen never needs pixel numbers.
    /// </summary>
    public class Screen
    {
        public readonly RectTransform sheet;
        public readonly Style style = new Style();
        readonly Transform page;
        readonly Sprite white;

        const string FooterPrefab = "Assets/BookPageCurl/Prefabs/ScreenFooter.prefab";
        const string LogoPrefab = "Assets/BookPageCurl/Prefabs/ScreenLogo.prefab";

        Screen(Transform page, RectTransform sheet)
        {
            this.page = page;
            this.sheet = sheet;
            white = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BookPageCurl/Sprites/White.png");
        }

        /// <summary>Clears the page's Sheet (and hides any old placeholder) and prepares the page to play.</summary>
        public static Screen Begin(Transform page)
        {
            Transform spread = page.Find("Spread");
            if (spread != null)
                spread.gameObject.SetActive(false);
            Transform old = page.Find("Sheet");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            RectTransform sheet = Stretch("Sheet", page);
            if (page.GetComponent<MotionSequence>() == null)
                page.gameObject.AddComponent<MotionSequence>();
            if (page.GetComponent<PageIntro>() == null)
                page.gameObject.AddComponent<PageIntro>();
            MotionSequence seq = page.GetComponent<MotionSequence>();
            seq.groupSameOrder = true; seq.interval = 0f; seq.startDelay = 0f; seq.playOnStart = false;
            return new Screen(page, sheet);
        }

        public void Finish()
        {
            MotionSequence seq = page.GetComponent<MotionSequence>();
            foreach (MotionBehaviour b in page.GetComponentsInChildren<MotionBehaviour>(true))
                b.order = 0;
            seq.Collect(true);
            EditorUtility.SetDirty(seq);
            Debug.Log("Screen built on " + page.name + ": " + seq.entries.Count + " animated items, " + seq.TotalDuration.ToString("0.0") + "s.");
        }

        // ---------- header ----------

        /// <summary>Eyebrow rule + label, the two line headline and an optional note on the right. Returns when the header is done.</summary>
        public void Header(string eyebrow, string headline, string note, float top)
        {
            float rule = top - 0.012f;
            RectTransform ruleRect = Rule(sheet, "EyebrowRule", 0.05f, rule, 0.07f, rule, style.mute, 2f);
            Fx.Sweep(ruleRect.gameObject, Beat.EyebrowRule);
            ruleRect.sizeDelta = new Vector2(0f, style.lineWidth);

            TMP_Text label = Text(sheet, "Eyebrow", eyebrow, style.light, 18, style.mute, 0.075f, top - 0.03f, 0.4f, top + 0.006f, TextAlignmentOptions.MidlineLeft);
            label.characterSpacing = 18f;
            Fx.Eyebrow(label.gameObject, Beat.Eyebrow);

            if (!string.IsNullOrEmpty(headline))
            {
                TMP_Text head = Text(sheet, "Headline", headline, style.regular, 56, style.white, 0.07f, top - 0.135f, 0.72f, top - 0.025f, TextAlignmentOptions.TopLeft);
                head.lineSpacing = -8f;
                Fx.Headline(head.gameObject, Beat.Headline);
            }

            if (!string.IsNullOrEmpty(note))
            {
                TMP_Text side = Text(sheet, "Note", note, style.light, 24, style.mute, 0.6f, top - 0.118f, 0.93f, top - 0.078f, TextAlignmentOptions.MidlineRight);
                Fx.Words(side.gameObject, MotionDirection.Bottom, 30f, Beat.Note);
            }

            Prefab(LogoPrefab, "Page 3/Sheet/Logo", "Logo");
        }

        // ---------- card ----------

        /// <summary>
        /// The framed card. A line is drawn at originY (card fraction), rests, then opens into boxes: everything above the line
        /// opens upward, and each divider below it opens downward one after the other. A card with a header row, a body and a
        /// strip is Card(..., originY: headerBottom, dividers: stripTop).
        /// </summary>
        public RectTransform Card(string name, float x0, float y0, float x1, float y1, float originY = 0.5f, params float[] dividers)
        {
            RectTransform card = Stretch(name, sheet);
            Place(card, x0, y0, x1, y1);
            RectTransform fill = Stretch("Fill", card);
            Image black = fill.gameObject.AddComponent<Image>();
            black.sprite = white; black.color = style.fill; black.raycastTarget = false;
            RectTransform frame = Stretch("Frame", card);
            Edge(frame, "Top", 0f, 1f, 1f, 1f, new Vector2(style.lineWidth, style.lineWidth));
            Edge(frame, "Bottom", 0f, 0f, 1f, 0f, new Vector2(style.lineWidth, style.lineWidth));
            Edge(frame, "SideLeft", 0f, 0f, 0f, 1f, new Vector2(style.lineWidth, 0f));
            Edge(frame, "SideRight", 1f, 0f, 1f, 1f, new Vector2(style.lineWidth, 0f));
            if (originY > 0.01f && originY < 0.99f && Mathf.Abs(originY - 0.5f) > 0.001f)
                Edge(frame, "Divider origin", 0f, originY, 1f, originY, new Vector2(style.lineWidth, style.lineWidth));
            for (int i = 0; i < dividers.Length; i++)
                Edge(frame, "Divider " + (i + 1), 0f, dividers[i], 1f, dividers[i], new Vector2(style.lineWidth, style.lineWidth));
            CardLineToBox box = Fx.CardFrame(frame.gameObject, Beat.CardLine, originY);
            box.fill = fill;
            return card;
        }

        /// <summary>
        /// Light that rises from the bottom edge of a card row (rowBottom..rowTop, card fractions). The row clips it so it never spills.
        /// The glow sprite keeps its half ellipse in the lower left, so it is placed to put that ellipse at centerX.
        /// </summary>
        public void Glow(RectTransform card, float centerX, float width, float rowBottom, float rowTop, float at)
        {
            RectTransform clip = Stretch("GlowClip", card);
            Place(clip, 0f, rowBottom, 1f, rowTop);
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform glow = Stretch("Glow", clip);
            float rectWidth = width / 0.44f;
            float left = centerX - 0.30f * rectWidth;
            Place(glow, left, 0f, left + rectWidth, 1.5f);
            Image image = glow.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Pages/Page2/Asset 110.png");
            image.raycastTarget = false;
            Fx.Glow(glow.gameObject, at);
        }

        // ---------- pieces ----------

        public TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color,
            float x0, float y0, float x1, float y1, TextAlignmentOptions align)
        {
            RectTransform rect = Stretch(name, parent);
            Place(rect, x0, y0, x1, y1);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.font = font; text.fontSize = size; text.color = color; text.alignment = align;
            text.enableWordWrapping = true; text.raycastTarget = false; text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        /// <summary>Text that reads word by word on a lane.</summary>
        public TMP_Text Read(Transform parent, Lane lane, string name, string value, TMP_FontAsset font, float size, Color color,
            float x0, float y0, float x1, float y1, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, float stagger = 0.045f)
        {
            TMP_Text text = Text(parent, name, value, font, size, color, x0, y0, x1, y1, align);
            Fx.Words(text.gameObject, MotionDirection.Bottom, 40f, lane.Take(CountWords(value), stagger), stagger, 0.5f, true);
            return text;
        }

        public RectTransform Icon(Transform parent, string name, string icon, float cx, float cy, float pixels, Color color, float at)
        {
            RectTransform rect = Stretch(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(cx, cy);
            rect.sizeDelta = new Vector2(pixels, pixels);
            rect.anchoredPosition = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = ScreenIcons.Get(icon); image.color = color; image.raycastTarget = false; image.preserveAspect = true;
            Fx.Icon(rect.gameObject, at);
            return rect;
        }

        /// <summary>A big number with a small caption under it. They arrive together on a lane.</summary>
        public void StatBlock(Transform parent, Lane lane, string name, string number, string caption, float x, float y0, float y1)
        {
            float at = lane.Take(2, 0.12f);
            float mid = Mathf.Lerp(y0, y1, 0.34f);
            TMP_Text big = Text(parent, name + " number", number, style.light, 86, style.white, x, mid, x + 0.15f, y1, TextAlignmentOptions.BottomLeft);
            Fx.Fade(big.gameObject, MotionDirection.Bottom, 30f, at, 0.7f);
            TMP_Text small = Text(parent, name + " caption", caption, style.light, 15, style.dim, x, y0, x + 0.15f, mid, TextAlignmentOptions.TopLeft);
            small.characterSpacing = 20f;
            Fx.Fade(small.gameObject, MotionDirection.Bottom, 16f, at + 0.15f, 0.55f);
        }

        /// <summary>An icon from a sprite you already have (for example the footer pictures).</summary>
        public RectTransform IconSprite(Transform parent, string name, Sprite sprite, float cx, float cy, float pixels, Color color, float at)
        {
            RectTransform rect = Stretch(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(cx, cy);
            rect.sizeDelta = new Vector2(pixels, pixels);
            rect.anchoredPosition = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false; image.preserveAspect = true;
            Fx.Icon(rect.gameObject, at);
            return rect;
        }

        /// <summary>A plain coloured bar. thickness is in canvas units on the thin side.</summary>
        public RectTransform Bar(Transform parent, string name, float x0, float y0, float x1, float y1, Color color, float thickness)
        {
            return Rule(parent, name, x0, y0, x1, y1, color, thickness);
        }

        public RectTransform HRule(Transform parent, string name, float x0, float x1, float y, float at, float duration = 0.9f)
        {
            RectTransform r = Rule(parent, name, x0, y, x1, y, style.line, style.lineWidth);
            Fx.WipeLeft(r.gameObject, at, duration);
            return r;
        }

        public RectTransform VRule(Transform parent, string name, float x, float y0, float y1, float at, float duration = 0.7f)
        {
            RectTransform r = Rule(parent, name, x, y0, x, y1, style.line, style.lineWidth);
            Fx.WipeUp(r.gameObject, at, duration);
            return r;
        }

        /// <summary>The shared footer: a rule and the three contact cells. Starts at the given time.</summary>
        public void Footer(float at)
        {
            Transform footer = Prefab(FooterPrefab, "Page 3/Sheet/Footer", "Footer");
            if (footer == null)
                return;
            float shift = at - Beat.FooterBase;
            foreach (MotionBehaviour b in footer.GetComponentsInChildren<MotionBehaviour>(true))
                b.delay = Mathf.Max(0f, b.delay + shift);
        }

        // ---------- plumbing ----------

        Transform Prefab(string prefabPath, string templatePath, string name)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                Transform template = page.parent.Find(templatePath);
                if (template == null)
                {
                    Debug.LogWarning("ScreenKit: " + prefabPath + " is missing and so is its template " + templatePath);
                    return null;
                }

                System.IO.Directory.CreateDirectory("Assets/BookPageCurl/Prefabs");
                GameObject copy = Object.Instantiate(template.gameObject, template.parent);
                copy.name = name;
                asset = PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
                Object.DestroyImmediate(copy);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, sheet);
            instance.name = name;
            return instance.transform;
        }

        public static int CountWords(string value)
        {
            string plain = Regex.Replace(value, "<[^>]+>", "");
            return plain.Split(new[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        }

        RectTransform Rule(Transform parent, string name, float x0, float y0, float x1, float y1, Color color, float thickness)
        {
            RectTransform rect = Stretch(name, parent);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            bool horizontal = Mathf.Abs(x1 - x0) >= Mathf.Abs(y1 - y0);
            rect.sizeDelta = horizontal ? new Vector2(0f, thickness) : new Vector2(thickness, 0f);
            rect.anchoredPosition = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = white; image.color = color; image.raycastTarget = false;
            return rect;
        }

        void Edge(RectTransform frame, string name, float x0, float y0, float x1, float y1, Vector2 size)
        {
            Rule(frame, name, x0, y0, x1, y1, style.line, 0f);
            RectTransform rect = (RectTransform)frame.Find(name);
            rect.sizeDelta = size;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
