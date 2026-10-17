using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the decoration and controls of the touch screen and the LED wheel as ordinary scene objects:
/// gradient and light orbs, breathing rings, glass buttons and the sound switch.
/// </summary>
public static class NIScenery
{
    static float Fade(float v) { return v; }

    static void DestroyChild(Transform parent, string childName)
    {
        Transform old = parent.Find(childName);
        if (old == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(old.gameObject);
        else
            Object.DestroyImmediate(old.gameObject);
    }

    static void AddOrb(Transform parent, string name, float ax, float ay, float size, Color color, float period, float drift, float delay)
    {
        Image orb = NIUI.Img(parent, name, color, NIUI.Orb());
        NIUI.Place(orb.rectTransform, new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        NIDrift d = orb.gameObject.AddComponent<NIDrift>();
        d.range = new Vector2(drift, drift * 0.7f);
        d.period = period;
        d.startDelay = delay;
        NIBreathe b = orb.gameObject.AddComponent<NIBreathe>();
        b.scaleTo = 1.08f;
        b.alphaTo = color.a * 0.45f;
        b.period = period * 0.6f;
        b.startDelay = delay * 0.5f;
    }

    // ---------- touch screen ----------

    public static void BuildTouch(CircularMenu menu, TalentWheelFlow flow)
    {
        RectTransform mirror = menu.touchMirror;
        if (mirror == null)
            return;

        Canvas canvas = mirror.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        DestroyChild(canvas.transform, "NITouchBackdrop");
        DestroyChild(mirror, "NITouchUi");

        // ---- backdrop ----
        RectTransform backdrop = NIUI.Rect("NITouchBackdrop", canvas.transform);
        NIUI.Fill(backdrop);
        backdrop.SetAsFirstSibling();

        Image gradient = NIUI.Img(backdrop, "Gradient", Color.white, NIUI.Art("ui_gradient_touch"));
        NIUI.Fill(gradient.rectTransform);

        Image x = NIUI.Img(backdrop, "BrandX", new Color(1f, 1f, 1f, 0.30f), NIUI.Art("brandx_solid"));
        x.preserveAspect = true;
        NIUI.Place(x.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(130f, 80f), new Vector2(560f, 640f));
        NIBreathe xb = x.gameObject.AddComponent<NIBreathe>();
        xb.scaleTo = 1.02f;
        xb.alphaTo = 0.22f;
        xb.period = 10f;

        AddOrb(backdrop, "Orb1", 0.12f, 0.86f, 700f, new Color(NIUI.Glow.r, NIUI.Glow.g, NIUI.Glow.b, 0.26f), 17f, 70f, 0f);
        AddOrb(backdrop, "Orb2", 0.90f, 0.16f, 760f, new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.12f), 21f, 80f, 1.2f);
        AddOrb(backdrop, "Orb3", 0.20f, 0.12f, 520f, new Color(NIUI.Blue.r, NIUI.Blue.g, NIUI.Blue.b, 0.22f), 19f, 60f, 2.4f);

        RectTransform rings = NIUI.Rect("Rings", backdrop);
        NIUI.Fill(rings);
        float[] sizes = { 1000f, 1240f, 1500f };
        float[] alphas = { 0.14f, 0.09f, 0.06f };
        for (int i = 0; i < sizes.Length; i++)
        {
            Image ring = NIUI.Img(rings, "Ring" + (i + 1), new Color(1f, 1f, 1f, alphas[i]), NIUI.Ring());
            NIUI.Center(ring.rectTransform, Vector2.zero, new Vector2(sizes[i], sizes[i]));
            NIBreathe b = ring.gameObject.AddComponent<NIBreathe>();
            b.scaleTo = 1.035f;
            b.alphaTo = alphas[i] * 0.4f;
            b.period = 6f + i * 2f;
            b.startDelay = i * 0.9f;
        }

        // the light that circles the wheel (CSS ringlight: one turn every eight seconds) and a slow dashed ring
        Image dashed = NIUI.Img(rings, "DashedRing", new Color(0.82f, 0.88f, 1f, 0.30f), NIUI.DashedRing());
        NIUI.Center(dashed.rectTransform, Vector2.zero, new Vector2(1010f, 1010f));
        NIBreathe dashedSpin = dashed.gameObject.AddComponent<NIBreathe>();
        dashedSpin.scaleTo = 1f;
        dashedSpin.spin = -5f;

        Image arc = NIUI.Img(rings, "RingLight", new Color(1f, 0.9f, 0.62f, 1f), NIUI.Art("ui_comet"));
        NIUI.Center(arc.rectTransform, Vector2.zero, new Vector2(1050f, 1050f));
        NIBreathe arcSpin = arc.gameObject.AddComponent<NIBreathe>();
        arcSpin.scaleTo = 1f;
        arcSpin.spin = 45f;

        Image vignette = NIUI.Img(backdrop, "Vignette", new Color(0f, 0.02f, 0.08f, 0.65f), NIUI.Art("ui_vignette"));
        NIUI.Fill(vignette.rectTransform);

        // ---- controls above the dial ----
        RectTransform ui = NIUI.Rect("NITouchUi", mirror);
        NIUI.Fill(ui);

        flow.crumbText = NIUI.Txt(ui, "Crumbs", "START", 28f, new Color(1f, 1f, 1f, 0.82f), FontStyle.Bold, TextAnchor.UpperCenter);
        NIUI.Place(flow.crumbText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(940f, 44f));
        flow.crumbText.horizontalOverflow = HorizontalWrapMode.Overflow;

        flow.hubText = NIUI.Txt(ui, "Question", "Who we are and what we build.", 54f, Color.white, FontStyle.Bold, TextAnchor.UpperCenter);
        NIUI.Place(flow.hubText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(940f, 80f));
        flow.hubText.resizeTextForBestFit = true;
        flow.hubText.resizeTextMinSize = 24;
        flow.hubText.resizeTextMaxSize = 54;
        Shadow shadow = flow.hubText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.08f, 0.2f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -3f);

        flow.startAgain = MakePill(ui, "StartAgain", "Start again", PillStyle.Navy, new Vector2(0f, 0f), new Vector2(170f, 72f), new Vector2(280f, 72f));
        flow.bookPill = MakePill(ui, "BookConsultation", "Book a consultation", PillStyle.Gold, new Vector2(1f, 0f), new Vector2(-170f, 72f), new Vector2(280f, 72f));
        flow.expertPill = MakePill(ui, "Expert", "Continue with our expert", PillStyle.Light, new Vector2(0f, 0f), new Vector2(170f, 168f), new Vector2(280f, 72f));
        flow.finishPill = MakePill(ui, "Finish", "Finish", PillStyle.Navy, new Vector2(1f, 0f), new Vector2(-170f, 168f), new Vector2(280f, 72f));
        flow.expertLabel = flow.expertPill.GetComponentInChildren<Text>();

        Button sound = MakePill(ui, "Sound", "Sound on", PillStyle.Navy, new Vector2(0f, 1f), new Vector2(124f, -40f), new Vector2(216f, 60f));
        flow.soundLabel = sound.GetComponentInChildren<Text>();
        flow.soundButton = sound;

        flow.rippleRoot = NIUI.Rect("Ripples", ui);
        NIUI.Fill(flow.rippleRoot);
        flow.shockRoot = NIUI.Rect("Shock", ui);
        NIUI.Fill(flow.shockRoot);

        StyleHubButtons(menu);
    }

    public enum PillStyle { Navy, Gold, Light }

    public static Button MakePill(Transform parent, string objectName, string caption, PillStyle style, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Color fill;
        Color ink;
        Color sheen;
        switch (style)
        {
            case PillStyle.Gold:
                fill = new Color(NIUI.Navy.r, NIUI.Navy.g, NIUI.Navy.b, 0.9f);
                ink = NIUI.Gold;
                sheen = new Color(1f, 0.88f, 0.55f, 0.95f);
                break;
            case PillStyle.Light:
                fill = new Color(1f, 1f, 1f, 0.96f);
                ink = NIUI.Navy;
                sheen = new Color(1f, 1f, 1f, 0.6f);
                break;
            default:
                fill = new Color(NIUI.Navy.r, NIUI.Navy.g, NIUI.Navy.b, 0.78f);
                ink = Color.white;
                sheen = new Color(0.8f, 0.9f, 1f, 0.5f);
                break;
        }

        Image plate = NIUI.RoundedImg(parent, objectName, fill, 31);
        NIUI.Place(plate.rectTransform, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
        plate.raycastTarget = true;

        Image glass = NIUI.Img(plate.transform, "Glass", sheen, NIUI.GlassPill(), Image.Type.Sliced);
        NIUI.Fill(glass.rectTransform);

        if (style == PillStyle.Gold)
        {
            RectTransform clip = NIUI.Rect("ShineClip", plate.transform);
            NIUI.Fill(clip, 4f, 4f, 4f, 4f);
            clip.gameObject.AddComponent<RectMask2D>();
            Image shine = NIUI.Img(clip, "Shine", new Color(1f, 0.95f, 0.75f, 0.9f), NIUI.Art("ui_shine"));
            NIUI.Center(shine.rectTransform, new Vector2(-size.x * 1.5f, 0f), new Vector2(size.x * 0.55f, size.y * 1.6f));
            NIShineSweep sweep = shine.gameObject.AddComponent<NIShineSweep>();
            sweep.travel = size.x * 0.9f;
            sweep.period = 5.5f;
        }

        Button button = plate.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        plate.gameObject.AddComponent<NIPressFeedback>();

        Text label = NIUI.Txt(plate.transform, "Label", caption, 32f, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
        NIUI.Fill(label.rectTransform, 16f, 4f, 16f, 4f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 32;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return button;
    }

    /// <summary>Gives the Tap to discover and Back buttons of the dial the same glass look.</summary>
    static void StyleHubButtons(CircularMenu menu)
    {
        Button[] hubs = { menu.discoverButton, menu.backButton };
        for (int i = 0; i < hubs.Length; i++)
        {
            Button hub = hubs[i];
            if (hub == null)
                continue;

            bool discover = i == 0;
            Image plate = hub.GetComponent<Image>();
            plate.sprite = NIUI.Art("ui_round_31");
            plate.type = Image.Type.Sliced;
            plate.color = discover ? new Color(NIUI.Navy.r, NIUI.Navy.g, NIUI.Navy.b, 0.92f) : new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.97f);

            if (hub.transform.Find("Glass") == null)
            {
                Image glass = NIUI.Img(hub.transform, "Glass", discover ? new Color(0.8f, 0.9f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.7f), NIUI.GlassPill(), Image.Type.Sliced);
                NIUI.Fill(glass.rectTransform);
                glass.transform.SetSiblingIndex(0);
            }

            if (discover && hub.transform.Find("Halo") == null)
            {
                Image halo = NIUI.Img(hub.transform, "Halo", new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.5f), NIUI.Halo());
                NIUI.Fill(halo.rectTransform, -90f, -70f, -90f, -70f);
                halo.transform.SetSiblingIndex(0);
                NIBreathe b = halo.gameObject.AddComponent<NIBreathe>();
                b.scaleTo = 1.06f;
                b.alphaTo = 0.18f;
                b.period = 3.2f;
            }

            Text text = hub.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontStyle = FontStyle.Bold;
                text.color = discover ? Color.white : NIUI.Navy;
            }

            if (hub.GetComponent<NIPressFeedback>() == null)
                hub.gameObject.AddComponent<NIPressFeedback>();
        }
    }

    // ---------- LED wheel ----------

    public static void BuildLed(CircularMenu menu)
    {
        Canvas led = null;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == "LargeLEDCircle_Canvas")
                led = canvases[i];
        }

        if (led == null)
            return;

        DestroyChild(led.transform, "NILedFx");
        RectTransform fx = NIUI.Rect("NILedFx", led.transform);
        NIUI.Fill(fx);

        Image glow = NIUI.Img(fx, "Glow", new Color(NIUI.Blue.r, NIUI.Blue.g, NIUI.Blue.b, 0.35f), NIUI.Radial());
        NIUI.Center(glow.rectTransform, Vector2.zero, new Vector2(1500f, 1500f));
        NIBreathe gb = glow.gameObject.AddComponent<NIBreathe>();
        gb.scaleTo = 1.05f;
        gb.alphaTo = 0.22f;
        gb.period = 8f;

        AddOrb(fx, "Orb1", 0.12f, 0.88f, 520f, new Color(NIUI.Glow.r, NIUI.Glow.g, NIUI.Glow.b, 0.18f), 18f, 50f, 0f);
        AddOrb(fx, "Orb2", 0.90f, 0.14f, 600f, new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.10f), 22f, 60f, 1.5f);

        float diameter = (menu.radius + menu.buttonSize * 0.15f) * 2f;
        Image halo = NIUI.Img(fx, "WheelHalo", new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.22f), NIUI.Halo());
        NIUI.Center(halo.rectTransform, Vector2.zero, new Vector2(diameter + 280f, diameter + 280f));
        NIBreathe hb = halo.gameObject.AddComponent<NIBreathe>();
        hb.scaleTo = 1.05f;
        hb.alphaTo = 0.1f;
        hb.period = 6f;

        Image vignette = NIUI.Img(fx, "Vignette", new Color(0f, 0.02f, 0.08f, 0.7f), NIUI.Art("ui_vignette"));
        NIUI.Fill(vignette.rectTransform);

        // right above the plain navy backdrop and below the wheel
        fx.SetSiblingIndex(Mathf.Min(1, led.transform.childCount - 1));
    }
}
