using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(BookScreens))]
public class BookScreensEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "The pages are panels in the scene. Open LeftCanvas, RightCanvas, and SingleCanvas, then open PagePanels. Change Editing Page to show a page in the Scene view. Select a button or a picture there and drag a new sprite onto its Image. Press Play to see it.",
            MessageType.Info);
        DrawDefaultInspector();
    }

    [MenuItem("Book/Animate Left Page 1")]
    public static void AnimateLeftPageOne()
    {
        GameObject canvas = GameObject.Find("LeftCanvas");
        Transform page = null;
        Transform[] all = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "Page 1" && all[i].parent != null && all[i].parent.name == "PagePanels")
                page = all[i];
        }

        Transform gradient = page.Find("GradientImage");
        Transform text = page.Find("Text");
        Image image = gradient.GetComponent<Image>();
        Sprite sprite = image != null ? image.sprite : gradient.GetComponent<MotionGlowGraphic>().shape;
        if (image != null)
            Object.DestroyImmediate(image);

        MotionGlowGraphic graphic = gradient.GetComponent<MotionGlowGraphic>();
        if (graphic == null)
            graphic = gradient.gameObject.AddComponent<MotionGlowGraphic>();
        graphic.shape = sprite;
        graphic.additive = false;
        graphic.blur = 0.04f;
        graphic.brightness = 1.05f;
        graphic.softness = 1f;
        graphic.colorA = new Color(0.62f, 0.86f, 1f, 1f);
        graphic.colorB = new Color(0f, 0.32f, 0.98f, 1f);
        graphic.gradientAngle = 90f;
        graphic.scrollSpeed = 0.04f;
        graphic.repeatGradient = true;
        graphic.PushProperties();

        MotionGlow glow = MotionStudio.Motion.Glow(gradient.gameObject, 1.7f, 0f);
        glow.growFrom = 0.08f;
        glow.fadeFrom = 0f;
        glow.fadeDuration = 1.05f;
        glow.brightnessFrom = 0.15f;
        glow.extraBlurFrom = 0.7f;
        glow.breathe = 0.03f;
        glow.Record();

        MotionItem words = MotionStudio.Motion.Words(text.gameObject, MotionDirection.None, 0f, 0.16f);
        words.fade = true;
        words.fadeFrom = 0f;
        words.duration = 0.6f;
        words.pieceDuration = 0.6f;
        words.split = TextSplit.Words;
        words.Record();

        MotionSequence sequence = MotionStudio.Motion.Sequence(page.gameObject, 0.55f);
        sequence.playOnStart = true;
        sequence.hideUntilPlayed = false;
        sequence.speed = 0.5f;
        sequence.Clear();
        sequence.AddTo(glow);
        sequence.AddTo(words);

        EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
        EditorSceneManager.SaveScene(page.gameObject.scene);
        Debug.Log("LeftCanvas Page 1 motion ready. " + text.GetComponent<TMP_Text>().text);
    }

    [MenuItem("Book/Animate Right Page 1")]
    public static void AnimateRightPageOne()
    {
        GameObject canvas = GameObject.Find("RightCanvas");
        Transform page = null;
        Transform[] all = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "Page 1" && all[i].parent != null && all[i].parent.name == "PagePanels")
                page = all[i];
        }

        Transform gradient = null;
        Transform sentence = null;
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsChildOf(page))
                continue;
            if (all[i].name == "GradientImage")
                gradient = all[i];
            if (all[i].name == "Text" && all[i].parent == page)
                sentence = all[i];
        }

        Image image = gradient.GetComponent<Image>();
        Sprite sprite = image != null ? image.sprite : gradient.GetComponent<MotionGlowGraphic>().shape;
        if (image != null)
            Object.DestroyImmediate(image);

        MotionGlowGraphic graphic = gradient.GetComponent<MotionGlowGraphic>();
        if (graphic == null)
            graphic = gradient.gameObject.AddComponent<MotionGlowGraphic>();
        graphic.shape = sprite;
        graphic.additive = false;
        graphic.blur = 0.04f;
        graphic.brightness = 1.05f;
        graphic.softness = 1f;
        graphic.colorA = new Color(0.62f, 0.86f, 1f, 1f);
        graphic.colorB = new Color(0f, 0.32f, 0.98f, 1f);
        graphic.gradientAngle = 90f;
        graphic.scrollSpeed = 0.04f;
        graphic.repeatGradient = true;
        graphic.PushProperties();

        MotionGlow glow = MotionStudio.Motion.Glow(gradient.gameObject, 1.7f, 0f);
        glow.growFrom = 0.08f;
        glow.fadeFrom = 0f;
        glow.fadeDuration = 1.05f;
        glow.brightnessFrom = 0.15f;
        glow.extraBlurFrom = 0.7f;
        glow.breathe = 0.03f;
        glow.Record();

        MotionItem words = MotionStudio.Motion.Words(sentence.gameObject, MotionDirection.None, 0f, 0.16f);
        words.fade = true;
        words.fadeFrom = 0f;
        words.duration = 0.6f;
        words.pieceDuration = 0.6f;
        words.split = TextSplit.Words;
        words.Record();

        MotionSequence sequence = MotionStudio.Motion.Sequence(page.gameObject, 0.25f, SequenceSpacing.AfterPrevious);
        sequence.playOnStart = true;
        sequence.hideUntilPlayed = false;
        sequence.speed = 0.5f;
        sequence.Clear();
        sequence.AddTo(glow);
        sequence.AddTo(words);

        int shown = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsChildOf(page) || all[i] == page)
                continue;
            if (all[i].name != "Icon" && all[i].name != "Icons")
                continue;
            MotionItem icon = MotionStudio.Motion.FadeIn(all[i].gameObject, MotionDirection.None, 0f, 0.45f);
            icon.scale = true;
            icon.scaleFrom = 0.82f;
            icon.Record();
            sequence.AddTo(icon);
            icon.order = 2;
            shown++;
        }

        EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
        EditorSceneManager.SaveScene(page.gameObject.scene);
        Debug.Log("RightCanvas Page 1 motion ready. Icons " + shown + ". " + sentence.GetComponent<TMP_Text>().text);
    }

    [MenuItem("Book/Animate Left Page 2")]
    public static void AnimateLeftPageTwo()
    {
        GameObject canvas = GameObject.Find("LeftCanvas");
        Transform page = null;
        Transform[] all = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "Page 2" && all[i].parent != null && all[i].parent.name == "PagePanels")
                page = all[i];
        }

        string[] rows = { "At A Glance", "Begin Here", "Seal Of Approval", "Get In Touch" };
        TMP_Text title = page.Find("Text").GetComponent<TMP_Text>();

        for (int r = 0; r < rows.Length; r++)
        {
            Transform row = page.Find(rows[r]);
            Transform line = row.Find("Line");
            MotionCard oldCard = line.GetComponent<MotionCard>();
            if (oldCard != null)
                Object.DestroyImmediate(oldCard);
            MotionClipEffect oldClip = line.GetComponent<MotionClipEffect>();
            if (oldClip != null)
                Object.DestroyImmediate(oldClip);
            RectMask2D oldMask = line.GetComponent<RectMask2D>();
            if (oldMask != null)
                Object.DestroyImmediate(oldMask);
            AxisReveal grow = line.GetComponent<AxisReveal>();
            if (grow == null)
                grow = line.gameObject.AddComponent<AxisReveal>();
            grow.axis = AxisReveal.Axis.Horizontal;
            grow.duration = 0.45f;
            grow.recorded = false;
            grow.Prepare();

            Transform second = row.Find("ImagetoHighlight2");
            AxisReveal vertical = second.GetComponent<AxisReveal>();
            if (vertical == null)
                vertical = second.gameObject.AddComponent<AxisReveal>();
            vertical.axis = AxisReveal.Axis.Vertical;
            vertical.duration = 0.45f;
            vertical.recorded = false;
            vertical.Prepare();

            PageRowHighlight click = row.GetComponent<PageRowHighlight>();
            if (click == null)
                click = row.gameObject.AddComponent<PageRowHighlight>();
            click.glow = row.Find("ImagetoHighlight").GetComponent<MotionGlow>();
            click.vertical = vertical;
            Button button = row.GetComponent<Button>();
            for (int c = button.onClick.GetPersistentEventCount() - 1; c >= 0; c--)
            {
                if (button.onClick.GetPersistentMethodName(c) == "Show")
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, c);
            }
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, click.Show);
        }

        MotionSequence sequence = MotionStudio.Motion.Sequence(page.gameObject, 0.12f, SequenceSpacing.AfterPrevious);
        sequence.playOnStart = true;
        sequence.hideUntilPlayed = false;
        sequence.speed = 0.5f;
        sequence.Clear();

        MotionItem heading = MotionStudio.Motion.Words(title.gameObject, MotionDirection.None, 0f, 0.08f);
        heading.duration = 0.45f;
        heading.pieceDuration = 0.45f;
        heading.Record();
        sequence.AddTo(heading);

        for (int r = 0; r < rows.Length; r++)
        {
            Transform row = page.Find(rows[r]);
            AxisReveal line = row.Find("Line").GetComponent<AxisReveal>();
            line.order = 0;
            line.delay = 0f;
            sequence.Add(line, 0f);

            MotionItem words = MotionStudio.Motion.Words(row.Find("Text").gameObject, MotionDirection.None, 0f, 0.1f);
            words.duration = 0.4f;
            words.pieceDuration = 0.4f;
            words.order = 0;
            words.delay = 0f;
            words.Record();
            sequence.Add(words, 0f);

            MotionItem icon = MotionStudio.Motion.FadeIn(row.Find("Icon").gameObject, MotionDirection.None, 0f, 0.35f);
            icon.scale = false;
            icon.order = 0;
            icon.delay = 0f;
            icon.Record();
            sequence.Add(icon, 0f);
        }

        EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
        EditorSceneManager.SaveScene(page.gameObject.scene);
        Debug.Log("LeftCanvas Page 2 rows ready.");
    }

    [MenuItem("Book/Build Main Page 3")]
    public static void BuildMainPageThree()
    {
        GameObject canvas = GameObject.Find("MainCanvas");
        Transform page = null;
        Transform[] all = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "Page 3" && all[i].parent != null && all[i].parent.name == "PagePanels")
                page = all[i];
        }

        Transform spread = page.Find("Spread");
        if (spread != null)
            spread.gameObject.SetActive(false);

        Transform existing = page.Find("Sheet");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        RectTransform sheet = NewStretch("Sheet", page);
        TMP_FontAsset light = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Inter_28pt-Light SDF.asset");
        TMP_FontAsset bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Inter_28pt-Bold SDF.asset");
        TMP_FontAsset semi = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Inter_24pt-SemiBold SDF.asset");
        Color white = new Color(0.93f, 0.95f, 0.98f, 1f);
        Color blue = new Color(0.55f, 0.72f, 1f, 1f);
        Color mute = new Color(0.78f, 0.82f, 0.9f, 1f);

        TMP_Text eyebrow = NewLabel(sheet, "Eyebrow", "AT A GLANCE", light, 18, mute, TextAlignmentOptions.MidlineLeft);
        SetAnchors(eyebrow.rectTransform, 0.07f, 0.78f, 0.4f, 0.84f);
        Rule(sheet, "EyebrowRule", 0.045f, 0.805f, 0.065f, 0.812f, mute);

        TMP_Text headline = NewLabel(sheet, "Headline", "FROM A LANGUAGE GAP\nTO <color=#8CB8FF>WORKPLACE CONFIDENCE.</color>", bold, 54, white, TextAlignmentOptions.TopLeft);
        SetAnchors(headline.rectTransform, 0.07f, 0.6f, 0.72f, 0.78f);
        headline.lineSpacing = -8f;

        RectTransform card = NewStretch("Card", sheet);
        SetAnchors(card, 0.07f, 0.34f, 0.78f, 0.56f);
        Frame(card, white);
        TMP_Text promise = NewLabel(card, "Promise", "GREAT IDEAS DESERVE\nGREAT DELIVERY.", semi, 26, white, TextAlignmentOptions.TopLeft);
        SetAnchors(promise.rectTransform, 0.05f, 0.18f, 0.38f, 0.82f);
        Rule(card, "Divider", 0.42f, 0.22f, 0.425f, 0.78f, new Color(1f, 1f, 1f, 0.35f));
        TMP_Text body = NewLabel(card, "Body", "The Language & Communication Academy at In^X Education gives institutes and organizations the communication skills to thrive in today's workplace, so they can say what they mean and mean what they say.", light, 20, mute, TextAlignmentOptions.TopLeft);
        SetAnchors(body.rectTransform, 0.46f, 0.16f, 0.96f, 0.84f);
        body.color = white;

        RectTransform footer = NewStretch("Footer", sheet);
        SetAnchors(footer, 0.05f, 0.04f, 0.95f, 0.14f);
        Rule(footer, "TopRule", 0f, 0.92f, 1f, 0.96f, new Color(1f, 1f, 1f, 0.28f));
        TMP_Text contact = NewLabel(footer, "Contact", "FOR MORE INFO,\nKINDLY CONTACT", light, 14, mute, TextAlignmentOptions.MidlineLeft);
        SetAnchors(contact.rectTransform, 0.02f, 0.08f, 0.2f, 0.85f);
        TMP_Text web = NewLabel(footer, "Web", "inxeducation.ac.ae", light, 18, white, TextAlignmentOptions.MidlineLeft);
        SetAnchors(web.rectTransform, 0.28f, 0.08f, 0.5f, 0.85f);
        TMP_Text phone = NewLabel(footer, "Phone", "+971 2 208 5700", light, 18, white, TextAlignmentOptions.MidlineLeft);
        SetAnchors(phone.rectTransform, 0.54f, 0.08f, 0.72f, 0.85f);
        TMP_Text mail = NewLabel(footer, "Mail", "info@inxeducation.ae", light, 18, white, TextAlignmentOptions.MidlineLeft);
        SetAnchors(mail.rectTransform, 0.76f, 0.08f, 0.98f, 0.85f);

        TMP_Text logo = NewLabel(sheet, "Logo", "In^X Next", bold, 28, white, TextAlignmentOptions.MidlineRight);
        SetAnchors(logo.rectTransform, 0.7f, 0.88f, 0.95f, 0.96f);
        logo.color = blue;

        EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
        EditorSceneManager.SaveScene(page.gameObject.scene);
        Debug.Log("Main canvas page 3 is one full page.");
    }

    static RectTransform NewStretch(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    static void SetAnchors(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static TMP_Text NewLabel(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rect = NewStretch(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    static void Rule(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
    {
        RectTransform rect = NewStretch(name, parent);
        SetAnchors(rect, x0, y0, x1, y1);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    static void Frame(RectTransform card, Color color)
    {
        Rule(card, "Top", 0f, 0.985f, 1f, 1f, color);
        Rule(card, "Bottom", 0f, 0f, 1f, 0.015f, color);
        Rule(card, "SideLeft", 0f, 0f, 0.004f, 1f, color);
        Rule(card, "SideRight", 0.996f, 0f, 1f, 1f, color);
    }

    static void CopyRowChrome(Transform source, Transform destination)
    {
        string[] pieces = { "Number", "Line", "Icon", "ImagetoHighlight" };
        for (int i = 0; i < pieces.Length; i++)
        {
            Transform existing = destination.Find(pieces[i]);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            Transform copy = Object.Instantiate(source.Find(pieces[i]), destination);
            copy.name = pieces[i];
            RectTransform from = source.Find(pieces[i]) as RectTransform;
            RectTransform to = copy as RectTransform;
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;
        }

        RectTransform sourceLabel = source.Find("Text") as RectTransform;
        RectTransform label = destination.Find("Text") as RectTransform;
        label.anchorMin = sourceLabel.anchorMin;
        label.anchorMax = sourceLabel.anchorMax;
        label.pivot = sourceLabel.pivot;
        label.anchoredPosition = sourceLabel.anchoredPosition;
        label.sizeDelta = sourceLabel.sizeDelta;
        TMP_Text fromText = sourceLabel.GetComponent<TMP_Text>();
        TMP_Text toText = label.GetComponent<TMP_Text>();
        toText.font = fromText.font;
        toText.fontSize = fromText.fontSize;
        toText.alignment = fromText.alignment;
        toText.fontStyle = fromText.fontStyle;
    }

    static void PrepareHighlight(Transform highlight)
    {
        Image image = highlight.GetComponent<Image>();
        Sprite sprite = image != null ? image.sprite : highlight.GetComponent<MotionGlowGraphic>().shape;
        if (image != null)
            Object.DestroyImmediate(image);
        MotionGlowGraphic graphic = highlight.GetComponent<MotionGlowGraphic>();
        if (graphic == null)
            graphic = highlight.gameObject.AddComponent<MotionGlowGraphic>();
        graphic.shape = sprite;
        graphic.additive = false;
        graphic.blur = 0.05f;
        graphic.brightness = 1.05f;
        graphic.softness = 1f;
        graphic.colorA = new Color(0.62f, 0.86f, 1f, 1f);
        graphic.colorB = new Color(0f, 0.32f, 0.98f, 1f);
        graphic.gradientAngle = 90f;
        graphic.scrollSpeed = 0.04f;
        graphic.repeatGradient = true;
        graphic.raycastTarget = false;
        graphic.PushProperties();
        MotionGlow glow = MotionStudio.Motion.Glow(highlight.gameObject, 0.7f, 0f);
        glow.growFrom = 0.2f;
        glow.fadeFrom = 0f;
        glow.fadeDuration = 0.45f;
        glow.brightnessFrom = 0.2f;
        glow.extraBlurFrom = 0.45f;
        glow.Record();
    }
}
