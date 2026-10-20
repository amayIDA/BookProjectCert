using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Display 1 is the spread: the left and right pages together on one canvas (SpreadCanvas, one camera).
/// Display 2 is the main page (MainCanvas, one camera). A turn lifts the right page, curls it across the spine
/// and lands it on the left, while the main page turns on its own display.
/// Each step owns both screens. Book pages are drawn from these steps, not from PNG files.
/// </summary>
[DefaultExecutionOrder(-50)]
public class BookScreens : MonoBehaviour
{
    const int PageWidth = 750;
    const int PageHeight = 860;

    public enum ButtonAction
    {
        OpenPage,
        TurnPage,
        Home
    }

    [Serializable]
    public class ButtonDef
    {
        [Tooltip("Used to remember which button is highlighted.")]
        public string id;
        [Tooltip("Words on the button. Leave this empty to show only the image.")]
        public string label;
        [Tooltip("Picture on the button. Leave this empty to keep the plain button.")]
        public Sprite image;
        public ButtonAction action = ButtonAction.OpenPage;
        [Tooltip("Page this button opens. The first page is 0.")]
        public int step;
    }

    [Serializable]
    public class Column
    {
        public string heading;
        [TextArea(2, 5)] public string body;
    }

    [Serializable]
    public class Step
    {
        public string screenTitle;
        [TextArea(1, 3)] public string prompt;
        public string selectedButton;
        public bool showDiscoverButtons = true;
        public bool showHome;
        public bool showTurn = true;
        public PageKind page = PageKind.Paper;
        public string pageHeading;
        [TextArea(3, 8)] public string pageBody;
        public Column[] columns;
        [Tooltip("Picture for the left page of the big book. Leave empty to keep the written page.")]
        public Sprite bookLeft;
        [Tooltip("Picture for the right page of the big book. On a cover page, this picture is the cover.")]
        public Sprite bookRight;
    }

    public enum PageKind
    {
        Cover,
        Endpaper,
        Open,
        Paper,
        Columns
    }

    [Header("Opening page")]
    [Tooltip("Page the book opens on when you press Play. The first page is 0.")]
    public int startingPage;
    [Tooltip("Page shown in the Scene view while you edit. Open LeftCanvas, RightCanvas, or SingleCanvas and look at PagePanels.")]
    public int editingPage;

    [Header("Display 1 buttons")]
    [Tooltip("Buttons on the left screen. Set the label, drag an image, and choose which page each one opens.")]
    public List<ButtonDef> displayOneButtons = new List<ButtonDef>();

    [Header("Display 2 buttons")]
    public ButtonDef iconButton = new ButtonDef { id = "icon", label = "Icon", action = ButtonAction.TurnPage };
    public ButtonDef homeButton = new ButtonDef { id = "home", label = "Home", action = ButtonAction.Home };
    public ButtonDef turnButton = new ButtonDef { id = "turn", label = "Turn the page", action = ButtonAction.TurnPage };

    [Header("Pages")]
    [Tooltip("One entry is one turn of the book. Edit the words here, or drag pictures into Book Left and Book Right.")]
    public List<Step> steps = new List<Step>();

    Book book;
    BookSwipe swipe;
    // one book spans both pages of the spread canvas, so it curls a page across the spine like a real book
    Book spreadBook;
    BookSwipe spreadSwipe;
    RectTransform spreadView;
    Sprite generatedBack;
    Sprite pageShadow;
    Sprite pageShadowLtr;
    AudioClip flipSound;
    AudioClip flipBackSound;
    int shownStep;
    string shownSelected = "";
    bool controlsDirty;
    bool sidesStay;
    string lastPressedId = "";
    [Tooltip("The first main page At A Glance shows. It then turns once more to the page after it.")]
    public int atAGlanceFirstPage = 2;
    [Tooltip("Page shown on the right canvas when Begin Here is pressed (the book video page). The first page is 0.")]
    public int beginHereRightPage = 2;
    [Tooltip("Main page shown when the page is turned after Begin Here. The first page is 0.")]
    public int beginHereMainPage = 4;
    const int ButtonsPage = 1;

    // the pages on the spread: left and right can differ (for example Begin Here shows a different right page)
    int leftIndex;
    int rightIndex;
    int sidesTurnTarget = -1;
    int sidesTurnRight = -1;
    string contextRow = "";
    Sprite[] leftBaked;
    Sprite[] rightBaked;
    Sprite stashedLeft;
    Sprite stashedRight;
    int stashedSpread = -1;
    Sprite[] stationaryMain;
    bool beginHereMode;
    bool scenariosShown;
    bool scenarioPending;
    [Tooltip("Index of the left and right pages (the Begin Here scenario lists) shown when the page is turned after Begin Here. The first page is 0.")]
    public int scenarioPage = 7;
    const int FirstDetailPage = 8;
    static readonly string[] ScenarioNames = { "Entry Level", "Frontline", "Functional", "Managers", "Executives" };
    int lastTargetPage = -1;
    Coroutine mainTwice;
    Vector2 pressPosition;
    Canvas pressedCanvas;
    bool swipeGesture;
    int lastSwipeFrame = -1;
    TMP_FontAsset font;
    Sprite knobSprite;
    Sprite whiteSprite;
    bool selectionPending;
    string pendingSelection;
    RectTransform[] sceneLeft = new RectTransform[0];
    RectTransform[] sceneRight = new RectTransform[0];
    RectTransform[] sceneBook = new RectTransform[0];
    readonly List<UnityEngine.Object> generated = new List<UnityEngine.Object>();

    static readonly Color Ink = new Color(0.18f, 0.14f, 0.12f, 1f);
    static readonly Color Paper = new Color(0.96f, 0.93f, 0.86f, 1f);
    static readonly Color Cover = new Color(0.33f, 0.15f, 0.17f, 1f);
    static readonly Color Gold = new Color(0.82f, 0.66f, 0.34f, 1f);
    static readonly Color Screen = new Color(0.11f, 0.12f, 0.15f, 1f);
    static readonly Color ButtonFace = new Color(0.94f, 0.91f, 0.84f, 1f);
    static readonly Color SelectedFace = new Color(0.18f, 0.42f, 0.32f, 1f);
    static readonly Color IconFace = new Color(0.73f, 0.81f, 0.90f, 1f);

    void Awake()
    {
        EnsureContent();
    }

    void OnValidate()
    {
        EnsureContent();
        if (Application.isPlaying)
            return;
        CollectScenePages();
        if (sceneLeft.Length > 0)
            ShowScenePage(Mathf.Clamp(editingPage, 0, sceneLeft.Length - 1));
    }

    public void EnsureContent()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultSteps();
        if (displayOneButtons == null)
            displayOneButtons = new List<ButtonDef>();
        if (displayOneButtons.Count == 0)
        {
            displayOneButtons.Add(new ButtonDef { id = "subject", label = "Subject", action = ButtonAction.OpenPage, step = 1 });
            displayOneButtons.Add(new ButtonDef { id = "overview", label = "Overview", action = ButtonAction.OpenPage, step = 2 });
            displayOneButtons.Add(new ButtonDef { id = "description", label = "Description", action = ButtonAction.OpenPage, step = 3 });
            displayOneButtons.Add(new ButtonDef { id = "look", label = "Look inside", action = ButtonAction.OpenPage, step = 2 });
        }

        EnsureButton(ref iconButton, "icon", "Icon", ButtonAction.TurnPage, 0);
        EnsureButton(ref homeButton, "home", "Home", ButtonAction.Home, 0);
        EnsureButton(ref turnButton, "turn", "Turn the page", ButtonAction.TurnPage, 0);
    }

    static void EnsureButton(ref ButtonDef button, string id, string label, ButtonAction action, int step)
    {
        if (button == null)
            button = new ButtonDef();
        if (!string.IsNullOrEmpty(button.id) || !string.IsNullOrEmpty(button.label))
            return;
        button.id = id;
        button.label = label;
        button.action = action;
        button.step = step;
    }

    IEnumerator Start()
    {
        font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        knobSprite = CircleSprite(128);
        whiteSprite = SolidSprite(Color.white);
        PrepareCanvases();
        CollectScenePages();
        Sprite[] pages = RenderPages();
        BuildBook(pages);
        BuildControlBooks();
        if (swipe != null && spreadSwipe != null)
            swipe.alsoFlip = new[] { spreadSwipe };
        if (swipe != null)
            swipe.interceptSwipe = SwipeIntercepted;
        if (spreadSwipe != null)
            spreadSwipe.interceptSwipe = SwipeIntercepted;
        int opening = Mathf.Clamp(startingPage, 0, PageCount() - 1);
        ShowControls(opening, Selection(opening));
        yield return null;
        Canvas.ForceUpdateCanvases();
        AlignCurl();
        SnapAll(opening * 2 + 1);
        SetControlBooksActive(false);
        SetMainBookActive(false);
    }

    void Update()
    {
        TrackSwipe();
        if (scenarioPending)
        {
            // the spread is turning (and the main page too when Begin Here was followed by a turn): wait for both
            if ((spreadSwipe != null && spreadSwipe.IsFlipping) || (swipe != null && swipe.IsFlipping))
                return;
            FinishSidesTurn();
            return;
        }

        if (IsAnyFlipping())
        {
            SetScreenUiActive(false);
            if (!sidesStay)
                SetControlBooksActive(true);
            return;
        }

        SetMainBookActive(swipe != null && swipe.IsFlipping);
        if (!controlsDirty)
            return;
        ShowControls(shownStep, shownSelected);
        controlsDirty = false;
        SetControlBooksActive(false);
    }

    void OnDestroy()
    {
        for (int i = 0; i < generated.Count; i++)
        {
            if (generated[i] != null)
                Destroy(generated[i]);
        }
    }

    public void ShowStep(int stepIndex, string selected, bool animateIfAdjacent)
    {
        if (book == null || swipe == null || swipe.IsFlipping)
            return;
        stepIndex = Mathf.Clamp(stepIndex, 0, PageCount() - 1);
        int targetPage = stepIndex * 2 + 1;
        lastTargetPage = targetPage;
        int delta = targetPage - book.currentPage;
        shownStep = stepIndex;
        shownSelected = selected;
        if (animateIfAdjacent && Mathf.Abs(delta) == 2)
        {
            selectionPending = true;
            pendingSelection = selected;
            AlignCurl();
            SetScreenUiActive(false);
            if (!sidesStay)
            {
                swipe.alsoFlip = spreadSwipe != null ? new[] { spreadSwipe } : null;
                SetControlBooksActive(true);
            }
            else
            {
                swipe.alsoFlip = null;
                SetControlBooksActive(false);
            }
            SetMainBookActive(true);
            swipe.BeginFlip(delta > 0);
            return;
        }

        selectionPending = false;
        SnapAll(targetPage);
        ShowControls(stepIndex, selected);
        controlsDirty = false;
        SetControlBooksActive(false);
    }

    void PrepareCanvases()
    {
        Canvas spread = SpreadScreen();
        Canvas single = MainScreen();
        Fit(spread, ReferenceSize(spread), true);
        Fit(single, new Vector2(1080f, 1920f), true);
        FitBookSpreads(single);
        HideOldBook(LeftHalf());
        HideOldBook(RightHalf());
        HideOldBook(single);
    }

    static Canvas FindCanvas(string canvasName)
    {
        GameObject go = GameObject.Find(canvasName);
        return go != null ? go.GetComponent<Canvas>() : null;
    }

    /// <summary>The one canvas that holds both pages (display 1).</summary>
    static Canvas SpreadScreen()
    {
        Canvas canvas = FindCanvas("SpreadCanvas");
        if (canvas == null)
            canvas = FindCanvas("LeftCanvas");
        return canvas;
    }

    /// <summary>The left page area of the spread. It keeps the old name LeftCanvas so every page and script that uses it still works.</summary>
    static Transform LeftHalf()
    {
        GameObject go = GameObject.Find("LeftCanvas");
        return go != null ? go.transform : null;
    }

    static Transform RightHalf()
    {
        GameObject go = GameObject.Find("RightCanvas");
        return go != null ? go.transform : null;
    }

    /// <summary>The main page canvas (display 2).</summary>
    static Canvas MainScreen()
    {
        Canvas canvas = FindCanvas("MainCanvas");
        if (canvas == null)
            canvas = DisplayScreen(1);
        return canvas;
    }

    static Canvas DisplayScreen(int display)
    {
        Canvas named = FindCanvas("SingleCanvas");
        if (named != null && (named.worldCamera == null || named.worldCamera.targetDisplay == display))
            return named;

        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Camera camera = canvases[i].worldCamera;
            if (camera != null && camera.targetDisplay == display)
                return canvases[i];
        }

        return named;
    }

    static Vector2 ReferenceSize(Component canvas)
    {
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.referenceResolution.x > 1f && scaler.referenceResolution.y > 1f)
                return scaler.referenceResolution;
            RectTransform rect = canvas.transform as RectTransform;
            if (rect != null && rect.rect.width > 2f && rect.rect.height > 2f)
                return rect.rect.size;
        }

        return new Vector2(1024f, 1820f);
    }

    static void Fit(Canvas canvas, Vector2 resolution, bool keepAspect)
    {
        if (canvas == null)
            return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = resolution;
        if (keepAspect)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        else
            scaler.matchWidthOrHeight = 0.5f;
    }

    /// <summary>With a looping video behind the pages, the baked page faces are see-through so the video keeps playing while a page turns.</summary>
    // where the white page sits inside each half of the spread artwork (fractions, from the bottom left)
    static readonly Rect LeftPaper = new Rect(0.086f, 0.130f, 0.866f, 0.762f);
    static readonly Rect RightPaper = new Rect(0.049f, 0.130f, 0.866f, 0.762f);

    static bool HasBackVideo(Component canvas)
    {
        return canvas != null && canvas.transform.Find("BackVideo") != null;
    }

    static Color PanelColor(Component canvas)
    {
        if (canvas != null)
        {
            Transform panel = canvas.transform.Find("Panel");
            if (panel != null)
            {
                Image image = panel.GetComponent<Image>();
                if (image != null)
                    return image.color;
            }
        }

        return Screen;
    }

    static void FitBookSpreads(Canvas canvas)
    {
        if (canvas == null)
            return;
        Transform panels = canvas.transform.Find("PagePanels");
        if (panels == null)
            return;
        for (int i = 0; i < panels.childCount; i++)
        {
            RectTransform spread = panels.GetChild(i).Find("Spread") as RectTransform;
            if (spread == null)
                continue;
            AspectRatioFitter fitter = spread.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = spread.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1500f / 860f;
        }
    }

    static void HideOldBook(Component canvas)
    {
        if (canvas == null)
            return;
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name != "Panel" && child.name != "PagePanels" && child.name != "BackVideo")
                child.gameObject.SetActive(false);
        }
    }

    int PageCount()
    {
        // the main pages; the spread has more pages (the scenario details) than the main canvas
        if (sceneBook.Length > 0)
            return sceneBook.Length;
        if (sceneLeft.Length > 0)
            return sceneLeft.Length;
        return steps.Count;
    }

    string Selection(int index)
    {
        if (index < 0 || steps == null || index >= steps.Count || steps[index] == null)
            return "";
        return steps[index].selectedButton;
    }

    public void PressButton(ButtonAction action, int step, string id)
    {
        Press(action, step, id);
    }

    void CollectScenePages()
    {
        sceneLeft = PageList(LeftHalf());
        sceneRight = PageList(RightHalf());
        sceneBook = PageList(MainScreen());
    }

    static RectTransform[] PageList(Component canvas)
    {
        if (canvas == null)
            return new RectTransform[0];
        Transform root = canvas.transform.Find("PagePanels");
        if (root == null)
            return new RectTransform[0];
        var pages = new List<RectTransform>();
        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform page = root.GetChild(i) as RectTransform;
            if (page != null)
                pages.Add(page);
        }
        return pages.ToArray();
    }

    void ShowScenePage(int index)
    {
        if (!sidesStay)
        {
            ActivateOnly(sceneLeft, index);
            ActivateOnly(sceneRight, index);
            leftIndex = index;
            rightIndex = index;
        }

        ActivateOnly(sceneBook, index);
        RaisePagePanels();
    }

    /// <summary>Shows these pages on the spread right now and keeps the page curl book on the same spread.</summary>
    void SetSides(int left, int right)
    {
        int count = Mathf.Max(1, sceneLeft.Length);
        left = Mathf.Clamp(left, 0, count - 1);
        right = Mathf.Clamp(right, 0, count - 1);
        bool rightChanged = right != rightIndex;
        leftIndex = left;
        rightIndex = right;
        ActivateOnly(sceneLeft, left);
        ActivateOnly(sceneRight, right);
        if (spreadBook != null)
            spreadBook.SnapToPage(left * 2 + 1);
        RaisePagePanels();
        if (rightChanged && right >= 0 && right < sceneRight.Length && sceneRight[right] != null)
            StartCoroutine(FadePageIn(sceneRight[right]));
    }

    /// <summary>A soft fade for a page that appears by itself, so it never just pops in.</summary>
    IEnumerator FadePageIn(RectTransform page)
    {
        CanvasGroup group = page.GetComponent<CanvasGroup>();
        if (group == null)
            group = page.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        float t = 0f;
        const float duration = 0.4f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            if (group == null)
                yield break;
            group.alpha = Mathf.SmoothStep(0f, 1f, t / duration);
            yield return null;
        }

        if (group != null)
            group.alpha = 1f;
    }

    static void ActivateOnly(RectTransform[] pages, int index)
    {
        if (pages == null)
            return;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].gameObject.SetActive(i == index);
        }
    }

    void RaisePagePanels()
    {
        Raise(LeftHalf());
        Raise(RightHalf());
        Raise(MainScreen());
    }

    static void Raise(Component canvas)
    {
        if (canvas == null)
            return;
        Transform pages = canvas.transform.Find("PagePanels");
        if (pages != null)
            pages.SetAsLastSibling();
    }

    bool HasScenePages()
    {
        return sceneLeft.Length > 0;
    }

    Sprite[] RenderPages()
    {
        if (sceneBook.Length > 0)
            return RenderSceneBook();

        var cameraObject = new GameObject("PageBakerCamera");
        cameraObject.transform.position = new Vector3(0f, 4000f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = PageHeight * 0.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 50f;
        camera.cullingMask = 1 << 5;

        var canvasObject = new GameObject("PageBaker", typeof(RectTransform));
        canvasObject.layer = 5;
        canvasObject.transform.position = new Vector3(0f, 4000f, 0f);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(PageWidth, PageHeight);
        canvasRect.localScale = Vector3.one;

        var pages = new List<Sprite>();
        Sprite back = Bake(camera, canvasRect, new Color(0.14f, 0.1f, 0.09f, 1f), root => { });
        pages.Add(Bake(camera, canvasRect, Cover, DrawCover));
        for (int i = 0; i < steps.Count; i++)
        {
            Step step = steps[i];
            if (step.page == PageKind.Cover)
                continue;
            pages.Add(Bake(camera, canvasRect, Paper, root => DrawStep(root, step, true)));
            pages.Add(Bake(camera, canvasRect, Paper, root => DrawStep(root, step, false)));
        }
        if (pages.Count > 0)
            pages[0].name = "cover";
        back.name = "back";
        generatedBack = back;

        Destroy(cameraObject);
        Destroy(canvasObject);
        return pages.ToArray();
    }

    Sprite Bake(Camera camera, RectTransform root, Color background, Action<RectTransform> draw)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);

        int width = Mathf.RoundToInt(root.sizeDelta.x);
        int height = Mathf.RoundToInt(root.sizeDelta.y);
        camera.orthographicSize = height * 0.5f;
        Image paper = NewImage("Paper", root, background);
        Stretch(paper.rectTransform);
        draw(root);
        Canvas.ForceUpdateCanvases();

        var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        camera.backgroundColor = background;
        camera.targetTexture = texture;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        camera.targetTexture = null;

        Sprite sprite = Sprite.Create(image, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        generated.Add(texture);
        generated.Add(image);
        generated.Add(sprite);
        return sprite;
    }

    void DrawCover(RectTransform root)
    {
        Step cover = null;
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].page == PageKind.Cover)
            {
                cover = steps[i];
                break;
            }
        }

        if (cover != null && cover.bookRight != null)
        {
            DrawFullImage(root, cover.bookRight);
            return;
        }

        Image band = NewImage("Band", root, Gold);
        SetRect(band.rectTransform, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.48f));
        AddText(root, "Languages\nand Communication\nAcademy", 64, Gold, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.9f));
        AddText(root, "Turn the page", 28, new Color(0.93f, 0.86f, 0.74f, 1f), FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.32f));
    }

    void DrawFullImage(RectTransform root, Sprite sprite)
    {
        Image picture = NewImage("Picture", root, Color.white);
        picture.sprite = sprite;
        picture.type = Image.Type.Simple;
        picture.preserveAspect = false;
        Stretch(picture.rectTransform);
    }

    void DrawStep(RectTransform root, Step step, bool left)
    {
        Sprite picture = left ? step.bookLeft : step.bookRight;
        if (picture != null)
        {
            DrawFullImage(root, picture);
            return;
        }

        Image rule = NewImage("Rule", root, Gold);
        if (left)
            SetRect(rule.rectTransform, new Vector2(0.94f, 0.06f), new Vector2(0.955f, 0.94f));
        else
            SetRect(rule.rectTransform, new Vector2(0.045f, 0.06f), new Vector2(0.06f, 0.94f));

        if (step.page == PageKind.Endpaper)
            return;

        if (step.page == PageKind.Open)
        {
            if (!left)
                AddText(root, "The book is open.\n\nTurn the page to read it.", 42, Ink, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.12f, 0.2f), new Vector2(0.88f, 0.8f));
            return;
        }

        if (step.page == PageKind.Columns)
        {
            if (left)
            {
                AddText(root, step.pageHeading, 36, Ink, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(0.1f, 0.62f), new Vector2(0.88f, 0.9f));
                AddText(root, step.pageBody, 26, Ink, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0.1f, 0.1f), new Vector2(0.88f, 0.58f));
            }
            else
                DrawColumns(root, step.columns);
            return;
        }

        if (left)
            AddText(root, step.pageHeading, 40, Ink, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(0.1f, 0.12f), new Vector2(0.88f, 0.88f));
        else
            AddText(root, step.pageBody, 28, Ink, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0.12f, 0.1f), new Vector2(0.9f, 0.9f));
    }

    void DrawColumns(RectTransform root, Column[] columns)
    {
        if (columns == null)
            return;
        int count = Mathf.Min(3, columns.Length);
        for (int i = 0; i < count; i++)
        {
            float x0 = 0.08f + i * 0.3f;
            float x1 = x0 + 0.27f;
            AddText(root, columns[i].heading, 26, Ink, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(x0, 0.72f), new Vector2(x1, 0.9f));
            AddText(root, columns[i].body, 22, Ink, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(x0, 0.08f), new Vector2(x1, 0.7f));
        }
    }

    void BuildBook(Sprite[] pages)
    {
        Canvas canvas = MainScreen();
        if (canvas == null || pages == null || pages.Length == 0)
            return;

        Book donor = canvas.GetComponentInChildren<Book>(true);
        BookSwipe donorSwipe = canvas.GetComponentInChildren<BookSwipe>(true);
        Sprite shadow = donor != null && donor.Shadow != null ? donor.Shadow.sprite : null;
        Sprite shadowLtr = donor != null && donor.ShadowLTR != null ? donor.ShadowLTR.sprite : null;
        whiteSprite = donor != null && donor.ClippingPlane != null ? donor.ClippingPlane.sprite : pages[0];
        AudioClip flip = donorSwipe != null ? donorSwipe.flipSound : null;
        AudioClip flipBack = donorSwipe != null ? donorSwipe.flipBackSound : null;

        pageShadow = shadow;
        pageShadowLtr = shadowLtr;
        flipSound = flip;
        flipBackSound = flipBack;
        RectTransform bookRect = CreateRect("ExperienceBook", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1500f, 860f), Vector2.zero);
        swipe = WireBook(bookRect, canvas, pages, generatedBack != null ? generatedBack : pages[0], true);
        book = swipe != null ? swipe.book : null;
        if (book != null)
            book.stationaryPages = stationaryMain;
    }

    BookSwipe WireBook(RectTransform bookRect, Canvas canvas, Sprite[] pages, Sprite background, bool catcherRaycast, int openingPage = 0)
    {
        if (bookRect == null || pages == null || pages.Length == 0)
            return null;

        Image clipping = PageImage("ClippingPlane", bookRect, whiteSprite, true);
        Image nextClip = PageImage("NextPageClip", bookRect, whiteSprite, true);
        Image shadowImage = PageImage("Shadow", bookRect, pageShadow, false);
        Image shadowLtrImage = PageImage("ShadowLTR", bookRect, pageShadowLtr, false);
        Image left = PageImage("Left", bookRect, pages[0], false);
        Image right = PageImage("Right", bookRect, pages[0], false);
        Image leftNext = PageImage("LeftNext", bookRect, pages[0], true);
        Image rightNext = PageImage("RightNext", bookRect, pages.Length > 1 ? pages[1] : pages[0], true);

        PlacePage(clipping.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.35f));
        PlacePage(nextClip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.12f));
        PlacePage(shadowImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.2f));
        PlacePage(shadowLtrImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.2f));
        PlacePage(left.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        PlacePage(right.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        PlacePage(leftNext.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f));
        PlacePage(rightNext.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        ZeroOffsets(left.rectTransform);
        ZeroOffsets(right.rectTransform);
        ZeroOffsets(leftNext.rectTransform);
        ZeroOffsets(rightNext.rectTransform);

        clipping.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        nextClip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        shadowImage.gameObject.SetActive(false);
        shadowLtrImage.gameObject.SetActive(false);
        left.gameObject.SetActive(false);
        right.gameObject.SetActive(false);

        Book turned = bookRect.gameObject.AddComponent<Book>();
        turned.canvas = canvas;
        turned.background = background;
        turned.bookPages = pages;
        turned.interactable = true;
        turned.enableShadowEffect = true;
        turned.followPointer = false;
        turned.currentPage = openingPage;
        turned.ClippingPlane = clipping;
        turned.NextPageClip = nextClip;
        turned.Shadow = shadowImage;
        turned.ShadowLTR = shadowLtrImage;
        turned.Left = left;
        turned.LeftNext = leftNext;
        turned.Right = right;
        turned.RightNext = rightNext;
        turned.BindPanel(bookRect);
        if (turned.OnFlip == null)
            turned.OnFlip = new UnityEngine.Events.UnityEvent();
        if (catcherRaycast)
            turned.OnFlip.AddListener(OnBookFlipped);

        Image catcher = PageImage("SwipeCatcher", bookRect, whiteSprite, true);
        PlacePage(catcher.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        ZeroOffsets(catcher.rectTransform);
        catcher.color = new Color(0f, 0f, 0f, 0f);
        catcher.raycastTarget = catcherRaycast;
        BookSwipe turnedSwipe = catcher.gameObject.AddComponent<BookSwipe>();
        turnedSwipe.book = turned;
        turnedSwipe.firstPage = openingPage;
        turnedSwipe.flipSound = flipSound;
        turnedSwipe.flipBackSound = flipBackSound;
        return turnedSwipe;
    }

    void BuildControlBooks()
    {
        Canvas.ForceUpdateCanvases();
        Canvas spread = SpreadScreen();
        Transform left = LeftHalf();
        Transform right = RightHalf();
        if (spread == null || left == null || right == null)
            return;

        Sprite[] leftPages = BakeControlPages(true);
        Sprite[] rightPages = BakeControlPages(false);
        leftBaked = leftPages;
        rightBaked = rightPages;
        Sprite[] spreadPages = SpreadPages(leftPages, rightPages);
        spreadView = CreateSpreadView(left.parent);
        spreadSwipe = WireBook(spreadView.GetChild(0) as RectTransform, spread, spreadPages, PanelPaper(left, HasBackVideo(left)), false, 1);
        spreadBook = spreadSwipe != null ? spreadSwipe.book : null;
        if (spreadSwipe != null)
        {
            // a longer, softer turn than the single page books used to have
            spreadSwipe.flipDuration = 0.9f;
        }

        SendControlBooksBack();
    }

    Sprite[] RenderSceneBook()
    {
        Canvas screen = MainScreen();
        Vector2 page = PanelContentSize(screen);
        int width = Mathf.Max(8, Mathf.RoundToInt(page.x));
        int height = Mathf.Max(8, Mathf.RoundToInt(page.y));
        Sprite panel = PanelPaper(screen);
        generatedBack = panel;
        var pages = new List<Sprite>();
        bool live = HasBackVideo(screen);
        Sprite empty = live ? Sprite.Create(new Texture2D(2, 2, TextureFormat.RGBA32, false), new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f) : null;
        var still = new List<Sprite>();
        for (int i = 0; i < sceneBook.Length; i++)
        {
            pages.Add(panel);
            pages.Add(BakePanel(sceneBook[i], width, height, screen, 4000f + i * 40f));
            if (live)
            {
                // what lies under a turning sheet is only the page content, so the video keeps playing there
                still.Add(empty);
                still.Add(BakePanel(sceneBook[i], width, height, screen, 4000f + i * 40f + 20f, true));
            }
        }

        stationaryMain = live ? still.ToArray() : null;
        return pages.ToArray();
    }

    static Transform FindNamed(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), childName);
            if (found != null)
                return found;
        }
        return null;
    }

    Sprite[] BakeControlPages(bool displayOne)
    {
        RectTransform[] source = displayOne ? sceneLeft : sceneRight;
        if (source.Length > 0)
        {
            Transform screen = displayOne ? LeftHalf() : RightHalf();
            Vector2 page = PanelContentSize(screen);
            int width = Mathf.RoundToInt(page.x);
            int height = Mathf.RoundToInt(page.y);
            var baked = new Sprite[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                // with the video behind, only the sheet of paper (the white page) turns; the book around it stays and keeps playing
                Rect? paper = HasBackVideo(screen) ? (displayOne ? LeftPaper : RightPaper) : (Rect?)null;
                baked[i] = BakePanel(source[i], width, height, screen, displayOne ? 6000f : 6500f, false, paper);
            }
            return baked;
        }

        var cameraObject = new GameObject("ControlBakerCamera");
        cameraObject.transform.position = new Vector3(0f, 6000f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 50f;
        camera.cullingMask = 1 << 5;

        var canvasObject = new GameObject("ControlBaker", typeof(RectTransform));
        canvasObject.layer = 5;
        canvasObject.transform.position = new Vector3(0f, 6000f, 0f);
        Canvas baker = canvasObject.AddComponent<Canvas>();
        baker.renderMode = RenderMode.WorldSpace;
        baker.worldCamera = camera;
        RectTransform rect = baker.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1080f, 1080f);
        rect.localScale = Vector3.one;

        var faces = new Sprite[steps.Count];
        for (int i = 0; i < steps.Count; i++)
        {
            Step step = steps[i];
            string selected = step.selectedButton;
            faces[i] = Bake(camera, rect, Screen, root =>
            {
                if (displayOne)
                    BuildDisplayOne(root, step, selected);
                else
                    BuildDisplayTwo(root, step);
            });
        }

        Destroy(cameraObject);
        Destroy(canvasObject);
        return faces;
    }

    Sprite BakeScreenBack()
    {
        var cameraObject = new GameObject("BackBakerCamera");
        cameraObject.transform.position = new Vector3(0f, 7000f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 50f;
        camera.cullingMask = 1 << 5;
        var canvasObject = new GameObject("BackBaker", typeof(RectTransform));
        canvasObject.layer = 5;
        canvasObject.transform.position = new Vector3(0f, 7000f, 0f);
        Canvas baker = canvasObject.AddComponent<Canvas>();
        baker.renderMode = RenderMode.WorldSpace;
        baker.worldCamera = camera;
        RectTransform rect = baker.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1080f, 1080f);
        rect.localScale = Vector3.one;
        Sprite sprite = Bake(camera, rect, Screen, root => { });
        Destroy(cameraObject);
        Destroy(canvasObject);
        return sprite;
    }

    static Sprite[] SpreadPages(Sprite[] leftFaces, Sprite[] rightFaces)
    {
        int count = Mathf.Min(leftFaces.Length, rightFaces.Length);
        var pages = new Sprite[count * 2];
        for (int i = 0; i < count; i++)
        {
            pages[i * 2] = leftFaces[i];
            pages[i * 2 + 1] = rightFaces[i];
        }
        return pages;
    }

    /// <summary>The page curl for the spread: one book as wide as both pages, clipped to the spread area.</summary>
    RectTransform CreateSpreadView(Transform area)
    {
        RectTransform view = CreateRect("ControlBook", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ZeroOffsets(view);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform areaRect = area as RectTransform;
        Vector2 size = areaRect != null && areaRect.rect.width > 2f ? areaRect.rect.size : new Vector2(2048f, 1820f);
        CreateRect("Book", view, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
        return view;
    }

    static Vector2 PanelContentSize(Component canvas)
    {
        if (canvas != null)
        {
            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (rect != null && rect.rect.width > 2f && rect.rect.height > 2f)
                return rect.rect.size;
        }

        return ReferenceSize(canvas);
    }

    void AlignCurl()
    {
        Canvas.ForceUpdateCanvases();
        AlignSpread();
        AlignExperience();
    }

    void AlignSpread()
    {
        if (spreadView == null)
            return;
        RectTransform area = spreadView.parent as RectTransform;
        if (area == null)
            return;
        spreadView.anchorMin = Vector2.zero;
        spreadView.anchorMax = Vector2.one;
        spreadView.pivot = new Vector2(0.5f, 0.5f);
        spreadView.offsetMin = Vector2.zero;
        spreadView.offsetMax = Vector2.zero;
        spreadView.localScale = Vector3.one;
        spreadView.localRotation = Quaternion.identity;
        Canvas.ForceUpdateCanvases();
        RectTransform bookRect = spreadView.childCount > 0 ? spreadView.GetChild(0) as RectTransform : null;
        if (bookRect == null)
            return;
        bookRect.anchorMin = new Vector2(0.5f, 0.5f);
        bookRect.anchorMax = new Vector2(0.5f, 0.5f);
        bookRect.pivot = new Vector2(0.5f, 0.5f);
        bookRect.sizeDelta = new Vector2(spreadView.rect.width, spreadView.rect.height);
        bookRect.anchoredPosition = Vector2.zero;
        Book curled = bookRect.GetComponent<Book>();
        if (curled != null)
            curled.Reflow();
    }

    void AlignExperience()
    {
        if (book == null)
            return;
        Canvas canvas = MainScreen();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
            return;
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;
        if (width < 2f || height < 2f)
            return;
        RectTransform bookRect = book.transform as RectTransform;
        RectTransform mask = bookRect.parent as RectTransform;
        if (mask == null || mask.name != "MainPage")
        {
            mask = CreateRect("MainPage", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ZeroOffsets(mask);
            mask.gameObject.AddComponent<RectMask2D>();
            bookRect.SetParent(mask, false);
        }
        bookRect.anchorMin = new Vector2(0.5f, 0.5f);
        bookRect.anchorMax = new Vector2(0.5f, 0.5f);
        bookRect.pivot = new Vector2(0.5f, 0.5f);
        bookRect.sizeDelta = new Vector2(width * 2f, height);
        bookRect.anchoredPosition = new Vector2(-width * 0.5f, 0f);
        book.Reflow();
    }

    static void CopyRect(RectTransform view, RectTransform target)
    {
        view.anchorMin = target.anchorMin;
        view.anchorMax = target.anchorMax;
        view.pivot = target.pivot;
        view.offsetMin = target.offsetMin;
        view.offsetMax = target.offsetMax;
        view.anchoredPosition = target.anchoredPosition;
        view.sizeDelta = target.sizeDelta;
        view.localScale = target.localScale;
        view.localRotation = target.localRotation;
    }

    static void PlaceOver(RectTransform item, RectTransform target)
    {
        RectTransform parent = item.parent as RectTransform;
        Canvas canvas = item.GetComponentInParent<Canvas>();
        Camera camera = canvas != null ? canvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 bottomLeft;
        Vector2 topRight;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(camera, corners[0]), camera, out bottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(camera, corners[2]), camera, out topRight);
        item.anchorMin = new Vector2(0.5f, 0.5f);
        item.anchorMax = new Vector2(0.5f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.sizeDelta = topRight - bottomLeft;
        item.anchoredPosition = (bottomLeft + topRight) * 0.5f;
    }

    void OnBookFlipped()
    {
        shownStep = Mathf.Clamp((book.currentPage - 1) / 2, 0, PageCount() - 1);
        shownSelected = selectionPending ? pendingSelection : Selection(shownStep);
        selectionPending = false;
        controlsDirty = true;
    }

    void TrackSwipe()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || IsAnyFlipping())
            return;

        Vector2 position = pointer.position.ReadValue();
        if (pointer.press.wasPressedThisFrame)
        {
            swipeGesture = false;
            pressPosition = position;
            pressedCanvas = CanvasAt(position);
        }

        if (pointer.press.isPressed && pressedCanvas != null)
        {
            float dx = position.x - pressPosition.x;
            float dy = position.y - pressPosition.y;
            if (Mathf.Abs(dx) > 80f && Mathf.Abs(dx) > Mathf.Abs(dy))
                swipeGesture = true;
        }

        if (pointer.press.wasReleasedThisFrame && swipeGesture && pressedCanvas != null)
        {
            bool next = position.x < pressPosition.x;
            pressedCanvas = null;
            TurnBySwipe(next);
        }
    }

    bool SwipeIntercepted(bool next)
    {
        if (contextRow.Length == 0 && !(beginHereMode && !scenariosShown))
            return false;
        TurnBySwipe(next);
        return true;
    }

    void TurnBySwipe(bool next)
    {
        if (book == null)
            return;
        if (Time.frameCount == lastSwipeFrame)
            return;
        lastSwipeFrame = Time.frameCount;
        swipeGesture = false;
        lastPressedId = "";
        CancelPending();
        if (beginHereMode && !scenariosShown && next)
        {
            Press(ButtonAction.TurnPage, 0, "");
            return;
        }

        if (contextRow.Length > 0)
        {
            // after a row button the spread turns by itself and the main page stays
            if (contextRow != "Begin Here")
                return;
            if (next)
            {
                if (leftIndex >= FirstDetailPage && leftIndex + 1 < sceneLeft.Length)
                    TurnSidesTo(leftIndex + 1);
            }
            else if (leftIndex >= FirstDetailPage)
            {
                // back from a scenario to the lists
                TurnSidesBack(scenarioPage, scenarioPage);
            }
            else if (leftIndex > ButtonsPage)
            {
                // back from the lists to the Begin Here pages
                scenariosShown = false;
                TurnSidesBack(ButtonsPage, beginHereRightPage);
            }
            return;
        }

        int step = (book.currentPage - 1) / 2 + (next ? 1 : -1);
        if (step < 0 || step >= PageCount())
            return;
        ShowStep(step, Selection(step), true);
    }

    Canvas CanvasAt(Vector2 screen)
    {
        Canvas spread = SpreadScreen();
        return OverCanvas(spread, screen) ? spread : null;
    }

    static bool OverCanvas(Canvas canvas, Vector2 screen)
    {
        if (canvas == null)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(canvas.transform as RectTransform, screen, canvas.worldCamera);
    }

    bool IsAnyFlipping()
    {
        if (swipe != null && swipe.IsFlipping)
            return true;
        if (spreadSwipe != null && spreadSwipe.IsFlipping)
            return true;
        return false;
    }

    void SnapAll(int page)
    {
        if (book != null)
            book.SnapToPage(page);
        if (sidesStay)
            return;
        int step = Mathf.Max(0, (page - 1) / 2);
        int sidePage = step * 2 + 1;
        if (spreadBook != null)
            spreadBook.SnapToPage(sidePage);
    }

    void SetScreenUiActive(bool active)
    {
        if (HasScenePages())
        {
            if (active)
                ShowScenePage(shownStep);
            else if (sidesStay)
                ActivateOnly(sceneBook, -1);
            else
            {
                ActivateOnly(sceneLeft, -1);
                ActivateOnly(sceneRight, -1);
                ActivateOnly(sceneBook, -1);
            }
            return;
        }

        SetUiActive(LeftHalf(), active);
        SetUiActive(RightHalf(), active);
    }

    static void SetUiActive(Component canvas, bool active)
    {
        if (canvas == null)
            return;
        Transform ui = canvas.transform.Find("ScreenUi");
        if (ui != null)
            ui.gameObject.SetActive(active);
    }

    void SetMainBookActive(bool active)
    {
        if (book == null)
            return;
        Transform root = book.transform;
        while (root != null && root.name != "ExperienceBook")
            root = root.parent;
        if (root != null && root.gameObject.activeSelf != active)
            root.gameObject.SetActive(active);
    }

    void SetControlBooksActive(bool active)
    {
        if (spreadView != null)
            spreadView.gameObject.SetActive(active);
        if (active)
            BringControlBooksToFront();
    }

    void BringControlBooksToFront()
    {
        if (spreadView != null)
            spreadView.SetAsLastSibling();
    }

    void SendControlBooksBack()
    {
        if (spreadView != null)
            spreadView.SetSiblingIndex(1);
    }

    void ShowControls(int stepIndex, string selected)
    {
        if (HasScenePages())
        {
            ShowScenePage(stepIndex);
            return;
        }

        Step step = steps[Mathf.Clamp(stepIndex, 0, steps.Count - 1)];
        Transform left = LeftHalf();
        Transform right = RightHalf();
        if (left != null)
            BuildDisplayOne(left, step, selected);
        if (right != null)
            BuildDisplayTwo(right, step);
    }

    void BuildDisplayOne(Transform canvas, Step step, string selected)
    {
        RectTransform root = FreshRoot("ScreenUi", canvas);
        AddText(root, step.screenTitle, step.showDiscoverButtons ? 48 : 64, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f));
        if (!step.showDiscoverButtons || displayOneButtons == null)
            return;

        for (int i = 0; i < displayOneButtons.Count; i++)
        {
            Vector2 min;
            Vector2 max;
            ButtonSlot(i, displayOneButtons.Count, out min, out max);
            AddControl(root, displayOneButtons[i], min, max, selected == displayOneButtons[i].id, false);
        }
    }

    static void ButtonSlot(int index, int count, out Vector2 min, out Vector2 max)
    {
        if (count == 4)
        {
            Vector2[] mins =
            {
                new Vector2(0.08f, 0.40f),
                new Vector2(0.52f, 0.40f),
                new Vector2(0.08f, 0.16f),
                new Vector2(0.52f, 0.16f)
            };
            Vector2[] maxs =
            {
                new Vector2(0.48f, 0.62f),
                new Vector2(0.92f, 0.62f),
                new Vector2(0.48f, 0.36f),
                new Vector2(0.92f, 0.36f)
            };
            min = mins[index];
            max = maxs[index];
            return;
        }

        int columns = 2;
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / 2f));
        int column = index % columns;
        int row = index / columns;
        float left = 0.08f;
        float right = 0.92f;
        float bottom = 0.16f;
        float top = 0.62f;
        float gap = 0.04f;
        float cellWidth = (right - left - gap) / columns;
        float cellHeight = (top - bottom - gap * (rows - 1)) / rows;
        float x = left + column * (cellWidth + gap);
        float y = top - (row + 1) * cellHeight - row * gap;
        min = new Vector2(x, y);
        max = new Vector2(x + cellWidth, y + cellHeight);
    }

    void BuildDisplayTwo(Transform canvas, Step step)
    {
        RectTransform root = FreshRoot("ScreenUi", canvas);
        if (!string.IsNullOrEmpty(step.prompt))
            AddText(root, step.prompt, 40, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.94f));

        AddControl(root, iconButton, new Vector2(0.28f, 0.34f), new Vector2(0.72f, 0.74f), false, true);

        if (step.showHome)
            AddControl(root, homeButton, new Vector2(0.08f, 0.06f), new Vector2(0.46f, 0.18f), false, false);
        if (step.showTurn)
        {
            Vector2 min = step.showHome ? new Vector2(0.54f, 0.06f) : new Vector2(0.28f, 0.08f);
            Vector2 max = step.showHome ? new Vector2(0.92f, 0.18f) : new Vector2(0.72f, 0.2f);
            AddControl(root, turnButton, min, max, false, false);
        }
    }

    void AddControl(RectTransform parent, ButtonDef def, Vector2 anchorMin, Vector2 anchorMax, bool selected, bool icon)
    {
        bool pictured = def.image != null;
        if (selected && pictured && !icon)
        {
            Image border = NewImage(def.id + "Border", parent, SelectedFace);
            SetRect(border.rectTransform, anchorMin, anchorMax);
            anchorMin += new Vector2(0.012f, 0.012f);
            anchorMax -= new Vector2(0.012f, 0.012f);
        }

        Color face = pictured ? Color.white : (icon ? IconFace : (selected ? SelectedFace : ButtonFace));
        Image image = NewImage(def.id, parent, face);
        image.sprite = pictured ? def.image : (icon ? knobSprite : whiteSprite);
        image.type = Image.Type.Simple;
        image.preserveAspect = icon && pictured;
        if (icon)
        {
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.54f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.54f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = new Vector2(460f, 460f);
            image.rectTransform.anchoredPosition = Vector2.zero;
        }
        else
            SetRect(image.rectTransform, anchorMin, anchorMax);
        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        Color tint = image.color;
        colors.normalColor = tint;
        colors.highlightedColor = tint * 1.05f;
        colors.pressedColor = new Color(tint.r * 0.82f, tint.g * 0.82f, tint.b * 0.82f, 1f);
        colors.selectedColor = tint;
        button.colors = colors;
        button.targetGraphic = image;

        if (!string.IsNullOrEmpty(def.label))
            AddText(image.rectTransform, def.label, icon ? 36 : 28, pictured ? Ink : (selected ? Color.white : Ink), FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f));

        ButtonAction action = def.action;
        int step = def.step;
        string id = def.id;
        button.onClick.AddListener(() => Press(action, step, id));
    }

    /// <summary>
    /// A new click wins over whatever the last one started: a delayed second turn is dropped and a turn in progress
    /// is finished on the spot, so the book is on a clean page when the new click's own turn begins.
    /// </summary>
    void CancelPending()
    {
        if (mainTwice != null)
        {
            StopCoroutine(mainTwice);
            mainTwice = null;
        }

        bool flipping = IsAnyFlipping();
        if (swipe != null)
            swipe.CancelFlip();
        if (spreadSwipe != null)
            spreadSwipe.CancelFlip();
        if (scenarioPending)
            FinishSidesTurn();
        if (!flipping)
            return;

        selectionPending = false;
        if (lastTargetPage >= 0)
            SnapAll(lastTargetPage);
        shownStep = Mathf.Clamp((lastTargetPage - 1) / 2, 0, PageCount() - 1);
        SetControlBooksActive(false);
        SetMainBookActive(false);
        // the main page stays up, even when the new click does not change it
        ActivateOnly(sceneBook, shownStep);
        RaisePagePanels();
    }

    /// <summary>Ends a turn of the spread: the new pages are shown and the page curl is put away.</summary>
    void FinishSidesTurn()
    {
        scenarioPending = false;
        RestoreFaces();
        SetControlBooksActive(false);
        if (sidesTurnTarget >= 0)
        {
            leftIndex = sidesTurnTarget;
            rightIndex = sidesTurnRight >= 0 ? sidesTurnRight : sidesTurnTarget;
            ActivateOnly(sceneLeft, leftIndex);
            ActivateOnly(sceneRight, rightIndex);
            if (spreadBook != null)
                spreadBook.SnapToPage(leftIndex * 2 + 1);
        }

        sidesTurnRight = -1;
        if (book != null)
        {
            // the main page stays up, even if the turn was interrupted before it was shown
            shownStep = Mathf.Clamp((book.currentPage - 1) / 2, 0, PageCount() - 1);
            ActivateOnly(sceneBook, shownStep);
            SetMainBookActive(false);
        }

        RaisePagePanels();
    }

    static bool IsRow(string id)
    {
        return string.Equals(id, "Begin Here", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "At A Glance", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "Seal Of Approval", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "Get In Touch", System.StringComparison.OrdinalIgnoreCase);
    }

    void Press(ButtonAction action, int step, string id)
    {
        if (swipeGesture)
            return;

        // Begin Here again plays its page once more: the video and the text come in again
        if (action != ButtonAction.TurnPage && action != ButtonAction.Home && id == "Begin Here" && beginHereMode
            && !IsAnyFlipping() && rightIndex == beginHereRightPage && beginHereRightPage < sceneRight.Length && sceneRight[beginHereRightPage] != null)
        {
            GameObject page = sceneRight[beginHereRightPage].gameObject;
            page.SetActive(false);
            page.SetActive(true);
            return;
        }

        // the same button again changes nothing; only the turn and home buttons repeat their action
        bool repeatable = action == ButtonAction.TurnPage || action == ButtonAction.Home;
        if (!repeatable && !string.IsNullOrEmpty(id) && id == lastPressedId)
            return;
        lastPressedId = repeatable ? "" : id;

        CancelPending();

        if (action == ButtonAction.Home)
        {
            beginHereMode = false;
            scenariosShown = false;
            contextRow = "";
            sidesStay = false;
            ShowStep(0, "", false);
            return;
        }

        if (action == ButtonAction.TurnPage)
        {
            // after Begin Here: the left and right canvas turn to the scenario lists and the main page turns with them
            if (beginHereMode && !scenariosShown)
            {
                scenariosShown = true;
                TurnMainOnly(beginHereMainPage);
                TurnSidesTo(scenarioPage);
                return;
            }

            // after any other button on the spread: only the spread turns, the main page stays as it is
            if (contextRow.Length > 0)
            {
                // only Begin Here lets the user turn the spread, from one scenario detail to the next
                if (contextRow == "Begin Here" && leftIndex >= FirstDetailPage && leftIndex + 1 < sceneLeft.Length)
                    TurnSidesTo(leftIndex + 1);
                return;
            }

            sidesStay = false;
            int next = book == null ? 0 : (book.currentPage - 1) / 2 + 1;
            if (next >= PageCount())
                return;
            ShowStep(next, Selection(next), true);
            return;
        }

        // a scenario on the Begin Here lists turns the whole spread to its detail pages
        int detail = System.Array.IndexOf(ScenarioNames, id);
        if (detail >= 0 && scenariosShown && leftIndex == scenarioPage)
        {
            TurnSidesTo(FirstDetailPage + detail);
            return;
        }

        beginHereMode = false;
        scenariosShown = false;
        contextRow = IsRow(id) ? id : "";

        if (string.Equals(id, "Begin Here", System.StringComparison.OrdinalIgnoreCase))
        {
            // only the right canvas changes: the book animation page. The main page is left alone.
            beginHereMode = true;
            sidesStay = true;
            SetSides(ButtonsPage, beginHereRightPage);
            return;
        }

        if (string.Equals(id, "Seal Of Approval", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "Get In Touch", System.StringComparison.OrdinalIgnoreCase))
        {
            // the main page turns to its page, the spread keeps the buttons page; Get In Touch also shows the thank you page on the right
            bool touch = string.Equals(id, "Get In Touch", System.StringComparison.OrdinalIgnoreCase);
            sidesStay = true;
            SetSides(ButtonsPage, touch ? step : ButtonsPage);
            TurnMainOnly(step);
            return;
        }

        if (string.Equals(id, "At A Glance", System.StringComparison.OrdinalIgnoreCase))
        {
            sidesStay = true;
            SetSides(ButtonsPage, ButtonsPage);
            mainTwice = StartCoroutine(TurnMainTwice());
            return;
        }

        sidesStay = false;
        ShowStep(step, id, true);
    }

    /// <summary>
    /// Turns the spread (not the main book) to the pages at this index with one page curl.
    /// The sheet that lifts shows the pages that are on screen now, and its back and what it uncovers are the new pages.
    /// </summary>
    void TurnSidesTo(int to)
    {
        if (spreadSwipe == null || spreadBook == null || leftBaked == null || to < 1 || to >= sceneLeft.Length || to >= sceneRight.Length)
            return;
        sidesStay = true;
        scenarioPending = true;
        sidesTurnTarget = to;

        int spread = to - 1;
        Sprite[] faces = spreadBook.bookPages;
        RestoreFaces();
        stashedSpread = spread;
        stashedLeft = faces[spread * 2];
        stashedRight = faces[spread * 2 + 1];
        faces[spread * 2] = leftBaked[Mathf.Clamp(leftIndex, 0, leftBaked.Length - 1)];
        faces[spread * 2 + 1] = rightBaked[Mathf.Clamp(rightIndex, 0, rightBaked.Length - 1)];

        ActivateOnly(sceneLeft, -1);
        ActivateOnly(sceneRight, -1);
        SetControlBooksActive(true);
        spreadBook.SnapToPage(spread * 2 + 1);
        spreadSwipe.BeginFlip(true);
    }

    /// <summary>Turns the spread back one page: the left page lifts across the spine and the earlier pages are uncovered.</summary>
    void TurnSidesBack(int toLeft, int toRight)
    {
        if (spreadSwipe == null || spreadBook == null || leftBaked == null || leftIndex < 1 || toLeft < 0 || toLeft >= leftBaked.Length || toRight >= rightBaked.Length)
            return;
        sidesStay = true;
        scenarioPending = true;
        sidesTurnTarget = toLeft;
        sidesTurnRight = toRight;

        int spread = leftIndex;
        Sprite[] faces = spreadBook.bookPages;
        RestoreFaces();
        // the earlier spread in the book is dressed as the pages we are going back to
        stashedSpread = spread - 1;
        stashedLeft = faces[stashedSpread * 2];
        stashedRight = faces[stashedSpread * 2 + 1];
        faces[stashedSpread * 2] = leftBaked[toLeft];
        faces[stashedSpread * 2 + 1] = rightBaked[toRight];
        // the right page that stays is the one on screen now
        spreadBook.bookPages[spread * 2 + 1] = rightBaked[Mathf.Clamp(rightIndex, 0, rightBaked.Length - 1)];
        spreadBook.bookPages[spread * 2] = leftBaked[Mathf.Clamp(leftIndex, 0, leftBaked.Length - 1)];

        ActivateOnly(sceneLeft, -1);
        ActivateOnly(sceneRight, -1);
        SetControlBooksActive(true);
        spreadBook.SnapToPage(spread * 2 + 1);
        spreadSwipe.BeginFlip(false);
    }

    void RestoreFaces()
    {
        if (stashedSpread < 0 || spreadBook == null || spreadBook.bookPages == null)
        {
            stashedSpread = -1;
            return;
        }

        spreadBook.bookPages[stashedSpread * 2] = stashedLeft;
        spreadBook.bookPages[stashedSpread * 2 + 1] = stashedRight;
        stashedSpread = -1;
    }

    /// <summary>
    /// Turns only the main book to the step with one page turn (Begin Here, Seal Of Approval, Get In Touch). The side screens (left and right) stay on the page they show.
    /// </summary>
    void TurnMainOnly(int stepIndex)
    {
        if (book == null)
            return;
        sidesStay = true;
        stepIndex = Mathf.Clamp(stepIndex, 0, PageCount() - 1);
        int targetPage = stepIndex * 2 + 1;
        int delta = targetPage - book.currentPage;
        if (Mathf.Abs(delta) > 2)
        {
            // start one turn away from the target so the book curls once, whatever page it was on
            book.SnapToPage(delta > 0 ? targetPage - 2 : targetPage + 2);
        }

        ShowStep(stepIndex, Selection(stepIndex), true);
    }

    /// <summary>
    /// At A Glance always shows the same two main pages, one after the other: pressing it again goes back to the first.
    /// </summary>
    IEnumerator TurnMainTwice()
    {
        sidesStay = true;
        for (int turn = 0; turn < 2; turn++)
        {
            int target = atAGlanceFirstPage + turn;
            if (target >= PageCount())
                yield break;
            ShowStep(target, Selection(target), true);
            while (IsAnyFlipping())
                yield return null;
            if (turn == 0)
                yield return new WaitForSeconds(20f);
        }
    }

    RectTransform FreshRoot(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            DestroyImmediate(existing.gameObject);
        return CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    TMP_Text AddText(RectTransform parent, string value, int size, Color color, FontStyle style, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = CreateRect("Text", parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        ZeroOffsets(rect);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = ToTmpStyle(style);
        text.color = color;
        text.alignment = ToTmpAlignment(anchor);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static FontStyles ToTmpStyle(FontStyle style)
    {
        if (style == FontStyle.Bold)
            return FontStyles.Bold;
        if (style == FontStyle.Italic)
            return FontStyles.Italic;
        if (style == FontStyle.BoldAndItalic)
            return FontStyles.Bold | FontStyles.Italic;
        return FontStyles.Normal;
    }

    static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
    {
        if (anchor == TextAnchor.UpperLeft)
            return TextAlignmentOptions.TopLeft;
        if (anchor == TextAnchor.UpperCenter)
            return TextAlignmentOptions.Top;
        if (anchor == TextAnchor.UpperRight)
            return TextAlignmentOptions.TopRight;
        if (anchor == TextAnchor.MiddleLeft)
            return TextAlignmentOptions.Left;
        if (anchor == TextAnchor.MiddleRight)
            return TextAlignmentOptions.Right;
        if (anchor == TextAnchor.LowerLeft)
            return TextAlignmentOptions.BottomLeft;
        if (anchor == TextAnchor.LowerCenter)
            return TextAlignmentOptions.Bottom;
        if (anchor == TextAnchor.LowerRight)
            return TextAlignmentOptions.BottomRight;
        return TextAlignmentOptions.Center;
    }

    Image NewImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), Vector2.zero);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    Image PageImage(string name, Transform parent, Sprite sprite, bool active)
    {
        Image image = NewImage(name, parent, Color.white);
        image.sprite = sprite;
        image.gameObject.SetActive(active);
        return image;
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Stretch(RectTransform rect)
    {
        SetRect(rect, Vector2.zero, Vector2.one);
    }

    static void PlacePage(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
    }

    Sprite FlatSprite(Color color, int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        generated.Add(texture);
        generated.Add(sprite);
        return sprite;
    }

    static Image PanelImage(Component canvas)
    {
        if (canvas == null)
            return null;
        Transform panel = canvas.transform.Find("Panel");
        return panel != null ? panel.GetComponent<Image>() : null;
    }

    Sprite PanelPaper(Component canvas, bool transparent = false)
    {
        Vector2 page = ReferenceSize(canvas);
        return BakePanel(null, Mathf.RoundToInt(page.x), Mathf.RoundToInt(page.y), canvas, 7200f, transparent);
    }

    void StampPanel(RectTransform root, Component canvas)
    {
        Image source = PanelImage(canvas);
        Image paper = NewImage("Paper", root, source != null ? source.color : Screen);
        paper.transform.SetAsFirstSibling();
        Stretch(paper.rectTransform);
        paper.sprite = source != null && source.sprite != null ? source.sprite : whiteSprite;
        if (source == null || source.sprite == null)
            return;
        paper.type = source.type;
        paper.preserveAspect = false;
        paper.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
    }

    Sprite BakePanel(RectTransform source, int width, int height, Color background, float cameraY)
    {
        return BakePanel(source, width, height, null, background, cameraY, false);
    }

    Sprite BakePanel(RectTransform source, int width, int height, Component screen, float cameraY, bool transparent = false, Rect? keep = null)
    {
        return BakePanel(source, width, height, screen, PanelColor(screen), cameraY, true, transparent, keep);
    }

    /// <summary>
    /// Bakes a page. transparent: no paper at all, just the page content. keep: only this part (0 to 1, from the bottom left)
    /// is paper, everything outside it is see-through, so the sheet is a sheet of paper and not the whole book.
    /// </summary>
    Sprite BakePanel(RectTransform source, int width, int height, Component screen, Color background, float cameraY, bool usePanelImage, bool transparent = false, Rect? keep = null)
    {
        bool seeThrough = usePanelImage && (transparent || keep.HasValue);
        if (transparent)
            background = new Color(background.r, background.g, background.b, 0f);
        else if (keep.HasValue)
            background = new Color(background.r, background.g, background.b, 0f);
        var cameraObject = new GameObject("PanelBakerCamera");
        cameraObject.transform.position = new Vector3(0f, cameraY, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = height * 0.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 50f;
        camera.cullingMask = 1 << 5;

        var canvasObject = new GameObject("PanelBaker", typeof(RectTransform));
        canvasObject.layer = 5;
        canvasObject.transform.position = new Vector3(0f, cameraY, 0f);
        Canvas baker = canvasObject.AddComponent<Canvas>();
        baker.renderMode = RenderMode.WorldSpace;
        baker.worldCamera = camera;
        RectTransform rect = baker.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;

        if (usePanelImage && !transparent)
            StampPanel(rect, screen);
        else
        {
            Image paper = NewImage("Paper", rect, background);
            paper.sprite = whiteSprite;
            Stretch(paper.rectTransform);
        }
        if (source != null)
        {
            if (usePanelImage && source.parent is RectTransform panels)
            {
                RectTransform copyPanels = Instantiate(panels, rect);
                copyPanels.gameObject.SetActive(true);
                CopyRect(copyPanels, panels);
                for (int i = 0; i < copyPanels.childCount; i++)
                    copyPanels.GetChild(i).gameObject.SetActive(copyPanels.GetChild(i).name == source.name);
            }
            else
            {
                RectTransform copy = Instantiate(source, rect);
                copy.gameObject.SetActive(true);
                copy.anchorMin = Vector2.zero;
                copy.anchorMax = Vector2.one;
                copy.offsetMin = Vector2.zero;
                copy.offsetMax = Vector2.zero;
                copy.pivot = new Vector2(0.5f, 0.5f);
                copy.localScale = Vector3.one;
                copy.localRotation = Quaternion.identity;
            }
        }

        Canvas.ForceUpdateCanvases();
        var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = texture;
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        var image = new Texture2D(width, height, seeThrough ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        if (keep.HasValue)
            ClearOutside(image, keep.Value);
        image.Apply();
        RenderTexture.active = previous;
        camera.targetTexture = null;
        Sprite sprite = Sprite.Create(image, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        generated.Add(texture);
        generated.Add(image);
        generated.Add(sprite);
        Destroy(canvasObject);
        Destroy(cameraObject);
        return sprite;
    }

    static void ClearOutside(Texture2D image, Rect keep)
    {
        int width = image.width;
        int height = image.height;
        int x0 = Mathf.RoundToInt(keep.xMin * width), x1 = Mathf.RoundToInt(keep.xMax * width);
        int y0 = Mathf.RoundToInt(keep.yMin * height), y1 = Mathf.RoundToInt(keep.yMax * height);
        Color32[] pixels = image.GetPixels32();
        for (int y = 0; y < height; y++)
        {
            bool rowInside = y >= y0 && y < y1;
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                if (rowInside && x >= x0 && x < x1)
                {
                    pixels[row + x].a = 255;
                    continue;
                }

                pixels[row + x] = new Color32(0, 0, 0, 0);
            }
        }

        image.SetPixels32(pixels);
    }

    Sprite SolidSprite(Color color)
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        generated.Add(tex);
        generated.Add(sprite);
        return sprite;
    }

    Sprite CircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f - 1f;
        var center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        generated.Add(tex);
        generated.Add(sprite);
        return sprite;
    }

    static void ZeroOffsets(RectTransform rect)
    {
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static List<Step> DefaultSteps()
    {
        return new List<Step>
        {
            new Step
            {
                screenTitle = "Languages and Communication Academy",
                prompt = "Turn the pages to reveal more",
                page = PageKind.Cover,
                showDiscoverButtons = false,
                showHome = false,
                showTurn = true
            },
            new Step
            {
                screenTitle = "Discover this book",
                selectedButton = "",
                page = PageKind.Open,
                showDiscoverButtons = true,
                showHome = false,
                showTurn = true
            },
            new Step
            {
                screenTitle = "Discover this book",
                selectedButton = "overview",
                page = PageKind.Paper,
                showDiscoverButtons = true,
                showHome = true,
                showTurn = true,
                pageHeading = "Great ideas deserve great delivery.",
                pageBody = "The Language & Communication Academy gives institutes and organisations the communication skills to thrive in today's workplace, so they can say what they mean and mean what they say.\n\nIt is built for the people who speak for a school, a company, or a public service."
            },
            new Step
            {
                screenTitle = "Discover this book",
                selectedButton = "description",
                page = PageKind.Columns,
                showDiscoverButtons = true,
                showHome = true,
                showTurn = true,
                pageHeading = "From a language people can stand behind.",
                pageBody = "The words have to last longer than the course. This is how the academy builds that.",
                columns = new[]
                {
                    new Column
                    {
                        heading = "The past",
                        body = "Courses were a one-off class. People left with notes, and the words faded when the meeting started."
                    },
                    new Column
                    {
                        heading = "How we are making the difference",
                        body = "The academy stays with the team. Practice is short, spoken, and tied to the work they do every week."
                    },
                    new Column
                    {
                        heading = "The future",
                        body = "People say what they mean, and the room understands them. The skill stays after the course ends."
                    }
                }
            }
        };
    }
}
