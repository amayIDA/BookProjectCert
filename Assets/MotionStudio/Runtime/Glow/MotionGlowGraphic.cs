using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    /// <summary>
    /// A soft shaped light. Give it any sprite or png as the shape, then control the blur, the brightness and the gradient
    /// that fills it. Put it inside a Mask (or under a RectMask2D) to keep the light inside a card, or leave it free.
    /// Animate it with <see cref="MotionGlow"/>.
    /// </summary>
    [AddComponentMenu("Motion Studio/Glow Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class MotionGlowGraphic : MaskableGraphic
    {
        [Header("Shape")]
        [Tooltip("The shape of the light. Its alpha channel is the shape. Any sprite or png works, a plain white blob or ring is a good start.")]
        public Sprite shape;

        [Tooltip("Use the brightness of the picture as the shape instead of its alpha (for black and white pngs).")]
        public bool shapeFromBrightness;

        [Header("Look")]
        [Range(0f, 1f)] public float blur = 0.35f;
        [Min(0f)] public float brightness = 1.4f;

        [Tooltip("Above 1 pulls the light towards the centre of the shape, below 1 spreads it out.")]
        [Range(0.2f, 4f)] public float softness = 1f;

        [Tooltip("Additive light glows on dark backgrounds. Alpha blends like a normal image.")]
        public bool additive = true;

        [Header("Gradient")]
        public Color colorA = new Color(0.35f, 0.65f, 1f, 1f);
        public Color colorB = new Color(1f, 0.45f, 0.85f, 1f);
        [Range(0f, 360f)] public float gradientAngle = 45f;
        [Tooltip("Moves the gradient across the shape.")]
        public float gradientOffset;

        [Tooltip("Moves the gradient continuously. Units per second, 0 is still.")]
        public float scrollSpeed;

        [Tooltip("Ping-pong the gradient so scrolling never ends in a flat color.")]
        public bool repeatGradient = true;

        Material instance;
        Vector4 lastRect = new Vector4(0f, 0f, 1f, 1f);
        static readonly int idRect = Shader.PropertyToID("_Rect");
        static readonly int idColorA = Shader.PropertyToID("_ColorA");
        static readonly int idColorB = Shader.PropertyToID("_ColorB");
        static readonly int idAngle = Shader.PropertyToID("_Angle");
        static readonly int idOffset = Shader.PropertyToID("_Offset");
        static readonly int idScroll = Shader.PropertyToID("_Scroll");
        static readonly int idRepeat = Shader.PropertyToID("_Repeat");
        static readonly int idBlur = Shader.PropertyToID("_Blur");
        static readonly int idBrightness = Shader.PropertyToID("_Brightness");
        static readonly int idSoftness = Shader.PropertyToID("_Softness");
        static readonly int idLuma = Shader.PropertyToID("_UseLuma");
        static readonly int idBlend = Shader.PropertyToID("_DstBlend");

        public override Texture mainTexture
        {
            get { return shape != null ? shape.texture : s_WhiteTexture; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            EnsureMaterial();
            PushProperties();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (instance != null)
            {
                if (Application.isPlaying)
                    Destroy(instance);
                else
                    DestroyImmediate(instance);
                instance = null;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureMaterial();
            PushProperties();
        }

        void EnsureMaterial()
        {
            if (instance != null)
                return;

            Material baseMaterial = Resources.Load<Material>("MotionStudio/MotionGlow");
            if (baseMaterial != null)
                instance = new Material(baseMaterial);
            else
            {
                Shader shader = Shader.Find("MotionStudio/UIGlow");
                if (shader == null)
                    return;
                instance = new Material(shader);
            }

            instance.hideFlags = HideFlags.HideAndDontSave;
            material = instance;
        }

        /// <summary>Send the look to the shader. Call after changing a value from code.</summary>
        public void PushProperties()
        {
            if (instance == null)
                return;

            Apply(instance);
            Material rendering = materialForRendering;
            if (rendering != null && rendering != instance)
                Apply(rendering);
        }

        void Apply(Material m)
        {
            m.SetVector(idRect, lastRect);
            m.SetColor(idColorA, colorA);
            m.SetColor(idColorB, colorB);
            m.SetFloat(idAngle, gradientAngle);
            m.SetFloat(idOffset, gradientOffset);
            m.SetFloat(idScroll, scrollSpeed);
            m.SetFloat(idRepeat, repeatGradient ? 1f : 0f);
            m.SetFloat(idBlur, blur);
            m.SetFloat(idBrightness, brightness);
            m.SetFloat(idSoftness, softness);
            m.SetFloat(idLuma, shapeFromBrightness ? 1f : 0f);
            m.SetFloat(idBlend, additive ? 1f : 10f);
        }

        public override void SetMaterialDirty()
        {
            base.SetMaterialDirty();
            PushProperties();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector4 v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
            lastRect = new Vector4(r.x, r.y, r.width, r.height);
            PushProperties();

            Vector2 uvMin = Vector2.zero;
            Vector2 uvMax = Vector2.one;
            if (shape != null)
            {
                Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(shape);
                uvMin = new Vector2(outer.x, outer.y);
                uvMax = new Vector2(outer.z, outer.w);
            }

            // The blur reaches beyond the edge of the picture, so the mesh is made a little larger than the rectangle.
            // The extra area samples the transparent edge of the shape, it never changes the layout size.
            float reach = Mathf.Clamp01(blur) * Mathf.Clamp01(blur) * 0.45f + Mathf.Clamp01(blur) * 0.02f;
            float pad = reach * 1.05f;
            Vector2 size = new Vector2(v.z - v.x, v.w - v.y);
            Vector2 uvSize = uvMax - uvMin;
            v = new Vector4(v.x - size.x * pad, v.y - size.y * pad, v.z + size.x * pad, v.w + size.y * pad);
            uvMin -= uvSize * pad;
            uvMax += uvSize * pad;

            Color32 c = color;
            AddVertex(vh, new Vector3(v.x, v.y), c, new Vector2(uvMin.x, uvMin.y));
            AddVertex(vh, new Vector3(v.x, v.w), c, new Vector2(uvMin.x, uvMax.y));
            AddVertex(vh, new Vector3(v.z, v.w), c, new Vector2(uvMax.x, uvMax.y));
            AddVertex(vh, new Vector3(v.z, v.y), c, new Vector2(uvMax.x, uvMin.y));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        static void AddVertex(VertexHelper vh, Vector3 position, Color32 color, Vector2 uv0)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = uv0;
            vh.AddVert(vertex);
        }
    }
}
