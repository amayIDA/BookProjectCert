using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    public enum ComposeStyle
    {
        /// <summary>Pieces fly in from outside and turn into place.</summary>
        Assemble,

        /// <summary>Pieces pop up one by one like pixels filling a grid.</summary>
        Pixel,

        /// <summary>Pieces slide in from one side without turning.</summary>
        Slide,

        /// <summary>Pieces only fade in, in a wave.</summary>
        Dissolve
    }

    public enum ComposeStart
    {
        Scatter,
        Left,
        Right,
        Top,
        Bottom,
        Radial
    }

    public enum ComposeWave
    {
        FromCenter,
        ToCenter,
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
        Diagonal,
        Random
    }

    /// <summary>
    /// Builds an icon out of pieces. Give it an Image or RawImage with any png or icon, it cuts the picture into a grid of tiles
    /// that fly, pop or fade into place in a wave and finish as the exact original picture.
    /// </summary>
    [AddComponentMenu("Motion Studio/Motion Icon Composer")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MotionIconComposer : MotionBehaviour
    {
        [Header("Picture")]
        [Tooltip("Image or RawImage that shows the icon. Leave empty to use the one on this object.")]
        public Graphic source;

        [Range(2, 40)] public int columns = 10;
        [Range(2, 40)] public int rows = 10;

        [Header("Look")]
        public ComposeStyle style = ComposeStyle.Assemble;
        public ComposeStart startFrom = ComposeStart.Scatter;
        public ComposeWave wave = ComposeWave.FromCenter;

        [Header("Timing")]
        [Min(0.1f)] public float duration = 1.8f;
        public MotionEase ease = MotionEase.AppleOut;

        [Tooltip("How much of the total time one piece needs. Small values make a sharp wave, large values a soft overlap.")]
        [Range(0.1f, 1f)] public float pieceShare = 0.45f;

        [Header("Pieces")]
        [Tooltip("Distance the pieces start away from the icon, in canvas units.")]
        public float distance = 320f;
        [Range(0f, 360f)] public float spinFrom = 120f;
        [Range(0f, 1.5f)] public float scaleFrom = 0.2f;
        [Range(0f, 1f)] public float randomness = 0.5f;
        public int seed = 7;

        [Tooltip("Show the original picture again when the animation is done so there are no seams.")]
        public bool restoreOriginalAtEnd = true;

        [Header("Fade")]
        [Tooltip("Seconds each piece needs to fade in. 0 uses a quarter of the piece time.")]
        [Min(0f)] public float fadeDuration = 0f;
        public MotionEase fadeEase = MotionEase.CubicOut;

        [Header("Color")]
        [Tooltip("Turn on to move every piece from the initial to the final color while it arrives.")]
        public bool changeColor;
        public Color colorFrom = Color.white;
        public Color colorTo = Color.white;
        [Tooltip("Seconds a piece needs for the color change. 0 uses the piece time.")]
        [Min(0f)] public float colorDuration = 0f;
        public MotionEase colorEase = MotionEase.SineInOut;
        Color restColor = Color.white;
        bool colorTouched;

        sealed class Tile
        {
            public RectTransform rect;
            public RawImage image;
            public Vector2 position;
            public Vector2 startOffset;
            public float startRotation;
            public float rank;
            public float random;
        }

        readonly List<Tile> tiles = new List<Tile>();
        RectTransform container;
        string builtKey;
        int rankKey = -1;

        // ---------- setup ----------

        Graphic Source
        {
            get
            {
                if (source == null)
                    source = GetComponent<Graphic>();
                return source;
            }
        }

        public override void Prepare()
        {
            Build(false);
        }

        void Awake()
        {
            Build(false);
        }

        string Key()
        {
            Graphic g = Source;
            Image image = g as Image;
            RawImage raw = g as RawImage;
            string id = image != null && image.sprite != null ? image.sprite.GetHashCode().ToString() : (raw != null && raw.texture != null ? raw.texture.GetHashCode().ToString() : "none");
            return id + "|" + columns + "|" + rows + "|" + seed + "|" + startFrom + "|" + wave + "|" + distance + "|" + spinFrom + "|" + randomness;
        }

        /// <summary>Cut the picture into tiles. Called automatically.</summary>
        [ContextMenu("Rebuild Tiles")]
        public void Rebuild()
        {
            Build(true);
        }

        void Build(bool force)
        {
            Graphic g = Source;
            if (g == null)
                return;

            string key = Key();
            if (!force && builtKey == key && container != null)
                return;

            ClearTiles();
            builtKey = key;

            Texture texture;
            Rect uv;
            Rect fitted;
            if (!ReadSource(g, out texture, out uv, out fitted) || texture == null)
                return;

            GameObject holder = new GameObject("~IconTiles", typeof(RectTransform));
            holder.hideFlags = HideFlags.HideAndDontSave;
            holder.layer = gameObject.layer;
            holder.transform.SetParent(transform, false);
            container = (RectTransform)holder.transform;
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            LayoutElement ignore = holder.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;

            Texture2D readable = texture as Texture2D;
            bool canRead = readable != null && readable.isReadable;
            System.Random random = new System.Random(seed);

            Vector2 tileSize = new Vector2(fitted.width / columns, fitted.height / rows);
            Vector2 centre = ((RectTransform)transform).rect.center;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Rect cellUv = new Rect(
                        uv.x + uv.width * x / columns,
                        uv.y + uv.height * y / rows,
                        uv.width / columns,
                        uv.height / rows);

                    if (canRead && IsEmpty(readable, cellUv))
                        continue;

                    GameObject go = new GameObject("Tile", typeof(RectTransform), typeof(RawImage));
                    go.hideFlags = HideFlags.HideAndDontSave;
                    go.layer = gameObject.layer;
                    go.transform.SetParent(container, false);
                    Tile tile = new Tile();
                    tile.rect = (RectTransform)go.transform;
                    tile.rect.anchorMin = tile.rect.anchorMax = new Vector2(0.5f, 0.5f);
                    tile.rect.pivot = new Vector2(0.5f, 0.5f);
                    // one pixel more so neighbouring tiles overlap and no gap shows while they move
                    tile.rect.sizeDelta = tileSize + Vector2.one * 0.75f;
                    Vector2 cellCentre = new Vector2(fitted.x + tileSize.x * (x + 0.5f), fitted.y + tileSize.y * (y + 0.5f)) - centre;
                    tile.position = cellCentre;
                    tile.rect.anchoredPosition = cellCentre;
                    tile.image = go.GetComponent<RawImage>();
                    tile.image.texture = texture;
                    tile.image.uvRect = cellUv;
                    tile.image.raycastTarget = false;
                    tile.random = (float)random.NextDouble();
                    tile.startRotation = (((float)random.NextDouble() * 2f - 1f) * spinFrom) * Mathf.Lerp(0.4f, 1f, randomness);
                    tile.startOffset = StartOffset(tile, cellCentre, random);
                    tiles.Add(tile);
                }
            }

            ComputeRanks();
        }

        bool ReadSource(Graphic g, out Texture texture, out Rect uv, out Rect fitted)
        {
            RectTransform rt = (RectTransform)transform;
            Rect rect = rt.rect;
            texture = null;
            uv = new Rect(0, 0, 1, 1);
            fitted = rect;

            Image image = g as Image;
            if (image != null && image.sprite != null)
            {
                Sprite sprite = image.sprite;
                texture = sprite.texture;
                Rect tr = sprite.textureRect;
                uv = new Rect(tr.x / texture.width, tr.y / texture.height, tr.width / texture.width, tr.height / texture.height);
                if (image.preserveAspect && sprite.rect.height > 0f)
                {
                    float aspect = sprite.rect.width / sprite.rect.height;
                    float w = rect.width;
                    float h = rect.height;
                    if (w / h > aspect)
                        w = h * aspect;
                    else
                        h = w / aspect;
                    fitted = new Rect(rect.center.x - w * 0.5f, rect.center.y - h * 0.5f, w, h);
                }

                return true;
            }

            RawImage raw = g as RawImage;
            if (raw != null && raw.texture != null)
            {
                texture = raw.texture;
                uv = raw.uvRect;
                return true;
            }

            return false;
        }

        static bool IsEmpty(Texture2D texture, Rect uv)
        {
            try
            {
                for (int sy = 0; sy < 3; sy++)
                {
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float u = uv.x + uv.width * (sx + 0.5f) / 3f;
                        float v = uv.y + uv.height * (sy + 0.5f) / 3f;
                        if (texture.GetPixelBilinear(u, v).a > 0.03f)
                            return false;
                    }
                }

                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        Vector2 StartOffset(Tile tile, Vector2 cellCentre, System.Random random)
        {
            float k = Mathf.Lerp(1f - randomness, 1f, tile.random);
            switch (startFrom)
            {
                case ComposeStart.Left: return new Vector2(-distance * k, 0f);
                case ComposeStart.Right: return new Vector2(distance * k, 0f);
                case ComposeStart.Top: return new Vector2(0f, distance * k);
                case ComposeStart.Bottom: return new Vector2(0f, -distance * k);
                case ComposeStart.Radial:
                {
                    Vector2 d = cellCentre.sqrMagnitude < 0.01f ? Vector2.up : cellCentre.normalized;
                    return d * distance * k;
                }
                default:
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance * k;
                }
            }
        }

        void ComputeRanks()
        {
            if (tiles.Count == 0)
                return;

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < tiles.Count; i++)
            {
                minX = Mathf.Min(minX, tiles[i].position.x);
                maxX = Mathf.Max(maxX, tiles[i].position.x);
                minY = Mathf.Min(minY, tiles[i].position.y);
                maxY = Mathf.Max(maxY, tiles[i].position.y);
            }

            float w = Mathf.Max(0.0001f, maxX - minX);
            float h = Mathf.Max(0.0001f, maxY - minY);
            Vector2 centre = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            float maxDistance = 0.0001f;
            for (int i = 0; i < tiles.Count; i++)
                maxDistance = Mathf.Max(maxDistance, (tiles[i].position - centre).magnitude);

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile t = tiles[i];
                float nx = (t.position.x - minX) / w;
                float ny = (t.position.y - minY) / h;
                switch (wave)
                {
                    case ComposeWave.FromCenter: t.rank = (t.position - centre).magnitude / maxDistance; break;
                    case ComposeWave.ToCenter: t.rank = 1f - (t.position - centre).magnitude / maxDistance; break;
                    case ComposeWave.LeftToRight: t.rank = nx; break;
                    case ComposeWave.RightToLeft: t.rank = 1f - nx; break;
                    case ComposeWave.TopToBottom: t.rank = 1f - ny; break;
                    case ComposeWave.BottomToTop: t.rank = ny; break;
                    case ComposeWave.Diagonal: t.rank = (nx + (1f - ny)) * 0.5f; break;
                    default: t.rank = t.random; break;
                }
            }
        }

        void ClearTiles()
        {
            tiles.Clear();
            if (container != null)
            {
                if (Application.isPlaying)
                    Destroy(container.gameObject);
                else
                    DestroyImmediate(container.gameObject);
                container = null;
            }
        }

        void OnDestroy()
        {
            ClearTiles();
        }

        // ---------- evaluation ----------

        public override float ContentDuration
        {
            get { return duration; }
        }

        protected override void EvaluateContent(float time)
        {
            Build(false);
            Graphic g = Source;
            if (g == null || container == null)
                return;

            if (changeColor && !colorTouched)
            {
                restColor = g.color;
                colorTouched = true;
            }

            bool finished = time >= duration && restoreOriginalAtEnd;
            if (finished)
            {
                container.gameObject.SetActive(false);
                g.enabled = true;
                if (changeColor)
                    g.color = colorTo;
                return;
            }

            g.enabled = false;
            container.gameObject.SetActive(true);

            float pieceTime = Mathf.Max(0.0001f, duration * pieceShare);
            float spread = duration - pieceTime;
            Color tint = colorTouched ? restColor : g.color;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile t = tiles[i];
                float local = time - t.rank * spread;
                float p = Mathf.Clamp01(local / pieceTime);
                float e = MotionEasing.Evaluate(ease, p);

                Vector2 offset = Vector2.zero;
                float rotation = 0f;
                float s = 1f;
                float fadeTime = fadeDuration > 0f ? fadeDuration : pieceTime * 0.25f;
                float alpha = MotionEasing.Evaluate(fadeEase, Mathf.Clamp01(local / Mathf.Max(0.0001f, fadeTime)));

                switch (style)
                {
                    case ComposeStyle.Assemble:
                        offset = t.startOffset * (1f - e);
                        rotation = t.startRotation * (1f - e);
                        s = Mathf.LerpUnclamped(scaleFrom, 1f, e);
                        break;
                    case ComposeStyle.Slide:
                        offset = t.startOffset * (1f - e);
                        break;
                    case ComposeStyle.Pixel:
                        s = Mathf.LerpUnclamped(0f, 1f, e);
                        break;
                    case ComposeStyle.Dissolve:
                        alpha = e;
                        break;
                }

                t.rect.anchoredPosition = t.position + offset;
                t.rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
                t.rect.localScale = new Vector3(s, s, 1f);
                Color c = tint;
                if (changeColor)
                {
                    float colorTime = colorDuration > 0f ? colorDuration : pieceTime;
                    c = Color.LerpUnclamped(colorFrom, colorTo, MotionEasing.Evaluate(colorEase, Mathf.Clamp01(local / Mathf.Max(0.0001f, colorTime))));
                    c.a = tint.a;
                }

                c.a = c.a * alpha;
                t.image.color = c;
            }
        }

        public override void ApplyRestState()
        {
            Graphic g = Source;
            if (container != null)
                container.gameObject.SetActive(false);
            if (g != null)
            {
                g.enabled = true;
                if (colorTouched)
                {
                    g.color = restColor;
                    colorTouched = false;
                }
            }
        }
    }
}
