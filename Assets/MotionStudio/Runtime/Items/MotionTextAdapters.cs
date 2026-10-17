using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    /// <summary>Finds the adapter that can animate the pieces of a text component (legacy Text, TextMeshPro).</summary>
    public static class MotionTextAdapters
    {
        static List<MotionTextAdapterFactory> factories;

        static void Discover()
        {
            if (factories != null)
                return;

            factories = new List<MotionTextAdapterFactory>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                string assemblyName = assemblies[a].GetName().Name;
                if (!assemblyName.StartsWith("MotionStudio", StringComparison.Ordinal))
                    continue;

                Type[] types;
                try
                {
                    types = assemblies[a].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || type.IsAbstract || !typeof(MotionTextAdapterFactory).IsAssignableFrom(type))
                        continue;
                    try
                    {
                        factories.Add((MotionTextAdapterFactory)Activator.CreateInstance(type));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public static IMotionTextAdapter Create(Component component)
        {
            if (component == null)
                return null;

            Discover();
            for (int i = 0; i < factories.Count; i++)
            {
                IMotionTextAdapter adapter = factories[i].Create(component);
                if (adapter != null)
                    return adapter;
            }

            return null;
        }

        /// <summary>Splits a string into pieces. Whitespace stays between the pieces. Rich text tags stay attached to the piece they belong to.</summary>
        public static void Split(string source, TextSplit split, List<string> pieces, List<string> separators)
        {
            pieces.Clear();
            separators.Clear();
            if (string.IsNullOrEmpty(source))
                return;

            if (split == TextSplit.Lines || split == TextSplit.Whole)
            {
                string[] lines = split == TextSplit.Whole ? new[] { source } : source.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    pieces.Add(lines[i]);
                    separators.Add(i < lines.Length - 1 ? "\n" : string.Empty);
                }

                return;
            }

            StringBuilder piece = new StringBuilder();
            StringBuilder gap = new StringBuilder();
            bool inTag = false;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (c == '<')
                    inTag = true;

                bool whitespace = !inTag && char.IsWhiteSpace(c);
                if (split == TextSplit.Words)
                {
                    if (whitespace)
                        gap.Append(c);
                    else
                    {
                        if (gap.Length > 0)
                        {
                            if (piece.Length > 0)
                            {
                                pieces.Add(piece.ToString());
                                separators.Add(gap.ToString());
                                piece.Length = 0;
                            }
                            else
                                piece.Append(gap.ToString());
                            gap.Length = 0;
                        }

                        piece.Append(c);
                    }
                }
                else
                {
                    if (whitespace)
                    {
                        if (pieces.Count > 0)
                            separators[pieces.Count - 1] += c;
                        else
                            piece.Append(c);
                    }
                    else
                    {
                        piece.Append(c);
                        if (!inTag)
                        {
                            pieces.Add(piece.ToString());
                            separators.Add(string.Empty);
                            piece.Length = 0;
                        }
                    }
                }

                if (c == '>')
                    inTag = false;
            }

            if (piece.Length > 0)
            {
                pieces.Add(piece.ToString());
                separators.Add(gap.ToString());
            }
            else if (gap.Length > 0 && pieces.Count > 0)
                separators[pieces.Count - 1] += gap.ToString();
        }
    }

    /// <summary>Legacy UnityEngine.UI.Text: pieces fade and change color through rich text tags. Offsets and scale are not possible with this component.</summary>
    public sealed class LegacyTextAdapterFactory : MotionTextAdapterFactory
    {
        public override IMotionTextAdapter Create(Component component)
        {
            Text text = component as Text;
            return text != null ? new LegacyTextAdapter(text) : null;
        }
    }

    public sealed class LegacyTextAdapter : IMotionTextAdapter
    {
        readonly Text text;
        readonly List<string> pieces = new List<string>();
        readonly List<string> separators = new List<string>();
        TextUnitState[] states = new TextUnitState[0];
        string source;
        string lastWritten;
        TextSplit current;
        readonly StringBuilder builder = new StringBuilder();

        public LegacyTextAdapter(Text text)
        {
            this.text = text;
        }

        public bool SupportsTransform
        {
            get { return false; }
        }

        string CurrentSource()
        {
            return text.text == lastWritten && source != null ? source : text.text;
        }

        public int UnitCount(TextSplit split)
        {
            List<string> p = new List<string>();
            List<string> s = new List<string>();
            MotionTextAdapters.Split(CurrentSource(), split, p, s);
            return p.Count;
        }

        public bool NeedsBegin(TextSplit split)
        {
            return source == null || current != split || (text.text != lastWritten && text.text != source);
        }

        public void Begin(TextSplit split)
        {
            source = CurrentSource();
            current = split;
            text.supportRichText = true;
            MotionTextAdapters.Split(source, split, pieces, separators);
            states = new TextUnitState[pieces.Count];
            for (int i = 0; i < states.Length; i++)
                states[i] = new TextUnitState { alpha = 1f, scale = 1f };
        }

        public void SetUnit(int index, TextUnitState state)
        {
            if (index >= 0 && index < states.Length)
                states[index] = state;
        }

        public void Commit()
        {
            builder.Length = 0;
            Color baseColor = text.color;
            for (int i = 0; i < pieces.Count; i++)
            {
                TextUnitState s = states[i];
                Color c = s.useColor ? s.color : baseColor;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(s.alpha) * 255f);
                builder.Append("<color=#")
                    .Append(((byte)Mathf.RoundToInt(c.r * 255f)).ToString("X2", CultureInfo.InvariantCulture))
                    .Append(((byte)Mathf.RoundToInt(c.g * 255f)).ToString("X2", CultureInfo.InvariantCulture))
                    .Append(((byte)Mathf.RoundToInt(c.b * 255f)).ToString("X2", CultureInfo.InvariantCulture))
                    .Append(a.ToString("X2", CultureInfo.InvariantCulture))
                    .Append('>')
                    .Append(pieces[i])
                    .Append("</color>")
                    .Append(separators[i]);
            }

            lastWritten = builder.ToString();
            text.text = lastWritten;
        }

        public void Restore()
        {
            if (source != null)
                text.text = source;
            lastWritten = null;
            source = null;
        }
    }
}
