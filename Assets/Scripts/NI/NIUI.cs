using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Type scale of the content display (canvas 1536 wide, read from about two metres).
/// One modular scale keeps the hierarchy clear: label, small, body, lead, card title, number, headline.
/// </summary>
public static class NIType
{
    public const float Micro = 15f;
    public const float Label = 16.5f;
    public const float Small = 19f;
    public const float Body = 23f;
    public const float Lead = 26f;
    public const float CardTitle = 31f;
    public const float Number = 42f;
    public const float Code = 52f;
    public const float Headline = 62f;
}

/// <summary>
/// Small helpers that build uGUI objects in code and load the Talent Wheel art.
/// All sprites come from real assets in Resources/NationalInitiative so scene objects keep their
/// references after saving and can be swapped in the Inspector.
/// </summary>
public static class NIUI
{
    /// <summary>The HTML was designed for a 1280 px wide screen. The content display is 1536 px wide.</summary>
    public const float S = 1.2f;

    public static readonly Color Page = Hex(0x000000);
    public static readonly Color Stage = Hex(0x02040A);
    public static readonly Color Navy = Hex(0x1A294F);
    public static readonly Color Blue = Hex(0x6785C3);
    public static readonly Color Glow = Hex(0xB4C6EE);
    public static readonly Color Gold = Hex(0xEED599);
    public static readonly Color Orange = Hex(0xC04F17);
    public static readonly Color Muted = Hex(0xB9C4DE);
    public static readonly Color Faint = Hex(0x7F8DB0);
    public static readonly Color Panel = new Color(26f / 255f, 41f / 255f, 79f / 255f, 0.75f);

    static Font font;
    static Sprite fallback;
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

    public static Color Hex(int rgb, float alpha = 1f)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
    }

    public static Color WithAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    public static float U(float value)
    {
        return value * S;
    }

    // ---------- fonts ----------

    /// <summary>Built-in font so the reference survives saving. Use Tools > National Initiative > Apply Selected Font to change it.</summary>
    public static Font Font
    {
        get
        {
            if (font != null)
                return font;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }

    // ---------- art loaded from Resources/NationalInitiative ----------

    public static Sprite Art(string name)
    {
        Sprite sprite;
        if (sprites.TryGetValue(name, out sprite) && sprite != null)
            return sprite;

        sprite = Resources.Load<Sprite>("NationalInitiative/" + name);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>("NationalInitiative/" + name);
            if (texture != null)
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        if (sprite == null)
            return Fallback();

        sprites[name] = sprite;
        return sprite;
    }

    static Sprite Fallback()
    {
        if (fallback != null)
            return fallback;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallback = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        fallback.hideFlags = HideFlags.HideAndDontSave;
        return fallback;
    }

    public static Texture2D ArtTexture(string name)
    {
        Texture2D texture;
        if (textures.TryGetValue(name, out texture) && texture != null)
            return texture;

        texture = Resources.Load<Texture2D>("NationalInitiative/" + name);
        textures[name] = texture;
        return texture;
    }

    public static Sprite Pixel() { return Art("ui_pixel"); }
    public static Sprite WheelDisc() { return Art("ui_wheel_disc"); }
    public static Sprite DashedRing() { return Art("ui_dashed_ring"); }
    public static Sprite PatternRing() { return Art("ui_pattern_ring"); }
    public static Sprite Hub() { return Art("ui_hub"); }
    public static Sprite OptFace() { return Art("ui_opt_face"); }
    public static Sprite OptFaceSel() { return Art("ui_opt_face_sel"); }
    public static Sprite Triangle() { return Art("ui_triangle"); }
    public static Sprite Disc() { return Art("ui_disc"); }
    public static Sprite Ring() { return Art("ui_ring"); }
    public static Sprite Radial() { return Art("ui_radial"); }
    public static Sprite Halo() { return Art("ui_halo"); }
    public static Sprite Orb() { return Art("ui_orb"); }
    public static Sprite Glass() { return Art("ui_glass"); }
    public static Sprite GlassPill() { return Art("ui_glass_pill"); }
    public static Sprite GlassSoft() { return Art("ui_glass_soft"); }

    /// <summary>Nine-sliced rounded rectangle. Use Image.Type.Sliced.</summary>
    public static Sprite Rounded(int radius)
    {
        if (radius <= 10)
            return Art("ui_round_6");
        if (radius <= 22)
            return Art("ui_round_16");
        return Art("ui_round_31");
    }

    // ---------- builders ----------

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent != null ? parent.gameObject.layer : 5;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Fill(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    public static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
    }

    public static Image Img(Transform parent, string name, Color color, Sprite sprite = null, Image.Type type = Image.Type.Simple)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : Pixel();
        image.type = type;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Image RoundedImg(Transform parent, string name, Color color, int radius)
    {
        return Img(parent, name, color, Rounded(radius), Image.Type.Sliced);
    }

    /// <summary>Frosted glass plate: translucent fill with a soft light edge.</summary>
    public static Image GlassImg(Transform parent, string name, Color tint, bool pill = false)
    {
        return Img(parent, name, tint, pill ? GlassPill() : Glass(), Image.Type.Sliced);
    }

    public static Text Txt(Transform parent, string name, string text, float size, Color color, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        RectTransform rect = Rect(name, parent);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = Font;
        label.fontSize = Mathf.RoundToInt(size);
        label.fontStyle = style;
        label.color = color;
        label.alignment = anchor;
        label.text = text;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = true;
        label.lineSpacing = 1.05f;
        return label;
    }

    public static VerticalLayoutGroup VStack(Transform parent, string name, float spacing = 0f, TextAnchor align = TextAnchor.UpperLeft, bool expandWidth = true)
    {
        RectTransform rect = Rect(name, parent);
        VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        group.spacing = spacing;
        group.childAlignment = align;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = expandWidth;
        group.childForceExpandHeight = false;
        return group;
    }

    public static HorizontalLayoutGroup HStack(Transform parent, string name, float spacing = 0f, TextAnchor align = TextAnchor.UpperLeft, bool expandHeight = false)
    {
        RectTransform rect = Rect(name, parent);
        HorizontalLayoutGroup group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.spacing = spacing;
        group.childAlignment = align;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = expandHeight;
        return group;
    }

    public static void Pad(LayoutGroup group, float left, float top, float right, float bottom)
    {
        group.padding = new RectOffset(Mathf.RoundToInt(left), Mathf.RoundToInt(right), Mathf.RoundToInt(top), Mathf.RoundToInt(bottom));
    }

    public static LayoutElement Elem(GameObject go, float minWidth = -1f, float minHeight = -1f, float prefWidth = -1f, float prefHeight = -1f, float flexWidth = -1f, float flexHeight = -1f)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();
        element.minWidth = minWidth;
        element.minHeight = minHeight;
        element.preferredWidth = prefWidth;
        element.preferredHeight = prefHeight;
        element.flexibleWidth = flexWidth;
        element.flexibleHeight = flexHeight;
        return element;
    }

    // ---------- easing ----------

    /// <summary>CSS cubic-bezier easing.</summary>
    public static float Bezier(float x1, float y1, float x2, float y2, float t)
    {
        t = Mathf.Clamp01(t);
        if (t <= 0f || t >= 1f)
            return t;

        float lo = 0f;
        float hi = 1f;
        float u = t;
        for (int i = 0; i < 18; i++)
        {
            float x = Cubic(x1, x2, u);
            if (Mathf.Abs(x - t) < 0.0005f)
                break;
            if (x < t)
                lo = u;
            else
                hi = u;
            u = (lo + hi) * 0.5f;
        }

        return Cubic(y1, y2, u);
    }

    static float Cubic(float a, float b, float t)
    {
        float s = 1f - t;
        return 3f * s * s * t * a + 3f * s * t * t * b + t * t * t;
    }

    public static float EaseRise(float t)
    {
        return Bezier(0.2f, 0.7f, 0.2f, 1f, t);
    }
}
