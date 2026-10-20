using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a video in a loop as the background of one or more RawImages that share it (for example the two halves of the spread).
/// Each target shows its own part of the picture through its uvRect, or the picture covers the target when cover is set.
/// </summary>
public class LoopVideo : MonoBehaviour
{
    [System.Serializable]
    public class Target
    {
        public RawImage image;
        [Tooltip("Part of the video this image shows (0 to 1, from the bottom left). Ignored when cover is set.")]
        public Rect uv = new Rect(0f, 0f, 1f, 1f);
    }

    public VideoClip clip;
    public Target[] targets;
    [Tooltip("Fill the image and crop the video to its shape instead of using the uv part.")]
    public bool cover;

    VideoPlayer player;
    RenderTexture texture;

    void OnEnable()
    {
        if (!Application.isPlaying || clip == null)
            return;
        if (texture == null)
        {
            texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
            texture.Create();
        }

        if (player == null)
        {
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            player.playbackSpeed = 1f;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = texture;
            player.clip = clip;
            player.sendFrameReadyEvents = true;
            player.frameReady += OnFrameReady;
            player.errorReceived += (source, message) => Debug.LogWarning("LoopVideo " + name + ": " + message);
        }

        for (int i = 0; i < targets.Length; i++)
        {
            RawImage image = targets[i].image;
            if (image == null)
                continue;
            image.texture = texture;
            // hidden until the first frame is ready, so the picture underneath shows instead of a black flash
            image.color = shown ? Color.white : new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            image.uvRect = cover ? CoverRect(image) : targets[i].uv;
        }

        StartCoroutine(Begin());
    }

    System.Collections.IEnumerator Begin()
    {
        player.Prepare();
        while (!player.isPrepared)
            yield return null;
        player.Play();
    }

    bool shown;

    void OnFrameReady(VideoPlayer source, long frame)
    {
        if (shown)
            return;
        shown = true;
        source.sendFrameReadyEvents = false;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].image != null)
                targets[i].image.color = Color.white;
        }
    }

    Vector2 lastSize;

    void LateUpdate()
    {
        if (!cover || targets == null)
            return;
        for (int i = 0; i < targets.Length; i++)
        {
            RawImage image = targets[i].image;
            if (image == null)
                continue;
            Vector2 size = ((RectTransform)image.transform).rect.size;
            if (size != lastSize)
            {
                lastSize = size;
                image.uvRect = CoverRect(image);
            }
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (player != null)
            player.Pause();
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    Rect CoverRect(RawImage image)
    {
        Rect rect = ((RectTransform)image.transform).rect;
        if (rect.width < 1f || rect.height < 1f)
            return new Rect(0f, 0f, 1f, 1f);
        float target = rect.width / rect.height;
        float source = (float)clip.width / clip.height;
        if (target > source)
        {
            float h = source / target;
            return new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }

        float w = target / source;
        return new Rect((1f - w) * 0.5f, 0f, w, 1f);
    }
}
