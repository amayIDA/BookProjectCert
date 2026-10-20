using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Horizontal layout that wraps to a new row, used for chips and the support journey.</summary>
public class NIFlowLayout : LayoutGroup
{
    public float spacingX = 6f;
    public float spacingY = 6f;

    readonly List<Vector2> sizes = new List<Vector2>();

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        float widest = 0f;
        for (int i = 0; i < rectChildren.Count; i++)
            widest = Mathf.Max(widest, LayoutUtility.GetPreferredSize(rectChildren[i], 0));

        SetLayoutInputForAxis(widest + padding.horizontal, widest + padding.horizontal, 1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float height = Measure(Mathf.Max(1f, rectTransform.rect.width - padding.horizontal));
        SetLayoutInputForAxis(height + padding.vertical, height + padding.vertical, 0f, 1);
    }

    public override void SetLayoutHorizontal()
    {
    }

    public override void SetLayoutVertical()
    {
        float available = Mathf.Max(1f, rectTransform.rect.width - padding.horizontal);
        Measure(available);
        float x = 0f;
        float y = 0f;
        float rowHeight = 0f;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            Vector2 size = sizes[i];
            if (x > 0f && x + size.x > available + 0.5f)
            {
                x = 0f;
                y += rowHeight + spacingY;
                rowHeight = 0f;
            }

            SetChildAlongAxis(rectChildren[i], 0, padding.left + x, size.x);
            SetChildAlongAxis(rectChildren[i], 1, padding.top + y, size.y);
            x += size.x + spacingX;
            rowHeight = Mathf.Max(rowHeight, size.y);
        }
    }

    float Measure(float available)
    {
        sizes.Clear();
        float x = 0f;
        float y = 0f;
        float rowHeight = 0f;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            Vector2 size = new Vector2(LayoutUtility.GetPreferredSize(rectChildren[i], 0), LayoutUtility.GetPreferredSize(rectChildren[i], 1));
            if (size.x > available + 0.5f)
            {
                // too wide for one row: let the text wrap inside the child
                size.x = available;
                rectChildren[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, available);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectChildren[i]);
                size.y = LayoutUtility.GetPreferredSize(rectChildren[i], 1);
            }

            sizes.Add(size);
            if (x > 0f && x + size.x > available + 0.5f)
            {
                x = 0f;
                y += rowHeight + spacingY;
                rowHeight = 0f;
            }

            x += size.x + spacingX;
            rowHeight = Mathf.Max(rowHeight, size.y);
        }

        return y + rowHeight;
    }
}

/// <summary>
/// One element of a view that eases in: it fades and grows to full size with a soft overshoot.
/// Delay and duration can be changed in the Inspector. Layout groups own the position of the element,
/// so only opacity and scale are animated.
/// </summary>
[DisallowMultipleComponent]
public class NIAnimItem : MonoBehaviour
{
    public float delay;
    public float duration = 0.7f;
    [Range(0.5f, 1f)] public float scaleFrom = 0.9f;

    CanvasGroup group;
    RectTransform rect;
    Sequence sequence;

    public static NIAnimItem Add(GameObject go, float delay, float duration = 0.7f, float rise = 0f)
    {
        NIAnimItem item = go.GetComponent<NIAnimItem>();
        if (item == null)
            item = go.AddComponent<NIAnimItem>();
        item.delay = delay;
        item.duration = duration;
        if (go.GetComponent<CanvasGroup>() == null)
            go.AddComponent<CanvasGroup>();
        return item;
    }

    void Cache()
    {
        if (group == null)
            group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        if (rect == null)
            rect = (RectTransform)transform;
    }

    public void Snap()
    {
        Cache();
        if (sequence != null)
            sequence.Kill();
        group.alpha = 1f;
        rect.localScale = Vector3.one;
    }

    public void Remember()
    {
    }

    public void Play(float extraDelay)
    {
        Cache();
        if (sequence != null)
            sequence.Kill();

        group.alpha = 0f;
        rect.localScale = Vector3.one * scaleFrom;

        float wait = delay + extraDelay;
        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        sequence.Insert(wait, group.DOFade(1f, duration * 0.75f).SetEase(Ease.OutCubic));
        sequence.Insert(wait, rect.DOScale(1f, duration).SetEase(Ease.OutBack, 1.4f));
    }
}

/// <summary>A screen state of the content display. Shows with a staggered entrance and leaves quickly.</summary>
[DisallowMultipleComponent]
public class NIView : MonoBehaviour
{
    CanvasGroup group;
    Tween outTween;

    CanvasGroup Group
    {
        get
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            return group;
        }
    }

    public void Show(float extraDelay = 0f)
    {
        if (outTween != null)
            outTween.Kill();

        gameObject.SetActive(true);
        Group.alpha = 1f;

        NIAnimItem[] items = GetComponentsInChildren<NIAnimItem>(true);
        for (int i = 0; i < items.Length; i++)
            items[i].Snap();

        RectTransform rect = (RectTransform)transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        for (int i = 0; i < items.Length; i++)
            items[i].Remember();

        if (!Application.isPlaying)
            return;

        for (int i = 0; i < items.Length; i++)
            items[i].Play(extraDelay);
    }

    /// <summary>Exit is shorter than the entrance so the screen never feels slow to respond.</summary>
    public void Hide(Action done = null)
    {
        if (!gameObject.activeSelf)
        {
            if (done != null)
                done();
            return;
        }

        if (!Application.isPlaying)
        {
            gameObject.SetActive(false);
            if (done != null)
                done();
            return;
        }

        if (outTween != null)
            outTween.Kill();
        outTween = Group.DOFade(0f, 0.18f).SetEase(Ease.InCubic).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
        {
            gameObject.SetActive(false);
            Group.alpha = 1f;
            if (done != null)
                done();
        });
    }
}

/// <summary>Headline with the word by word fade of the HTML file, driven with rich text colour alpha.</summary>
public class NIHeadline : MonoBehaviour
{
    sealed class Word
    {
        public string text;
        public bool bold;
    }

    public Text label;
    readonly List<Word> words = new List<Word>();
    Color baseColor = Color.white;
    float start;
    float outStart = -1f;
    string pending;
    bool hasPending;
    bool animating;
    float outDelay = 0.2f;

    public void Init(Text text)
    {
        label = text;
        baseColor = text.color;
    }

    /// <summary>Set the text at once, used for the preview in the Scene view.</summary>
    public void Immediate(string markup)
    {
        if (label == null)
            return;
        baseColor = Color.white;
        label.color = baseColor;
        label.text = markup;
    }

    /// <summary>Cross fade to the next headline. Markup supports &lt;b&gt;.</summary>
    public void Show(string markup, float outDuration = 0.2f)
    {
        if (!Application.isPlaying)
        {
            Immediate(markup);
            return;
        }

        if (label != null)
            baseColor = new Color(1f, 1f, 1f, 1f);
        pending = markup;
        hasPending = true;
        outStart = Time.unscaledTime;
        outDelay = outDuration;
        animating = true;
    }

    void Update()
    {
        if (label == null || !animating)
            return;

        float now = Time.unscaledTime;
        if (hasPending)
        {
            float k = Mathf.Clamp01((now - outStart) / Mathf.Max(0.01f, outDelay));
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - k));
            if (k >= 1f)
            {
                Parse(pending);
                hasPending = false;
                start = now;
                label.color = baseColor;
            }

            return;
        }

        float elapsed = now - start;
        bool done = true;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < words.Count; i++)
        {
            float t = (elapsed - i * 0.07f) / 0.95f;
            if (t < 1f)
                done = false;
            float a = NIUI.EaseRise(Mathf.Clamp01(t));
            string hex = ColorUtility.ToHtmlStringRGBA(new Color(baseColor.r, baseColor.g, baseColor.b, a));
            if (i > 0)
                sb.Append(' ');
            sb.Append("<color=#").Append(hex).Append('>');
            if (words[i].bold)
                sb.Append("<b>");
            sb.Append(words[i].text);
            if (words[i].bold)
                sb.Append("</b>");
            sb.Append("</color>");
        }

        label.text = sb.ToString();
        if (done)
            animating = false;
    }

    void Parse(string markup)
    {
        words.Clear();
        if (string.IsNullOrEmpty(markup))
            return;

        bool bold = false;
        StringBuilder current = new StringBuilder();
        int i = 0;
        while (i < markup.Length)
        {
            if (markup[i] == '<')
            {
                int close = markup.IndexOf('>', i);
                string tag = close > i ? markup.Substring(i, close - i + 1) : null;
                if (tag == "<b>")
                {
                    bold = true;
                    i = close + 1;
                    continue;
                }

                if (tag == "</b>")
                {
                    Flush(current, bold);
                    bold = false;
                    i = close + 1;
                    continue;
                }
            }

            char c = markup[i];
            if (char.IsWhiteSpace(c))
                Flush(current, bold);
            else
                current.Append(c);
            i++;
        }

        Flush(current, bold);
    }

    void Flush(StringBuilder current, bool bold)
    {
        if (current.Length == 0)
            return;
        words.Add(new Word { text = current.ToString(), bold = bold });
        current.Length = 0;
    }
}

/// <summary>Flip card: starts face down (the numbered back) and turns over with a soft overshoot.</summary>
public class NICardFlip : MonoBehaviour
{
    public RectTransform front;
    public RectTransform back;

    Tween tween;

    void Awake()
    {
        if (Application.isPlaying)
            Apply(180f);
    }

    public void Close()
    {
        if (tween != null)
            tween.Kill();
        Apply(180f);
    }

    public void Open()
    {
        if (tween != null)
            tween.Kill();
        tween = DOVirtual.Float(180f, 0f, 0.95f, Apply).SetEase(Ease.OutBack, 1.25f).SetUpdate(true).SetLink(gameObject);
    }

    void Apply(float degrees)
    {
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
        bool showFront = cos > 0f;
        if (front != null)
            front.gameObject.SetActive(showFront);
        if (back != null)
            back.gameObject.SetActive(!showFront);
        transform.localScale = new Vector3(Mathf.Max(0.02f, Mathf.Abs(cos)), 1f, 1f);
    }
}

/// <summary>Fills a column of identical entries (chips, ticks, list rows) from text. The first child is the template.</summary>
public class NIListBinder : MonoBehaviour
{
    public RectTransform container;
    public bool numbered;

    public void Bind(IList<string> values)
    {
        if (container == null)
            container = (RectTransform)transform;
        if (container.childCount == 0)
            return;

        RectTransform template = (RectTransform)container.GetChild(0);
        int count = values != null ? values.Count : 0;

        // remove everything but the template and the entries we still need
        for (int i = container.childCount - 1; i >= 1; i--)
        {
            GameObject extra = container.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(extra);
            else
                DestroyImmediate(extra);
        }

        template.gameObject.SetActive(count > 0);
        for (int i = 0; i < count; i++)
        {
            RectTransform entry = i == 0 ? template : Instantiate(template, container);
            entry.gameObject.SetActive(true);
            entry.name = template.name;
            Text[] texts = entry.GetComponentsInChildren<Text>(true);
            if (texts.Length == 0)
                continue;
            if (numbered && texts.Length > 1)
            {
                texts[0].text = (i + 1).ToString();
                texts[1].text = values[i];
            }
            else
                texts[texts.Length - 1].text = values[i];

            NIAnimItem item = entry.GetComponent<NIAnimItem>();
            if (item != null)
                item.delay = 0.15f + i * 0.09f;
        }
    }
}

/// <summary>Slow loop for glows and rings: breathing scale and opacity, or a steady spin.</summary>
public class NIBreathe : MonoBehaviour
{
    public float scaleTo = 1.06f;
    public float alphaTo = -1f;
    public float period = 5f;
    public float startDelay;
    [Tooltip("Degrees per second. Zero keeps the rotation still.")]
    public float spin;

    Tween scaleTween;
    Tween alphaTween;
    Tween spinTween;
    Vector3 baseScale;
    float baseAlpha;
    Graphic graphic;

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        baseScale = transform.localScale;
        graphic = GetComponent<Graphic>();
        if (scaleTo > 0f && Mathf.Abs(scaleTo - 1f) > 0.0001f)
            scaleTween = transform.DOScale(baseScale * scaleTo, period * 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(startDelay).SetUpdate(true).SetLink(gameObject);

        if (graphic != null && alphaTo >= 0f)
        {
            baseAlpha = graphic.color.a;
            alphaTween = graphic.DOFade(alphaTo, period * 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(startDelay).SetUpdate(true).SetLink(gameObject);
        }

        if (Mathf.Abs(spin) > 0.01f)
            spinTween = transform.DOLocalRotate(new Vector3(0f, 0f, spin > 0f ? -360f : 360f), 360f / Mathf.Abs(spin), RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetLink(gameObject);
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (scaleTween != null) scaleTween.Kill();
        if (alphaTween != null) alphaTween.Kill();
        if (spinTween != null) spinTween.Kill();
        transform.localScale = baseScale == Vector3.zero ? transform.localScale : baseScale;
        if (graphic != null && baseAlpha > 0f)
        {
            Color c = graphic.color;
            c.a = baseAlpha;
            graphic.color = c;
        }
    }
}

/// <summary>Lets a soft light drift around slowly, like bokeh in the background.</summary>
public class NIDrift : MonoBehaviour
{
    public Vector2 range = new Vector2(60f, 40f);
    public float period = 12f;
    public float startDelay;

    Tween moveX;
    Tween moveY;
    Vector2 origin;

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        RectTransform rect = (RectTransform)transform;
        origin = rect.anchoredPosition;
        moveX = rect.DOAnchorPosX(origin.x + range.x, period).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(startDelay).SetUpdate(true).SetLink(gameObject);
        moveY = rect.DOAnchorPosY(origin.y + range.y, period * 0.73f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(startDelay).SetUpdate(true).SetLink(gameObject);
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (moveX != null) moveX.Kill();
        if (moveY != null) moveY.Kill();
        ((RectTransform)transform).anchoredPosition = origin;
    }
}

/// <summary>Press feedback for buttons: sinks a little on touch and springs back, with a soft sound.</summary>
public class NIPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public static event Action<PointerEventData> AnyPress;

    public RectTransform target;
    [Range(0.8f, 1f)] public float pressedScale = 0.94f;
    public string pressSound = "asmr_press";

    Tween tween;
    bool down;

    RectTransform Target
    {
        get { return target != null ? target : (RectTransform)transform; }
    }

    public static void RaisePress(PointerEventData eventData)
    {
        if (AnyPress != null)
            AnyPress(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        down = true;
        if (tween != null)
            tween.Kill();
        tween = Target.DOScale(pressedScale, 0.12f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        if (!string.IsNullOrEmpty(pressSound) && NIAudio.Instance != null)
            NIAudio.Instance.PlayVaried(pressSound, 0.96f, 1.06f, 1f);
        RaisePress(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    void Release()
    {
        if (!down)
            return;

        down = false;
        if (tween != null)
            tween.Kill();
        tween = Target.DOScale(1f, 0.55f).SetEase(Ease.OutBack, 2.2f).SetUpdate(true).SetLink(gameObject);
    }

    void OnDisable()
    {
        if (tween != null)
            tween.Kill();
        down = false;
        Target.localScale = Vector3.one;
    }
}

/// <summary>A light streak that sweeps across a button now and then.</summary>
public class NIShineSweep : MonoBehaviour
{
    public float travel = 420f;
    public float period = 6f;
    public float startDelay = 1.5f;

    Tween tween;

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        RectTransform rect = (RectTransform)transform;
        rect.anchoredPosition = new Vector2(-travel, rect.anchoredPosition.y);
        tween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetLoops(-1)
            .AppendInterval(startDelay)
            .Append(rect.DOAnchorPosX(travel, 1.1f).SetEase(Ease.InOutSine))
            .AppendInterval(Mathf.Max(0.1f, period - 1.1f))
            .AppendCallback(() => rect.anchoredPosition = new Vector2(-travel, rect.anchoredPosition.y));
    }

    void OnDisable()
    {
        if (tween != null)
            tween.Kill();
    }
}
