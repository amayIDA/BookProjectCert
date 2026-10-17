using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fits each canvas to its display, then applies placement and sizes from
/// StreamingAssets/display-layout.json when that file exists.
/// Edit the JSON next to the built player and restart. No rebuild required.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class DisplayLayoutConfig : MonoBehaviour
{
    const string FileName = "display-layout.json";

    [Serializable]
    public class ScreenSize
    {
        public int width = 1080;
        public int height = 1080;
    }

    [Serializable]
    public class LayoutFile
    {
        public ScreenSize touch = new ScreenSize { width = 1080, height = 1080 };
        public ScreenSize led = new ScreenSize { width = 1032, height = 946 };
        public ScreenSize content = new ScreenSize { width = 1536, height = 1024 };
        public MenuLayout menu = new MenuLayout();
    }

    [Serializable]
    public class MenuLayout
    {
        public float radius = 350f;
        public float buttonSize = 190f;
        public float touchRadius = 1400f;
        public float touchButtonSize = 780f;
        public float[] touchWindowSize = { 960f, 960f };
        public float[] touchWindowPosition = { 0f, 0f };
        public float contentPadding = 72f;
        public float[] contentLabelSize = { 1400f, 160f };
        public float[] contentLabelOffset = { 0f, -420f };
        public float itemContextSize = 280f;
    }

    public string touchCanvasName = "TouchScreen_Canvas";
    public string ledCanvasName = "LargeLEDCircle_Canvas";
    public string contentCanvasName = "ContentDisplay_Canvas";

    public LayoutFile layout = new LayoutFile();

    [Tooltip("When on, a display-layout.json file next to the build replaces these values.")]
    public bool loadJsonInPlayer = true;

    void OnEnable()
    {
        Apply();
    }

    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        LayoutFile file = layout;
        if (loadJsonInPlayer)
            file = ReadFile() ?? layout;

        FitCanvas(touchCanvasName, file.touch);
        FitCanvas(ledCanvasName, file.led);
        FitCanvas(contentCanvasName, file.content);
        ApplyMenu(file.menu);
    }

    static void FitCanvas(string canvasName, ScreenSize size)
    {
        if (size == null || size.width < 1 || size.height < 1)
            return;

        Canvas canvas = FindCanvas(canvasName);
        if (canvas == null)
            return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        // Only touch the scaler when something differs, so opening the scene does not mark it as unsaved.
        Vector2 reference = new Vector2(size.width, size.height);
        bool same = scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                    && scaler.referenceResolution == reference
                    && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                    && Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f);
        if (same)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = reference;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.enabled = false;
        scaler.enabled = true;
        Canvas.ForceUpdateCanvases();
    }

    static void ApplyMenu(MenuLayout menu)
    {
        if (menu == null)
            return;

        CircularMenu circle = FindAnyObjectByType<CircularMenu>();
        if (circle == null)
            return;

        circle.radius = menu.radius;
        circle.buttonSize = Mathf.Max(1f, menu.buttonSize);
        circle.touchRadius = Mathf.Max(1f, menu.touchRadius);
        circle.touchButtonSize = Mathf.Max(1f, menu.touchButtonSize);
        circle.touchWindowSize = Vec(menu.touchWindowSize, circle.touchWindowSize);
        circle.touchWindowPosition = Vec(menu.touchWindowPosition, circle.touchWindowPosition);
        circle.contentPadding = menu.contentPadding;
        circle.contentLabelSize = Vec(menu.contentLabelSize, circle.contentLabelSize);
        circle.contentLabelOffset = Vec(menu.contentLabelOffset, circle.contentLabelOffset);
        circle.itemContextSize = Mathf.Max(1f, menu.itemContextSize);
        circle.RebuildNow();
    }

    static Vector2 Vec(float[] values, Vector2 fallback)
    {
        if (values == null || values.Length < 2)
            return fallback;
        return new Vector2(values[0], values[1]);
    }

    static LayoutFile ReadFile()
    {
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonUtility.FromJson<LayoutFile>(File.ReadAllText(path));
        }
        catch (Exception error)
        {
            Debug.LogWarning("Could not read " + path + ": " + error.Message);
            return null;
        }
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
}
