using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a video once on a RawImage every time its page is shown, then holds the last frame.
/// The picture stays invisible until the first frame is ready, so there is never a white flash.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class PageVideo : MonoBehaviour
{
    public VideoClip clip;
    [Tooltip("Seconds to wait after the page shows before the video starts.")]
    public float startDelay = 0.3f;

    VideoPlayer player;
    RawImage image;
    RenderTexture texture;
    bool frameShown;

    void Awake()
    {
        image = GetComponent<RawImage>();
    }

    void OnEnable()
    {
        if (!Application.isPlaying || clip == null)
            return;
        // the page is copied while the book is being baked, those copies must stay silent and invisible
        Canvas root = GetComponentInParent<Canvas>();
        if (root != null && root.name == "PanelBaker")
            return;
        Hide();
        StartCoroutine(Play());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (player != null)
            player.Stop();
        Hide();
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    IEnumerator Play()
    {
        Prepare();
        frameShown = false;
        player.Stop();
        player.sendFrameReadyEvents = true;
        player.Prepare();
        while (!player.isPrepared)
            yield return null;

        float wait = startDelay;
        while (wait > 0f)
        {
            wait -= Time.unscaledDeltaTime;
            yield return null;
        }

        player.time = 0.0;
        player.Play();
    }

    void Prepare()
    {
        if (player != null)
            return;
        image = GetComponent<RawImage>();
        texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        image.texture = texture;

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = false;
        player.skipOnDrop = true;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.clip = clip;
        player.sendFrameReadyEvents = true;
        player.frameReady += OnFrameReady;
    }

    void OnFrameReady(VideoPlayer source, long frame)
    {
        if (frameShown)
            return;
        frameShown = true;
        image.color = Color.white;
        source.sendFrameReadyEvents = false;
    }

    void Hide()
    {
        if (image == null)
            image = GetComponent<RawImage>();
        if (image != null)
            image.color = new Color(1f, 1f, 1f, 0f);
    }
}
