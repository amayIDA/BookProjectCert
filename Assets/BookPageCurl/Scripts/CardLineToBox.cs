using MotionStudio;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws the card frame as a line, then opens that line into the boxes designed in the scene.
/// The line is drawn at originY. Edges above it open upward, edges below it open downward one after the other,
/// so a card with a header row, a body and a strip opens as three boxes.
/// </summary>
public class CardLineToBox : MotionBehaviour
{
    public float lineDuration = 0.5f;
    public float openDuration = 0.65f;
    [Tooltip("Pause between the finished line and the box opening.")]
    public float holdDuration;
    [Tooltip("Height (0..1 of the card) where the first line is drawn. 0.5 = the middle.")]
    public float originY = 0.5f;
    [Tooltip("The first box below the line starts opening this long after the upper half.")]
    public float lowerLag;
    [Tooltip("Each further box below opens this long after the one above it.")]
    public float stripLag = 0.3f;
    [Tooltip("The line is drawn from the left edge to the right instead of out from the centre.")]
    public bool drawFromLeft;
    [Tooltip("The line starts in this colour and settles to the frame colour as the box opens.")]
    public bool tintLine;
    public Color lineTint = new Color(0.4f, 0.5f, 1f, 1f);
    public float lineThickness = 2f;
    [Tooltip("Optional solid background (put it behind everything in the card). It grows with the boxes so the card is black inside.")]
    public RectTransform fill;
    public bool recorded;
    public Vector3 restScale = Vector3.one;

    RectTransform rect;
    Edge[] edges = new Edge[8];
    int edgeCount;
    int lowerLevels;
    Vector2 fillMin = Vector2.zero;
    Vector2 fillMax = Vector2.one;
    int[] lower = new int[8];
    float[] ys = new float[8];

    struct Edge
    {
        public RectTransform rect;
        public Graphic graphic;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public bool horizontal;
        public Color color;
    }

    public override float ContentDuration
    {
        get
        {
            if (!recorded)
                Prepare();
            return lineDuration + holdDuration + openDuration + lowerLag + stripLag * Mathf.Max(0, lowerLevels - 1);
        }
    }

    public override void Prepare()
    {
        if (rect == null)
            rect = transform as RectTransform;
        if (rect == null)
            return;

        Transform extra = rect.Find("~OpenLine");
        if (extra != null)
            Destroy(extra.gameObject);

        if (!recorded || edgeCount == 0)
        {
            restScale = rect.localScale;
            edgeCount = 0;
            for (int i = 0; i < rect.childCount && edgeCount < edges.Length; i++)
            {
                RectTransform child = rect.GetChild(i) as RectTransform;
                if (child == null || child == fill)
                    continue;
                Edge edge = new Edge();
                edge.rect = child;
                edge.graphic = child.GetComponent<Graphic>();
                edge.color = edge.graphic != null ? edge.graphic.color : Color.white;
                edge.anchorMin = child.anchorMin;
                edge.anchorMax = child.anchorMax;
                edge.anchoredPosition = child.anchoredPosition;
                edge.sizeDelta = child.sizeDelta;
                float spanX = Mathf.Abs(edge.anchorMax.x - edge.anchorMin.x);
                float spanY = Mathf.Abs(edge.anchorMax.y - edge.anchorMin.y);
                edge.horizontal = spanX >= spanY;
                edges[edgeCount] = edge;
                edgeCount++;
            }
            if (fill != null)
            {
                fillMin = fill.anchorMin;
                fillMax = fill.anchorMax;
            }

            lowerLevels = 0;
            for (int i = 0; i < edgeCount; i++)
            {
                if (edges[i].horizontal && RestY(edges[i]) < originY - 0.01f)
                    lowerLevels++;
            }

            recorded = true;
        }
    }

    static float RestY(Edge e)
    {
        return (e.anchorMin.y + e.anchorMax.y) * 0.5f;
    }

    public override void ApplyRestState()
    {
        Prepare();
        if (rect != null)
            rect.localScale = restScale;
        for (int i = 0; i < edgeCount; i++)
            Restore(edges[i]);
        RestoreFill();
    }

    protected override void EvaluateContent(float time)
    {
        Prepare();
        if (rect == null)
            return;

        rect.localScale = restScale;
        bool done = time >= ContentDuration - 0.0001f;
        if (done)
        {
            for (int i = 0; i < edgeCount; i++)
                Restore(edges[i]);
            RestoreFill();
            return;
        }

        float across = MotionEasing.Evaluate(MotionEase.AppleOut, Mathf.Clamp01(time / Mathf.Max(0.0001f, lineDuration)));
        float openStart = lineDuration + holdDuration;
        float openUp = MotionEasing.Evaluate(MotionEase.AppleOut, Mathf.Clamp01((time - openStart) / Mathf.Max(0.0001f, openDuration)));
        float settle = MotionEasing.Evaluate(MotionEase.SineOut, Mathf.Clamp01((time - lineDuration * 0.5f) / Mathf.Max(0.0001f, lineDuration * 0.5f + holdDuration + openDuration)));

        // the edges below the line, highest first: each one opens from the edge above it
        int count = 0;
        for (int i = 0; i < edgeCount; i++)
        {
            if (!edges[i].horizontal || edges[i].rect == null || RestY(edges[i]) >= originY - 0.01f)
                continue;
            int at = count++;
            while (at > 0 && RestY(edges[lower[at - 1]]) < RestY(edges[i]))
            {
                lower[at] = lower[at - 1];
                at--;
            }

            lower[at] = i;
        }

        float yLow = originY;
        float yHigh = originY;
        for (int i = 0; i < edgeCount; i++)
        {
            if (!edges[i].horizontal || edges[i].rect == null)
                continue;
            float rest = RestY(edges[i]);
            if (rest >= originY - 0.01f)
            {
                ys[i] = rest > originY + 0.01f ? Mathf.Lerp(originY, rest, openUp) : originY;
                yHigh = Mathf.Max(yHigh, ys[i]);
            }
        }

        float[] opens = new float[count];
        for (int j = 0; j < count; j++)
        {
            int i = lower[j];
            float delay = lowerLag + j * stripLag;
            opens[j] = MotionEasing.Evaluate(MotionEase.AppleOut, Mathf.Clamp01((time - openStart - delay) / Mathf.Max(0.0001f, openDuration)));
            float from = j == 0 ? originY : ys[lower[j - 1]];
            ys[i] = Mathf.Lerp(from, RestY(edges[i]), opens[j]);
            yLow = Mathf.Min(yLow, ys[i]);
        }

        if (fill != null)
        {
            fill.anchorMin = new Vector2(fillMin.x, Mathf.Lerp(fillMin.y, fillMax.y, yLow));
            fill.anchorMax = new Vector2(fillMax.x, Mathf.Lerp(fillMin.y, fillMax.y, yHigh));
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Graphic fillGraphic = fill.GetComponent<Graphic>();
            if (fillGraphic != null)
                fillGraphic.enabled = yHigh - yLow > 0.002f;
        }

        for (int i = 0; i < edgeCount; i++)
        {
            Edge edge = edges[i];
            if (edge.rect == null)
                continue;

            if (tintLine && edge.graphic != null)
                edge.graphic.color = Color.Lerp(lineTint, edge.color, settle);

            if (edge.horizontal)
            {
                float rest = RestY(edge);
                float open = openUp;
                for (int j = 0; j < count; j++)
                {
                    if (lower[j] == i)
                        open = opens[j];
                }

                float span = edge.anchorMax.y - edge.anchorMin.y;
                float left = drawFromLeft ? edge.anchorMin.x : Mathf.Lerp(0.5f, edge.anchorMin.x, across);
                float right = drawFromLeft ? Mathf.Lerp(edge.anchorMin.x, edge.anchorMax.x, across) : Mathf.Lerp(0.5f, edge.anchorMax.x, across);
                edge.rect.anchorMin = new Vector2(left, ys[i] - span * 0.5f * open);
                edge.rect.anchorMax = new Vector2(right, ys[i] + span * 0.5f * open);
                edge.rect.pivot = new Vector2(0.5f, 0.5f);
                edge.rect.sizeDelta = new Vector2(
                    Mathf.Lerp(0f, edge.sizeDelta.x, open),
                    Mathf.Lerp(lineThickness, edge.sizeDelta.y, open));
                edge.rect.anchoredPosition = Vector2.Lerp(Vector2.zero, edge.anchoredPosition, open);
                if (edge.graphic != null)
                    edge.graphic.enabled = across >= 0.01f;
            }
            else
            {
                edge.rect.anchorMin = new Vector2(edge.anchorMin.x, yLow);
                edge.rect.anchorMax = new Vector2(edge.anchorMax.x, yHigh);
                edge.rect.sizeDelta = new Vector2(edge.sizeDelta.x, 0f);
                edge.rect.anchoredPosition = edge.anchoredPosition;
                if (edge.graphic != null)
                    edge.graphic.enabled = yHigh - yLow > 0.002f;
            }
        }
    }

    void RestoreFill()
    {
        if (fill == null)
            return;
        fill.anchorMin = fillMin;
        fill.anchorMax = fillMax;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Graphic fillGraphic = fill.GetComponent<Graphic>();
        if (fillGraphic != null)
            fillGraphic.enabled = true;
    }

    static void Restore(Edge edge)
    {
        if (edge.rect == null)
            return;
        edge.rect.anchorMin = edge.anchorMin;
        edge.rect.anchorMax = edge.anchorMax;
        edge.rect.anchoredPosition = edge.anchoredPosition;
        edge.rect.sizeDelta = edge.sizeDelta;
        if (edge.graphic != null)
        {
            edge.graphic.enabled = true;
            edge.graphic.color = edge.color;
        }
    }
}
