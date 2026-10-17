using UnityEditor;
using UnityEngine;

namespace MotionStudio.Editor
{
    /// <summary>Inspector for every single animation component: the normal fields plus a preview you can play and scrub.</summary>
    [CustomEditor(typeof(MotionBehaviour), true)]
    [CanEditMultipleObjects]
    public class MotionBehaviourEditor : UnityEditor.Editor
    {
        float scrub;

        public override void OnInspectorGUI()
        {
            MotionBehaviour b = (MotionBehaviour)target;
            MotionEditorStyle.Banner("Motion Studio · " + MotionEditorStyle.KindOf(b), Describe(b));

            DrawDefaultInspector();

            if (targets.Length > 1)
                return;

            GUILayout.Space(8);
            DrawTools(b);
            GUILayout.Space(4);
            DrawPreview(b);
        }

        static string Describe(MotionBehaviour b)
        {
            if (b is MotionItem)
                return "Fade, slide, scale and color change. Text can animate word by word.";
            if (b is MotionCard)
                return "A line grows from the centre and opens into the card, or the card grows out of another object.";
            if (b is MotionGlow)
                return "A soft shaped light that grows to the size you set and keeps moving.";
            if (b is MotionIconComposer)
                return "Builds the picture out of pieces that fly, pop or fade into place.";
            return string.Empty;
        }

        void DrawTools(MotionBehaviour b)
        {
            EditorGUILayout.BeginHorizontal();
            if (b is MotionItem && GUILayout.Button(new GUIContent("Record Rest Pose", "Use how the object looks in the scene right now as the final state of the animation.")))
            {
                Undo.RecordObject(b, "Record Rest Pose");
                ((MotionItem)b).Record();
                EditorUtility.SetDirty(b);
            }

            if (b is MotionGlow && GUILayout.Button("Record Rest Pose"))
            {
                Undo.RecordObject(b, "Record Rest Pose");
                ((MotionGlow)b).Record();
                EditorUtility.SetDirty(b);
            }

            if (b is MotionIconComposer && GUILayout.Button("Rebuild Tiles"))
                ((MotionIconComposer)b).Rebuild();

            if (GUILayout.Button("Show Rest State"))
            {
                MotionPreview.Stop();
                b.ApplyRestState();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawPreview(MotionBehaviour b)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(MotionPreview.IsActive(b) && MotionPreview.IsPlaying ? "Pause" : "▶  Play", GUILayout.Height(24)))
            {
                if (MotionPreview.IsActive(b) && MotionPreview.IsPlaying)
                    MotionPreview.Pause();
                else
                    MotionPreview.Play(b);
            }

            if (GUILayout.Button("■  Reset", GUILayout.Height(24), GUILayout.Width(80)))
                MotionPreview.Stop();
            EditorGUILayout.EndHorizontal();

            float duration = Mathf.Max(0.01f, b.Duration);
            float shown = MotionPreview.IsActive(b) ? MotionPreview.Time : scrub;
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider("Time", shown, 0f, duration);
            if (EditorGUI.EndChangeCheck())
            {
                scrub = value;
                MotionPreview.Scrub(b, value);
            }

            EditorGUILayout.EndVertical();
            if (MotionPreview.IsPlaying && MotionPreview.IsActive(b))
                Repaint();
        }

        void OnDisable()
        {
            if (target != null && MotionPreview.IsActive((MotionBehaviour)target))
                MotionPreview.Stop();
        }
    }
}
