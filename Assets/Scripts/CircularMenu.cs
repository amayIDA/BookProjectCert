using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Circular image menu. Add entries in the list and each one becomes a button
/// spaced evenly around a circle. Radius updates immediately, including in Play mode.
/// Swipe around the circle to rotate it. Click a button to show that entry on the content display.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class CircularMenu : MonoBehaviour
{
    [Serializable]
    public class CircleItem
    {
        public string name = "Item";

        [Tooltip("Picture used for this button on the circle.")]
        public Sprite buttonImage;

        [Tooltip("Picture shown on ContentDisplay when this button is pressed. If empty, the button image is used.")]
        public Sprite contentImage;

        [Tooltip("Picture shown on Item Context while this item is in the touch window.")]
        public Sprite itemContext;

        [Tooltip("Optional extra content on ContentDisplay. Scene objects are shown and hidden. Prefab assets are created on the content canvas.")]
        public GameObject content;
    }

    static readonly Color[] Palette =
    {
        new Color(0.90f, 0.32f, 0.34f),
        new Color(0.93f, 0.55f, 0.22f),
        new Color(0.93f, 0.78f, 0.25f),
        new Color(0.30f, 0.70f, 0.45f),
        new Color(0.22f, 0.55f, 0.86f),
        new Color(0.48f, 0.36f, 0.82f)
    };

    [Header("ITEMS")]
    public List<CircleItem> items = new List<CircleItem>();

    [Header("LAYOUT")]
    [Min(0f)]
    [Tooltip("Distance from the center to each button, in pixels on the large LED. Updates immediately, including during Play mode.")]
    public float radius = 780f;

    [Min(1f)]
    public float buttonSize = 260f;

    [Tooltip("Angle of the first button. 90 puts it at the top.")]
    public float startAngle = 90f;

    [Tooltip("Rotation of the large LED circle, in degrees. The touch window does not change this.")]
    public float ledRotation;

    [Tooltip("Rotation used by the touch dial, in degrees.")]
    public float rotation;

    [Range(1f, 1.5f)]
    public float selectedScale = 1.12f;

    [Header("SWIPE")]
    public float rotationSensitivity = 1f;

    [Tooltip("How quickly a flick slows down. Lower values coast longer.")]
    public float inertiaDamping = 3.5f;

    [Tooltip("After a swipe, ease the nearest button onto a slot.")]
    public bool snapAfterSwipe = true;

    public float snapSpeed = 540f;
    public float clickSlop = 22f;

    [Header("DISPLAYS")]
    [Tooltip("Also show the same circle on TouchScreen_Canvas so that display can swipe and tap it.")]
    public bool showOnTouchScreen = true;

    [Header("TOUCH DIAL")]
    [Tooltip("The touch screen shows one item through a window. A swipe turns the dial and brings the next item into that window.")]
    public bool oneItemWindow = true;

    public Vector2 touchWindowSize = new Vector2(2200f, 2200f);
    public Vector2 touchWindowPosition = Vector2.zero;

    [Tooltip("Direction of the circle that sits in the window. 90 is the top.")]
    public float focusAngle = 90f;

    [Min(0)]
    [Tooltip("Which list item sits in the touch window. 0 is the first item. Changing this moves that item into the window and does not open its content.")]
    public int itemInWindow;

    [Min(1f)]
    public float touchRadius = 3200f;

    [Min(1f)]
    public float touchButtonSize = 1800f;

    [Tooltip("Margin around the picture on the content display, in canvas pixels.")]
    public string idleHeadline = "National ambitions. Delivered by its people.";
    public string idleSubtitle = "Developing Emirati talent at every stage.";

    public float contentPadding = 72f;

    public Vector2 contentLabelSize = new Vector2(1400f, 160f);
    public Vector2 contentLabelOffset = new Vector2(0f, -420f);

    [Tooltip("Center picture on the large LED, in canvas pixels.")]
    public float itemContextSize = 280f;

    public Image contentImage;
    public Image itemContextImage;
    public Text contentLabel;
    public Text contentSubtitle;
    public RectTransform contentHost;
    public RectTransform touchMirror;
    public RectTransform touchWindow;
    public RectTransform ledWheel;
    public RectTransform touchDial;
    public Button discoverButton;
    public Button backButton;

    [Header("TALENT WHEEL")]
    [Tooltip("Draw the buttons as round icon discs like the Talent Wheel and highlight the item in the touch window.")]
    public bool niStyle;

    [Tooltip("The content display is drawn by another component. The menu will not write its own label or picture.")]
    public bool externalContent;

    [Tooltip("Sprite for the round buttons. Leave empty to use Resources/NationalInitiative/ui_disc.")]
    public Sprite niDisc;

    [Tooltip("Soft glow around the selected button. Leave empty to use Resources/NationalInitiative/ui_halo.")]
    public Sprite niHalo;

    [Header("LED WHEEL POSITION")]
    [Tooltip("On: the LED wheel always starts with its items in their default places (first item at the top), no matter which item sits in the touch window at the start. Off: the LED wheel turns so the same item as the touch window is at the top.")]
    public bool ledKeepsDefaultPosition = true;

    [Header("TALENT WHEEL HALO (applies to every wheel item on every level)")]
    [Tooltip("How far the glow reaches beyond the edge of a button on the LED wheel, as a fraction of the button size.")]
    [Range(0f, 0.6f)] public float ledHaloSpread = 0.26f;

    [Tooltip("The same for the big buttons of the touch dial. Keep it small, the dial window clips anything outside it.")]
    [Range(0f, 0.3f)] public float touchHaloSpread = 0.07f;

    [Tooltip("Opacity of the glow around the selected button on the LED wheel.")]
    [Range(0f, 1f)] public float haloOpacity = 0.75f;

    [Tooltip("Opacity of the glow around the selected button of the touch dial.")]
    [Range(0f, 1f)] public float touchHaloOpacity = 0.75f;

    public Color haloColor = new Color(0.933f, 0.835f, 0.600f, 1f);

    [Header("TALENT WHEEL TEXT")]
    [Tooltip("The names under the icons shrink to fit the button, between these two sizes. The Font Size of the Text itself is ignored while Best Fit is on.")]
    public int ledLabelMinSize = 10;
    public int ledLabelMaxSize = 26;
    public int touchLabelMinSize = 24;
    public int touchLabelMaxSize = 64;

    [Tooltip("Blue face of the round buttons. Leave empty to use Resources/NationalInitiative/ui_opt_face.")]
    public Sprite niFace;

    [Tooltip("Gold face of the selected button. Leave empty to use Resources/NationalInitiative/ui_opt_face_sel.")]
    public Sprite niFaceSelected;

    public Action<int> selectHandler;
    public Action<int> discoverHandler;
    public Action backHandler;
    public Action<int> windowChanged;
    public Action activityHandler;

    public int SelectedIndex { get; private set; } = -1;
    public bool SuppressClick { get; private set; }

    int lastSelectFrame = -1;

    readonly List<CircularMenuButton> ledButtons = new List<CircularMenuButton>();
    readonly List<CircularMenuButton> mirrorButtons = new List<CircularMenuButton>();
    readonly Dictionary<GameObject, GameObject> spawnedContent = new Dictionary<GameObject, GameObject>();

    [SerializeField, HideInInspector] int homeBaseItem;
    int syncedWindowItem = int.MinValue;

    /// <summary>How far the LED wheel is turned back so it keeps its default arrangement while the touch dial starts on another item.</summary>
    float LedOffset()
    {
        if (!ledKeepsDefaultPosition || items == null || items.Count == 0)
            return 0f;
        return 360f / items.Count * homeBaseItem;
    }
    int lastWindow = -1;
    bool tweening;
    float tweenTarget;
    Action tweenDone;
    bool editorRefreshQueued;
    bool dragging;
    bool touchDrag;
    bool motionOnTouch;
    bool settle;
    float angularVelocity;
    float travel;
    Vector2 lastPointer;

    static Sprite discSprite;
    static Sprite pixelSprite;
    static Font builtinFont;

    void Reset()
    {
        radius = 780f;
        buttonSize = 260f;
        startAngle = 90f;
        showOnTouchScreen = true;
        if (items == null || items.Count == 0)
            items = DefaultItems();
    }

    void OnEnable()
    {
        RequestEditorRefresh();
    }

    void OnValidate()
    {
        RequestEditorRefresh();
    }

    void Start()
    {
        RebuildNow();
        ClearContent();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            TickMotion();
            return;
        }

        if (NeedsEditorSync())
            RebuildNow();
    }

    bool NeedsEditorSync()
    {
        int count = items != null ? items.Count : 0;
        if (CountButtons(LedButtonRoot()) != count)
            return true;

        if (!showOnTouchScreen)
            return false;

        if (oneItemWindow && touchDial == null)
            return true;

        RectTransform touchRoot = oneItemWindow ? touchDial : touchMirror;
        return touchRoot != null && CountButtons(touchRoot) != count;
    }

    static int CountButtons(RectTransform root)
    {
        if (root == null)
            return 0;

        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).GetComponent<CircularMenuButton>() != null)
                count++;
        }

        return count;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (niStyle && oneItemWindow && items != null && items.Count > 0)
        {
            int inWindow = ItemInWindow();
            SelectedIndex = inWindow;
            if (inWindow != lastWindow)
            {
                lastWindow = inWindow;
                if (windowChanged != null)
                    windowChanged(inWindow);
            }
        }

        RebuildNow();
        RefreshLiveContent();

        if (!dragging)
            SuppressClick = false;
    }

    [ContextMenu("Rebuild Circle")]
    public void RebuildNow()
    {
        if (items == null)
            items = new List<CircleItem>();

        if (contentImage == null || contentHost == null || GetComponent<CircularMenuSurface>() == null)
            EnsureWiring();
        else if (!showOnTouchScreen)
        {
            if (touchMirror != null && touchMirror.gameObject.activeSelf)
                touchMirror.gameObject.SetActive(false);
        }
        else if (touchMirror == null || !touchMirror.gameObject.activeSelf)
            EnsureTouchMirror();

        PrepareTouchDial();

        if (ShortSide((RectTransform)transform) < 1f || (touchMirror != null && ShortSide(touchMirror) < 1f))
            Canvas.ForceUpdateCanvases();

        EnsureLedWheel();
        SyncButtons(ledWheel, ledButtons);
        SyncButtons(TouchButtonRoot(), mirrorButtons);
        ApplyChosenWindowItem();
        ApplyVisuals(ledButtons, false);
        ApplyVisuals(mirrorButtons, false);

        EnsureContentUi();
        LayoutBoth();
    }

    void ApplyChosenWindowItem()
    {
        if (!oneItemWindow || dragging || settle || tweening || items == null || items.Count == 0)
            return;

        int index = Mathf.Clamp(itemInWindow, 0, items.Count - 1);
        itemInWindow = index;
        if (index == syncedWindowItem)
            return;

        float step = 360f / items.Count;
        rotation = focusAngle - startAngle - step * index;
        homeBaseItem = index;
        if (niStyle)
            SelectedIndex = index;
        syncedWindowItem = index;
    }

    void ClearContent()
    {
        if (externalContent)
            return;

        SelectedIndex = -1;
        if (contentImage != null)
        {
            contentImage.sprite = null;
            contentImage.enabled = false;
        }

        if (contentLabel != null)
        {
            contentLabel.gameObject.SetActive(true);
            contentLabel.text = string.IsNullOrEmpty(idleHeadline)
                ? "National ambitions. Delivered by its people."
                : idleHeadline;
        }

        if (contentSubtitle != null)
        {
            contentSubtitle.gameObject.SetActive(true);
            contentSubtitle.text = idleSubtitle;
        }

        if (items == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                SetContentActive(items[i].content, false);
        }
    }

    void LayoutBoth()
    {
        ledRotation = rotation + LedOffset();
        if (ledWheel != null)
            ledWheel.localEulerAngles = new Vector3(0f, 0f, ledRotation);
        LayoutButtons(ledButtons, radius, buttonSize, 0f, -ledRotation);
        if (touchMirror != null && showOnTouchScreen)
        {
            if (oneItemWindow && touchDial != null)
                LayoutTouchDial();
            else
                LayoutButtons(mirrorButtons, ScaleTo(touchMirror, radius), ScaleTo(touchMirror, buttonSize), rotation, 0f);
        }

        RefreshItemContext();
    }

    void RefreshItemContext()
    {
        if (itemContextImage == null)
        {
            Canvas canvas = FindCanvas("LargeLEDCircle_Canvas");
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find("ItemContext");
                if (existing != null)
                    itemContextImage = existing.GetComponent<Image>();
            }
        }

        if (itemContextImage == null || items == null || items.Count == 0)
            return;

        int index = SelectedIndex;
        if (index < 0 || index >= items.Count)
        {
            itemContextImage.enabled = false;
            return;
        }

        RectTransform contextRect = itemContextImage.rectTransform;
        contextRect.anchorMin = contextRect.anchorMax = new Vector2(0.5f, 0.5f);
        contextRect.pivot = new Vector2(0.5f, 0.5f);
        contextRect.sizeDelta = new Vector2(itemContextSize, itemContextSize);

        Sprite sprite = items[index] != null ? items[index].itemContext : null;
        if (itemContextImage.sprite == sprite && itemContextImage.enabled == (sprite != null))
            return;

        itemContextImage.sprite = sprite;
        itemContextImage.preserveAspect = true;
        itemContextImage.color = Color.white;
        itemContextImage.enabled = sprite != null;
    }

    void LayoutTouchDial()
    {
        if (touchWindow != null)
        {
            touchWindow.anchorMin = touchWindow.anchorMax = new Vector2(0.5f, 0.5f);
            touchWindow.pivot = new Vector2(0.5f, 0.5f);
            touchWindow.sizeDelta = touchWindowSize;
            touchWindow.anchoredPosition = touchWindowPosition;
        }

        float radians = focusAngle * Mathf.Deg2Rad;
        touchDial.anchorMin = touchDial.anchorMax = new Vector2(0.5f, 0.5f);
        touchDial.pivot = new Vector2(0.5f, 0.5f);
        touchDial.sizeDelta = Vector2.zero;
        touchDial.anchoredPosition = -new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * touchRadius;
        LayoutButtons(mirrorButtons, touchRadius, touchButtonSize, rotation, 0f);
        EnsureTouchActions();
    }

    /// <summary>Replace the wheel items and put one of them in the touch window without animation.</summary>
    public void SetItems(List<CircleItem> list, int focus = 0)
    {
        items = list ?? new List<CircleItem>();
        tweening = false;
        tweenDone = null;
        settle = false;
        dragging = false;
        angularVelocity = 0f;
        itemInWindow = Mathf.Clamp(focus, 0, Mathf.Max(0, items.Count - 1));
        syncedWindowItem = int.MinValue;
        if (items.Count > 0)
            rotation = focusAngle - startAngle - 360f / items.Count * itemInWindow;
        lastWindow = itemInWindow;
        SelectedIndex = itemInWindow;
        RebuildNow();
    }

    public int WindowItem
    {
        get { return items != null && items.Count > 0 ? ItemInWindow() : -1; }
    }

    /// <summary>Turn the dial until the item is in the touch window, then call done.</summary>
    public void RotateToItem(int index, Action done)
    {
        if (items == null || items.Count == 0)
        {
            if (done != null)
                done();
            return;
        }

        index = Mathf.Clamp(index, 0, items.Count - 1);
        float target = focusAngle - startAngle - 360f / items.Count * index;
        float remaining = Mathf.DeltaAngle(rotation, target);
        if (Mathf.Abs(remaining) < 0.5f)
        {
            if (done != null)
                done();
            return;
        }

        settle = false;
        angularVelocity = 0f;
        tweenTarget = rotation + remaining;
        tweenDone = done;
        tweening = true;
        motionOnTouch = true;
    }

    public void Select(int index)
    {
        if (items == null || index < 0 || index >= items.Count)
            return;

        if (selectHandler != null)
        {
            selectHandler(index);
            return;
        }

        if (index == SelectedIndex && lastSelectFrame == Time.frameCount)
            return;

        lastSelectFrame = Time.frameCount;
        SelectedIndex = index;
        ShowContent(items[index]);
        if (isActiveAndEnabled)
            RebuildNow();
    }

    public void PrepareDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void BeginDrag(PointerEventData eventData, RectTransform space)
    {
        dragging = true;
        tweening = false;
        if (activityHandler != null)
            activityHandler();
        touchDrag = oneItemWindow && IsTouchSpace(space);
        settle = false;
        angularVelocity = 0f;
        SuppressClick = false;
        travel = 0f;
        lastPointer = eventData.position;
    }

    public void Drag(PointerEventData eventData, RectTransform space)
    {
        if (!dragging)
            BeginDrag(eventData, space);

        travel = Mathf.Max(travel, Vector2.Distance(eventData.position, eventData.pressPosition));
        if (travel > clickSlop)
            SuppressClick = true;

        if (activityHandler != null)
            activityHandler();

        RectTransform dial = DragSpace(space);
        Vector2 previous = lastPointer;
        lastPointer = eventData.position;

        float delta = RotationDelta(dial, eventData, previous);
        delta *= rotationSensitivity;
        if (Mathf.Abs(delta) < 0.01f)
            return;

        rotation += delta;
        motionOnTouch = true;
        angularVelocity = delta / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        LayoutBoth();
    }

    float RotationDelta(RectTransform dial, PointerEventData eventData, Vector2 previous)
    {
        Camera camera = eventData.pressEventCamera;
        if (camera == null || !camera.enabled)
        {
            Canvas canvas = dial.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.worldCamera != null)
                camera = canvas.worldCamera;
        }

        Vector2 fromLocal;
        Vector2 toLocal;
        bool fromOk = RectTransformUtility.ScreenPointToLocalPointInRectangle(dial, previous, camera, out fromLocal);
        bool toOk = RectTransformUtility.ScreenPointToLocalPointInRectangle(dial, eventData.position, camera, out toLocal);
        if (oneItemWindow && IsTouchSpace(dial))
            return TouchDialDelta(eventData, previous, camera);

        if (fromOk && toOk && fromLocal.sqrMagnitude > 25f && toLocal.sqrMagnitude > 25f)
            return Vector2.SignedAngle(fromLocal, toLocal);

        float width = Mathf.Max(dial.rect.width, 1f);
        return -eventData.delta.x / width * 360f;
    }

    float TouchDialDelta(PointerEventData eventData, Vector2 previous, Camera camera)
    {
        RectTransform space = touchMirror != null ? touchMirror : (RectTransform)transform;
        Vector2 fromLocal;
        Vector2 toLocal;
        bool fromOk = RectTransformUtility.ScreenPointToLocalPointInRectangle(space, previous, camera, out fromLocal);
        bool toOk = RectTransformUtility.ScreenPointToLocalPointInRectangle(space, eventData.position, camera, out toLocal);
        Vector2 delta = fromOk && toOk ? toLocal - fromLocal : eventData.delta;

        float radians = focusAngle * Mathf.Deg2Rad;
        Vector2 tangent = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
        float along = Vector2.Dot(delta, tangent);
        int count = items != null && items.Count > 0 ? items.Count : 1;
        float step = 360f / count;
        return along / Mathf.Max(touchButtonSize, 1f) * step;
    }

    bool IsTouchSpace(RectTransform space)
    {
        return touchMirror != null && space != null && (space == touchMirror || space.IsChildOf(touchMirror));
    }

    RectTransform DragSpace(RectTransform space)
    {
        if (space == null)
            return (RectTransform)transform;

        if (ledWheel != null && (space == ledWheel || space.IsChildOf(ledWheel)))
            return (RectTransform)transform;

        return space;
    }

    RectTransform LedButtonRoot()
    {
        return ledWheel != null ? ledWheel : (RectTransform)transform;
    }

    void EnsureLedWheel()
    {
        if (ledWheel == null)
        {
            Transform existing = transform.Find("WheelItems");
            if (existing != null)
                ledWheel = existing as RectTransform;
        }

        if (ledWheel == null)
        {
            ledWheel = CreateRect("WheelItems", transform);
            ledWheel.anchorMin = ledWheel.anchorMax = new Vector2(0.5f, 0.5f);
            ledWheel.pivot = new Vector2(0.5f, 0.5f);
            ledWheel.sizeDelta = Vector2.zero;
            ledWheel.anchoredPosition = Vector2.zero;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<CircularMenuButton>() != null)
                child.SetParent(ledWheel, false);
        }

        EnsureWheelChrome();
    }

    void EnsureWheelChrome()
    {
        Transform canvas = transform.parent;
        if (canvas == null)
            return;

        Image backdrop = FindChromeImage(canvas, "LedBackdrop");
        if (backdrop == null)
        {
            backdrop = CreateChromeImage(canvas, "LedBackdrop");
            Stretch(backdrop.rectTransform);
            backdrop.color = new Color(0.114f, 0.173f, 0.310f, 1f);
            backdrop.raycastTarget = false;
            backdrop.transform.SetAsFirstSibling();
        }

        Image disc = FindChromeImage(transform, "WheelDisc");
        if (disc == null)
        {
            disc = CreateChromeImage(transform, "WheelDisc");
            float diameter = (radius + buttonSize * 0.15f) * 2f;
            RectTransform rect = disc.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.anchoredPosition = Vector2.zero;
            disc.sprite = DiscSprite();
            disc.color = new Color(0.086f, 0.133f, 0.290f, 1f);
            disc.raycastTarget = true;
            CircularMenuSurface surface = disc.gameObject.AddComponent<CircularMenuSurface>();
            surface.menu = this;
        }

        Image ring = FindChromeImage(transform, "WheelRing");
        if (ring == null)
        {
            ring = CreateChromeImage(transform, "WheelRing");
            RectTransform discRect = disc.rectTransform;
            RectTransform rect = ring.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = discRect.sizeDelta + new Vector2(28f, 28f);
            rect.anchoredPosition = Vector2.zero;
            ring.sprite = DiscSprite();
            ring.color = new Color(0.933f, 0.835f, 0.600f, 0.45f);
            ring.raycastTarget = false;
        }

        Image notch = FindChromeImage(transform, "WheelNotch");
        if (notch == null)
        {
            notch = CreateChromeImage(transform, "WheelNotch");
            RectTransform rect = notch.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(36f, 28f);
            rect.anchoredPosition = new Vector2(0f, -(radius + buttonSize * 0.55f));
            notch.color = new Color(0.933f, 0.835f, 0.600f, 1f);
            notch.raycastTarget = false;
        }

        if (ledWheel != null)
            ledWheel.SetAsLastSibling();
    }

    static Image FindChromeImage(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        return existing != null ? existing.GetComponent<Image>() : null;
    }

    static Image CreateChromeImage(Transform parent, string objectName)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = PixelSprite();
        return image;
    }

    public void EndDrag(PointerEventData eventData)
    {
        dragging = false;
        if (travel < clickSlop)
            angularVelocity = 0f;
        else if (touchDrag)
        {
            angularVelocity = 0f;
            settle = true;
        }
        else
            settle = snapAfterSwipe;
    }

    public void Scroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y;
        if (Mathf.Abs(scroll) > 20f)
            scroll /= 120f;

        rotation += scroll * 18f;
        angularVelocity = 0f;
        settle = snapAfterSwipe;
        LayoutBoth();
    }

    void TickMotion()
    {
        if (dragging)
            return;

        float dt = Time.unscaledDeltaTime;
        if (tweening)
        {
            float remaining = Mathf.DeltaAngle(rotation, tweenTarget);
            float stepSize = Mathf.Max(240f, Mathf.Abs(remaining) * 7f) * dt;
            if (Mathf.Abs(remaining) <= stepSize)
            {
                rotation += remaining;
                tweening = false;
                RememberWindowItem();
                LayoutBoth();
                Action done = tweenDone;
                tweenDone = null;
                if (done != null)
                    done();
            }
            else
            {
                rotation += Mathf.Sign(remaining) * stepSize;
                LayoutBoth();
            }

            return;
        }

        if (Mathf.Abs(angularVelocity) > 12f)
        {
            rotation += angularVelocity * dt;
            angularVelocity *= Mathf.Exp(-inertiaDamping * dt);
            LayoutBoth();
            return;
        }

        angularVelocity = 0f;
        if (!settle || items == null || items.Count < 2)
        {
            settle = false;
            return;
        }

        float step = 360f / items.Count;
        float target = Mathf.Round(rotation / step) * step;
        float delta = Mathf.DeltaAngle(rotation, target);
        float maxStep = snapSpeed * dt;
        float applied = Mathf.Abs(delta) <= maxStep ? delta : Mathf.Sign(delta) * maxStep;
        rotation += applied;

        if (Mathf.Abs(delta) <= maxStep)
        {
            settle = false;
            if (motionOnTouch)
                RememberWindowItem();
        }

        LayoutBoth();
    }

    void RefreshLiveContent()
    {
        if (externalContent)
            return;

        if (SelectedIndex < 0 || SelectedIndex >= items.Count || contentImage == null)
            return;

        Sprite sprite = ContentSprite(items[SelectedIndex]);
        if (contentImage.sprite == sprite)
            return;

        contentImage.sprite = sprite;
        contentImage.enabled = sprite != null;
        contentImage.preserveAspect = true;
        UpdateLabel(items[SelectedIndex], sprite);
    }

    void ShowContent(CircleItem item)
    {
        if (externalContent)
            return;

        EnsureWiring();

        Sprite sprite = ContentSprite(item);
        if (contentImage != null)
        {
            contentImage.sprite = sprite;
            contentImage.enabled = sprite != null;
            contentImage.preserveAspect = true;
            contentImage.color = Color.white;
        }

        UpdateLabel(item, sprite);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].content != item.content)
                SetContentActive(items[i].content, false);
        }

        foreach (var pair in spawnedContent)
        {
            if (pair.Value != null && (item.content == null || pair.Key != item.content))
                pair.Value.SetActive(false);
        }

        SetContentActive(item.content, true);
    }

    void UpdateLabel(CircleItem item, Sprite sprite)
    {
        if (contentLabel == null)
            return;

        contentLabel.gameObject.SetActive(true);
        contentLabel.text = item != null && !string.IsNullOrEmpty(item.name) ? item.name : idleHeadline;
        if (contentSubtitle != null)
        {
            contentSubtitle.gameObject.SetActive(true);
            contentSubtitle.text = "In the notch";
        }
    }

    Sprite ContentSprite(CircleItem item)
    {
        if (item == null)
            return null;
        return item.contentImage != null ? item.contentImage : item.buttonImage;
    }

    void SetContentActive(GameObject source, bool active)
    {
        if (source == null || IsProtected(source))
            return;

        if (source.scene.IsValid())
        {
            source.SetActive(active);
            return;
        }

        if (!active)
        {
            GameObject existing;
            if (spawnedContent.TryGetValue(source, out existing) && existing != null)
                existing.SetActive(false);
            return;
        }

        GameObject instance;
        if (!spawnedContent.TryGetValue(source, out instance) || instance == null)
        {
            Transform parent = contentHost != null ? contentHost : ContentCanvasTransform();
            instance = Instantiate(source, parent);
            instance.name = source.name;
            spawnedContent[source] = instance;
        }

        instance.SetActive(true);
    }

    bool IsProtected(GameObject go)
    {
        if (go == null)
            return true;
        if (go.GetComponent<Canvas>() != null)
            return true;
        if (go == gameObject)
            return true;
        if (contentImage != null && go == contentImage.gameObject)
            return true;
        if (contentLabel != null && go == contentLabel.gameObject)
            return true;
        if (contentHost != null && go == contentHost.gameObject)
            return true;
        if (touchMirror != null && go == touchMirror.gameObject)
            return true;
        return false;
    }

    void EnsureWiring()
    {
        if (!gameObject.scene.IsValid())
            return;

        EnsureSelfSurface();
        EnsureTouchMirror();
        EnsureContentUi();
    }

    void EnsureSelfSurface()
    {
        Stretch((RectTransform)transform);

        Image image = GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();

        image.sprite = PixelSprite();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        CircularMenuSurface[] surfaces = GetComponents<CircularMenuSurface>();
        CircularMenuSurface surface = null;
        for (int i = 0; i < surfaces.Length; i++)
        {
            if (surface == null)
                surface = surfaces[i];
            else
                Destroy(surfaces[i]);
        }

        if (surface == null)
            surface = gameObject.AddComponent<CircularMenuSurface>();
        surface.menu = this;
    }

    void PrepareTouchDial()
    {
        if (!showOnTouchScreen || touchMirror == null)
            return;

        Stretch(touchMirror);
        touchMirror.localScale = Vector3.one;

        if (!oneItemWindow)
            return;

        if (touchWindow == null)
        {
            Transform existing = touchMirror.Find("DialWindow");
            if (existing != null)
                touchWindow = existing as RectTransform;
        }

        if (touchWindow == null)
        {
            touchWindow = CreateRect("DialWindow", touchMirror);
            Image shade = touchWindow.gameObject.AddComponent<Image>();
            shade.sprite = PixelSprite();
            shade.color = new Color(1f, 1f, 1f, 0f);
            shade.raycastTarget = false;
            touchWindow.gameObject.AddComponent<RectMask2D>();
        }

        if (touchDial == null)
        {
            Transform existing = touchWindow.Find("Dial");
            if (existing != null)
                touchDial = existing as RectTransform;
        }

        if (touchDial == null)
            touchDial = CreateRect("Dial", touchWindow);

        CircularMenuButton[] buttons = touchMirror.GetComponentsInChildren<CircularMenuButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].transform.parent != touchDial)
                buttons[i].transform.SetParent(touchDial, false);
        }
    }

    RectTransform TouchButtonRoot()
    {
        if (!showOnTouchScreen)
            return null;
        if (oneItemWindow && touchDial != null)
            return touchDial;
        return touchMirror;
    }

    void RememberWindowItem()
    {
        if (!oneItemWindow || items == null || items.Count == 0)
            return;

        int index = ItemInWindow();
        itemInWindow = index;
        syncedWindowItem = index;
    }

    int ItemInWindow()
    {
        int count = items.Count;
        float step = 360f / count;
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + rotation + step * i;
            float distance = Mathf.Abs(Mathf.DeltaAngle(angle, focusAngle));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    void EnsureTouchMirror()
    {
        if (!showOnTouchScreen)
        {
            if (touchMirror != null)
                touchMirror.gameObject.SetActive(false);
            return;
        }

        Canvas canvas = FindCanvas("TouchScreen_Canvas");
        if (canvas == null)
            return;

        if (touchMirror == null)
        {
            Transform existing = canvas.transform.Find("CircularMenuTouch");
            if (existing != null)
                touchMirror = existing as RectTransform;
        }

        if (touchMirror == null)
        {
            touchMirror = CreateRect("CircularMenuTouch", canvas.transform);
            Stretch(touchMirror);
        }

        touchMirror.gameObject.SetActive(true);
        Stretch(touchMirror);

        Image image = touchMirror.GetComponent<Image>();
        if (image == null)
            image = touchMirror.gameObject.AddComponent<Image>();
        image.sprite = PixelSprite();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        CircularMenuSurface surface = touchMirror.GetComponent<CircularMenuSurface>();
        if (surface == null)
            surface = touchMirror.gameObject.AddComponent<CircularMenuSurface>();
        surface.menu = this;
        EnsureTouchActions();
    }

    void EnsureTouchActions()
    {
        if (touchMirror == null)
            return;

        discoverButton = EnsureHubButton(discoverButton, "DiscoverButton", "Tap to discover", new Vector2(0f, 168f), true);
        backButton = EnsureHubButton(backButton, "BackButton", "Back", new Vector2(0f, 72f), false);
        discoverButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        discoverButton.onClick.AddListener(DiscoverWindowItem);
        backButton.onClick.AddListener(Back);
        discoverButton.transform.SetAsLastSibling();
        backButton.transform.SetAsLastSibling();
    }

    Button EnsureHubButton(Button existing, string objectName, string caption, Vector2 position, bool filled)
    {
        if (existing == null)
        {
            Transform found = touchMirror.Find(objectName);
            if (found != null)
                existing = found.GetComponent<Button>();
        }

        bool created = existing == null;
        if (existing == null)
        {
            RectTransform rect = CreateRect(objectName, touchMirror);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = SquareSprite();
            existing = rect.gameObject.AddComponent<Button>();
            existing.targetGraphic = image;

            RectTransform labelRect = CreateRect("Label", rect);
            Stretch(labelRect);
            Text label = labelRect.gameObject.AddComponent<Text>();
            label.font = BuiltinFont();
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 46;
            label.raycastTarget = false;

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 80f);
            rect.anchoredPosition = position;
        }

        RectTransform buttonRect = existing.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(420f, 80f);
        buttonRect.anchoredPosition = position;

        CircularMenuSurface hubDrag = existing.GetComponent<CircularMenuSurface>();
        if (hubDrag != null)
        {
            if (Application.isPlaying)
                Destroy(hubDrag);
            else
                DestroyImmediate(hubDrag);
        }

        Image plate = existing.GetComponent<Image>();
        Text text = existing.GetComponentInChildren<Text>(true);
        if (niStyle)
        {
            if (text != null)
                text.text = caption;
            return existing;
        }

        if (filled)
        {
            plate.color = new Color(0.102f, 0.161f, 0.310f, 0.94f);
            if (text != null)
                text.color = Color.white;
        }
        else
        {
            plate.color = new Color(0.933f, 0.835f, 0.600f, 0.96f);
            if (text != null)
                text.color = new Color(0.102f, 0.161f, 0.310f, 1f);
        }

        if (text != null)
            text.text = caption;

        return existing;
    }

    public void DiscoverWindowItem()
    {
        if (SuppressClick || items == null || items.Count == 0)
            return;

        if (discoverHandler != null)
        {
            discoverHandler(ItemInWindow());
            return;
        }

        Select(ItemInWindow());
    }

    public void Back()
    {
        if (SuppressClick)
            return;

        if (backHandler != null)
        {
            backHandler();
            return;
        }

        ClearContent();
        RefreshItemContext();
    }

    void EnsureContentUi()
    {
        Canvas canvas = FindCanvas("ContentDisplay_Canvas");
        if (canvas == null)
            return;

        Image screen = FindChromeImage(canvas.transform, "ContentBackdrop");
        if (screen == null)
        {
            screen = CreateChromeImage(canvas.transform, "ContentBackdrop");
            Stretch(screen.rectTransform);
            screen.color = new Color(0.008f, 0.016f, 0.039f, 1f);
            screen.raycastTarget = false;
            screen.transform.SetAsFirstSibling();
        }

        bool createdImage = contentImage == null;
        if (contentImage == null)
        {
            Transform existing = canvas.transform.Find("MenuContentImage");
            if (existing != null)
                contentImage = existing.GetComponent<Image>();
        }

        if (contentImage == null)
        {
            RectTransform rect = CreateRect("MenuContentImage", canvas.transform);
            contentImage = rect.gameObject.AddComponent<Image>();
            createdImage = true;
        }

        if (createdImage)
        {
            RectTransform imageRect = contentImage.rectTransform;
            imageRect.anchorMin = new Vector2(0.04f, 0.18f);
            imageRect.anchorMax = new Vector2(0.42f, 0.88f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            contentImage.preserveAspect = true;
            contentImage.raycastTarget = false;
            contentImage.color = Color.white;
        }

        if (contentLabel == null)
        {
            Transform existing = canvas.transform.Find("MenuContentLabel");
            if (existing != null)
                contentLabel = existing.GetComponent<Text>();
        }

        bool createdLabel = contentLabel == null;
        if (contentLabel == null)
        {
            RectTransform rect = CreateRect("MenuContentLabel", canvas.transform);
            contentLabel = rect.gameObject.AddComponent<Text>();
        }

        if (createdLabel)
        {
            RectTransform labelRect = contentLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0.46f, 0.42f);
            labelRect.anchorMax = new Vector2(0.96f, 0.82f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            contentLabel.alignment = TextAnchor.MiddleLeft;
            contentLabel.fontSize = 42;
            contentLabel.color = Color.white;
            contentLabel.raycastTarget = false;
            contentLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentLabel.verticalOverflow = VerticalWrapMode.Overflow;
            contentLabel.resizeTextForBestFit = true;
            contentLabel.resizeTextMinSize = 24;
            contentLabel.resizeTextMaxSize = 52;
            contentLabel.text = idleHeadline;
        }

        if (contentLabel.font == null)
            contentLabel.font = BuiltinFont();

        if (contentSubtitle == null)
        {
            Transform existingSubtitle = canvas.transform.Find("MenuContentSubtitle");
            if (existingSubtitle != null)
                contentSubtitle = existingSubtitle.GetComponent<Text>();
        }

        if (contentSubtitle == null)
        {
            RectTransform rect = CreateRect("MenuContentSubtitle", canvas.transform);
            contentSubtitle = rect.gameObject.AddComponent<Text>();
            rect.anchorMin = new Vector2(0.46f, 0.22f);
            rect.anchorMax = new Vector2(0.96f, 0.42f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            contentSubtitle.font = BuiltinFont();
            contentSubtitle.alignment = TextAnchor.UpperLeft;
            contentSubtitle.fontSize = 24;
            contentSubtitle.color = new Color(0.725f, 0.769f, 0.871f, 1f);
            contentSubtitle.raycastTarget = false;
            contentSubtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentSubtitle.text = idleSubtitle;
        }

        if (contentHost == null)
        {
            Transform existing = canvas.transform.Find("MenuContentHost");
            if (existing != null)
                contentHost = existing as RectTransform;
        }

        if (contentHost == null)
            contentHost = CreateRect("MenuContentHost", canvas.transform);

        Stretch(contentHost);
        contentHost.SetAsLastSibling();
    }

    bool SyncButtons(RectTransform root, List<CircularMenuButton> buffer)
    {
        buffer.Clear();
        if (root == null || items == null)
            return false;

        var found = new List<CircularMenuButton>();
        for (int i = 0; i < root.childCount; i++)
        {
            CircularMenuButton button = root.GetChild(i).GetComponent<CircularMenuButton>();
            if (button != null)
                found.Add(button);
        }

        bool changed = found.Count != items.Count;
        while (found.Count > items.Count)
        {
            CircularMenuButton extra = found[found.Count - 1];
            found.RemoveAt(found.Count - 1);
            DestroyObject(extra.gameObject);
        }

        while (found.Count < items.Count)
            found.Add(CreateButton(root));

        buffer.AddRange(found);
        return changed;
    }

    CircularMenuButton CreateButton(RectTransform parent)
    {
        RectTransform rect = CreateRect("Button", parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image background = rect.gameObject.AddComponent<Image>();
        background.sprite = SquareSprite();
        background.type = Image.Type.Simple;
        background.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        RectTransform iconRect = CreateRect("Icon", rect);
        iconRect.anchorMin = new Vector2(0.08f, 0.08f);
        iconRect.anchorMax = new Vector2(0.92f, 0.92f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.enabled = false;

        RectTransform labelRect = CreateRect("Label", rect);
        Stretch(labelRect);
        Text label = labelRect.gameObject.AddComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.color = Color.white;
        label.font = BuiltinFont();
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 18;
        label.resizeTextMaxSize = 64;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        CircularMenuButton itemButton = rect.gameObject.AddComponent<CircularMenuButton>();
        itemButton.Setup(background, icon, label, button);
        return itemButton;
    }

    void ApplyVisuals(List<CircularMenuButton> buttons, bool wheel)
    {
        int notch = wheel ? LedNotchIndex() : -1;
        for (int i = 0; i < buttons.Count; i++)
        {
            CircleItem item = items[i];
            string label = item != null && !string.IsNullOrEmpty(item.name) ? item.name : "Button " + (i + 1);
            if (buttons[i].gameObject.name != label)
                buttons[i].gameObject.name = label;

            if (niStyle)
            {
                buttons[i].BindNI(this, i, item != null ? item.buttonImage : null, label, i == SelectedIndex);
                continue;
            }

            bool atNotch = i == notch;
            Color color = atNotch
                ? new Color(0.933f, 0.835f, 0.600f, 1f)
                : new Color(0.169f, 0.255f, 0.463f, 1f);
            if (!wheel)
                color = Palette[i % Palette.Length];

            buttons[i].Bind(this, i, item != null ? item.buttonImage : null, color, label, wheel);
        }
    }

    int LedNotchIndex()
    {
        if (items == null || items.Count == 0)
            return -1;

        float step = 360f / items.Count;
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < items.Count; i++)
        {
            float angle = startAngle + ledRotation + step * i;
            float distance = Mathf.Abs(Mathf.DeltaAngle(angle, -90f));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    void LayoutButtons(List<CircularMenuButton> buttons, float usedRadius, float usedSize, float usedRotation, float upright)
    {
        int count = buttons.Count;
        if (count == 0)
            return;

        float step = 360f / count;
        Vector2 size = new Vector2(usedSize, usedSize);
        for (int i = 0; i < count; i++)
        {
            float degrees = startAngle + usedRotation + step * i;
            float radians = degrees * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * usedRadius;
            float scale = i == SelectedIndex ? selectedScale : 1f;
            buttons[i].Place(position, size, scale, upright);
        }
    }

    float ScaleTo(RectTransform space, float value)
    {
        float from = ShortSide((RectTransform)transform);
        float to = ShortSide(space);
        if (from < 1f || to < 1f)
            return value;
        return value * (to / from);
    }

    static float ShortSide(RectTransform rect)
    {
        if (rect == null)
            return 0f;
        return Mathf.Min(Mathf.Abs(rect.rect.width), Mathf.Abs(rect.rect.height));
    }

    Transform ContentCanvasTransform()
    {
        Canvas canvas = FindCanvas("ContentDisplay_Canvas");
        return canvas != null ? canvas.transform : transform;
    }

    static Canvas FindCanvas(string canvasName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == canvasName)
                return canvases[i];
        }

        return null;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = parent != null ? parent.gameObject.layer : 5;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void DestroyObject(GameObject go)
    {
        if (go == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(go);
            return;
        }

        ClearEditorSelection(go);
        DestroyImmediate(go);
    }

    static void ClearEditorSelection(GameObject go)
    {
#if UNITY_EDITOR
        if (go == null)
            return;

        UnityEngine.Object[] selected = UnityEditor.Selection.objects;
        bool selectedDestroyedObject = false;
        for (int i = 0; i < selected.Length; i++)
        {
            GameObject selectedObject = selected[i] as GameObject;
            Component selectedComponent = selected[i] as Component;
            if (selectedComponent != null)
                selectedObject = selectedComponent.gameObject;

            if (selectedObject != null && (selectedObject == go || selectedObject.transform.IsChildOf(go.transform)))
                selectedDestroyedObject = true;
        }

        if (selectedDestroyedObject)
            UnityEditor.Selection.activeObject = null;
#else
        if (go == null)
            return;
#endif
    }

    static List<CircleItem> DefaultItems()
    {
        var list = new List<CircleItem>();
        for (int i = 0; i < 6; i++)
            list.Add(new CircleItem { name = "Item " + (i + 1) });
        return list;
    }

    public static Sprite SquareSprite()
    {
        return PixelSprite();
    }

    public static Sprite DiscSprite()
    {
        if (discSprite != null)
            return discSprite;

        Sprite asset = Resources.Load<Sprite>("NationalInitiative/ui_disc");
        if (asset != null)
        {
            discSprite = asset;
            return discSprite;
        }

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        float radiusPixels = size * 0.5f - 0.5f;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radiusPixels - distance + 0.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        discSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        discSprite.name = "CircleMenuDisc";
        discSprite.hideFlags = HideFlags.HideAndDontSave;
        return discSprite;
    }

    static Sprite PixelSprite()
    {
        if (pixelSprite != null)
            return pixelSprite;

        Sprite pixelAsset = Resources.Load<Sprite>("NationalInitiative/ui_pixel");
        if (pixelAsset != null)
        {
            pixelSprite = pixelAsset;
            return pixelSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.hideFlags = HideFlags.HideAndDontSave;
        return pixelSprite;
    }

    static Font BuiltinFont()
    {
        if (builtinFont != null)
            return builtinFont;

        builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont == null)
            builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return builtinFont;
    }

    void RequestEditorRefresh()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || editorRefreshQueued)
            return;

        editorRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            editorRefreshQueued = false;
            if (this == null)
                return;

            RebuildNow();
        };
#endif
    }
}

public class CircularMenuSurface : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public CircularMenu menu;

    public void OnPointerDown(PointerEventData eventData)
    {
        NIPressFeedback.RaisePress(eventData);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (menu != null)
            menu.PrepareDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (menu != null)
            menu.BeginDrag(eventData, (RectTransform)transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (menu != null)
            menu.Drag(eventData, (RectTransform)transform);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (menu != null)
            menu.EndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (menu != null)
            menu.Scroll(eventData);
    }
}

public class CircularMenuButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    CircularMenu menu;
    Image face;
    Image faceSelected;
    Image halo;
    bool niSelected;
    bool niSelectedKnown;
    bool niPressed;
    Tween faceTween;
    Image background;
    Image icon;
    Text label;
    Button button;
    int index;

    public int ItemIndex
    {
        get { return index; }
    }

    public void Setup(Image backgroundImage, Image iconImage, Text labelText, Button buttonControl)
    {
        background = backgroundImage;
        icon = iconImage;
        label = labelText;
        button = buttonControl;
    }

    public void Bind(CircularMenu owner, int itemIndex, Sprite sprite, Color color, string itemName, bool wheel)
    {
        menu = owner;
        index = itemIndex;

        if (background == null)
            background = GetComponent<Image>();

        if (background != null)
        {
            Mask mask = background.GetComponent<Mask>();
            if (mask != null)
            {
                if (Application.isPlaying)
                    Destroy(mask);
                else
                    DestroyImmediate(mask);
            }

            background.type = Image.Type.Simple;
            if (wheel || sprite == null)
            {
                background.sprite = CircularMenu.DiscSprite();
                background.color = color;
                background.preserveAspect = true;
            }
            else
            {
                background.sprite = sprite;
                background.color = Color.white;
                background.preserveAspect = true;
            }
        }

        if (icon == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                icon = iconTransform.GetComponent<Image>();
        }

        if (icon != null)
        {
            icon.sprite = wheel ? sprite : null;
            icon.enabled = wheel && sprite != null;
            icon.color = Color.white;
        }

        if (label != null)
        {
            label.text = itemName;
            label.enabled = wheel || sprite == null;
            float luminance = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
            label.color = luminance > 0.72f ? new Color(0.1f, 0.1f, 0.12f, 1f) : Color.white;
        }

        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (menu == null || !menu.niStyle || face == null || !Application.isPlaying)
            return;

        niPressed = true;
        if (faceTween != null)
            faceTween.Kill();
        faceTween = face.rectTransform.DOScale(0.92f, 0.12f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        if (NIAudio.Instance != null)
            NIAudio.Instance.PlayVaried("asmr_press", 0.95f, 1.05f, 0.9f);
        NIPressFeedback.RaisePress(eventData);
    }

    void SettleFace()
    {
        if (face == null || !Application.isPlaying)
            return;

        niPressed = false;
        if (faceTween != null)
            faceTween.Kill();
        faceTween = face.rectTransform.DOScale(niSelected ? 1.05f : 1f, 0.6f).SetEase(Ease.OutBack, 2.4f).SetUpdate(true).SetLink(gameObject);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SettleFace();
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        HandleClick();
    }

    /// <summary>Round navy disc with a white icon and a label, gold when it is the selected item.</summary>
    public void BindNI(CircularMenu owner, int itemIndex, Sprite sprite, string itemName, bool selected)
    {
        menu = owner;
        index = itemIndex;

        if (background == null)
            background = GetComponent<Image>();
        if (label == null)
        {
            Transform labelTransform = transform.Find("Label");
            label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        }

        if (button == null)
            button = GetComponent<Button>();

        Sprite disc = owner.niDisc != null ? owner.niDisc : CircularMenu.DiscSprite();
        if (background != null)
        {
            Mask mask = background.GetComponent<Mask>();
            if (mask != null)
            {
                if (Application.isPlaying)
                    Destroy(mask);
                else
                    DestroyImmediate(mask);
            }

            background.type = Image.Type.Simple;
            background.sprite = disc;
            background.preserveAspect = true;
            background.color = selected ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.30f);
        }

        // soft halo behind the face
        if (halo == null)
        {
            Transform haloTransform = transform.Find("Halo");
            halo = haloTransform != null ? haloTransform.GetComponent<Image>() : null;
        }

        if (halo == null)
        {
            GameObject haloObject = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            haloObject.layer = gameObject.layer;
            haloObject.transform.SetParent(transform, false);
            halo = haloObject.GetComponent<Image>();
            halo.raycastTarget = false;
            RectTransform haloRect = (RectTransform)haloObject.transform;
            haloRect.anchorMin = new Vector2(-0.26f, -0.26f);
            haloRect.anchorMax = new Vector2(1.26f, 1.26f);
            haloRect.offsetMin = Vector2.zero;
            haloRect.offsetMax = Vector2.zero;
            haloObject.transform.SetAsFirstSibling();
            NIBreathe breathe = haloObject.AddComponent<NIBreathe>();
            breathe.scaleTo = 1.07f;
            breathe.period = 3.6f;
        }

        Sprite haloSprite = owner.niHalo != null ? owner.niHalo : NIUI.Halo();
        if (halo.sprite != haloSprite)
            halo.sprite = haloSprite;

        // The big touch buttons sit in a clipped window, so their glow stays close to the disc.
        float spread = ((RectTransform)transform).sizeDelta.x > 400f ? owner.touchHaloSpread : owner.ledHaloSpread;
        RectTransform haloTransform2 = halo.rectTransform;
        haloTransform2.anchorMin = new Vector2(-spread, -spread);
        haloTransform2.anchorMax = new Vector2(1f + spread, 1f + spread);

        if (face == null)
        {
            Transform faceTransform = transform.Find("Face");
            face = faceTransform != null ? faceTransform.GetComponent<Image>() : null;
        }

        if (face == null)
        {
            GameObject faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));
            faceObject.layer = gameObject.layer;
            faceObject.transform.SetParent(transform, false);
            face = faceObject.GetComponent<Image>();
            face.raycastTarget = false;
            RectTransform faceRect = (RectTransform)faceObject.transform;
            faceRect.anchorMin = new Vector2(0.018f, 0.018f);
            faceRect.anchorMax = new Vector2(0.982f, 0.982f);
            faceRect.offsetMin = Vector2.zero;
            faceRect.offsetMax = Vector2.zero;
            faceObject.transform.SetSiblingIndex(1);
        }

        Sprite faceSprite = owner.niFace != null ? owner.niFace : NIUI.OptFace();
        Sprite selectedSprite = owner.niFaceSelected != null ? owner.niFaceSelected : NIUI.OptFaceSel();
        if (face.sprite != faceSprite)
            face.sprite = faceSprite;
        face.color = Color.white;

        // gold face of the selected item sits above the blue one and fades in
        if (faceSelected == null)
        {
            Transform selTransform = face.transform.Find("FaceSelected");
            faceSelected = selTransform != null ? selTransform.GetComponent<Image>() : null;
        }

        if (faceSelected == null)
        {
            GameObject selObject = new GameObject("FaceSelected", typeof(RectTransform), typeof(Image));
            selObject.layer = gameObject.layer;
            selObject.transform.SetParent(face.transform, false);
            faceSelected = selObject.GetComponent<Image>();
            faceSelected.raycastTarget = false;
            RectTransform selRect = (RectTransform)selObject.transform;
            selRect.anchorMin = Vector2.zero;
            selRect.anchorMax = Vector2.one;
            selRect.offsetMin = Vector2.zero;
            selRect.offsetMax = Vector2.zero;
        }

        if (faceSelected.sprite != selectedSprite)
            faceSelected.sprite = selectedSprite;

        float haloAlpha = ((RectTransform)transform).sizeDelta.x > 400f ? owner.touchHaloOpacity : owner.haloOpacity;
        Color haloColor = new Color(owner.haloColor.r, owner.haloColor.g, owner.haloColor.b, selected ? haloAlpha : 0f);
        Color selectedColor = new Color(1f, 1f, 1f, selected ? 1f : 0f);
        if (!Application.isPlaying)
        {
            // while editing there is no animation, so always show the true state
            faceSelected.color = selectedColor;
            halo.color = haloColor;
        }

        if (!niSelectedKnown || selected != niSelected)
        {
            bool animate = Application.isPlaying && niSelectedKnown;
            niSelected = selected;
            niSelectedKnown = true;
            if (animate)
            {
                faceSelected.DOKill();
                halo.DOKill();
                faceSelected.DOColor(selectedColor, 0.5f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(faceSelected.gameObject);
                halo.DOColor(haloColor, 0.6f).SetUpdate(true).SetLink(halo.gameObject);
                if (!niPressed)
                {
                    if (faceTween != null)
                        faceTween.Kill();
                    faceTween = face.rectTransform.DOScale(selected ? 1.05f : 1f, 0.7f).SetEase(Ease.OutBack, 2f).SetUpdate(true).SetLink(gameObject);
                }
            }
            else
            {
                faceSelected.color = selectedColor;
                halo.color = haloColor;
                face.rectTransform.localScale = Vector3.one * (selected ? 1.05f : 1f);
            }
        }

        Color ink = selected ? new Color(0.102f, 0.161f, 0.310f, 1f) : Color.white;

        if (icon == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                icon = iconTransform.GetComponent<Image>();
        }

        if (icon != null)
        {
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.27f, 0.47f);
            iconRect.anchorMax = new Vector2(0.73f, 0.85f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.preserveAspect = true;
            icon.color = ink;
        }

        if (label != null)
        {
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.1f, 0.1f);
            labelRect.anchorMax = new Vector2(0.9f, 0.46f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            bool big = ((RectTransform)transform).sizeDelta.x > 400f;
            label.alignment = TextAnchor.UpperCenter;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = big ? owner.touchLabelMinSize : owner.ledLabelMinSize;
            label.resizeTextMaxSize = big ? owner.touchLabelMaxSize : owner.ledLabelMaxSize;
            label.enabled = true;
            label.text = itemName;
            label.color = ink;
        }

        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    public void Place(Vector2 anchoredPosition, Vector2 size, float scale, float uprightDegrees)
    {
        RectTransform rect = (RectTransform)transform;
        if ((rect.anchoredPosition - anchoredPosition).sqrMagnitude > 0.01f)
            rect.anchoredPosition = anchoredPosition;
        if ((rect.sizeDelta - size).sqrMagnitude > 0.01f)
            rect.sizeDelta = size;

        Vector3 nextScale = new Vector3(scale, scale, 1f);
        if ((rect.localScale - nextScale).sqrMagnitude > 0.0001f)
            rect.localScale = nextScale;

        Quaternion upright = Quaternion.Euler(0f, 0f, uprightDegrees);
        if (rect.localRotation != upright)
            rect.localRotation = upright;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (menu != null)
            menu.PrepareDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CircularMenu owner = Owner();
        if (owner != null)
            owner.BeginDrag(eventData, (RectTransform)transform.parent);
    }

    public void OnDrag(PointerEventData eventData)
    {
        CircularMenu owner = Owner();
        if (owner != null)
            owner.Drag(eventData, (RectTransform)transform.parent);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CircularMenu owner = Owner();
        if (owner != null)
            owner.EndDrag(eventData);
    }

    CircularMenu Owner()
    {
        if (menu == null)
            menu = GetComponentInParent<CircularMenu>();
        return menu;
    }

    void HandleClick()
    {
        if (menu != null && !menu.SuppressClick)
            menu.Select(index);
    }
}
