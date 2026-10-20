#if MOTIONSTUDIO_TMP
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MotionStudio
{
    /// <summary>TextMeshPro: every word, character or line fades, moves, scales and changes color on its own.</summary>
    public sealed class TmpTextAdapterFactory : MotionTextAdapterFactory
    {
        public override IMotionTextAdapter Create(Component component)
        {
            TMP_Text text = component as TMP_Text;
            return text != null ? new TmpTextAdapter(text) : null;
        }
    }

    public sealed class TmpTextAdapter : IMotionTextAdapter
    {
        readonly TMP_Text text;
        TextSplit current;
        string capturedText;
        readonly List<int[]> units = new List<int[]>();
        Vector3[][] baseVertices;
        Color32[][] baseColors;
        Vector4[][] baseUvs0;
        const float SoftestScale = 0.05f;
        TextUnitState[] states = new TextUnitState[0];

        public TmpTextAdapter(TMP_Text text)
        {
            this.text = text;
        }

        public bool SupportsTransform
        {
            get { return true; }
        }

        public bool NeedsBegin(TextSplit split)
        {
            return baseVertices == null || current != split || capturedText != text.text;
        }

        public int UnitCount(TextSplit split)
        {
            Collect(split, false);
            return units.Count;
        }

        public void Begin(TextSplit split)
        {
            current = split;
            text.ForceMeshUpdate(true, true);
            Collect(split, true);
            capturedText = text.text;

            TMP_TextInfo info = text.textInfo;
            int meshCount = info.meshInfo.Length;
            baseVertices = new Vector3[meshCount][];
            baseColors = new Color32[meshCount][];
            baseUvs0 = new Vector4[meshCount][];
            for (int m = 0; m < meshCount; m++)
            {
                Vector3[] v = info.meshInfo[m].vertices;
                Color32[] c = info.meshInfo[m].colors32;
                baseVertices[m] = v != null ? (Vector3[])v.Clone() : new Vector3[0];
                baseColors[m] = c != null ? (Color32[])c.Clone() : new Color32[0];
                Vector4[] uv = info.meshInfo[m].uvs0;
                baseUvs0[m] = uv != null ? (Vector4[])uv.Clone() : new Vector4[0];
            }

            states = new TextUnitState[units.Count];
            for (int i = 0; i < states.Length; i++)
                states[i] = new TextUnitState { alpha = 1f, scale = 1f };
        }

        // each unit is a list of character indexes (visible characters only)
        void Collect(TextSplit split, bool forceUpdate)
        {
            if (forceUpdate || text.textInfo == null || text.textInfo.characterCount == 0)
                text.ForceMeshUpdate(true, true);

            units.Clear();
            TMP_TextInfo info = text.textInfo;
            if (info == null)
                return;

            switch (split)
            {
                case TextSplit.Words:
                    int wordCount = info.wordInfo != null ? Mathf.Min(info.wordCount, info.wordInfo.Length) : 0;
                    List<List<int>> words = new List<List<int>>();
                    bool[] claimed = new bool[Mathf.Max(0, info.characterCount)];
                    for (int w = 0; w < wordCount; w++)
                    {
                        TMP_WordInfo word = info.wordInfo[w];
                        List<int> chars = new List<int>();
                        int limit = info.characterInfo != null ? info.characterInfo.Length : 0;
                        for (int c = 0; c < word.characterCount; c++)
                        {
                            int index = word.firstCharacterIndex + c;
                            if (index < 0 || index >= limit || index >= claimed.Length)
                                continue;
                            chars.Add(index);
                            claimed[index] = true;
                        }

                        if (chars.Count > 0)
                            words.Add(chars);
                    }

                    int characterLimit = info.characterInfo != null ? Mathf.Min(info.characterCount, info.characterInfo.Length) : 0;
                    for (int c = 0; c < characterLimit; c++)
                    {
                        if (c < claimed.Length && claimed[c])
                            continue;
                        if (!info.characterInfo[c].isVisible)
                            continue;
                        if (words.Count == 0)
                        {
                            words.Add(new List<int>());
                            words[0].Add(c);
                            continue;
                        }

                        int owner = 0;
                        for (int w = 0; w < words.Count; w++)
                        {
                            if (words[w][0] <= c)
                                owner = w;
                        }

                        words[owner].Add(c);
                    }

                    for (int w = 0; w < words.Count; w++)
                        units.Add(words[w].ToArray());

                    break;
                case TextSplit.Lines:
                    int lineCount = info.lineInfo != null ? Mathf.Min(info.lineCount, info.lineInfo.Length) : 0;
                    for (int l = 0; l < lineCount; l++)
                    {
                        TMP_LineInfo line = info.lineInfo[l];
                        units.Add(Characters(info, line.firstCharacterIndex, line.characterCount));
                    }

                    break;
                default:
                    int characterCount = info.characterInfo != null ? Mathf.Min(info.characterCount, info.characterInfo.Length) : 0;
                    for (int c = 0; c < characterCount; c++)
                    {
                        if (info.characterInfo[c].isVisible)
                            units.Add(new[] { c });
                    }

                    break;
            }
        }

        static int[] Characters(TMP_TextInfo info, int first, int count)
        {
            int limit = info.characterInfo != null ? info.characterInfo.Length : 0;
            List<int> chars = new List<int>();
            for (int c = 0; c < count; c++)
            {
                int index = first + c;
                if (index >= 0 && index < limit)
                    chars.Add(index);
            }

            return chars.ToArray();
        }

        public void SetUnit(int index, TextUnitState state)
        {
            if (index >= 0 && index < states.Length)
                states[index] = state;
        }

        public void Commit()
        {
            TMP_TextInfo info = text.textInfo;
            if (info == null || baseVertices == null)
                return;

            for (int u = 0; u < units.Count; u++)
            {
                TextUnitState s = states[u];
                int[] chars = units[u];

                // centre of the whole piece, so scaling grows from its middle
                Vector3 centre = Vector3.zero;
                int visible = 0;
                for (int k = 0; k < chars.Length; k++)
                {
                    TMP_CharacterInfo ci = info.characterInfo[chars[k]];
                    if (!ci.isVisible)
                        continue;
                    int vi = ci.vertexIndex;
                    Vector3[] bv = baseVertices[ci.materialReferenceIndex];
                    centre += (bv[vi] + bv[vi + 2]) * 0.5f;
                    visible++;
                }

                if (visible > 0)
                    centre /= visible;

                for (int k = 0; k < chars.Length; k++)
                {
                    TMP_CharacterInfo ci = info.characterInfo[chars[k]];
                    if (!ci.isVisible)
                        continue;

                    int m = ci.materialReferenceIndex;
                    int vi = ci.vertexIndex;
                    Vector3[] destination = info.meshInfo[m].vertices;
                    Color32[] colors = info.meshInfo[m].colors32;
                    Vector3[] bv = baseVertices[m];
                    Color32[] bc = baseColors[m];
                    Vector4[] uvs0 = info.meshInfo[m].uvs0;
                    Vector4[] buv = baseUvs0[m];
                    bool hidden = s.alpha <= 0.001f;

                    for (int j = 0; j < 4; j++)
                    {
                        if (hidden)
                        {
                            destination[vi + j] = new Vector3(0f, -100000f, 0f);
                            colors[vi + j] = new Color32(0, 0, 0, 0);
                            continue;
                        }

                        Vector3 p = bv[vi + j];
                        p = centre + (p - centre) * s.scale;
                        p += new Vector3(s.offset.x, s.offset.y, 0f);
                        destination[vi + j] = p;

                        Color32 baseColor = bc[vi + j];
                        Color32 c = baseColor;
                        if (s.useColor)
                        {
                            Color tint = s.color;
                            c = new Color32(
                                (byte)Mathf.Clamp(Mathf.RoundToInt(baseColor.r * tint.r), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt(baseColor.g * tint.g), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt(baseColor.b * tint.b), 0, 255),
                                baseColor.a);
                        }
                        c.a = (byte)Mathf.RoundToInt(baseColor.a * Mathf.Clamp01(s.alpha));
                        colors[vi + j] = c;

                        // the SDF shader sharpens edges by |uv0.w| (its sign means bold), so a smaller value gives soft, blurred letters
                        if (uvs0 != null && buv != null && vi + j < uvs0.Length && vi + j < buv.Length)
                        {
                            Vector4 uv = buv[vi + j];
                            uv.w *= Mathf.Lerp(1f, SoftestScale, Mathf.Clamp01(s.blur));
                            uvs0[vi + j] = uv;
                        }
                    }
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Uv0);
        }

        public void Restore()
        {
            baseVertices = null;
            capturedText = null;
            text.ForceMeshUpdate(true, true);
        }
    }
}
#endif
