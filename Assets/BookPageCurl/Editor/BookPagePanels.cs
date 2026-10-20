using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BookPagePanels
{
    const string WhitePath = "Assets/BookPageCurl/Sprites/White.png";
    const string CirclePath = "Assets/BookPageCurl/Sprites/ButtonCircle.png";

    static readonly Color Ink = new Color(0.18f, 0.14f, 0.12f, 1f);
    static readonly Color Paper = new Color(0.96f, 0.93f, 0.86f, 1f);
    static readonly Color Cover = new Color(0.33f, 0.15f, 0.17f, 1f);
    static readonly Color Gold = new Color(0.82f, 0.66f, 0.34f, 1f);
    static readonly Color ButtonFace = new Color(0.94f, 0.91f, 0.84f, 1f);
    static readonly Color SelectedFace = new Color(0.18f, 0.42f, 0.32f, 1f);
    static readonly Color IconFace = new Color(0.73f, 0.81f, 0.90f, 1f);
    static readonly Color Dark = new Color(0.14f, 0.1f, 0.09f, 1f);

    [MenuItem("Book/Build Page Panels")]
    public static void Build()
    {
        BookScreens screens = Object.FindAnyObjectByType<BookScreens>();
        Canvas left = Find("LeftCanvas");
        Canvas right = Find("RightCanvas");
        Canvas single = Find("SingleCanvas");
        if (screens == null || left == null || right == null || single == null)
        {
            Debug.LogError("Build Page Panels needs DisplaySetup, LeftCanvas, RightCanvas, and SingleCanvas.");
            return;
        }

        screens.EnsureContent();
        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        Sprite circle = CircleSprite();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        Rebuild(left.transform, screens, true, white, circle, font);
        Rebuild(right.transform, screens, false, white, circle, font);
        RebuildBook(single.transform, screens, white, font);
        screens.editingPage = Mathf.Clamp(screens.editingPage, 0, screens.steps.Count - 1);
        ShowOnly(left.transform, screens.editingPage);
        ShowOnly(right.transform, screens.editingPage);
        ShowOnly(single.transform, screens.editingPage);

        EditorUtility.SetDirty(screens);
        EditorSceneManager.MarkSceneDirty(screens.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Page panels are on LeftCanvas, RightCanvas, and SingleCanvas. Change Editing Page on DisplaySetup to view another page.");
    }

    static void Rebuild(Transform canvas, BookScreens screens, bool displayOne, Sprite white, Sprite circle, TMP_FontAsset font)
    {
        Transform existing = canvas.Find("PagePanels");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
        RectTransform root = Stretch("PagePanels", canvas);
        for (int i = 0; i < screens.steps.Count; i++)
        {
            RectTransform page = Stretch("Page " + (i + 1), root);
            if (displayOne)
                BuildDisplayOne(page, screens, screens.steps[i], white, font);
            else
                BuildDisplayTwo(page, screens, screens.steps[i], white, circle, font);
        }
    }

    static void RebuildBook(Transform canvas, BookScreens screens, Sprite white, TMP_FontAsset font)
    {
        Transform existing = canvas.Find("PagePanels");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
        RectTransform root = Stretch("PagePanels", canvas);
        for (int i = 0; i < screens.steps.Count; i++)
        {
            RectTransform page = Stretch("Page " + (i + 1), root);
            RectTransform spread = Box("Spread", page, new Vector2(1500f, 860f), Vector2.zero);
            BookScreens.Step step = screens.steps[i];
            if (step.page == BookScreens.PageKind.Cover)
            {
                Image back = Fill("Back", spread, Dark, white);
                SetAnchors(back.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f));
                RectTransform cover = Half("Cover", spread, false);
                if (step.bookRight != null)
                    FullImage("Picture", cover, step.bookRight);
                else
                {
                    Fill("Background", cover, Cover, white);
                    Rule(cover, Gold, white, false);
                    Label(cover, "Languages\nand Communication\nAcademy", 64, Gold, FontStyle.Bold, TextAnchor.MiddleCenter, font, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.9f));
                    Label(cover, "Turn the page", 28, new Color(0.93f, 0.86f, 0.74f, 1f), FontStyle.Normal, TextAnchor.MiddleCenter, font, new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.32f));
                }
                continue;
            }

            RectTransform left = Half("Left", spread, true);
            RectTransform right = Half("Right", spread, false);
            if (step.bookLeft != null)
                FullImage("Picture", left, step.bookLeft);
            else
                DrawFace(left, step, true, white, font);
            if (step.bookRight != null)
                FullImage("Picture", right, step.bookRight);
            else
                DrawFace(right, step, false, white, font);
        }
    }

    static void DrawFace(RectTransform page, BookScreens.Step step, bool left, Sprite white, TMP_FontAsset font)
    {
        Fill("Background", page, Paper, white);
        Rule(page, Gold, white, left);
        if (step.page == BookScreens.PageKind.Endpaper)
            return;
        if (step.page == BookScreens.PageKind.Open)
        {
            if (!left)
                Label(page, "The book is open.\n\nTurn the page to read it.", 42, Ink, FontStyle.Bold, TextAnchor.MiddleCenter, font, new Vector2(0.12f, 0.2f), new Vector2(0.88f, 0.8f));
            return;
        }

        if (step.page == BookScreens.PageKind.Columns)
        {
            if (left)
            {
                Label(page, step.pageHeading, 36, Ink, FontStyle.Bold, TextAnchor.UpperLeft, font, new Vector2(0.1f, 0.62f), new Vector2(0.88f, 0.9f));
                Label(page, step.pageBody, 26, Ink, FontStyle.Normal, TextAnchor.UpperLeft, font, new Vector2(0.1f, 0.1f), new Vector2(0.88f, 0.58f));
            }
            else if (step.columns != null)
            {
                int count = Mathf.Min(3, step.columns.Length);
                for (int i = 0; i < count; i++)
                {
                    float x0 = 0.08f + i * 0.3f;
                    float x1 = x0 + 0.27f;
                    Label(page, step.columns[i].heading, 26, Ink, FontStyle.Bold, TextAnchor.UpperLeft, font, new Vector2(x0, 0.72f), new Vector2(x1, 0.9f));
                    Label(page, step.columns[i].body, 22, Ink, FontStyle.Normal, TextAnchor.UpperLeft, font, new Vector2(x0, 0.08f), new Vector2(x1, 0.7f));
                }
            }
            return;
        }

        if (left)
            Label(page, step.pageHeading, 40, Ink, FontStyle.Bold, TextAnchor.UpperLeft, font, new Vector2(0.1f, 0.12f), new Vector2(0.88f, 0.88f));
        else
            Label(page, step.pageBody, 28, Ink, FontStyle.Normal, TextAnchor.UpperLeft, font, new Vector2(0.12f, 0.1f), new Vector2(0.9f, 0.9f));
    }

    static void BuildDisplayOne(RectTransform page, BookScreens screens, BookScreens.Step step, Sprite white, TMP_FontAsset font)
    {
        Label(page, step.screenTitle, step.showDiscoverButtons ? 48 : 64, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, font, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f));
        if (!step.showDiscoverButtons || screens.displayOneButtons == null)
            return;
        for (int i = 0; i < screens.displayOneButtons.Count; i++)
        {
            Vector2 min;
            Vector2 max;
            Slot(i, screens.displayOneButtons.Count, out min, out max);
            BookScreens.ButtonDef def = screens.displayOneButtons[i];
            bool selected = step.selectedButton == def.id;
            Control(page, def, min, max, selected, false, white, null, font);
        }
    }

    static void BuildDisplayTwo(RectTransform page, BookScreens screens, BookScreens.Step step, Sprite white, Sprite circle, TMP_FontAsset font)
    {
        if (!string.IsNullOrEmpty(step.prompt))
            Label(page, step.prompt, 40, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, font, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.94f));
        Control(page, screens.iconButton, new Vector2(0.28f, 0.34f), new Vector2(0.72f, 0.74f), false, true, white, circle, font);
        if (step.showHome)
            Control(page, screens.homeButton, new Vector2(0.08f, 0.06f), new Vector2(0.46f, 0.18f), false, false, white, null, font);
        if (step.showTurn)
        {
            Vector2 min = step.showHome ? new Vector2(0.54f, 0.06f) : new Vector2(0.28f, 0.08f);
            Vector2 max = step.showHome ? new Vector2(0.92f, 0.18f) : new Vector2(0.72f, 0.2f);
            Control(page, screens.turnButton, min, max, false, false, white, null, font);
        }
    }

    static void Control(RectTransform parent, BookScreens.ButtonDef def, Vector2 min, Vector2 max, bool selected, bool icon, Sprite white, Sprite circle, TMP_FontAsset font)
    {
        if (def == null)
            return;
        string name = string.IsNullOrEmpty(def.label) ? def.id : def.label;
        Sprite sprite = def.image != null ? def.image : (icon ? circle : white);
        Color face = def.image != null ? Color.white : (icon ? IconFace : (selected ? SelectedFace : ButtonFace));
        Image image = Block(name, parent, face, sprite);
        if (icon)
        {
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.54f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.54f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = new Vector2(460f, 460f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.preserveAspect = def.image != null;
        }
        else
            SetAnchors(image.rectTransform, min, max);
        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = face;
        colors.highlightedColor = face * 1.05f;
        colors.pressedColor = new Color(face.r * 0.82f, face.g * 0.82f, face.b * 0.82f, 1f);
        colors.selectedColor = face;
        button.colors = colors;
        button.targetGraphic = image;

        if (!string.IsNullOrEmpty(def.label))
            Label(image.rectTransform, def.label, icon ? 36 : 28, def.image != null ? Ink : (selected ? Color.white : Ink), FontStyle.Bold, TextAnchor.MiddleCenter, font, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f));

        BookControl control = image.gameObject.AddComponent<BookControl>();
        control.action = def.action;
        control.page = def.step;
        UnityEventTools.AddPersistentListener(button.onClick, control.Click);
    }

    static void Slot(int index, int count, out Vector2 min, out Vector2 max)
    {
        if (count == 4)
        {
            Vector2[] mins = { new Vector2(0.08f, 0.40f), new Vector2(0.52f, 0.40f), new Vector2(0.08f, 0.16f), new Vector2(0.52f, 0.16f) };
            Vector2[] maxs = { new Vector2(0.48f, 0.62f), new Vector2(0.92f, 0.62f), new Vector2(0.48f, 0.36f), new Vector2(0.92f, 0.36f) };
            min = mins[Mathf.Clamp(index, 0, 3)];
            max = maxs[Mathf.Clamp(index, 0, 3)];
            return;
        }

        int column = index % 2;
        int row = index / 2;
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / 2f));
        float left = 0.08f;
        float right = 0.92f;
        float bottom = 0.16f;
        float top = 0.62f;
        float gap = 0.04f;
        float cellWidth = (right - left - gap) / 2f;
        float cellHeight = (top - bottom - gap * (rows - 1)) / rows;
        float x = left + column * (cellWidth + gap);
        float y = top - (row + 1) * cellHeight - row * gap;
        min = new Vector2(x, y);
        max = new Vector2(x + cellWidth, y + cellHeight);
    }

    static void ShowOnly(Transform canvas, int index)
    {
        Transform root = canvas.Find("PagePanels");
        if (root == null)
            return;
        for (int i = 0; i < root.childCount; i++)
            root.GetChild(i).gameObject.SetActive(i == index);
    }

    static Canvas Find(string canvasName)
    {
        GameObject go = GameObject.Find(canvasName);
        return go != null ? go.GetComponent<Canvas>() : null;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform rect = New(name, parent);
        SetAnchors(rect, Vector2.zero, Vector2.one);
        return rect;
    }

    static RectTransform Half(string name, Transform parent, bool left)
    {
        RectTransform rect = New(name, parent);
        SetAnchors(rect, left ? Vector2.zero : new Vector2(0.5f, 0f), left ? new Vector2(0.5f, 1f) : Vector2.one);
        return rect;
    }

    static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position)
    {
        RectTransform rect = New(name, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    static RectTransform New(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Image Fill(string name, Transform parent, Color color, Sprite sprite)
    {
        Image image = Block(name, parent, color, sprite);
        SetAnchors(image.rectTransform, Vector2.zero, Vector2.one);
        return image;
    }

    static Image Block(string name, Transform parent, Color color, Sprite sprite)
    {
        RectTransform rect = New(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void FullImage(string name, Transform parent, Sprite sprite)
    {
        Image image = Block(name, parent, Color.white, sprite);
        SetAnchors(image.rectTransform, Vector2.zero, Vector2.one);
    }

    static void Rule(RectTransform page, Color color, Sprite white, bool left)
    {
        Image rule = Block("Rule", page, color, white);
        if (left)
            SetAnchors(rule.rectTransform, new Vector2(0.94f, 0.06f), new Vector2(0.955f, 0.94f));
        else
            SetAnchors(rule.rectTransform, new Vector2(0.045f, 0.06f), new Vector2(0.06f, 0.94f));
    }

    static void Label(RectTransform parent, string value, int size, Color color, FontStyle style, TextAnchor anchor, TMP_FontAsset font, Vector2 min, Vector2 max)
    {
        RectTransform rect = New("Text", parent);
        SetAnchors(rect, min, max);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value ?? "";
        text.fontSize = size;
        text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : style == FontStyle.Italic ? FontStyles.Italic : style == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic : FontStyles.Normal;
        text.color = color;
        text.alignment = anchor == TextAnchor.UpperLeft ? TextAlignmentOptions.TopLeft : anchor == TextAnchor.UpperCenter ? TextAlignmentOptions.Top : anchor == TextAnchor.MiddleCenter ? TextAlignmentOptions.Center : anchor == TextAnchor.MiddleLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    static Sprite CircleSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
        if (existing != null)
            return existing;

        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f - 1f;
        var center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }
        texture.Apply();
        System.IO.File.WriteAllBytes(CirclePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(CirclePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
    }
}
