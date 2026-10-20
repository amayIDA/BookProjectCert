using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns the old three display setup (LeftCanvas, RightCanvas, MainCanvas, three cameras) into two displays:
/// display 1 is one SpreadCanvas that holds the left and right pages together, display 2 is the MainCanvas.
/// Nothing on the pages moves: LeftCanvas and RightCanvas keep their names and their PagePanels, they just become
/// the two halves of the spread instead of separate canvases.
/// The background is TwoPagesTrim.png: TwoPages.png with its white margin cut off, so the spread fills the whole screen.
/// </summary>
public static class SpreadSetup
{
    const string TwoPagesPath = "Assets/UI/TwoPagesTrim.png";
    static readonly Vector2 SpreadSize = new Vector2(2080f, 1704f);

    /// <summary>
    /// Makes the spread fill the whole screen: the trimmed background replaces the old one and the two pages stretch
    /// over the full canvas, so there is no white around it on any screen shape.
    /// </summary>
    [MenuItem("Book/Setup/Fit Spread To Screen")]
    public static void FitToScreen()
    {
        GameObject root = GameObject.Find("SpreadCanvas");
        if (root == null)
        {
            Debug.LogError("Run Book/Setup/Merge Left And Right Into One Spread first.");
            return;
        }

        Sprite leftSprite;
        Sprite rightSprite;
        if (!SliceTwoPages(out leftSprite, out rightSprite))
            return;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = SpreadSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        RectTransform area = (RectTransform)root.transform.Find("Spread");
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.pivot = new Vector2(0.5f, 0.5f);
        area.offsetMin = Vector2.zero;
        area.offsetMax = Vector2.zero;

        SetPanel(GameObject.Find("LeftCanvas"), leftSprite);
        SetPanel(GameObject.Find("RightCanvas"), rightSprite);

        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log("The spread now fills the screen.");
    }

    /// <summary>A canvas object sits at some depth from its camera. A half must sit on the spread plane, or it is drawn smaller.</summary>
    static void FlattenDepth(GameObject half)
    {
        if (half == null)
            return;
        Transform t = half.transform;
        Vector3 position = t.localPosition;
        position.z = 0f;
        t.localPosition = position;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    static void SetPanel(GameObject half, Sprite sprite)
    {
        FlattenDepth(half);
        Transform panel = half != null ? half.transform.Find("Panel") : null;
        Image image = panel != null ? panel.GetComponent<Image>() : null;
        if (image == null)
            return;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }

    [MenuItem("Book/Setup/Merge Left And Right Into One Spread")]
    public static void Merge()
    {
        GameObject leftGo = GameObject.Find("LeftCanvas");
        GameObject rightGo = GameObject.Find("RightCanvas");
        GameObject mainGo = GameObject.Find("MainCanvas");
        if (leftGo == null || rightGo == null || mainGo == null)
        {
            Debug.LogError("Spread setup needs LeftCanvas, RightCanvas and MainCanvas.");
            return;
        }

        if (GameObject.Find("SpreadCanvas") != null)
        {
            Debug.Log("The spread is already set up. Use Book/Setup/Refit Spread Pages to change how the pages sit.");
            return;
        }

        Sprite leftSprite;
        Sprite rightSprite;
        if (!SliceTwoPages(out leftSprite, out rightSprite))
            return;

        // ---- cameras: one for the spread, one for the main page ----
        Canvas leftCanvas = leftGo.GetComponent<Canvas>();
        Camera spreadCamera = leftCanvas != null ? leftCanvas.worldCamera : null;
        if (spreadCamera == null)
            spreadCamera = GameObject.Find("LeftCamera").GetComponent<Camera>();
        spreadCamera.gameObject.name = "SpreadCamera";
        spreadCamera.targetDisplay = 0;
        spreadCamera.clearFlags = CameraClearFlags.SolidColor;
        spreadCamera.backgroundColor = Color.white;

        Canvas rightCanvas = rightGo.GetComponent<Canvas>();
        GameObject rightCamera = rightCanvas != null && rightCanvas.worldCamera != null ? rightCanvas.worldCamera.gameObject : GameObject.Find("RightCamera");
        Canvas mainCanvas = mainGo.GetComponent<Canvas>();
        if (mainCanvas != null && mainCanvas.worldCamera != null)
            mainCanvas.worldCamera.targetDisplay = 1;

        // ---- the spread canvas ----
        GameObject root = new GameObject("SpreadCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = leftGo.layer;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = spreadCamera;
        canvas.planeDistance = leftCanvas != null ? leftCanvas.planeDistance : 100f;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = SpreadSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.transform.SetSiblingIndex(leftGo.transform.GetSiblingIndex());

        // the two pages together, exactly as big as the background picture
        GameObject area = new GameObject("Spread", typeof(RectTransform));
        area.layer = leftGo.layer;
        RectTransform areaRect = area.GetComponent<RectTransform>();
        areaRect.SetParent(root.transform, false);
        areaRect.anchorMin = areaRect.anchorMax = new Vector2(0.5f, 0.5f);
        areaRect.pivot = new Vector2(0.5f, 0.5f);
        areaRect.sizeDelta = SpreadSize;
        areaRect.anchoredPosition = Vector2.zero;

        BecomeHalf(leftGo, areaRect, 0f, 0.5f, leftSprite);
        BecomeHalf(rightGo, areaRect, 0.5f, 1f, rightSprite);

        if (rightCamera != null)
            Object.DestroyImmediate(rightCamera);

        EditorSceneManager.MarkSceneDirty(leftGo.scene);
        EditorSceneManager.SaveScene(leftGo.scene);
        Debug.Log("Spread canvas ready: display 1 shows both pages (one camera), display 2 shows the main page (one camera).");
    }

    static void BecomeHalf(GameObject go, RectTransform area, float x0, float x1, Sprite background)
    {
        // a half is no longer a canvas of its own
        GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            Object.DestroyImmediate(raycaster);
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        if (scaler != null)
            Object.DestroyImmediate(scaler);
        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas != null)
            Object.DestroyImmediate(canvas);

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(area, false);
        rect.anchorMin = new Vector2(x0, 0f);
        rect.anchorMax = new Vector2(x1, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        FlattenDepth(go);

        Transform panel = go.transform.Find("Panel");
        if (panel != null)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = background;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
                image.raycastTarget = false;
            }
        }

        Transform pages = go.transform.Find("PagePanels");
        if (pages != null)
        {
            AspectRatioFitter fitter = pages.GetComponent<AspectRatioFitter>();
            if (fitter != null)
                Object.DestroyImmediate(fitter);
            RectTransform pagesRect = (RectTransform)pages;
            pagesRect.anchorMin = Vector2.zero;
            pagesRect.anchorMax = Vector2.one;
            pagesRect.pivot = new Vector2(0.5f, 0.5f);
            pagesRect.offsetMin = Vector2.zero;
            pagesRect.offsetMax = Vector2.zero;
            pagesRect.localScale = Vector3.one;
        }
    }

    /// <summary>Cuts TwoPages.png into its left and right page so each half can keep its own Panel image.</summary>
    static bool SliceTwoPages(out Sprite left, out Sprite right)
    {
        left = null;
        right = null;
        TextureImporter importer = AssetImporter.GetAtPath(TwoPagesPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError(TwoPagesPath + " was not found.");
            return false;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TwoPagesPath);
        float width = texture.width;
        float height = texture.height;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.spritePixelsPerUnit = 100f;
        importer.spritesheet = new[]
        {
            new SpriteMetaData { name = "TwoPagesTrim_Left", rect = new Rect(0f, 0f, width * 0.5f, height), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f) },
            new SpriteMetaData { name = "TwoPagesTrim_Right", rect = new Rect(width * 0.5f, 0f, width * 0.5f, height), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f) }
        };
        importer.SaveAndReimport();

        foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(TwoPagesPath))
        {
            Sprite sprite = asset as Sprite;
            if (sprite == null)
                continue;
            if (sprite.name == "TwoPagesTrim_Left")
                left = sprite;
            else if (sprite.name == "TwoPagesTrim_Right")
                right = sprite;
        }

        if (left == null || right == null)
        {
            Debug.LogError("Could not cut " + TwoPagesPath + " into two pages.");
            return false;
        }

        return true;
    }
}
