using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates the content display hierarchy as ordinary scene objects.</summary>
public partial class NIContentScreen
{
    static float U(float v) { return NIUI.U(v); }

    static readonly Color GlassTint = new Color(0.78f, 0.86f, 1f, 1f);

    public static NIContentScreen Build(Transform canvas)
    {
        Transform old = canvas.Find("NIScreen");
        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        RectTransform rect = NIUI.Rect("NIScreen", canvas);
        NIUI.Fill(rect);
        NIContentScreen screen = rect.gameObject.AddComponent<NIContentScreen>();
        screen.BuildAll(rect);
        return screen;
    }

    // ---------- small builders ----------

    static void Flex(Component c)
    {
        LayoutElement element = c.GetComponent<LayoutElement>();
        if (element == null)
            element = c.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.flexibleHeight = 0f;
    }

    static Text Sub(Transform parent, string text, float delay)
    {
        Text t = NIUI.Txt(parent, "Sub", text, NIType.Lead, NIUI.Muted, FontStyle.Normal);
        t.lineSpacing = 1.12f;
        LayoutElement le = NIUI.Elem(t.gameObject, -1f, -1f, U(640f), -1f, 0f, 0f);
        le.flexibleWidth = 0f;
        NIAnimItem.Add(t.gameObject, delay);
        return t;
    }

    static NIView NewView(Transform parent, string name)
    {
        VerticalLayoutGroup v = NIUI.VStack(parent, name, U(16f), TextAnchor.UpperLeft, false);
        v.gameObject.AddComponent<CanvasGroup>();
        LayoutElement le = v.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;
        NIView view = v.gameObject.AddComponent<NIView>();
        return view;
    }

    static RectTransform Card(Transform parent, string name, Color tint, bool soft, float padH, float padV, float spacing)
    {
        Image plate = NIUI.GlassImg(parent, name, tint);
        if (soft)
            plate.sprite = NIUI.GlassSoft();
        VerticalLayoutGroup v = plate.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(Mathf.RoundToInt(padH), Mathf.RoundToInt(padH), Mathf.RoundToInt(padV), Mathf.RoundToInt(padV));
        v.spacing = spacing;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return (RectTransform)plate.transform;
    }

    /// <summary>Small coloured bar that sits on the top edge of a glass card.</summary>
    static void Accent(Transform card, Color color)
    {
        Image bar = NIUI.RoundedImg(card, "Accent", color, 6);
        LayoutElement le = NIUI.Elem(bar.gameObject);
        le.ignoreLayout = true;
        bar.rectTransform.anchorMin = new Vector2(0f, 1f);
        bar.rectTransform.anchorMax = new Vector2(0f, 1f);
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);
        bar.rectTransform.anchoredPosition = new Vector2(U(16f), 0f);
        bar.rectTransform.sizeDelta = new Vector2(U(46f), U(3.5f));
    }

    static RectTransform Step(Transform parent, string number, string title, string em, float delay, Color accent)
    {
        RectTransform card = Card(parent, "Step", GlassTint, false, U(14f), U(20f), U(5f));
        NIUI.Elem(card.gameObject, 0f, -1f, -1f, -1f, 1f, 0f);
        Accent(card, accent);
        if (number != null)
            NIUI.Txt(card, "Number", number, NIType.Number, NIUI.Glow, FontStyle.Normal);
        if (title != null)
            NIUI.Txt(card, "Title", title, NIType.Body, Color.white, FontStyle.Bold);
        if (em != null)
            NIUI.Txt(card, "Em", em, NIType.Small, NIUI.Muted, FontStyle.Normal);
        NIAnimItem.Add(card.gameObject, delay);
        return card;
    }

    static RectTransform Ladder(Transform parent, string[] stageIds, float firstDelay, float stagger)
    {
        HorizontalLayoutGroup row = NIUI.HStack(parent, "Ladder", U(8f), TextAnchor.UpperLeft, true);
        row.childForceExpandWidth = true;
        Flex(row);
        for (int i = 0; i < stageIds.Length; i++)
            Step(row.transform, "0" + (i + 1), NIData.StageName(stageIds[i]), NIData.StageLine[stageIds[i]], firstDelay + i * stagger, NIUI.Blue);
        return (RectTransform)row.transform;
    }

    /// <summary>Container of caret + text lines. The first child is the template used by NIListBinder.</summary>
    static NIListBinder Ticks(Transform parent, string[] lines, Color textColor)
    {
        VerticalLayoutGroup v = NIUI.VStack(parent, "Ticks", U(5f));
        NIListBinder binder = v.gameObject.AddComponent<NIListBinder>();
        binder.container = (RectTransform)v.transform;
        for (int i = 0; i < lines.Length; i++)
        {
            HorizontalLayoutGroup row = NIUI.HStack(v.transform, "Tick", U(8f), TextAnchor.UpperLeft);
            row.childForceExpandWidth = false;
            RectTransform holder = NIUI.Rect("Caret", row.transform);
            NIUI.Elem(holder.gameObject, U(12f), U(9f), U(12f), U(9f));
            Image caret = NIUI.Img(holder, "Image", Color.white, NIUI.Art("caret_gold"));
            caret.preserveAspect = true;
            NIUI.Place(caret.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -U(5f)), new Vector2(U(12f), U(9f)));
            Text t = NIUI.Txt(row.transform, "Text", lines[i], NIType.Small + 1f, textColor, FontStyle.Bold);
            NIUI.Elem(t.gameObject, 0f, -1f, -1f, -1f, 1f, 0f);
        }

        return binder;
    }

    static NIListBinder Chips(Transform parent, string[] values)
    {
        RectTransform holder = NIUI.Rect("Chips", parent);
        NIFlowLayout flow = holder.gameObject.AddComponent<NIFlowLayout>();
        flow.spacingX = U(5f);
        flow.spacingY = U(5f);
        NIListBinder binder = holder.gameObject.AddComponent<NIListBinder>();
        binder.container = holder;
        for (int i = 0; i < values.Length; i++)
        {
            HorizontalLayoutGroup row = NIUI.HStack(holder, "Chip", 0f, TextAnchor.MiddleCenter);
            row.padding = new RectOffset(Mathf.RoundToInt(U(10f)), Mathf.RoundToInt(U(10f)), Mathf.RoundToInt(U(4.5f)), Mathf.RoundToInt(U(4.5f)));
            Image plate = NIUI.RoundedImg(row.transform, "Plate", NIUI.Navy, 31);
            NIUI.Elem(plate.gameObject).ignoreLayout = true;
            NIUI.Fill(plate.rectTransform);
            Image sheen = NIUI.Img(plate.transform, "Sheen", new Color(1f, 1f, 1f, 0.75f), NIUI.GlassPill(), Image.Type.Sliced);
            NIUI.Fill(sheen.rectTransform);
            Text t = NIUI.Txt(row.transform, "Text", values[i], NIType.Label, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        return binder;
    }

    // ---------- main build ----------

    void BuildAll(RectTransform root)
    {
        // ---- background layers ----
        Image bg = NIUI.Img(root, "Background", Color.white, NIUI.Art("ui_gradient_night"));
        NIUI.Fill(bg.rectTransform);

        RectTransform orbs = NIUI.Rect("Orbs", root);
        NIUI.Fill(orbs);
        float[][] orbData =
        {
            new[] { 0.10f, 0.78f, 760f, 0f, 0.16f, 15f, 9f },
            new[] { 0.34f, 0.16f, 560f, 1f, 0.08f, 19f, 11f },
            new[] { 0.80f, 0.72f, 860f, 0f, 0.11f, 17f, 8f },
            new[] { 0.64f, 0.12f, 480f, 2f, 0.09f, 21f, 12f },
            new[] { 0.94f, 0.40f, 640f, 0f, 0.09f, 16f, 10f }
        };
        for (int i = 0; i < orbData.Length; i++)
        {
            float[] o = orbData[i];
            Color c = o[3] < 0.5f ? NIUI.Blue : (o[3] < 1.5f ? NIUI.Gold : NIUI.Glow);
            Image orb = NIUI.Img(orbs, "Orb" + (i + 1), new Color(c.r, c.g, c.b, o[4]), NIUI.Orb());
            NIUI.Place(orb.rectTransform, new Vector2(o[0], o[1]), new Vector2(o[0], o[1]), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(o[2], o[2]));
            NIDrift drift = orb.gameObject.AddComponent<NIDrift>();
            drift.range = new Vector2(70f + i * 12f, 46f + i * 9f);
            drift.period = o[5];
            drift.startDelay = i * 0.7f;
            NIBreathe breathe = orb.gameObject.AddComponent<NIBreathe>();
            breathe.scaleTo = 1.08f;
            breathe.alphaTo = o[4] * 0.45f;
            breathe.period = o[6];
            breathe.startDelay = i * 0.4f;
        }

        Image glow = NIUI.Img(root, "Glow", new Color(103f / 255f, 133f / 255f, 195f / 255f, 0.30f), NIUI.Radial());
        NIUI.Place(glow.rectTransform, new Vector2(0.22f, 0.5f), new Vector2(0.22f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 1400f));
        NIBreathe glowBreathe = glow.gameObject.AddComponent<NIBreathe>();
        glowBreathe.scaleTo = 1.05f;
        glowBreathe.alphaTo = 0.22f;
        glowBreathe.period = 9f;

        Image brand = NIUI.Img(root, "BrandX", new Color(1f, 1f, 1f, 0.045f), NIUI.Art("brandx_lines"));
        brand.preserveAspect = true;
        NIUI.Place(brand.rectTransform, new Vector2(0.04f, 0.5f), new Vector2(0.04f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(U(260f) * 2.3f, U(300f) * 2.3f));
        // The cross-hatched X behind the hologram stays visible while the particles change shape, so it is off by default.
        brand.gameObject.SetActive(false);

        // soft halo and a slow arc of light under the hologram
        Image halo = NIUI.Img(root, "HoloHalo", new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.11f), NIUI.Halo());
        NIUI.Place(halo.rectTransform, new Vector2(0.21f, 0.54f), new Vector2(0.21f, 0.54f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 700f));
        NIBreathe haloBreathe = halo.gameObject.AddComponent<NIBreathe>();
        haloBreathe.scaleTo = 1.07f;
        haloBreathe.alphaTo = 0.05f;
        haloBreathe.period = 7f;

        Image arc = NIUI.Img(root, "HoloArc", new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0.55f), NIUI.Art("ui_arc"));
        NIUI.Place(arc.rectTransform, new Vector2(0.21f, 0.54f), new Vector2(0.21f, 0.54f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 680f));
        NIBreathe arcSpin = arc.gameObject.AddComponent<NIBreathe>();
        arcSpin.scaleTo = 1f;
        arcSpin.spin = 16f;

        // The HTML hologram floats on the plain dark screen without a ring. These two are off by default; switch them on to taste.
        halo.gameObject.SetActive(false);
        arc.gameObject.SetActive(false);

        // ---- hologram ----
        RectTransform holo = NIUI.Rect("Hologram", root);
        NIUI.Fill(holo);
        field = holo.gameObject.AddComponent<NIParticleField>();
        field.Init(holo);
        if (!Application.isPlaying)
            field.PreviewStatic();

        Image vignette = NIUI.Img(root, "Vignette", new Color(1f, 1f, 1f, 0.9f), NIUI.Art("ui_vignette"));
        NIUI.Fill(vignette.rectTransform);

        Image scan = NIUI.Img(root, "ScanLines", new Color(0.7f, 0.78f, 0.93f, 0.02f), NIUI.Art("ui_scan"), Image.Type.Tiled);
        NIUI.Fill(scan.rectTransform);

        // ---- text panel ----
        panel = NIUI.Rect("Panel", root);
        panel.anchorMin = new Vector2(0.4225f, 0f);
        panel.anchorMax = Vector2.one;
        panel.offsetMin = new Vector2(0f, U(92f));
        panel.offsetMax = new Vector2(-U(36f), -U(36f));
        VerticalLayoutGroup column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        column.spacing = U(18f);
        column.childAlignment = TextAnchor.MiddleLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        eyebrow = NIUI.Txt(panel, "Eyebrow", string.Empty, NIType.Label + 1f, NIUI.Gold, FontStyle.Bold);
        eyebrow.gameObject.SetActive(false);

        Text holoText = NIUI.Txt(panel, "HoloText", "National ambitions. <b>Delivered by its people.</b>", NIType.Headline, Color.white, FontStyle.Normal);
        holoText.lineSpacing = 0.92f;
        headline = holoText.gameObject.AddComponent<NIHeadline>();
        headline.Init(holoText);

        VerticalLayoutGroup body = NIUI.VStack(panel, "Body", U(12f), TextAnchor.UpperLeft, true);
        bodyRoot = (RectTransform)body.transform;

        BuildIdle();
        BuildWho();
        BuildLevel();
        BuildApproach();
        BuildReveal();
        BuildProgramme();
        BuildConsult();
        BuildPipe(root);
        BuildToast(root);

        // only the idle view is visible in the editor to start with
        NIView[] all = AllViews();
        for (int i = 0; i < all.Length; i++)
            all[i].gameObject.SetActive(all[i] == viewIdle);
        current = viewIdle;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    void BuildIdle()
    {
        viewIdle = NewView(bodyRoot, "View_Idle");
        Sub(viewIdle.transform, "Developing Emirati talent at every stage.", 0f);
        Ladder(viewIdle.transform, new[] { "work", "role", "sector", "leader" }, 0.3f, 0.15f);
        Sub(viewIdle.transform, "Press <b>Tap to discover</b> on the touch screen to begin.", 1f);
    }

    void BuildWho()
    {
        viewWho = NewView(bodyRoot, "View_Who");
        Sub(viewWho.transform, "We develop Emirati talent at every stage, from a first job to future leadership.", 0f);
        string[] ids = new string[NIData.StageList.Length];
        for (int i = 0; i < ids.Length; i++)
            ids[i] = NIData.StageList[i][0];
        Ladder(viewWho.transform, ids, 0.2f, 0.12f);
        Sub(viewWho.transform, "Swipe the wheel and tap <b>Pick your sector</b> to find your path.", 0.9f);
    }

    void BuildLevel()
    {
        viewLevel = NewView(bodyRoot, "View_Level");
        RectTransform card = Card(viewLevel.transform, "NotchCard", GlassTint, false, U(24f), U(26f), U(8f));
        NIUI.Elem(card.gameObject, 0f, -1f, -1f, -1f, 1f, 0f);
        Accent(card, NIUI.Gold);
        NIUI.Txt(card, "Label", "IN THE NOTCH", NIType.Label, NIUI.Gold, FontStyle.Bold);
        levelTitle = NIUI.Txt(card, "Title", "Pick your sector", NIType.CardTitle + 3f, Color.white, FontStyle.Bold);
        levelHint = NIUI.Txt(card, "Hint", "Hiring, upskilling and leadership for your business.", NIType.Body, NIUI.Muted, FontStyle.Normal);
        NIAnimItem.Add(card.gameObject, 0f);
        Sub(viewLevel.transform, "Tap Discover to open it. Swipe the wheel to see the others.", 0.2f);
    }

    void BuildApproach()
    {
        viewApproach = NewView(bodyRoot, "View_Approach");
        Sub(viewApproach.transform, "Recognised by the UAE’s leading education authorities.", 0f);

        HorizontalLayoutGroup logos = NIUI.HStack(viewApproach.transform, "Logos", U(10f), TextAnchor.UpperLeft, true);
        logos.childForceExpandWidth = true;
        Flex(logos);
        string[] files = { "logo_actvet", "logo_nqc", "logo_adek" };
        for (int i = 0; i < files.Length; i++)
        {
            Image plate = NIUI.RoundedImg(logos.transform, "Logo" + (i + 1), new Color(1f, 1f, 1f, 0.97f), 16);
            plate.sprite = NIUI.Art("ui_round_16");
            NIUI.Elem(plate.gameObject, 0f, U(84f), -1f, U(84f), 1f, 0f);
            Image logo = NIUI.Img(plate.transform, "Image", Color.white, NIUI.Art(files[i]));
            logo.preserveAspect = true;
            NIUI.Fill(logo.rectTransform, U(10f), U(10f), U(10f), U(10f));
            NIAnimItem.Add(plate.gameObject, 0.2f + i * 0.12f, 0.6f);
        }

        Sub(viewApproach.transform, "<b>Bespoke solutions.</b> Tell us the goal. We’ll build the pathway.", 0.6f);
        Sub(viewApproach.transform, "<b>Talent success.</b> From first engagement to placement, we stay with your talent.", 0.75f);

        RectTransform journey = NIUI.Rect("Journey", viewApproach.transform);
        NIFlowLayout flow = journey.gameObject.AddComponent<NIFlowLayout>();
        flow.spacingX = U(6f);
        flow.spacingY = U(6f);
        Flex(journey);
        for (int i = 0; i < NIData.Support.Length; i++)
        {
            if (i > 0)
            {
                Image link = NIUI.Img(journey, "Link", NIUI.Gold);
                NIUI.Elem(link.gameObject, U(16f), U(2f), U(16f), U(2f));
            }

            HorizontalLayoutGroup pill = NIUI.HStack(journey, "Step" + (i + 1), 0f, TextAnchor.MiddleCenter);
            pill.padding = new RectOffset(Mathf.RoundToInt(U(13f)), Mathf.RoundToInt(U(13f)), Mathf.RoundToInt(U(7f)), Mathf.RoundToInt(U(7f)));
            Image plate = NIUI.RoundedImg(pill.transform, "Plate", new Color(1f, 1f, 1f, 0.96f), 31);
            NIUI.Elem(plate.gameObject).ignoreLayout = true;
            NIUI.Fill(plate.rectTransform);
            Text t = NIUI.Txt(pill.transform, "Text", NIData.Support[i], NIType.Small, NIUI.Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            NIAnimItem.Add(pill.gameObject, 0.9f + i * 0.12f, 0.6f);
        }
    }

    RectTransform CardShell(Transform parent, int n, string label, Color face, Color labelColor, out RectTransform frontOut)
    {
        RectTransform card = NIUI.Rect("Card" + (n + 1), parent);
        VerticalLayoutGroup v = card.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = true;
        NIUI.Elem(card.gameObject, 0f, U(250f), -1f, -1f, 1f, 0f);

        Image plate = NIUI.RoundedImg(card, "Front", face, 16);
        VerticalLayoutGroup fv = plate.gameObject.AddComponent<VerticalLayoutGroup>();
        fv.padding = new RectOffset(Mathf.RoundToInt(U(18f)), Mathf.RoundToInt(U(18f)), Mathf.RoundToInt(U(20f)), Mathf.RoundToInt(U(20f)));
        fv.spacing = U(10f);
        fv.childControlWidth = true;
        fv.childControlHeight = true;
        fv.childForceExpandWidth = true;
        fv.childForceExpandHeight = false;
        Image sheen = NIUI.Img(plate.transform, "Sheen", new Color(1f, 1f, 1f, 0.55f), NIUI.GlassSoft(), Image.Type.Sliced);
        NIUI.Elem(sheen.gameObject).ignoreLayout = true;
        NIUI.Fill(sheen.rectTransform);
        NIUI.Txt(plate.transform, "Label", label.ToUpperInvariant(), NIType.Label, labelColor, FontStyle.Bold);

        Image backPlate = NIUI.RoundedImg(card, "Back", NIUI.Hex(0x3B5598), 16);
        NIUI.Elem(backPlate.gameObject).ignoreLayout = true;
        NIUI.Fill(backPlate.rectTransform);
        Image pattern = NIUI.Img(backPlate.transform, "Pattern", Color.white, NIUI.Art("pattern_tile"), Image.Type.Tiled);
        NIUI.Fill(pattern.rectTransform);
        Image backSheen = NIUI.Img(backPlate.transform, "Sheen", new Color(1f, 1f, 1f, 0.6f), NIUI.GlassSoft(), Image.Type.Sliced);
        NIUI.Fill(backSheen.rectTransform);
        Text num = NIUI.Txt(backPlate.transform, "Number", "0" + (n + 1), 66f, Color.white, FontStyle.Normal);
        NIUI.Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -U(16f)), new Vector2(-U(32f), U(70f)));
        Text lab = NIUI.Txt(backPlate.transform, "Label", label.ToUpperInvariant(), NIType.Label, NIUI.Gold, FontStyle.Bold);
        NIUI.Place(lab.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, U(16f)), new Vector2(-U(32f), U(20f)));

        NICardFlip flip = card.gameObject.AddComponent<NICardFlip>();
        flip.front = (RectTransform)plate.transform;
        flip.back = (RectTransform)backPlate.transform;
        frontOut = (RectTransform)plate.transform;
        return card;
    }

    void BuildReveal()
    {
        viewReveal = NewView(bodyRoot, "View_Reveal");
        HorizontalLayoutGroup row = NIUI.HStack(viewReveal.transform, "Cards", U(10f), TextAnchor.UpperLeft, true);
        row.childForceExpandWidth = true;
        Flex(row);

        RectTransform front;
        RectTransform c0 = CardShell(row.transform, 0, "The challenge", new Color(NIUI.Navy.r, NIUI.Navy.g, NIUI.Navy.b, 0.96f), NIUI.Gold, out front);
        revealTitle = NIUI.Txt(front, "Title", "Meeting Emiratisation targets", NIType.CardTitle, Color.white, FontStyle.Bold);
        revealHint = NIUI.Txt(front, "Hint", "Hire Emiratis ready to perform.", NIType.Body, Color.white);

        RectTransform c1 = CardShell(row.transform, 1, "How we help", new Color(1f, 1f, 1f, 0.97f), NIUI.Blue, out front);
        revealHelp = NIUI.Txt(front, "Help", "We supply job-ready and role-ready Emirati talent.", NIType.Body, NIUI.Navy);
        revealChips = Chips(front, NIData.Priv[0].offer);

        RectTransform c2 = CardShell(row.transform, 2, "What you get", new Color(NIUI.Blue.r, NIUI.Blue.g, NIUI.Blue.b, 0.96f), NIUI.Navy, out front);
        revealTicks = Ticks(front, NIData.Priv[0].get, Color.white);

        cards = new[] { c0.GetComponent<NICardFlip>(), c1.GetComponent<NICardFlip>(), c2.GetComponent<NICardFlip>() };
    }

    void BuildProgramme()
    {
        viewProgramme = NewView(bodyRoot, "View_Programme");
        progLine = Sub(viewProgramme.transform, NIData.Programmes[0].line, 0f);

        VerticalLayoutGroup list = NIUI.VStack(viewProgramme.transform, "List", U(6f));
        Flex(list);
        progRows = list.gameObject.AddComponent<NIListBinder>();
        progRows.container = (RectTransform)list.transform;
        progRows.numbered = true;
        string[] sample = NIData.Programmes[0].list;
        for (int i = 0; i < sample.Length; i++)
        {
            HorizontalLayoutGroup row = NIUI.HStack(list.transform, "Row", U(10f), TextAnchor.MiddleLeft);
            row.padding = new RectOffset(Mathf.RoundToInt(U(18f)), Mathf.RoundToInt(U(18f)), Mathf.RoundToInt(U(13f)), Mathf.RoundToInt(U(13f)));
            Image plate = NIUI.GlassImg(row.transform, "Plate", GlassTint);
            NIUI.Elem(plate.gameObject).ignoreLayout = true;
            NIUI.Fill(plate.rectTransform);
            Text n = NIUI.Txt(row.transform, "Number", (i + 1).ToString(), NIType.Number - 6f, NIUI.Glow, FontStyle.Normal, TextAnchor.MiddleLeft);
            NIUI.Elem(n.gameObject, U(34f), -1f, U(34f), -1f, 0f, 0f);
            Text t = NIUI.Txt(row.transform, "Text", sample[i], NIType.Body, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
            NIUI.Elem(t.gameObject, 0f, -1f, -1f, -1f, 1f, 0f);
            NIAnimItem.Add(row.gameObject, 0.2f + i * 0.12f, 0.6f);
        }

        RectTransform benefits = Card(viewProgramme.transform, "Benefits", GlassTint, false, U(16f), U(16f), U(6f));
        NIUI.Elem(benefits.gameObject, 0f, -1f, -1f, -1f, 1f, 0f);
        Accent(benefits, NIUI.Gold);
        progBenefitsLabel = NIUI.Txt(benefits, "Label", "BENEFITS", NIType.Label, NIUI.Gold, FontStyle.Bold);
        progTicks = Ticks(benefits, NIData.Programmes[0].get, Color.white);
        NIAnimItem.Add(benefits.gameObject, 0.8f);
    }

    void BuildConsult()
    {
        viewConsult = NewView(bodyRoot, "View_Consult");
        Sub(viewConsult.transform, "Speak with our Academy team beside you, or scan to book your consultation.", 0f);

        HorizontalLayoutGroup row = NIUI.HStack(viewConsult.transform, "Consult", U(12f), TextAnchor.UpperLeft, true);
        row.childForceExpandWidth = false;
        Flex(row);
        qrBook = QrCard(row.transform, "QrBook", "https://inxeducation.ac.ae/book", "Book your consultation", 0.15f);
        qrAcademy = QrCard(row.transform, "QrAcademy", NIData.ContactUrl, "Explore the Academy", 0.3f);

        RectTransform info = Card(row.transform, "CodeCard", new Color(0.55f, 0.65f, 0.95f, 1f), false, U(22f), U(16f), U(4f));
        info.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        NIUI.Elem(info.gameObject, 0f, -1f, -1f, -1f, 1f, 0f);
        Image bar = NIUI.RoundedImg(info, "GoldBar", NIUI.Gold, 6);
        NIUI.Elem(bar.gameObject).ignoreLayout = true;
        bar.rectTransform.anchorMin = new Vector2(0f, 0f);
        bar.rectTransform.anchorMax = new Vector2(0f, 1f);
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);
        bar.rectTransform.anchoredPosition = new Vector2(U(8f), 0f);
        bar.rectTransform.sizeDelta = new Vector2(U(4f), -U(28f));
        NIUI.Txt(info, "Label", "YOUR SESSION CODE", NIType.Label, NIUI.Gold, FontStyle.Bold);
        codeText = NIUI.Txt(info, "Code", "NI-1234", NIType.Code, Color.white, FontStyle.Bold);
        handoffText = NIUI.Txt(info, "Message", "Show this code to our expert beside you.", NIType.Small, NIUI.Muted);
        exploredText = NIUI.Txt(info, "Explored", "You explored: Meeting Emiratisation targets", NIType.Small, NIUI.Muted);
        NIAnimItem.Add(info.gameObject, 0.45f);
    }

    RawImage QrCard(Transform parent, string name, string content, string caption, float delay)
    {
        RectTransform card = Card(parent, name, new Color(1f, 1f, 1f, 1f), false, U(12f), U(12f), U(8f));
        card.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.98f);
        card.GetComponent<Image>().sprite = NIUI.Art("ui_round_16");
        card.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
        NIUI.Elem(card.gameObject, U(205f), -1f, U(205f), -1f, 0f, 0f);

        RectTransform holder = NIUI.Rect("Code", card);
        NIUI.Elem(holder.gameObject, U(165f), U(165f), U(165f), U(165f));
        RawImage image = holder.gameObject.AddComponent<RawImage>();
        image.texture = QrTexture(content);
        image.color = Color.white;
        image.raycastTarget = false;

        NIUI.Txt(card, "Caption", caption, NIType.Small, NIUI.Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
        NIAnimItem.Add(card.gameObject, delay);
        return image;
    }

    // ---------- pipeline and toast ----------

    void BuildPipe(RectTransform root)
    {
        pipeRoot = NIUI.Rect("Pipeline", root);
        pipeRoot.anchorMin = new Vector2(0f, 0f);
        pipeRoot.anchorMax = new Vector2(1f, 0f);
        pipeRoot.pivot = new Vector2(0.5f, 0f);
        pipeRoot.offsetMin = new Vector2(U(36f), U(18f));
        pipeRoot.offsetMax = new Vector2(-U(36f), U(18f) + U(62f));

        Image band = NIUI.GlassImg(pipeRoot, "Band", new Color(0.7f, 0.8f, 1f, 0.55f), true);
        NIUI.Elem(band.gameObject).ignoreLayout = true;
        NIUI.Fill(band.rectTransform, -U(10f), -U(2f), -U(10f), -U(2f));

        HorizontalLayoutGroup row = pipeRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = U(14f);
        row.padding = new RectOffset(Mathf.RoundToInt(U(6f)), Mathf.RoundToInt(U(6f)), 0, 0);
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        Text label = NIUI.Txt(pipeRoot, "Label", "YOUR TALENT PIPELINE", NIType.Label, NIUI.Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform nodes = NIUI.Rect("Nodes", pipeRoot);
        NIUI.Elem(nodes.gameObject, -1f, U(50f), -1f, U(50f), 1f, 0f);
        HorizontalLayoutGroup nodeRow = nodes.gameObject.AddComponent<HorizontalLayoutGroup>();
        nodeRow.childAlignment = TextAnchor.UpperCenter;
        nodeRow.childControlWidth = true;
        nodeRow.childControlHeight = true;
        nodeRow.childForceExpandWidth = true;
        nodeRow.childForceExpandHeight = true;

        Image line = NIUI.Img(nodes, "Line", new Color(180f / 255f, 198f / 255f, 238f / 255f, 0.2f));
        NIUI.Elem(line.gameObject).ignoreLayout = true;
        line.rectTransform.anchorMin = new Vector2(0.1f, 1f);
        line.rectTransform.anchorMax = new Vector2(0.9f, 1f);
        line.rectTransform.pivot = new Vector2(0.5f, 1f);
        line.rectTransform.anchoredPosition = new Vector2(0f, -U(11f));
        line.rectTransform.sizeDelta = new Vector2(0f, 2f);

        int count = NIData.StageList.Length;
        pipeHolders = new RectTransform[count];
        pipeFills = new Image[count];
        pipeRings = new Image[count];
        pipeGlows = new Image[count];
        pipeLabels = new Text[count];
        for (int i = 0; i < count; i++)
        {
            string id = NIData.StageList[i][0];
            VerticalLayoutGroup cell = NIUI.VStack(nodes, "Node_" + id, U(5f), TextAnchor.UpperCenter, false);

            RectTransform holder = NIUI.Rect("Dot", cell.transform);
            NIUI.Elem(holder.gameObject, U(20f), U(20f), U(20f), U(20f));
            Image glow = NIUI.Img(holder, "Glow", new Color(NIUI.Gold.r, NIUI.Gold.g, NIUI.Gold.b, 0f), NIUI.Radial());
            NIUI.Center(glow.rectTransform, Vector2.zero, new Vector2(U(58f), U(58f)));
            Image ring = NIUI.Img(holder, "Ring", RingOff, NIUI.Disc());
            NIUI.Fill(ring.rectTransform);
            Image fill = NIUI.Img(holder, "Fill", Off, NIUI.Disc());
            NIUI.Fill(fill.rectTransform, U(2.2f), U(2.2f), U(2.2f), U(2.2f));

            Text name = NIUI.Txt(cell.transform, "Name", NIData.StageList[i][1], NIType.Micro, NIUI.Faint, FontStyle.Bold, TextAnchor.UpperCenter);
            NIUI.Elem(name.gameObject, -1f, -1f, -1f, -1f, 1f, 0f);

            pipeHolders[i] = holder;
            pipeFills[i] = fill;
            pipeRings[i] = ring;
            pipeGlows[i] = glow;
            pipeLabels[i] = name;
        }

        pipeCount = NIUI.Txt(pipeRoot, "Count", "0 of 5", NIType.Small, NIUI.Gold, FontStyle.Bold, TextAnchor.MiddleRight);
        pipeCount.horizontalOverflow = HorizontalWrapMode.Overflow;

        // show two stages as lit in the editor so the styling is visible
        HashSet<string> sample = new HashSet<string> { "work", "role" };
        SetPipe(sample, null);
    }

    void BuildToast(RectTransform root)
    {
        toastRoot = NIUI.Rect("Toast", root);
        toastRoot.anchorMin = toastRoot.anchorMax = new Vector2(0.5f, 1f);
        toastRoot.pivot = new Vector2(0.5f, 1f);
        toastRoot.anchoredPosition = new Vector2(0f, U(200f));
        toastRoot.sizeDelta = new Vector2(U(460f), U(64f));

        Image plate = toastRoot.gameObject.AddComponent<Image>();
        plate.sprite = NIUI.GlassPill();
        plate.type = Image.Type.Sliced;
        plate.color = new Color(1f, 1f, 1f, 1f);
        plate.raycastTarget = false;
        Image solid = NIUI.RoundedImg(toastRoot, "Solid", new Color(1f, 1f, 1f, 0.97f), 31);
        NIUI.Elem(solid.gameObject).ignoreLayout = true;
        NIUI.Fill(solid.rectTransform);
        solid.transform.SetAsFirstSibling();

        HorizontalLayoutGroup row = toastRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(Mathf.RoundToInt(U(10f)), Mathf.RoundToInt(U(26f)), Mathf.RoundToInt(U(10f)), Mathf.RoundToInt(U(10f)));
        row.spacing = U(12f);
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        ContentSizeFitter fitter = toastRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform iconHolder = NIUI.Rect("Icon", toastRoot);
        NIUI.Elem(iconHolder.gameObject, U(36f), U(36f), U(36f), U(36f));
        Image iconBg = iconHolder.gameObject.AddComponent<Image>();
        iconBg.sprite = NIUI.Disc();
        iconBg.color = NIUI.Navy;
        iconBg.raycastTarget = false;
        Image caret = NIUI.Img(iconHolder, "Caret", Color.white, NIUI.Art("caret_gold"));
        caret.preserveAspect = true;
        NIUI.Center(caret.rectTransform, Vector2.zero, new Vector2(U(18f), U(13f)));

        VerticalLayoutGroup texts = NIUI.VStack(toastRoot, "Texts", 0f, TextAnchor.MiddleLeft, false);
        toastTitle = NIUI.Txt(texts.transform, "Title", "Added to your talent pipeline", NIType.Body, NIUI.Navy, FontStyle.Bold);
        toastTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
        toastSub = NIUI.Txt(texts.transform, "Sub", "Work Ready", NIType.Small, NIUI.Hex(0x4A5B84), FontStyle.Normal);
        toastSub.horizontalOverflow = HorizontalWrapMode.Overflow;
    }
}
