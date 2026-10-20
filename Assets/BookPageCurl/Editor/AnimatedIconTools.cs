using System.Collections.Generic;
using MotionStudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Swaps the picture icons for the animated (transparent video) icons in Assets/UI/Animated Icons.
///   Small icons play once when they appear. Large icons loop and drift slowly up and down (SoftBob).
/// Every video is kept twice as WebM with alpha: full size for the large icons, a small copy for the small ones.
/// </summary>
public static class AnimatedIconTools
{
    const string Folder = "Assets/UI/Animated Icons/Web/";
    const string MaterialPath = "Assets/BookPageCurl/Shaders/VideoIcon.mat";
    const float FrameWidth = 794f, FrameHeight = 642f;
    public static readonly Color Navy = new Color(0.17f, 0.22f, 0.42f, 1f);

    // the area of the frame the icon ever occupies, in pixels (x0, y0, x1, y1 from the top left), measured over the whole video
    static readonly Dictionary<string, int[]> Glyphs = new Dictionary<string, int[]>
    {
        { "C-levels", new[] { 192, 115, 600, 506 } },
        { "Discover_this_book", new[] { 202, 147, 591, 477 } },
        { "Entry_level", new[] { 247, 107, 546, 512 } },
        { "Frontline", new[] { 185, 108, 609, 514 } },
        { "Functional", new[] { 179, 103, 613, 518 } },
        { "Idle", new[] { 304, 196, 490, 450 } },
        { "Managers", new[] { 151, 53, 642, 567 } },
        { "Thats_all", new[] { 151, 53, 641, 567 } },
        { "Turn_the_page", new[] { 202, 99, 593, 517 } },
    };

    public static readonly string[] ScenarioVideos = { "Entry_level", "Frontline", "Functional", "Managers", "C-levels" };

    /// <summary>One material for the full-size videos and one for the small copies (their lines are thinner in the video).</summary>
    public static Material VideoMaterial(bool large)
    {
        string path = large ? MaterialPath : MaterialPath.Replace(".mat", "Small.mat");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("BookUI/VideoIcon"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetFloat("_Recolor", 1f);
        material.SetFloat("_BlackKey", 0f);
        material.SetFloat("_SourceStroke", large ? 8f : 2.7f);
        material.SetFloat("_MinStroke", 1.8f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>The part of the video frame that holds the icon, as a RawImage uvRect, with a small margin.</summary>
    static Rect Crop(string video)
    {
        int[] g;
        if (!Glyphs.TryGetValue(video, out g))
            return new Rect(0f, 0f, 1f, 1f);
        float pad = 10f;
        float x0 = Mathf.Max(0f, g[0] - pad), x1 = Mathf.Min(FrameWidth, g[2] + pad);
        float y0 = Mathf.Max(0f, g[1] - pad), y1 = Mathf.Min(FrameHeight, g[3] + pad);
        return new Rect(x0 / FrameWidth, 1f - y1 / FrameHeight, (x1 - x0) / FrameWidth, (y1 - y0) / FrameHeight);
    }

    /// <summary>
    /// Turns an Image icon into a video icon in place: same object, same animations on it, same place. The icon is fitted inside the box
    /// the old picture used (or the box given) at the video's own proportions.
    /// </summary>
    public static AnimatedIcon Convert(RectTransform target, string video, bool large, Color tint, Vector2? box = null, bool bob = false)
    {
        string path = Folder + video + (large ? "" : "_small") + ".webm";
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
        if (clip == null)
        {
            Debug.LogError("Animated icon video not found: " + path);
            return null;
        }

        Image old = target.GetComponent<Image>();
        Sprite poster = old != null ? old.sprite : null;
        Vector2 size = box ?? target.rect.size;
        if (old != null)
            Object.DestroyImmediate(old);

        RawImage image = target.GetComponent<RawImage>();
        if (image == null)
            image = target.gameObject.AddComponent<RawImage>();
        image.material = VideoMaterial(large);
        image.color = tint;
        image.raycastTarget = false;

        // an animation on this icon remembers the old picture's colour (white for a coloured sprite) and puts it back
        // whenever the page starts; it must remember the tint instead, or the icon turns white on a white page
        foreach (MotionItem item in target.GetComponents<MotionItem>())
        {
            item.restColor = tint;
            if (item.changeColor)
                item.colorTo = tint;
            EditorUtility.SetDirty(item);
        }

        Rect crop = Crop(video);
        image.uvRect = crop;
        float aspect = crop.width * FrameWidth / (crop.height * FrameHeight);
        Vector2 fitted = size.x / size.y > aspect ? new Vector2(size.y * aspect, size.y) : new Vector2(size.x, size.x / aspect);
        if (target.anchorMin == target.anchorMax)
            target.sizeDelta = fitted;

        AnimatedIcon icon = target.GetComponent<AnimatedIcon>();
        if (icon == null)
            icon = target.gameObject.AddComponent<AnimatedIcon>();
        icon.clip = clip;
        icon.loop = large;
        icon.crop = crop;
        // the still picture is a frame of the same video, so a page copied for a page turn shows the same icon
        Sprite still = ScreenKit.ScreenPages.LoadSprite("Assets/UI/Animated Icons/Posters/" + video + ".png");
        icon.poster = still != null ? still : (poster != null ? poster : icon.poster);
        icon.startDelay = 0f;

        SoftBob drift = target.GetComponent<SoftBob>();
        if (bob && drift == null)
            drift = target.gameObject.AddComponent<SoftBob>();
        if (drift != null)
        {
            // a slow, calm drift: far enough to notice, slow enough to be restful
            drift.distance = 14f;
            drift.seconds = 4.6f;
        }

        EditorUtility.SetDirty(target.gameObject);
        return icon;
    }

    static Transform Page(string canvas, string page)
    {
        GameObject go = GameObject.Find(canvas);
        return go == null ? null : go.transform.Find("PagePanels/" + page);
    }

    [MenuItem("Book/Icons/Apply Animated Icons")]
    public static void Apply()
    {
        int count = 0;

        // ---- large icons: loop, drift slowly ----
        count += Large("RightCanvas", "Page 1", "BigIcon", "Idle");
        count += Large("RightCanvas", "Page 2", "BigIcon", "Discover_this_book");
        for (int i = 0; i < 5; i++)
            count += Large("RightCanvas", "Page " + (9 + i), "BigIcon", ScenarioVideos[i]);
        count += AddLarge("LeftCanvas", "Page 14", "Thats_all", new Vector2(0.5f, 0.5f), 520f, 0.3f);
        count += AddLarge("RightCanvas", "Page 7", "Thats_all", new Vector2(0.5f, 0.66f), 280f, 0.2f);
        count += AddLarge("RightCanvas", "Page 3", "Turn_the_page", new Vector2(0.5f, 0.57f), 480f, 0.3f);

        // ---- small icons: play once when they appear ----
        string[] leftRows = { "Entry Level", "Frontline", "Functional" };
        for (int i = 0; i < 3; i++)
            count += Small("LeftCanvas", "Page 8", leftRows[i] + "/Icon", ScenarioVideos[i], new Color(0.12f, 0.16f, 0.38f, 1f), 42f);
        string[] rightRows = { "Managers", "Executives" };
        for (int i = 0; i < 2; i++)
            count += Small("RightCanvas", "Page 8", rightRows[i] + "/Icon", ScenarioVideos[3 + i], new Color(0.12f, 0.16f, 0.38f, 1f), 42f);

        // the white icons on the main canvas question pages
        for (int q = 0; q < 15; q++)
        {
            string page = "Question " + (q / 3 + 1) + "-" + (q % 3 + 1);
            count += Small("MainCanvas", page, "Sheet/Card/Scenario icon", ScenarioVideos[q / 3], new Color(0.93f, 0.95f, 0.98f, 1f), 64f);
        }

        Debug.Log("Animated icons applied: " + count);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    static int Large(string canvas, string pageName, string objectName, string video)
    {
        Transform page = Page(canvas, pageName);
        Transform icon = page != null ? page.Find(objectName) : null;
        if (icon == null)
        {
            Debug.LogWarning("No " + canvas + "/" + pageName + "/" + objectName);
            return 0;
        }

        RectTransform rect = (RectTransform)icon;
        Convert(rect, video, true, Navy, rect.rect.size, true);
        return 1;
    }

    static int Small(string canvas, string pageName, string objectPath, string video, Color tint, float pixels)
    {
        Transform page = Page(canvas, pageName);
        Transform icon = page != null ? page.Find(objectPath) : null;
        if (icon == null)
        {
            Debug.LogWarning("No " + canvas + "/" + pageName + "/" + objectPath);
            return 0;
        }

        Convert((RectTransform)icon, video, false, tint, new Vector2(pixels, pixels), false);
        return 1;
    }

    /// <summary>A new large icon on a page that had none. It fades in with the page's own animation and then loops and drifts.</summary>
    public static int AddLarge(string canvas, string pageName, string video, Vector2 anchor, float pixels, float at)
    {
        Transform page = Page(canvas, pageName);
        if (page == null)
            return 0;
        Transform existing = page.Find("AnimatedIcon");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject go = new GameObject("AnimatedIcon", typeof(RectTransform));
        go.layer = page.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(page, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(pixels, pixels);
        rect.anchoredPosition = Vector2.zero;
        go.AddComponent<RawImage>();
        Convert(rect, video, true, Navy, new Vector2(pixels, pixels), true);

        MotionItem fade = ScreenKit.Fx.Fade(go, MotionDirection.Bottom, 36f, at, 0.8f);
        fade.order = 0;
        MotionSequence seq = page.GetComponent<MotionSequence>();
        if (seq != null)
        {
            seq.Add(fade, 0f);
            EditorUtility.SetDirty(seq);
        }

        return 1;
    }
}
