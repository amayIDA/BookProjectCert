using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The disc of the large LED wheel, drawn like the wheel of the HTML file: a navy plate with a dark ring and a thin
/// gold edge, a dashed inner ring, the X pattern band, the dark hub with the question, a gold notch above the
/// selected item and a light that circles the rim. All parts are scene objects under the CircularMenu, sized from
/// the menu radius, so the sprites can be swapped in the Inspector.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class NILedWheel : MonoBehaviour
{
    public CircularMenu menu;

    [Header("Parts")]
    public RectTransform band;
    public RectTransform goldRing;
    public RectTransform disc;
    public RectTransform dashed;
    public RectTransform pattern;
    public RectTransform hub;
    public RectTransform ringLight;
    public RectTransform ringLightBack;
    public RectTransform notch;
    public Text hubTitle;
    public Text hubSub;

    [Header("Size")]
    [Tooltip("When on, the disc, rings, hub and notch are sized from the menu radius every frame. Turn it off to place and size them by hand.")]
    public bool autoSize = true;
    [Tooltip("Space between the outer edge of the buttons and the rim of the disc.")]
    public float margin = 16f;
    [Range(0.2f, 0.45f)] public float hubScale = 0.31f;
    [Tooltip("Where the icon of the item in the window sits inside the hub, above its centre.")]
    public float iconLift = 62f;

    // Only write values that really changed. Writing the same values every frame would mark the scene as unsaved.
    static void Size(RectTransform rect, float diameter)
    {
        if (rect == null)
            return;

        Vector2 centre = new Vector2(0.5f, 0.5f);
        if (rect.anchorMin != centre) rect.anchorMin = centre;
        if (rect.anchorMax != centre) rect.anchorMax = centre;
        if (rect.pivot != centre) rect.pivot = centre;
        if (rect.anchoredPosition != Vector2.zero) rect.anchoredPosition = Vector2.zero;
        Vector2 size = new Vector2(diameter, diameter);
        if ((rect.sizeDelta - size).sqrMagnitude > 0.0001f) rect.sizeDelta = size;
    }

    float lastKey = float.NaN;

    public float Diameter
    {
        get { return menu == null ? 800f : 2f * (menu.radius + menu.buttonSize * 0.5f + margin); }
    }

    void LateUpdate()
    {
        Apply();
    }

    public void Apply()
    {
        if (menu == null || !autoSize)
            return;

        float key = menu.radius * 1000f + menu.buttonSize * 7f + margin * 13f + hubScale * 3f;
        if (Mathf.Approximately(key, lastKey) && notch != null && hub != null)
            return;
        lastKey = key;

        float d = Diameter;
        Size(band, d + 22f);
        Size(goldRing, d + 25f);
        Size(disc, d);
        Size(dashed, d * 0.925f);
        Size(pattern, d);
        Size(hub, d * hubScale);
        Size(ringLight, d + 78f);
        Size(ringLightBack, d + 52f);

        if (notch != null)
        {
            Vector2 centre = new Vector2(0.5f, 0.5f);
            if (notch.anchorMin != centre) notch.anchorMin = centre;
            if (notch.anchorMax != centre) notch.anchorMax = centre;
            if (notch.pivot != centre) notch.pivot = centre;
            if ((notch.sizeDelta - new Vector2(66f, 55f)).sqrMagnitude > 0.0001f) notch.sizeDelta = new Vector2(66f, 55f);
            if (Mathf.Abs(notch.localEulerAngles.z - 180f) > 0.01f) notch.localEulerAngles = new Vector3(0f, 0f, 180f);
            Vector2 notchPosition = new Vector2(0f, d * 0.5f + 20f);
            if ((notch.anchoredPosition - notchPosition).sqrMagnitude > 0.0001f) notch.anchoredPosition = notchPosition;
        }

    }

    /// <summary>Puts the hub title and sub text in their default place. Used once when building, and from the component menu.</summary>
    [ContextMenu("Reset hub text layout")]
    public void LayoutHubTexts()
    {
        if (menu == null || hub == null || hubTitle == null)
            return;

        float h = Diameter * hubScale;
        RectTransform title = hubTitle.rectTransform;
        title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
        title.pivot = new Vector2(0.5f, 1f);
        title.sizeDelta = new Vector2(h * 0.84f, h * 0.36f);
        title.anchoredPosition = new Vector2(0f, h * 0.02f);
        hubTitle.resizeTextForBestFit = true;
        hubTitle.resizeTextMinSize = 12;
        hubTitle.resizeTextMaxSize = 25;

        RectTransform sub = hubSub.rectTransform;
        sub.anchorMin = sub.anchorMax = new Vector2(0.5f, 0.5f);
        sub.pivot = new Vector2(0.5f, 1f);
        sub.sizeDelta = new Vector2(h * 0.84f, 24f);
        sub.anchoredPosition = new Vector2(0f, -h * 0.4f);
    }

    public void SetHub(string title, string sub)
    {
        if (hubTitle != null)
            hubTitle.text = title;
        if (hubSub != null)
            hubSub.text = string.IsNullOrEmpty(sub) ? string.Empty : sub.ToUpperInvariant();
    }

    // ---------- build ----------

    static RectTransform Part(Transform parent, string name, Sprite sprite, Color color)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing != null)
            image = existing.GetComponent<Image>();
        else
            image = NIUI.Img(parent, name, color, sprite);
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        return image.rectTransform;
    }

    public static NILedWheel Build(CircularMenu menu)
    {
        NILedWheel wheel = menu.GetComponent<NILedWheel>();
        if (wheel == null)
            wheel = menu.gameObject.AddComponent<NILedWheel>();
        wheel.menu = menu;

        Transform root = menu.transform;
        Color gold = NIUI.Gold;

        wheel.band = Part(root, "WheelBand", NIUI.Disc(), new Color(26f / 255f, 41f / 255f, 79f / 255f, 0.55f));
        wheel.goldRing = Part(root, "WheelGoldRing", NIUI.Ring(), new Color(gold.r, gold.g, gold.b, 0.45f));

        Transform plate = root.Find("WheelDisc");
        if (plate != null)
        {
            Image plateImage = plate.GetComponent<Image>();
            plateImage.sprite = NIUI.WheelDisc();
            plateImage.color = Color.white;
            wheel.disc = plateImage.rectTransform;
        }

        wheel.dashed = Part(root, "WheelDashed", NIUI.DashedRing(), new Color(0.71f, 0.78f, 0.93f, 0.55f));
        wheel.pattern = Part(root, "WheelPattern", NIUI.PatternRing(), new Color(1f, 1f, 1f, 0.45f));
        wheel.hub = Part(root, "WheelHub", NIUI.Hub(), Color.white);

        Transform title = wheel.hub.Find("HubTitle");
        wheel.hubTitle = title != null ? title.GetComponent<Text>() : NIUI.Txt(wheel.hub, "HubTitle", "Who we are and what we build.", 22f, Color.white, FontStyle.Bold, TextAnchor.UpperCenter);
        Transform sub = wheel.hub.Find("HubSub");
        wheel.hubSub = sub != null ? sub.GetComponent<Text>() : NIUI.Txt(wheel.hub, "HubSub", "TAP TO DISCOVER", 13f, gold, FontStyle.Bold, TextAnchor.UpperCenter);

        wheel.ringLight = Part(root, "WheelRingLight", NIUI.Art("ui_comet"), new Color(1f, 0.9f, 0.62f, 1f));
        if (wheel.ringLight.GetComponent<NIBreathe>() == null)
        {
            NIBreathe spin = wheel.ringLight.gameObject.AddComponent<NIBreathe>();
            spin.scaleTo = 1f;
            spin.spin = 45f; // one turn every eight seconds, like the CSS animation
        }

        // a fainter second light that circles the other way
        wheel.ringLightBack = Part(root, "WheelRingLightBack", NIUI.Art("ui_comet"), new Color(0.7f, 0.82f, 1f, 0.55f));
        if (wheel.ringLightBack.GetComponent<NIBreathe>() == null)
        {
            NIBreathe spin2 = wheel.ringLightBack.gameObject.AddComponent<NIBreathe>();
            spin2.scaleTo = 1f;
            spin2.spin = -28f;
        }

        // the plain gold ring of the first version is replaced by the thin gold edge
        Transform oldRing = root.Find("WheelRing");
        if (oldRing != null)
            oldRing.gameObject.SetActive(false);

        Transform notchTransform = root.Find("WheelNotch");
        if (notchTransform != null)
        {
            Image notchImage = notchTransform.GetComponent<Image>();
            notchImage.sprite = NIUI.Triangle();
            notchImage.color = Color.white;
            notchImage.raycastTarget = false;
            wheel.notch = notchImage.rectTransform;
        }

        // draw order, bottom to top
        wheel.band.SetSiblingIndex(0);
        wheel.goldRing.SetSiblingIndex(1);
        if (wheel.disc != null)
            wheel.disc.SetSiblingIndex(2);
        wheel.dashed.SetSiblingIndex(3);
        wheel.pattern.SetSiblingIndex(4);
        wheel.hub.SetSiblingIndex(5);
        wheel.ringLight.SetSiblingIndex(6);
        wheel.ringLightBack.SetSiblingIndex(6);
        if (wheel.notch != null)
            wheel.notch.SetSiblingIndex(7);

        wheel.Apply();
        wheel.LayoutHubTexts();
        return wheel;
    }
}
