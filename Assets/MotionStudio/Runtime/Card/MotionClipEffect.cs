using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    /// <summary>
    /// Shows only a window of an Image, RawImage or Text. Used by <see cref="MotionCard"/> to grow a card out of a line.
    /// The window is given in 0..1 coordinates of the rectangle of the object. Triangles are clipped exactly,
    /// so sliced sprites, rounded cards and text all work.
    /// </summary>
    [AddComponentMenu("")]
    [RequireComponent(typeof(Graphic))]
    public class MotionClipEffect : BaseMeshEffect
    {
        Vector2 windowMin = Vector2.zero;
        Vector2 windowMax = Vector2.one;
        bool active;

        static readonly List<UIVertex> stream = new List<UIVertex>();
        static readonly List<UIVertex> output = new List<UIVertex>();
        static readonly List<UIVertex> polygonA = new List<UIVertex>();
        static readonly List<UIVertex> polygonB = new List<UIVertex>();

        public bool Active
        {
            get { return active; }
        }

        public void SetWindow(Vector2 min, Vector2 max)
        {
            if (active && windowMin == min && windowMax == max)
                return;

            active = true;
            windowMin = min;
            windowMax = max;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public void ClearWindow()
        {
            if (!active)
                return;

            active = false;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || !active)
                return;

            Rect rect = graphic.rectTransform.rect;
            float minX = Mathf.Lerp(rect.xMin, rect.xMax, windowMin.x);
            float maxX = Mathf.Lerp(rect.xMin, rect.xMax, windowMax.x);
            float minY = Mathf.Lerp(rect.yMin, rect.yMax, windowMin.y);
            float maxY = Mathf.Lerp(rect.yMin, rect.yMax, windowMax.y);

            vh.GetUIVertexStream(stream);
            output.Clear();

            if (maxX - minX > 0.0001f && maxY - minY > 0.0001f)
            {
                for (int i = 0; i + 2 < stream.Count; i += 3)
                {
                    polygonA.Clear();
                    polygonA.Add(stream[i]);
                    polygonA.Add(stream[i + 1]);
                    polygonA.Add(stream[i + 2]);

                    Clip(polygonA, polygonB, 0, minX, true);
                    Clip(polygonB, polygonA, 0, maxX, false);
                    Clip(polygonA, polygonB, 1, minY, true);
                    Clip(polygonB, polygonA, 1, maxY, false);

                    for (int v = 1; v + 1 < polygonA.Count; v++)
                    {
                        output.Add(polygonA[0]);
                        output.Add(polygonA[v]);
                        output.Add(polygonA[v + 1]);
                    }
                }
            }

            vh.Clear();
            if (output.Count > 0)
                vh.AddUIVertexTriangleStream(output);
        }

        // Sutherland-Hodgman against one half plane. axis 0 = x, 1 = y. keepGreater: keep values >= edge.
        static void Clip(List<UIVertex> input, List<UIVertex> result, int axis, float edge, bool keepGreater)
        {
            result.Clear();
            int count = input.Count;
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                UIVertex a = input[i];
                UIVertex b = input[(i + 1) % count];
                float av = axis == 0 ? a.position.x : a.position.y;
                float bv = axis == 0 ? b.position.x : b.position.y;
                bool aIn = keepGreater ? av >= edge : av <= edge;
                bool bIn = keepGreater ? bv >= edge : bv <= edge;

                if (aIn)
                    result.Add(a);
                if (aIn != bIn)
                {
                    float t = (edge - av) / (bv - av);
                    result.Add(Lerp(a, b, t));
                }
            }
        }

        static UIVertex Lerp(UIVertex a, UIVertex b, float t)
        {
            UIVertex v = new UIVertex();
            v.position = Vector3.LerpUnclamped(a.position, b.position, t);
            v.normal = Vector3.LerpUnclamped(a.normal, b.normal, t);
            v.tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, t);
            v.color = Color32.LerpUnclamped(a.color, b.color, t);
            v.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t);
            v.uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, t);
            v.uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, t);
            v.uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, t);
            return v;
        }
    }
}
