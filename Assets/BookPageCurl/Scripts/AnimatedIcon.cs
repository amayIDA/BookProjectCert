using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// An icon that is a transparent video (WebM with alpha) drawn on a RawImage through the BookUI/VideoIcon shader.
/// Once: plays from the start every time the page is shown, then holds its last frame.
/// Loop: plays for as long as the page is shown.
/// The picture is invisible until its first frame is ready and then fades in, so an icon never flashes or pops.
/// Copies of a page made for the page turn (the baker) show the old still picture instead, so a turning sheet is complete.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class AnimatedIcon : MonoBehaviour
{
    public VideoClip clip;
    public bool loop;
    [Tooltip("The part of the video that holds the icon (0 to 1, from the bottom left). The frame around it is empty.")]
    public Rect crop = new Rect(0f, 0f, 1f, 1f);
    [Tooltip("The still picture shown when the page is copied for a page turn.")]
    public Sprite poster;
    [Tooltip("Seconds to wait after the page shows before the icon starts.")]
    public float startDelay;
    [Tooltip("Seconds the icon takes to fade in once its first frame is ready.")]
    public float fadeIn = 0.4f;

    VideoPlayer player;
    RenderTexture texture;
    RawImage image;
    Color tint = Color.white;
    bool tintRead;
    bool frameShown;
    bool posterShown;
    float fade;

    void Awake()
    {
        image = GetComponent<RawImage>();
        ReadTint();
    }

    void ReadTint()
    {
        if (tintRead)
            return;
        tintRead = true;
        tint = image.color;
        tint.a = 1f;
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        image = GetComponent<RawImage>();
        ReadTint();

        Canvas root = GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas.name.EndsWith("Baker"))
        {
            ShowPoster();
            return;
        }

        if (clip == null)
            return;
        Hide();
        posterShown = false;
        StartCoroutine(Play());
        StartCoroutine(PosterFallback());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (player != null)
            player.Stop();
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    void Hide()
    {
        fade = 0f;
        frameShown = false;
        SetAlpha(0f);
    }

    void SetAlpha(float alpha)
    {
        Color c = tint;
        c.a = alpha;
        image.color = c;
    }

    void ShowPoster()
    {
        if (poster == null)
            return;
        Rect r = poster.textureRect;
        Texture2D t = poster.texture;
        image.texture = t;
        image.uvRect = new Rect(r.x / t.width, r.y / t.height, r.width / t.width, r.height / t.height);
        SetAlpha(1f);
    }

    IEnumerator Play()
    {
        Prepare();
        player.Stop();
        player.isLooping = loop;
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

        player.Play();

        // wait for the first picture, then fade the icon in
        while (!frameShown)
            yield return null;
        if (posterShown)
        {
            // the still picture was standing in: hand over to the video
            posterShown = false;
            image.texture = texture;
            image.uvRect = crop;
            SetAlpha(1f);
            yield break;
        }

        while (fade < 1f)
        {
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeIn));
            SetAlpha(Mathf.SmoothStep(0f, 1f, fade));
            yield return null;
        }
    }

    /// <summary>If the video is slow to start, the still picture stands in so the icon is never missing.</summary>
    IEnumerator PosterFallback()
    {
        float limit = 1.2f;
        while (limit > 0f)
        {
            limit -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!frameShown && poster != null)
        {
            posterShown = true;
            ShowPoster();
        }
    }

    void Prepare()
    {
        if (player != null)
            return;
        texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
        texture.Create();
        image.texture = texture;
        image.uvRect = crop;

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.skipOnDrop = true;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.clip = clip;
        player.frameReady += OnFrameReady;
    }

    void OnFrameReady(VideoPlayer source, long frame)
    {
        if (frameShown)
            return;
        frameShown = true;
        source.sendFrameReadyEvents = false;
    }
}
