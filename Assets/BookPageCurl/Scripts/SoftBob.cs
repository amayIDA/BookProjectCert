using UnityEngine;

/// <summary>
/// A soft up and down drift for an icon. It adds a small offset on top of wherever the icon is,
/// so it works with the page intro animation and with a position set in the scene.
/// </summary>
[DisallowMultipleComponent]
public class SoftBob : MonoBehaviour
{
    [Tooltip("How far it moves up and down, in canvas units.")]
    public float distance = 12f;
    [Tooltip("Seconds for one full up and down.")]
    public float seconds = 3.6f;
    [Tooltip("Seconds to wait after the page shows, so the intro animation finishes first.")]
    public float startDelay = 1.2f;
    [Tooltip("Seconds to fade the movement in, so it never starts with a jerk.")]
    public float rampSeconds = 1.2f;

    RectTransform rect;
    float shownAt;
    Vector2 applied;

    void OnEnable()
    {
        rect = transform as RectTransform;
        shownAt = Time.unscaledTime;
        applied = Vector2.zero;
    }

    void OnDisable()
    {
        // leave the icon where its own animation or the scene put it
        if (rect != null)
            rect.anchoredPosition -= applied;
        applied = Vector2.zero;
    }

    void LateUpdate()
    {
        if (rect == null)
            return;

        float elapsed = Time.unscaledTime - shownAt - startDelay;
        float ramp = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, rampSeconds));
        ramp = ramp * ramp * (3f - 2f * ramp);
        float wave = Mathf.Sin(Mathf.Max(0f, elapsed) / Mathf.Max(0.2f, seconds) * Mathf.PI * 2f);
        Vector2 offset = new Vector2(0f, wave * distance * ramp);

        // other scripts write the absolute position, so take back our last offset first
        rect.anchoredPosition += offset - applied;
        applied = offset;
    }
}
