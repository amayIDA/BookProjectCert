using MotionStudio;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Grows an image out from its centre on one axis, toward the size set in the scene.
/// </summary>
public class AxisReveal : MotionBehaviour
{
    public enum Axis
    {
        Horizontal,
        Vertical
    }

    public Axis axis = Axis.Horizontal;
    [Tooltip("Reveal downward from the top edge instead of growing from the centre.")]
    public bool fromTop;
    [Tooltip("Reveal upward from the bottom edge.")]
    public bool fromBottom;
    [Tooltip("Reveal from the left edge to the right.")]
    public bool fromLeft;
    [Tooltip("Reveal from the middle out to both sides. Put this on an empty container; the picture goes inside it as a child.")]
    public bool fromCenterX;
    [Tooltip("The line is drawn leftward from a point further right, then its right end pulls in to the size designed in the scene.")]
    public bool sweepFromRight;
    [Tooltip("Anchor x where the sweep starts.")]
    public float sweepStart = 0.27f;
    public float sweepLeadTime = 0.65f;
    public float sweepTailStart = 0.35f;
    [Tooltip("Starts as a long line and the right end pulls in to the size designed in the scene.")]
    public bool squeezeFromRight;
    public float squeezeFrom = 7f;
    public float duration = 0.45f;
    public bool recorded;
    public Vector3 restScale = Vector3.one;
    public Vector2 restPosition;
    public Vector2 restSize;
    public Vector2 restAnchorMin;
    public Vector2 restAnchorMax;

    RectTransform rect;
    Image.Type restImageType = Image.Type.Simple;
    bool imageRecorded;

    public override float ContentDuration
    {
        get { return duration; }
    }

    public override void Prepare()
    {
        if (rect == null)
            rect = transform as RectTransform;
        if (rect == null || recorded)
            return;
        // A sweep that is already drawn out must not be saved as the finished line.
        if (sweepFromRight && rect.anchorMax.x >= sweepStart - 0.0001f)
            return;
        // A squeeze that is already stretched must not be saved as the finished line.
        if (squeezeFromRight && rect.anchorMax.x >= 0.2f)
            return;
        restScale = rect.localScale;
        restPosition = rect.anchoredPosition;
        restSize = rect.sizeDelta;
        restAnchorMin = rect.anchorMin;
        restAnchorMax = rect.anchorMax;
        recorded = true;
    }

    public override void ApplyRestState()
    {
        Prepare();
        if (rect != null)
        {
            rect.localScale = restScale;
            if (squeezeFromRight || sweepFromRight)
            {
                rect.anchorMin = restAnchorMin;
                rect.anchorMax = restAnchorMax;
                rect.sizeDelta = restSize;
                rect.anchoredPosition = restPosition;
            }
        }
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null)
            graphic.enabled = true;
        RectMask2D mask = GetComponent<RectMask2D>();
        if (mask != null)
            mask.padding = Vector4.zero;
        RestoreImage();
    }

    /// <summary>
    /// A rectangular mask never clips a picture on its own object, so wipes on an Image use its fill instead.
    /// An object without a picture (a container) is clipped by its own mask, which hides its children.
    /// </summary>
    Image FillTarget()
    {
        Image image = GetComponent<Image>();
        return image != null && image.sprite != null ? image : null;
    }

    void RememberImage(Image image)
    {
        if (imageRecorded)
            return;
        restImageType = image.type;
        imageRecorded = true;
    }

    void RestoreImage()
    {
        Image image = FillTarget();
        if (image == null || !imageRecorded)
            return;
        image.type = restImageType;
        image.fillAmount = 1f;
    }

    bool FillWipe(Image.FillMethod method, int origin, float amount)
    {
        Image image = FillTarget();
        if (image == null)
            return false;
        RememberImage(image);
        if (amount >= 0.999f)
        {
            RestoreImage();
            image.enabled = true;
            return true;
        }

        image.type = Image.Type.Filled;
        image.fillMethod = method;
        image.fillOrigin = origin;
        image.fillAmount = Mathf.Clamp01(amount);
        image.enabled = amount > 0.001f;
        return true;
    }

    protected override void EvaluateContent(float time)
    {
        Prepare();
        if (rect == null)
            return;

        float progress = Mathf.Clamp01(time / Mathf.Max(0.0001f, duration));
        float eased = MotionEasing.Evaluate(MotionEase.AppleOut, progress);
        if (sweepFromRight)
        {
            float lead = MotionEasing.Evaluate(MotionEase.QuadOut, Mathf.Clamp01(time / Mathf.Max(0.0001f, sweepLeadTime)));
            float tail = MotionEasing.Evaluate(MotionEase.QuadOut, Mathf.Clamp01((time - sweepTailStart) / Mathf.Max(0.0001f, duration - sweepTailStart)));
            float start = Mathf.Max(sweepStart, restAnchorMax.x);
            rect.localScale = restScale;
            rect.anchorMin = new Vector2(Mathf.Lerp(start, restAnchorMin.x, lead), restAnchorMin.y);
            rect.anchorMax = new Vector2(Mathf.Lerp(start, restAnchorMax.x, tail), restAnchorMax.y);
            rect.sizeDelta = new Vector2(0f, restSize.y);
            rect.anchoredPosition = restPosition;
            Graphic sweepGraphic = GetComponent<Graphic>();
            if (sweepGraphic != null)
                sweepGraphic.enabled = time > 0f;
            return;
        }

        if (fromCenterX)
        {
            rect.localScale = restScale;
            RectMask2D centre = GetComponent<RectMask2D>();
            if (centre == null)
                centre = gameObject.AddComponent<RectMask2D>();
            float half = Mathf.Max(0f, rect.rect.width) * 0.5f * (1f - eased);
            centre.padding = new Vector4(half, 0f, half, 0f);
            Graphic centreGraphic = GetComponent<Graphic>();
            if (centreGraphic != null)
                centreGraphic.enabled = time > 0f;
            return;
        }

        if (fromLeft || fromBottom)
        {
            rect.localScale = restScale;
            if (fromLeft
                ? FillWipe(Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Left, eased)
                : FillWipe(Image.FillMethod.Vertical, (int)Image.OriginVertical.Bottom, eased))
                return;
            RectMask2D wipe = GetComponent<RectMask2D>();
            if (wipe == null)
                wipe = gameObject.AddComponent<RectMask2D>();
            float width = Mathf.Max(0f, rect.rect.width);
            float tall = Mathf.Max(0f, rect.rect.height);
            wipe.padding = new Vector4(0f, 0f, fromLeft ? width * (1f - eased) : 0f, fromBottom ? tall * (1f - eased) : 0f);
            Graphic wipeGraphic = GetComponent<Graphic>();
            if (wipeGraphic != null)
                wipeGraphic.enabled = time > 0f;
            return;
        }

        if (squeezeFromRight)
        {
            rect.localScale = restScale;
            rect.anchorMin = restAnchorMin;
            float wide = Mathf.Min(0.62f, restAnchorMax.x + 0.5f);
            float right = eased >= 1f ? restAnchorMax.x : Mathf.Lerp(wide, restAnchorMax.x, eased);
            rect.anchorMax = new Vector2(right, restAnchorMax.y);
            rect.sizeDelta = eased >= 1f ? restSize : new Vector2(0f, restSize.y);
            rect.anchoredPosition = restPosition;
            Graphic lineGraphic = GetComponent<Graphic>();
            if (lineGraphic != null)
                lineGraphic.enabled = true;
            return;
        }

        if (fromTop)
        {
            rect.localScale = restScale;
            if (FillWipe(Image.FillMethod.Vertical, (int)Image.OriginVertical.Top, eased))
                return;
            RectMask2D topMask = GetComponent<RectMask2D>();
            if (topMask == null)
                topMask = gameObject.AddComponent<RectMask2D>();
            float height = Mathf.Max(0f, rect.rect.height);
            topMask.padding = new Vector4(0f, height * (1f - eased), 0f, 0f);
            Graphic topGraphic = GetComponent<Graphic>();
            if (topGraphic != null)
                topGraphic.enabled = eased >= 0.02f;
            return;
        }

        Vector3 scale = restScale;
        if (axis == Axis.Horizontal)
            scale.x = restScale.x * eased;
        else
            scale.y = restScale.y * eased;

        bool visible = eased >= 0.04f;
        rect.localScale = visible ? scale : new Vector3(scale.x, axis == Axis.Vertical ? 0f : scale.y, scale.z);
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null)
            graphic.enabled = visible;
        RectMask2D mask = GetComponent<RectMask2D>();
        if (mask != null)
            mask.padding = Vector4.zero;
    }
}
