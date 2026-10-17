using UnityEditor;
using UnityEngine;

namespace MotionStudio.Editor
{
    /// <summary>Shared look of the Motion Studio inspectors.</summary>
    public static class MotionEditorStyle
    {
        public static readonly Color Accent = new Color(0.93f, 0.78f, 0.45f, 1f);
        public static readonly Color Panel = new Color(0.16f, 0.17f, 0.2f, 1f);
        public static readonly Color Bar = new Color(0.36f, 0.55f, 0.95f, 1f);

        static GUIStyle title;
        static GUIStyle sub;

        public static void Banner(string text, string subtitle)
        {
            if (title == null)
            {
                title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = Accent } };
                sub = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, normal = { textColor = new Color(0.7f, 0.74f, 0.82f) } };
            }

            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(r.x - 4, r.y - 2, r.width + 8, r.height + 6), Panel);
            EditorGUILayout.LabelField(text, title);
            EditorGUILayout.LabelField(subtitle, sub);
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
        }

        public static Color ColorFor(MotionBehaviour b)
        {
            if (b is MotionItem)
                return new Color(0.36f, 0.55f, 0.95f);
            if (b is MotionCard)
                return new Color(0.93f, 0.78f, 0.45f);
            if (b is MotionGlow)
                return new Color(0.75f, 0.45f, 0.95f);
            if (b is MotionIconComposer)
                return new Color(0.3f, 0.8f, 0.62f);
            return new Color(0.6f, 0.6f, 0.6f);
        }

        public static string KindOf(MotionBehaviour b)
        {
            if (b is MotionItem)
                return "Item";
            if (b is MotionCard)
                return "Card";
            if (b is MotionGlow)
                return "Glow";
            if (b is MotionIconComposer)
                return "Icon";
            return "Motion";
        }
    }
}
