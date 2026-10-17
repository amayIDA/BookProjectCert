using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Particle hologram of the central screen. A direct port of the HG object in the HTML file:
/// roughly 1500 glowing particles that form words, icons, the brand X or a rotating sphere.
/// Positions are kept in the same top-left pixel space as the HTML canvas.
/// </summary>
[ExecuteAlways]
public class NIParticleField : MonoBehaviour
{
    sealed class Particle
    {
        public float x, y, vx, vy, tx, ty;
        public bool has;
        public float size;
        public float phase;
        public bool gold;
        public float depth = 1f;
        public float alpha;
        public float radius;
    }

    sealed class Ring
    {
        public float x, y, r, v, a;
        public Color color;
    }

    sealed class Mask
    {
        public Color32[] pixels;
        public int width;
        public int height;
        public int minX, minY, maxX, maxY;
    }

    const float Step = 1f / 60f;

    readonly List<Particle> ps = new List<Particle>();
    readonly List<Ring> rings = new List<Ring>();
    readonly List<Vector3> sphere = new List<Vector3>();
    readonly Dictionary<string, Mask> masks = new Dictionary<string, Mask>();

    public NIParticleLayer blueLayer;
    public NIParticleLayer goldLayer;
    [Tooltip("Additive material for the glow. Created by the builder.")]
    public Material additiveMaterial;
    RectTransform area;

    float width;
    float height;
    float flash;
    float gold = 0.12f;
    float angle;
    bool sphereMode;
    string kind;
    string arg;
    float argGold;
    float simTime;
    float accumulator;
    Coroutine wordsRoutine;

    public float W { get { return width; } }
    public float H { get { return height; } }

    public void Init(RectTransform rect)
    {
        area = rect;
        if (blueLayer == null)
            blueLayer = MakeLayer("ParticlesBlue", false);
        if (goldLayer == null)
            goldLayer = MakeLayer("ParticlesGold", true);
        blueLayer.field = this;
        goldLayer.field = this;
        Resize();
    }

    NIParticleLayer MakeLayer(string layerName, bool isGold)
    {
        Transform existing = area.Find(layerName);
        NIParticleLayer layer = existing != null ? existing.GetComponent<NIParticleLayer>() : null;
        if (layer == null)
        {
            RectTransform rect = NIUI.Rect(layerName, area);
            NIUI.Fill(rect);
            layer = rect.gameObject.AddComponent<NIParticleLayer>();
        }

        if (layer.GetComponent<CanvasRenderer>() == null)
            layer.gameObject.AddComponent<CanvasRenderer>();

        layer.field = this;
        layer.isGold = isGold;
        layer.raycastTarget = false;
        layer.texture = NIUI.ArtTexture(isGold ? "dot_gold" : "dot_blue");

        if (additiveMaterial == null)
            additiveMaterial = Resources.Load<Material>("NationalInitiative/ParticleAdditive");

        if (additiveMaterial == null)
        {
            Shader shader = Shader.Find("NI/UIAdditive");
            if (shader != null)
                additiveMaterial = new Material(shader);
        }

        if (additiveMaterial != null)
            layer.material = additiveMaterial;
        return layer;
    }

    void OnEnable()
    {
        if (area == null)
            area = GetComponent<RectTransform>();
        if (!Application.isPlaying && EnsureLayers())
            PreviewStatic();
    }

    /// <summary>Floating particles for the Scene view, so the hologram is visible while editing.</summary>
    public void PreviewStatic()
    {
        if (!EnsureLayers())
            return;
        blueLayer.field = this;
        goldLayer.field = this;
        width = 0f;
        Resize();

        // No shape in the editor: just the floating particles, like the moment between two words.
        ClearFormation();
        sphereMode = false;
        for (int i = 0; i < ps.Count; i++)
            ps[i].has = false;
        for (int i = 0; i < 160; i++)
            Simulate();
        blueLayer.SetVerticesDirty();
        goldLayer.SetVerticesDirty();
    }

    void Resize()
    {
        float w = Mathf.Max(1f, area.rect.width);
        float h = Mathf.Max(1f, area.rect.height);
        if (Mathf.Abs(w - width) < 0.5f && Mathf.Abs(h - height) < 0.5f && ps.Count > 0)
            return;

        width = w;
        height = h;
        int count = width < 640f ? 750 : 1500;
        while (ps.Count < count)
        {
            ps.Add(new Particle
            {
                x = UnityEngine.Random.value * width,
                y = UnityEngine.Random.value * height,
                size = 0.9f + UnityEngine.Random.value * 1.6f,
                phase = UnityEngine.Random.value * 6.28f
            });
        }

        if (ps.Count > count)
            ps.RemoveRange(count, ps.Count - count);

        int total = Mathf.Min(count, 900);
        sphere.Clear();
        float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int i = 0; i < total; i++)
        {
            float y = 1f - (i / (float)(total - 1)) * 2f;
            float rad = Mathf.Sqrt(1f - y * y);
            float theta = golden * i;
            sphere.Add(new Vector3(Mathf.Cos(theta) * rad, y, Mathf.Sin(theta) * rad));
        }

        if (kind != null)
            Formation(kind, arg, argGold);
    }

    // ---------- zone ----------

    float ZoneCx { get { return width * 0.21f; } }
    float ZoneCy { get { return height * 0.46f; } }
    float ZoneW { get { return width * 0.34f; } }
    float ZoneH { get { return height * 0.56f; } }

    // ---------- public effects ----------

    public void Flash(float value)
    {
        flash = value;
    }

    public void Scatter(float power)
    {
        for (int i = 0; i < ps.Count; i++)
        {
            Particle p = ps[i];
            p.has = false;
            float a = UnityEngine.Random.value * 6.28f;
            p.vx += Mathf.Cos(a) * power * UnityEngine.Random.value;
            p.vy += Mathf.Sin(a) * power * UnityEngine.Random.value;
        }

        sphereMode = false;
    }

    public void Burst(float power)
    {
        for (int i = 0; i < ps.Count; i++)
        {
            Particle p = ps[i];
            float dx = p.x - ZoneCx;
            float dy = p.y - ZoneCy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < 0.0001f)
                d = 1f;
            float k = power * (0.4f + UnityEngine.Random.value);
            p.vx += dx / d * k;
            p.vy += dy / d * k;
        }
    }

    public void AddRing(Color color, float speed)
    {
        rings.Add(new Ring { x = ZoneCx, y = ZoneCy, r = 10f, v = speed <= 0f ? 9f : speed, a = 1f, color = color });
    }

    public void ClearFormation()
    {
        if (wordsRoutine != null)
        {
            StopCoroutine(wordsRoutine);
            wordsRoutine = null;
        }

        kind = null;
    }

    /// <summary>kind: words, sphere, icon, brandx.</summary>
    public void Formation(string newKind, string newArg = null, float newGold = 0f)
    {
        if (wordsRoutine != null)
        {
            StopCoroutine(wordsRoutine);
            wordsRoutine = null;
        }

        kind = newKind;
        arg = newArg;
        argGold = newGold;

        if (kind == "words")
        {
            gold = 0.12f;
            if (Application.isPlaying)
                wordsRoutine = StartCoroutine(Words());
            else
                SetTargets(SampleWord("work"));
        }
        else if (kind == "sphere")
        {
            gold = newGold > 0f ? newGold : 0.15f;
            sphereMode = true;
            for (int i = 0; i < ps.Count; i++)
            {
                ps[i].has = i < sphere.Count;
                ps[i].gold = UnityEngine.Random.value < gold;
            }
        }
        else if (kind == "brandx")
        {
            gold = newGold > 0f ? newGold : 0.3f;
            SetTargets(SampleBrandX());
        }
        else if (kind == "icon")
        {
            gold = 0.22f;
            SetTargets(SampleIcon(arg));
        }
    }

    IEnumerator Words()
    {
        string[] seq = { "work", "role", "sector", "leader", "*x" };
        int index = 0;
        while (true)
        {
            Scatter(2.2f);
            yield return new WaitForSeconds(0.9f);
            string word = seq[index++ % seq.Length];
            SetTargets(word == "*x" ? SampleBrandX() : SampleWord(word));
            yield return new WaitForSeconds(3.3f);
        }
    }

    // ---------- sampling ----------

    void SetTargets(List<Vector2> pts)
    {
        sphereMode = false;
        for (int i = 0; i < ps.Count; i++)
        {
            Particle p = ps[i];
            if (i < pts.Count)
            {
                p.tx = pts[i].x + (UnityEngine.Random.value - 0.5f);
                p.ty = pts[i].y + (UnityEngine.Random.value - 0.5f);
                p.has = true;
            }
            else
                p.has = false;

            p.gold = UnityEngine.Random.value < gold;
        }
    }

    Mask GetMask(string name)
    {
        Mask mask;
        if (masks.TryGetValue(name, out mask))
            return mask;

        Texture2D texture = NIUI.ArtTexture(name);
        if (texture != null)
        {
            try
            {
                mask = new Mask { pixels = texture.GetPixels32(), width = texture.width, height = texture.height };
                mask.minX = mask.width;
                mask.minY = mask.height;
                mask.maxX = -1;
                mask.maxY = -1;
                for (int y = 0; y < mask.height; y++)
                {
                    for (int x = 0; x < mask.width; x++)
                    {
                        if (mask.pixels[y * mask.width + x].a > 120)
                        {
                            if (x < mask.minX) mask.minX = x;
                            if (x > mask.maxX) mask.maxX = x;
                            if (y < mask.minY) mask.minY = y;
                            if (y > mask.maxY) mask.maxY = y;
                        }
                    }
                }
            }
            catch (Exception)
            {
                mask = null;
            }
        }

        masks[name] = mask;
        return mask;
    }

    List<Vector2> SampleWord(string word)
    {
        Mask mask = GetMask("word_" + word);
        if (mask == null || mask.maxX < 0)
            return new List<Vector2>();

        float bw = mask.maxX - mask.minX + 1;
        float bh = mask.maxY - mask.minY + 1;
        float scale = Mathf.Min(ZoneW * 0.94f / bw, ZoneH * 0.58f / bh);
        float w = bw * scale;
        float h = bh * scale;
        return SampleRegion(mask, mask.minX, mask.minY, mask.maxX + 1, mask.maxY + 1, ZoneCx - w / 2f, ZoneCy - h / 2f, w, h);
    }

    List<Vector2> SampleBrandX()
    {
        Mask mask = GetMask("brandx_mask");
        if (mask == null)
            return new List<Vector2>();

        float h = Mathf.Min(ZoneH * 1.05f, ZoneW);
        float w = h * 290f / 332f;
        return SampleRegion(mask, 0, 0, mask.width, mask.height, ZoneCx - w / 2f, ZoneCy - h / 2f, w, h);
    }

    List<Vector2> SampleIcon(string icon)
    {
        Mask mask = GetMask("icon_" + icon);
        if (mask == null)
            return new List<Vector2>();

        float s = Mathf.Min(ZoneH * 1.05f, ZoneW);
        return SampleRegion(mask, 0, 0, mask.width, mask.height, ZoneCx - s / 2f, ZoneCy - s / 2f, s, s);
    }

    /// <summary>Port of samplePixels: raise the step until about 1.4 times the particle budget remains, then shuffle and cut.</summary>
    List<Vector2> SampleRegion(Mask mask, int sx0, int sy0, int sx1, int sy1, float ox, float oy, float w, float h)
    {
        float limit = ps.Count * 0.92f;
        int step = 2;
        int cols = Mathf.Max(1, Mathf.CeilToInt(w));
        int rows = Mathf.Max(1, Mathf.CeilToInt(h));
        List<Vector2> pts = new List<Vector2>();
        float srcW = sx1 - sx0;
        float srcH = sy1 - sy0;
        for (;;)
        {
            pts.Clear();
            for (int y = 0; y < rows; y += step)
            {
                for (int x = 0; x < cols; x += step)
                {
                    int mx = sx0 + Mathf.Min((int)(x / w * srcW), (int)srcW - 1);
                    int my = sy0 + Mathf.Min((int)((1f - y / h) * srcH), (int)srcH - 1);
                    my = Mathf.Clamp(my, 0, mask.height - 1);
                    if (mask.pixels[my * mask.width + mx].a > 120)
                        pts.Add(new Vector2(ox + x, oy + y));
                }
            }

            if (pts.Count <= limit * 1.5f || step > 40)
                break;
            step++;
        }

        for (int i = pts.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector2 tmp = pts[i];
            pts[i] = pts[j];
            pts[j] = tmp;
        }

        int keep = Mathf.Min(pts.Count, Mathf.FloorToInt(limit));
        if (pts.Count > keep)
            pts.RemoveRange(keep, pts.Count - keep);
        return pts;
    }

    // ---------- simulation ----------

    /// <summary>Recreates the two particle layers when they were deleted or destroyed in the scene.</summary>
    bool EnsureLayers()
    {
        if (area == null)
            area = GetComponent<RectTransform>();
        if (area == null)
            return false;

        if (blueLayer == null || goldLayer == null)
            Init(area);

        return blueLayer != null && goldLayer != null;
    }

    void Update()
    {
        if (!Application.isPlaying || area == null)
            return;

        if (!EnsureLayers())
            return;

        Resize();
        accumulator += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        int guard = 0;
        while (accumulator >= Step && guard < 5)
        {
            accumulator -= Step;
            guard++;
            Simulate();
        }

        blueLayer.SetVerticesDirty();
        goldLayer.SetVerticesDirty();
    }

    void Simulate()
    {
        simTime += Step * 1000f;
        float t = simTime;

        if (sphereMode)
        {
            angle += 0.0035f;
            float R = Mathf.Min(ZoneW, ZoneH) * 0.55f;
            float ca = Mathf.Cos(angle);
            float sa = Mathf.Sin(angle);
            float ct = Mathf.Cos(0.45f);
            float st = Mathf.Sin(0.45f);
            float breathe = 1f + Mathf.Sin(t * 0.0012f) * 0.04f;
            for (int i = 0; i < sphere.Count && i < ps.Count; i++)
            {
                Vector3 s = sphere[i];
                Particle p = ps[i];
                float X = s.x * ca - s.z * sa;
                float Z = s.x * sa + s.z * ca;
                float Y = s.y * ct - Z * st;
                float Z2 = s.y * st + Z * ct;
                p.tx = ZoneCx + X * R * breathe;
                p.ty = ZoneCy + Y * R * breathe;
                p.depth = (Z2 + 1f) * 0.5f;
                p.has = true;
            }
        }

        float tw = t * 0.003f;
        for (int i = 0; i < ps.Count; i++)
        {
            Particle p = ps[i];
            if (p.has)
            {
                p.vx += (p.tx - p.x) * 0.045f;
                p.vy += (p.ty - p.y) * 0.045f;
                p.vx *= 0.84f;
                p.vy *= 0.84f;
            }
            else
            {
                p.vx += Mathf.Sin(t * 0.0004f + p.phase) * 0.012f;
                p.vy += Mathf.Cos(t * 0.0005f + p.phase * 1.3f) * 0.012f;
                p.vx *= 0.985f;
                p.vy *= 0.985f;
                if (p.x < 0f) p.x += width;
                if (p.x > width) p.x -= width;
                if (p.y < 0f) p.y += height;
                if (p.y > height) p.y -= height;
            }

            p.x += p.vx;
            p.y += p.vy;

            float a = p.has ? (0.55f + 0.45f * Mathf.Sin(tw + p.phase)) : 0.16f;
            if (sphereMode && p.has)
                a *= 0.25f + 0.75f * p.depth;
            p.radius = p.size * NIUI.S * (p.has ? 1.9f : 1.3f) * (1f + flash * 1.2f);
            p.alpha = Mathf.Min(1f, a * (1f + flash));
        }

        for (int i = rings.Count - 1; i >= 0; i--)
        {
            Ring r = rings[i];
            r.r += r.v;
            r.v *= 0.985f;
            r.a *= 0.955f;
            if (r.a < 0.02f)
                rings.RemoveAt(i);
        }

        flash *= 0.94f;
    }

    // ---------- drawing ----------

    public void Fill(VertexHelper vh, bool goldPass)
    {
        vh.Clear();
        float hw = width * 0.5f;
        float hh = height * 0.5f;
        int quad = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            Particle p = ps[i];
            if (p.gold != goldPass)
                continue;

            float x = p.x - hw;
            float y = hh - p.y;
            float s = p.radius;
            Color32 c = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(p.alpha) * 255f));
            AddQuad(vh, x - s, y - s, x + s, y + s, c, 0f, 0f, 1f, 1f);
            quad++;
        }

        if (!goldPass)
        {
            for (int i = 0; i < rings.Count; i++)
                DrawRing(vh, rings[i], hw, hh);
        }
    }

    static void AddQuad(VertexHelper vh, float x0, float y0, float x1, float y1, Color32 c, float u0, float v0, float u1, float v1)
    {
        int index = vh.currentVertCount;
        vh.AddVert(new Vector3(x0, y0), c, new Vector2(u0, v0));
        vh.AddVert(new Vector3(x0, y1), c, new Vector2(u0, v1));
        vh.AddVert(new Vector3(x1, y1), c, new Vector2(u1, v1));
        vh.AddVert(new Vector3(x1, y0), c, new Vector2(u1, v0));
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }

    void DrawRing(VertexHelper vh, Ring ring, float hw, float hh)
    {
        const int segments = 56;
        float rx = ring.r;
        float ry = ring.r * 0.32f;
        float thickness = 1.4f;
        Color32 c = new Color32((byte)(ring.color.r * 255f), (byte)(ring.color.g * 255f), (byte)(ring.color.b * 255f), (byte)(Mathf.Clamp01(ring.a) * 255f));
        Vector2 previous = new Vector2(ring.x - hw + rx, hh - ring.y);
        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector2 current = new Vector2(ring.x - hw + Mathf.Cos(a) * rx, hh - ring.y + Mathf.Sin(a) * ry);
            Vector2 dir = (current - previous).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x) * thickness;
            int index = vh.currentVertCount;
            // The centre of the dot texture is solid white, so sample there.
            Vector2 uv = new Vector2(0.5f, 0.5f);
            vh.AddVert(previous - n, c, uv);
            vh.AddVert(previous + n, c, uv);
            vh.AddVert(current + n, c, uv);
            vh.AddVert(current - n, c, uv);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
            previous = current;
        }
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public class NIParticleLayer : MaskableGraphic
{
    public NIParticleField field;
    public bool isGold;
    public Texture texture;

    public override Texture mainTexture
    {
        get { return texture != null ? texture : s_WhiteTexture; }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (field == null)
        {
            vh.Clear();
            return;
        }

        field.Fill(vh, isGold);
    }
}
