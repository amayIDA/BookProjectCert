using System.Collections;
using MotionStudio;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On click, the row's gradient (ImagetoHighlight) is revealed from the top down and the second highlight opens from the centre.
/// Only one row is active at a time: clicking another row resets this one and stops whatever it was still doing.
/// The button picture itself is left as it was designed.
/// </summary>
public class PageRowHighlight : MonoBehaviour
{
    public MotionGlow glow;
    public AxisReveal vertical;
    [Tooltip("Seconds the gradient takes to reveal from the top down.")]
    public float wipeDuration = 0.5f;
    [Tooltip("The second highlight (the thin line) is drawn from the top down by this component, not by its own animation.")]
    public float verticalDuration = 0.8f;
    [Tooltip("Seconds after the click before the second highlight starts.")]
    public float verticalDelay = 0.1f;

    static PageRowHighlight active;

    RectMask2D mask;
    RectMask2D verticalMask;

    void OnEnable()
    {
        // the row that was clicked last shows its highlight again when the page comes back, every other row starts clean
        if (active == this)
            StartCoroutine(Reveal());
        else
            ResetRow();
    }

    void Start()
    {
        // the clip object is made here, after activation, because objects cannot be re-parented while their parent is being enabled
        if (glow != null && mask == null)
        {
            mask = ClipParentFor(glow.transform as RectTransform);
            SetWipe(active == this ? 1f : 0f);
        }
    }

    void OnDestroy()
    {
        if (active == this)
            active = null;
    }

    /// <summary>Back to the designed, not yet clicked look.</summary>
    public void ResetRow()
    {
        StopAllCoroutines();
        if (glow != null)
        {
            glow.gameObject.SetActive(true);
            glow.Stop();
            glow.Prepare();
            glow.ApplyStartState();
            SetWipe(0f);
        }

        if (vertical != null)
        {
            vertical.gameObject.SetActive(true);
            vertical.Stop();
            vertical.Prepare();
            vertical.ApplyRestState();
            SetVerticalWipe(0f);
        }
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (glow != null)
        {
            glow.Stop();
            glow.gameObject.SetActive(false);
        }

        if (vertical != null)
        {
            vertical.Stop();
            vertical.gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        if (active != null && active != this)
            active.Hide();
        active = this;
        StopAllCoroutines();
        // the click may have switched the page, which switches this row off: it then plays when the page returns
        if (isActiveAndEnabled)
            StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        if (glow != null)
            glow.gameObject.SetActive(true);
        if (vertical != null)
            vertical.gameObject.SetActive(true);
        SetWipe(0f);

        yield return null;

        if (glow != null && glow.gameObject.activeInHierarchy)
            glow.Play();
        if (vertical != null)
        {
            // this component draws the thin line itself, so nothing else can reset it half way
            vertical.Stop();
            vertical.ApplyRestState();
            SetVerticalWipe(0f);
        }

        float t = 0f;
        float end = Mathf.Max(wipeDuration, verticalDelay + verticalDuration);
        while (t < end)
        {
            t += Time.unscaledDeltaTime;
            if (glow != null)
                SetWipe(MotionEasing.Evaluate(MotionEase.AppleOut, Mathf.Clamp01(t / Mathf.Max(0.0001f, wipeDuration))));
            if (vertical != null)
                SetVerticalWipe(MotionEasing.Evaluate(MotionEase.AppleInOut, Mathf.Clamp01((t - verticalDelay) / Mathf.Max(0.0001f, verticalDuration))));
            yield return null;
        }

        SetWipe(1f);
        SetVerticalWipe(1f);
    }

    /// <summary>0 shows nothing, 1 shows all. The visible part grows from the top edge downward.</summary>
    void SetVerticalWipe(float progress)
    {
        if (vertical == null)
            return;

        Image picture = vertical.GetComponent<Image>();
        if (picture != null && picture.sprite != null)
        {
            // a filled image is drawn only from its top edge down to fillAmount, so it cannot appear all at once
            picture.enabled = true;
            picture.type = Image.Type.Filled;
            picture.fillMethod = Image.FillMethod.Vertical;
            picture.fillOrigin = (int)Image.OriginVertical.Top;
            picture.fillAmount = Mathf.Clamp01(progress);
            RectMask2D old = vertical.GetComponent<RectMask2D>();
            if (old != null)
                old.padding = Vector4.zero;
            return;
        }

        if (verticalMask == null)
        {
            verticalMask = vertical.GetComponent<RectMask2D>();
            if (verticalMask == null)
                verticalMask = vertical.gameObject.AddComponent<RectMask2D>();
        }

        float height = Mathf.Max(0f, ((RectTransform)vertical.transform).rect.height);
        verticalMask.padding = new Vector4(0f, height * (1f - progress), 0f, 0f);
        Graphic line = vertical.GetComponent<Graphic>();
        if (line != null)
            line.enabled = progress > 0.01f;
    }

    /// <summary>0 shows nothing, 1 shows all. The gradient is revealed from the top edge downward.</summary>
    void SetWipe(float progress)
    {
        if (glow == null)
            return;
        if (mask == null)
            return;

        // a mask clips what is inside it, so the glow sits in a clip object that is much larger than the row
        RectTransform clip = (RectTransform)mask.transform;
        RectTransform rect = (RectTransform)glow.transform;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        float bottom = clip.InverseTransformPoint(corners[0]).y;
        float top = clip.InverseTransformPoint(corners[1]).y;
        float hidden = Mathf.Max(0f, (top - bottom) * (1f - Mathf.Clamp01(progress)));
        // padding is left, bottom, right, top: hiding the bottom leaves the top visible first
        mask.padding = new Vector4(0f, Mathf.Max(0f, bottom - clip.rect.yMin) + hidden, 0f, 0f);
        if (progress >= 0.999f)
            mask.padding = Vector4.zero;
    }

    /// <summary>
    /// Puts the object inside a clip object of the same centre and far larger size, so nothing is cut when it is fully shown
    /// and the object keeps its own position and size.
    /// </summary>
    static RectMask2D ClipParentFor(RectTransform target)
    {
        if (target == null)
            return null;
        RectTransform parent = target.parent as RectTransform;
        if (parent == null)
            return null;
        if (parent.name == "~Clip")
            return parent.GetComponent<RectMask2D>();

        RectTransform clip = new GameObject("~Clip", typeof(RectTransform)).GetComponent<RectTransform>();
        clip.SetParent(parent, false);
        clip.SetSiblingIndex(target.GetSiblingIndex());
        clip.anchorMin = Vector2.zero;
        clip.anchorMax = Vector2.one;
        clip.offsetMin = new Vector2(-4000f, -4000f);
        clip.offsetMax = new Vector2(4000f, 4000f);
        RectMask2D clipper = clip.gameObject.AddComponent<RectMask2D>();
        target.SetParent(clip, false);
        return clipper;
    }
}
