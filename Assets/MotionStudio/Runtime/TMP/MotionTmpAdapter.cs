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
            for (int m = 0; m < meshCount; m++)
            {
                Vector3[] v = info.meshInfo[m].vertices;
                Color32[] c = info.meshInfo[m].colors32;
                baseVertices[m] = v != null ? (Vector3[])v.Clone() : new Vector3[0];
                baseColors[m] = c != null ? (Color32[])c.Clone() : new Color32[0];
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
                    for (int w = 0; w < info.wordCount; w++)
                    {
                        TMP_WordInfo word = info.wordInfo[w];
                        List<int> chars = new List<int>();
                        for (int c = 0; c < word.characterCount; c++)
                            chars.Add(word.firstCharacterIndex + c);
                        units.Add(chars.ToArray());
                    }

                    break;
                case TextSplit.Lines:
                    for (int l = 0; l < info.lineCount; l++)
                    {
                        TMP_LineInfo line = info.lineInfo[l];
                        List<int> chars = new List<int>();
                        for (int c = 0; c < line.characterCount; c++)
                            chars.Add(line.firstCharacterIndex + c);
                        units.Add(chars.ToArray());
                    }

                    break;
                default:
                    for (int c = 0; c < info.characterCount; c++)
                    {
                        if (info.characterInfo[c].isVisible)
                            units.Add(new[] { c });
                    }

                    break;
            }
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

                    for (int j = 0; j < 4; j++)
                    {
                        Vector3 p = bv[vi + j];
                        p = centre + (p - centre) * s.scale;
                        p += new Vector3(s.offset.x, s.offset.y, 0f);
                        destination[vi + j] = p;

                        Color32 baseColor = bc[vi + j];
                        Color32 c = s.useColor ? (Color32)s.color : baseColor;
                        c.a = (byte)Mathf.RoundToInt(baseColor.a * Mathf.Clamp01(s.alpha));
                        colors[vi + j] = c;
                    }
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
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
