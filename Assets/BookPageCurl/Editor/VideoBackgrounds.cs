using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Puts the looping background videos behind the spread (display 1) and the main page (display 2).</summary>
public static class VideoBackgrounds
{
    const string SpreadVideo = "Assets/UI/Back_Book_Loop.mp4";
    const string MainVideo = "Assets/UI/Back_Front_Screen_Loop.mp4";

    [MenuItem("Book/Setup/Use Looping Video Backgrounds")]
    public static void Apply()
    {
        VideoClip spreadClip = AssetDatabase.LoadAssetAtPath<VideoClip>(SpreadVideo);
        VideoClip mainClip = AssetDatabase.LoadAssetAtPath<VideoClip>(MainVideo);
        GameObject left = GameObject.Find("LeftCanvas");
        GameObject right = GameObject.Find("RightCanvas");
        GameObject main = GameObject.Find("MainCanvas");
        if (spreadClip == null || mainClip == null || left == null || right == null || main == null)
        {
            Debug.LogError("Videos or canvases were not found.");
            return;
        }

        // the spread video is TwoPages.png in motion; the screen shows it without its white margin (x 40, y 123, 2080 x 1704 of 2160 x 1920)
        float u = 40f / 2160f, w = 2080f / 2160f, v = (1920f - 123f - 1704f) / 1920f, h = 1704f / 1920f;
        RawImage leftImage = Backdrop(left.transform);
        RawImage rightImage = Backdrop(right.transform);
        Transform spread = left.transform.parent;
        LoopVideo loop = Ensure(spread.gameObject);
        loop.clip = spreadClip;
        loop.cover = false;
        loop.targets = new[]
        {
            new LoopVideo.Target { image = leftImage, uv = new Rect(u, v, w * 0.5f, h) },
            new LoopVideo.Target { image = rightImage, uv = new Rect(u + w * 0.5f, v, w * 0.5f, h) }
        };

        RawImage mainImage = Backdrop(main.transform);
        LoopVideo mainLoop = Ensure(main);
        mainLoop.clip = mainClip;
        mainLoop.cover = true;
        mainLoop.targets = new[] { new LoopVideo.Target { image = mainImage } };

        EditorUtility.SetDirty(loop);
        EditorUtility.SetDirty(mainLoop);
        EditorSceneManager.MarkSceneDirty(main.scene);
        EditorSceneManager.SaveScene(main.scene);
        Debug.Log("Looping video backgrounds are set.");
    }

    static LoopVideo Ensure(GameObject go)
    {
        LoopVideo loop = go.GetComponent<LoopVideo>();
        return loop != null ? loop : go.AddComponent<LoopVideo>();
    }

    /// <summary>A RawImage right above the Panel picture, below the pages. The Panel stays: it is what the page turns are baked on.</summary>
    static RawImage Backdrop(Transform canvas)
    {
        Transform existing = canvas.Find("BackVideo");
        if (existing != null)
            return existing.GetComponent<RawImage>();
        GameObject go = new GameObject("BackVideo", typeof(RectTransform), typeof(RawImage));
        go.layer = canvas.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(canvas, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Transform panel = canvas.Find("Panel");
        rect.SetSiblingIndex(panel != null ? panel.GetSiblingIndex() + 1 : 0);
        RawImage image = go.GetComponent<RawImage>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;
        return image;
    }
}
