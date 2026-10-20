using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BookSceneSetup
{
    const string PageFolder = "Assets/BookPageCurl/Pages";
    const string SpriteFolder = "Assets/BookPageCurl/Sprites";
    const string SoundFolder = "Assets/BookPageCurl/Sounds";

    [MenuItem("Book/Rebuild Page Flip")]
    public static void Build()
    {
        var spreadCanvas = GameObject.Find("Canvas");
        var singleCanvas = GameObject.Find("Canvas (1)");
        if (spreadCanvas == null || singleCanvas == null)
        {
            Debug.LogError("BookSceneSetup could not find Canvas and Canvas (1).");
            return;
        }

        EnsureSpriteImport(SpriteFolder + "/shadow.png");
        EnsureSpriteImport(SpriteFolder + "/shadowLTR.png");
        Sprite white = GetOrCreateWhite();
        Sprite cover = CreateCover(PageFolder + "/Cover.png", new Color(0.32f, 0.16f, 0.18f), new Color(0.78f, 0.62f, 0.32f), "BOOK");
        Sprite backCover = CreateCover(PageFolder + "/BackCover.png", new Color(0.18f, 0.16f, 0.15f), new Color(0.55f, 0.48f, 0.36f), "END");
        Sprite pageBack = CreatePage(PageFolder + "/PageBack.png", new Color(0.90f, 0.86f, 0.78f), new Color(0.78f, 0.73f, 0.64f), "", false);

        Color[] papers =
        {
            new Color(0.97f, 0.94f, 0.88f),
            new Color(0.93f, 0.95f, 0.90f),
            new Color(0.90f, 0.93f, 0.96f),
            new Color(0.96f, 0.91f, 0.90f),
            new Color(0.94f, 0.91f, 0.96f),
            new Color(0.96f, 0.95f, 0.88f),
            new Color(0.90f, 0.95f, 0.93f),
            new Color(0.96f, 0.93f, 0.86f)
        };
        var pages = new Sprite[papers.Length];
        for (int i = 0; i < papers.Length; i++)
        {
            Color ink = new Color(0.22f, 0.18f, 0.16f);
            pages[i] = CreatePage(PageFolder + "/Page" + (i + 1) + ".png", papers[i], ink, (i + 1).ToString(), true);
        }

        TintPanel(spreadCanvas, new Color(0.20f, 0.13f, 0.12f, 1f));
        TintPanel(singleCanvas, new Color(0.14f, 0.16f, 0.18f, 1f));

        ReplaceChild(spreadCanvas.transform, "SpreadBook");
        ReplaceChild(singleCanvas.transform, "SinglePageBook");

        Sprite shadow = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/shadow.png");
        Sprite shadowLtr = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/shadowLTR.png");

        BookSwipe spreadSwipe = BuildSpreadBook(spreadCanvas.GetComponent<Canvas>(), pages, cover, white, shadow, shadowLtr);
        BookSwipe singleSwipe = BuildSinglePageBook(singleCanvas.GetComponent<Canvas>(), pages, pageBack, backCover, white, shadow, shadowLtr);
        spreadSwipe.linkedSwipe = singleSwipe;
        singleSwipe.linkedSwipe = spreadSwipe;

        DisableExtraListeners();
        if (Object.FindAnyObjectByType<SecondDisplayActivator>() == null)
        {
            var setup = new GameObject("DisplaySetup");
            setup.AddComponent<SecondDisplayActivator>();
        }

        EditorSceneManager.MarkSceneDirty(spreadCanvas.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Page flip books are linked. Display 1 has " + pages.Length + " pages and display 2 has " + (pages.Length / 2) + ".");
    }

    [MenuItem("Book/Create Split Display Scene")]
    public static void CreateSplitDisplayScene()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.isDirty)
        {
            Debug.LogError("The open scene has unsaved changes, so the split scene was not created. Save it first. BookScene was not modified.");
            return;
        }

        EnsureSpriteImport(SpriteFolder + "/shadow.png");
        EnsureSpriteImport(SpriteFolder + "/shadowLTR.png");
        Sprite white = GetOrCreateWhite();
        Sprite cover = AssetDatabase.LoadAssetAtPath<Sprite>(PageFolder + "/Cover.png");
        Sprite backCover = AssetDatabase.LoadAssetAtPath<Sprite>(PageFolder + "/BackCover.png");
        Sprite pageBack = AssetDatabase.LoadAssetAtPath<Sprite>(PageFolder + "/PageBack.png");
        var pages = new Sprite[8];
        for (int i = 0; i < pages.Length; i++)
            pages[i] = AssetDatabase.LoadAssetAtPath<Sprite>(PageFolder + "/Page" + (i + 1) + ".png");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var leftCamera = CreateDisplayCamera("LeftCamera", 0, new Vector3(-2000f, 1f, -10f), true);
        var rightCamera = CreateDisplayCamera("RightCamera", 1, new Vector3(0f, 1f, -10f), false);
        var singleCamera = CreateDisplayCamera("SingleCamera", 2, new Vector3(2000f, 1f, -10f), false);

        var leftCanvas = CreateDisplayCanvas("LeftCanvas", leftCamera, new Vector2(960f, 1080f), new Color(0.20f, 0.13f, 0.12f, 1f));
        var rightCanvas = CreateDisplayCanvas("RightCanvas", rightCamera, new Vector2(960f, 1080f), new Color(0.20f, 0.13f, 0.12f, 1f));
        var singleCanvas = CreateDisplayCanvas("SingleCanvas", singleCamera, new Vector2(260f, 463f), new Color(0.14f, 0.16f, 0.18f, 1f));

        Sprite shadow = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/shadow.png");
        Sprite shadowLtr = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/shadowLTR.png");

        BookSwipe leftSwipe = BuildHalfSpread(leftCanvas, pages, cover, white, shadow, shadowLtr, true);
        BookSwipe rightSwipe = BuildHalfSpread(rightCanvas, pages, cover, white, shadow, shadowLtr, false);
        BookSwipe singleSwipe = BuildSinglePageBook(singleCanvas, pages, pageBack, backCover, white, shadow, shadowLtr);

        leftSwipe.alsoFlip = new[] { rightSwipe, singleSwipe };
        rightSwipe.alsoFlip = new[] { leftSwipe, singleSwipe };
        singleSwipe.alsoFlip = new[] { leftSwipe, rightSwipe };

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        var inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModule != null)
            eventSystem.AddComponent(inputModule);

        new GameObject("DisplaySetup").AddComponent<ExtraDisplayActivator>();

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        const string path = "Assets/Scenes/BookSplitScene.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Created " + path + ". Display 1 is the left page, display 2 is the right page, display 3 is the single page. BookScene was not changed.");
    }

    static BookSwipe BuildHalfSpread(Canvas canvas, Sprite[] pages, Sprite cover, Sprite white, Sprite shadow, Sprite shadowLtr, bool showLeft)
    {
        const float pageWidth = 780f;
        const float pageHeight = 900f;
        var viewport = CreateRect("SpreadHalf", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(pageWidth, pageHeight), Vector2.zero, new Vector2(0.5f, 0.5f));
        viewport.gameObject.AddComponent<RectMask2D>();
        float bookX = showLeft ? pageWidth * 0.5f : -pageWidth * 0.5f;
        var bookObject = CreateRect("SpreadBook", viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(pageWidth * 2f, pageHeight), new Vector2(bookX, 0f), new Vector2(0.5f, 0.5f));
        return WireBook(bookObject, canvas, pages, cover, white, shadow, shadowLtr, 1);
    }

    static Camera CreateDisplayCamera(string name, int display, Vector3 position, bool audioListener)
    {
        var cameraObject = new GameObject(name);
        if (audioListener)
            cameraObject.tag = "MainCamera";
        cameraObject.transform.position = position;
        var camera = cameraObject.AddComponent<Camera>();
        camera.targetDisplay = display;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.09f, 0.08f, 1f);
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        var extraData = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (extraData != null)
            cameraObject.AddComponent(extraData);
        if (audioListener)
            cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    static Canvas CreateDisplayCanvas(string name, Camera camera, Vector2 size, Color panelColor)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform));
        canvasObject.layer = 5;
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 100f;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = size;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var panel = CreateRect("Panel", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        var image = panel.gameObject.AddComponent<Image>();
        image.color = panelColor;
        image.raycastTarget = false;
        return canvas;
    }

    static BookSwipe BuildSpreadBook(Canvas canvas, Sprite[] pages, Sprite cover, Sprite white, Sprite shadow, Sprite shadowLtr)
    {
        var bookObject = CreateRect("SpreadBook", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1560f, 900f), Vector2.zero, new Vector2(0.5f, 0.5f));
        return WireBook(bookObject, canvas, pages, cover, white, shadow, shadowLtr, 1);
    }

    static BookSwipe BuildSinglePageBook(Canvas canvas, Sprite[] pages, Sprite pageBack, Sprite backCover, Sprite white, Sprite shadow, Sprite shadowLtr)
    {
        var root = CreateRect("SinglePageBook", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(240f, 440f), Vector2.zero, new Vector2(0.5f, 0.5f));
        root.gameObject.AddComponent<RectMask2D>();

        var bookObject = CreateRect("Book", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480f, 440f), new Vector2(-120f, 0f), new Vector2(0.5f, 0.5f));
        int spreadCount = pages.Length / 2;
        var singlePages = new Sprite[spreadCount * 2];
        for (int i = 0; i < spreadCount; i++)
        {
            singlePages[i * 2] = pages[i * 2 + 1];
            singlePages[i * 2 + 1] = pageBack;
        }
        return WireBook(bookObject, canvas, singlePages, backCover, white, shadow, shadowLtr, 0);
    }

    static BookSwipe WireBook(RectTransform bookObject, Canvas canvas, Sprite[] pages, Sprite background, Sprite white, Sprite shadow, Sprite shadowLtr, int startPage)
    {
        var clipping = CreatePageImage("ClippingPlane", bookObject, white, true);
        var nextClip = CreatePageImage("NextPageClip", bookObject, white, true);
        var shadowImage = CreatePageImage("Shadow", bookObject, shadow, false);
        var shadowLtrImage = CreatePageImage("ShadowLTR", bookObject, shadowLtr, false);
        var left = CreatePageImage("Left", bookObject, pages.Length > 0 ? pages[0] : background, false);
        var right = CreatePageImage("Right", bookObject, pages.Length > 0 ? pages[0] : background, false);
        Sprite leftSprite = startPage > 0 && startPage <= pages.Length ? pages[startPage - 1] : background;
        Sprite rightSprite = startPage >= 0 && startPage < pages.Length ? pages[startPage] : background;
        var leftNext = CreatePageImage("LeftNext", bookObject, leftSprite, true);
        var rightNext = CreatePageImage("RightNext", bookObject, rightSprite, true);

        SetAnchors(clipping.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.35f));
        SetAnchors(nextClip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.12f));
        SetAnchors(shadowImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.2f));
        SetAnchors(shadowLtrImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.2f));
        SetAnchors(left.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        SetAnchors(right.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        SetAnchors(leftNext.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f));
        SetAnchors(rightNext.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        leftNext.rectTransform.offsetMin = Vector2.zero;
        leftNext.rectTransform.offsetMax = Vector2.zero;
        rightNext.rectTransform.offsetMin = Vector2.zero;
        rightNext.rectTransform.offsetMax = Vector2.zero;
        left.rectTransform.offsetMin = Vector2.zero;
        left.rectTransform.offsetMax = Vector2.zero;
        right.rectTransform.offsetMin = Vector2.zero;
        right.rectTransform.offsetMax = Vector2.zero;

        AddMask(clipping);
        AddMask(nextClip);
        shadowImage.gameObject.SetActive(false);
        shadowLtrImage.gameObject.SetActive(false);
        left.gameObject.SetActive(false);
        right.gameObject.SetActive(false);

        var book = bookObject.gameObject.AddComponent<Book>();
        book.canvas = canvas;
        book.background = background;
        book.bookPages = pages;
        book.interactable = true;
        book.enableShadowEffect = true;
        book.followPointer = false;
        book.currentPage = startPage;
        book.ClippingPlane = clipping;
        book.NextPageClip = nextClip;
        book.Shadow = shadowImage;
        book.ShadowLTR = shadowLtrImage;
        book.Left = left;
        book.LeftNext = leftNext;
        book.Right = right;
        book.RightNext = rightNext;

        var serialized = new SerializedObject(book);
        serialized.FindProperty("BookPanel").objectReferenceValue = bookObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var catcher = CreatePageImage("SwipeCatcher", bookObject, white, true);
        SetAnchors(catcher.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        catcher.rectTransform.offsetMin = Vector2.zero;
        catcher.rectTransform.offsetMax = Vector2.zero;
        catcher.color = new Color(0f, 0f, 0f, 0f);
        catcher.raycastTarget = true;
        var swipe = catcher.gameObject.AddComponent<BookSwipe>();
        swipe.book = book;
        swipe.firstPage = startPage;
        swipe.flipSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "/PageFlipSound.mp3");
        swipe.flipBackSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "/PageFlipBack.mp3");
        return swipe;
    }

    static void AddMask(Image image)
    {
        var mask = image.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    static Image CreatePageImage(string name, Transform parent, Sprite sprite, bool active)
    {
        var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), Vector2.zero, new Vector2(0.5f, 0.5f));
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        rect.gameObject.SetActive(active);
        return image;
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void ReplaceChild(Transform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    static void TintPanel(GameObject canvas, Color color)
    {
        var panel = canvas.transform.Find("Panel");
        if (panel == null)
            return;
        var image = panel.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    static void DisableExtraListeners()
    {
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var listener in listeners)
        {
            if (listener.gameObject.name != "Main Camera")
                listener.enabled = false;
        }
    }

    static Sprite GetOrCreateWhite()
    {
        string path = SpriteFolder + "/White.png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        EnsureSpriteImport(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureSpriteImport(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    static Sprite CreateCover(string path, Color paper, Color accent, string label)
    {
        var tex = NewTexture();
        Fill(tex, paper);
        Border(tex, 28, accent);
        Border(tex, 18, paper);
        DrawString(tex, label, tex.width / 2, tex.height / 2, 18, accent);
        return SaveSprite(path, tex);
    }

    static Sprite CreatePage(string path, Color paper, Color ink, string label, bool numbered)
    {
        var tex = NewTexture();
        Fill(tex, paper);
        Border(tex, 10, Darken(paper, 0.08f));
        if (numbered)
        {
            DrawString(tex, "PAGE", tex.width / 2, tex.height / 2 + 150, 8, ink);
            DrawString(tex, label, tex.width / 2, tex.height / 2 - 20, 22, ink);
            DrawLine(tex, 80, tex.height / 2 - 120, tex.width - 80, Darken(paper, 0.12f));
            DrawLine(tex, 80, tex.height / 2 - 150, tex.width - 140, Darken(paper, 0.08f));
            DrawLine(tex, 80, tex.height / 2 - 180, tex.width - 100, Darken(paper, 0.08f));
        }
        return SaveSprite(path, tex);
    }

    static Texture2D NewTexture()
    {
        return new Texture2D(512, 724, TextureFormat.RGBA32, false);
    }

    static Sprite SaveSprite(string path, Texture2D tex)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        EnsureSpriteImport(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Fill(Texture2D tex, Color color)
    {
        var pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
    }

    static void Border(Texture2D tex, int thickness, Color color)
    {
        int w = tex.width;
        int h = tex.height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x < thickness || y < thickness || x >= w - thickness || y >= h - thickness)
                    tex.SetPixel(x, y, color);
            }
        }
    }

    static void DrawLine(Texture2D tex, int x0, int y, int x1, Color color)
    {
        int thickness = 4;
        for (int yy = y; yy < y + thickness && yy < tex.height; yy++)
        {
            if (yy < 0)
                continue;
            for (int x = x0; x < x1 && x < tex.width; x++)
            {
                if (x >= 0)
                    tex.SetPixel(x, yy, color);
            }
        }
    }

    static Color Darken(Color color, float amount)
    {
        return new Color(color.r - amount, color.g - amount, color.b - amount, 1f);
    }

    static void DrawString(Texture2D tex, string text, int centerX, int baseline, int scale, Color color)
    {
        if (string.IsNullOrEmpty(text))
            return;
        int glyphWidth = 5 * scale;
        int gap = scale;
        int total = text.Length * glyphWidth + (text.Length - 1) * gap;
        int x = centerX - total / 2;
        int y = baseline - (7 * scale) / 2;
        foreach (char character in text)
        {
            DrawGlyph(tex, character, x, y, scale, color);
            x += glyphWidth + gap;
        }
    }

    static void DrawGlyph(Texture2D tex, char character, int originX, int originY, int scale, Color color)
    {
        string[] rows;
        if (!Glyphs.TryGetValue(character, out rows))
            return;
        for (int row = 0; row < rows.Length; row++)
        {
            int pixelY = originY + (rows.Length - 1 - row) * scale;
            for (int col = 0; col < rows[row].Length; col++)
            {
                if (rows[row][col] != '1')
                    continue;
                int pixelX = originX + col * scale;
                for (int yy = 0; yy < scale; yy++)
                {
                    for (int xx = 0; xx < scale; xx++)
                    {
                        int px = pixelX + xx;
                        int py = pixelY + yy;
                        if (px >= 0 && py >= 0 && px < tex.width && py < tex.height)
                            tex.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        { '0', new[] { "11111", "10001", "10001", "10001", "10001", "10001", "11111" } },
        { '1', new[] { "00100", "01100", "00100", "00100", "00100", "00100", "11111" } },
        { '2', new[] { "11111", "00001", "00001", "11111", "10000", "10000", "11111" } },
        { '3', new[] { "11111", "00001", "00001", "11111", "00001", "00001", "11111" } },
        { '4', new[] { "10001", "10001", "10001", "11111", "00001", "00001", "00001" } },
        { '5', new[] { "11111", "10000", "10000", "11111", "00001", "00001", "11111" } },
        { '6', new[] { "11111", "10000", "10000", "11111", "10001", "10001", "11111" } },
        { '7', new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" } },
        { '8', new[] { "11111", "10001", "10001", "11111", "10001", "10001", "11111" } },
        { '9', new[] { "11111", "10001", "10001", "11111", "00001", "00001", "11111" } },
        { 'A', new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" } },
        { 'B', new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" } },
        { 'C', new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" } },
        { 'D', new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" } },
        { 'E', new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" } },
        { 'G', new[] { "01111", "10000", "10000", "10011", "10001", "10001", "01111" } },
        { 'K', new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" } },
        { 'N', new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" } },
        { 'O', new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" } },
        { 'P', new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" } }
    };
}
