using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MotionStudio.Editor
{
    /// <summary>Inspector of the sequence: order list, interval, a timeline and a preview player.</summary>
    [CustomEditor(typeof(MotionSequence))]
    public class MotionSequenceEditor : UnityEditor.Editor
    {
        ReorderableList list;
        SerializedProperty entries;
        float scrub;

        void OnEnable()
        {
            entries = serializedObject.FindProperty("entries");
            list = new ReorderableList(serializedObject, entries, true, true, true, true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Order of appearance (top plays first)");
            list.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
            list.drawElementCallback = DrawElement;
            list.onChangedCallback = l => ((MotionSequence)target).MarkDirty();
            list.onAddCallback = l =>
            {
                entries.arraySize++;
                SerializedProperty element = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                element.FindPropertyRelative("target").objectReferenceValue = null;
                element.FindPropertyRelative("offset").floatValue = 0f;
                element.FindPropertyRelative("enabled").boolValue = true;
            };
        }

        void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            SerializedProperty element = entries.GetArrayElementAtIndex(index);
            SerializedProperty targetProp = element.FindPropertyRelative("target");
            SerializedProperty offset = element.FindPropertyRelative("offset");
            SerializedProperty enabled = element.FindPropertyRelative("enabled");

            rect.y += 3f;
            rect.height = EditorGUIUtility.singleLineHeight;
            float x = rect.x;
            float w = rect.width;

            Rect number = new Rect(x, rect.y, 24f, rect.height);
            EditorGUI.LabelField(number, (index + 1).ToString(), EditorStyles.miniBoldLabel);

            Rect toggle = new Rect(x + 22f, rect.y, 18f, rect.height);
            EditorGUI.PropertyField(toggle, enabled, GUIContent.none);

            MotionBehaviour b = targetProp.objectReferenceValue as MotionBehaviour;
            Rect kind = new Rect(x + 42f, rect.y, 40f, rect.height);
            if (b != null)
            {
                Color c = GUI.color;
                GUI.color = MotionEditorStyle.ColorFor(b);
                EditorGUI.LabelField(kind, MotionEditorStyle.KindOf(b), EditorStyles.miniBoldLabel);
                GUI.color = c;
            }

            Rect field = new Rect(x + 84f, rect.y, w - 84f - 190f, rect.height);
            EditorGUI.PropertyField(field, targetProp, GUIContent.none);

            Rect orderRect = new Rect(field.xMax + 6f, rect.y, 70f, rect.height);
            if (b != null)
            {
                EditorGUI.BeginChangeCheck();
                int order = EditorGUI.IntField(orderRect, b.order);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(b, "Change order");
                    b.order = order;
                    EditorUtility.SetDirty(b);
                    ((MotionSequence)target).MarkDirty();
                }
            }

            Rect offsetRect = new Rect(orderRect.xMax + 6f, rect.y, 100f, rect.height);
            EditorGUI.PropertyField(offsetRect, offset, GUIContent.none);
        }

        public override void OnInspectorGUI()
        {
            MotionSequence sequence = (MotionSequence)target;
            MotionEditorStyle.Banner("Motion Studio · Sequence", "Plays the items below in order. Same Order number = same moment. Interval = time between items.");

            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Order column = item.Order, last column = extra offset (s)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            list.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Collect Children", "Fill the list with every Motion Studio component below this object, sorted by Order.")))
            {
                Undo.RecordObject(sequence, "Collect");
                sequence.Collect(true);
                EditorUtility.SetDirty(sequence);
            }

            if (GUILayout.Button(new GUIContent("Sort by Order", "Sort the list by each item's Order number.")))
            {
                Undo.RecordObject(sequence, "Sort");
                sequence.SortByOrder();
                EditorUtility.SetDirty(sequence);
            }

            if (GUILayout.Button(new GUIContent("Number by List", "Give the items the numbers 1, 2, 3 ... in the order of this list.")))
            {
                Undo.RecordObject(sequence, "Number");
                foreach (var e in sequence.entries)
                {
                    if (e != null && e.target != null)
                        Undo.RecordObject(e.target, "Number");
                }

                sequence.NumberEntriesByListOrder();
                EditorUtility.SetDirty(sequence);
            }

            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Spacing", EditorStyles.boldLabel);
            DrawProperties("interval", "spacing", "groupSameOrder");
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            DrawProperties("playOnStart", "startDelay", "hideUntilPlayed", "loop", "loopPause", "speed", "useUnscaledTime");
            DrawProperties("onStarted", "onCompleted");

            GUILayout.Space(8);
            DrawTimeline(sequence);
            GUILayout.Space(4);
            DrawPreview(sequence);
        }

        void DrawProperties(params string[] names)
        {
            serializedObject.Update();
            for (int i = 0; i < names.Length; i++)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(names[i]));
            if (serializedObject.ApplyModifiedProperties())
                ((MotionSequence)target).MarkDirty();
        }

        void DrawTimeline(MotionSequence sequence)
        {
            float total = Mathf.Max(0.05f, sequence.TotalDuration);
            int count = sequence.entries.Count;
            float rowHeight = 16f;
            float height = 28f + Mathf.Max(1, count) * (rowHeight + 2f);
            Rect rect = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, MotionEditorStyle.Panel);

            Rect area = new Rect(rect.x + 8f, rect.y + 22f, rect.width - 16f, rect.height - 26f);
            EditorGUI.LabelField(new Rect(rect.x + 8f, rect.y + 2f, 200f, 16f), "Timeline  " + total.ToString("0.00") + " s", EditorStyles.miniBoldLabel);

            // ruler
            for (int s = 0; s <= Mathf.CeilToInt(total); s++)
            {
                float x = area.x + area.width * s / total;
                if (x > area.xMax)
                    break;
                EditorGUI.DrawRect(new Rect(x, area.y - 6f, 1f, 5f), new Color(1f, 1f, 1f, 0.35f));
                EditorGUI.LabelField(new Rect(x + 2f, area.y - 18f, 30f, 14f), s + "s", EditorStyles.miniLabel);
            }

            for (int i = 0; i < count; i++)
            {
                MotionSequence.Entry e = sequence.entries[i];
                float y = area.y + i * (rowHeight + 2f);
                EditorGUI.DrawRect(new Rect(area.x, y, area.width, rowHeight), new Color(1f, 1f, 1f, 0.04f));
                if (e == null || e.target == null)
                    continue;

                float start = sequence.StartOf(i);
                float end = start + e.target.Duration;
                float x0 = area.x + area.width * start / total;
                float x1 = area.x + area.width * end / total;
                Color c = MotionEditorStyle.ColorFor(e.target);
                if (!e.enabled)
                    c.a = 0.25f;
                Rect bar = new Rect(x0, y + 1f, Mathf.Max(3f, x1 - x0), rowHeight - 2f);
                EditorGUI.DrawRect(bar, c);
                GUIStyle label = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.black } };
                GUI.Label(bar, " " + e.target.DisplayName, label);
            }

            // playhead
            float head = MotionPreview.IsActive(sequence) ? MotionPreview.Time : scrub;
            float hx = area.x + area.width * Mathf.Clamp01(head / total);
            EditorGUI.DrawRect(new Rect(hx, area.y - 4f, 2f, area.height + 4f), MotionEditorStyle.Accent);
        }

        void DrawPreview(MotionSequence sequence)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Preview in the editor (no Play mode needed)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            bool active = MotionPreview.IsActive(sequence);
            if (GUILayout.Button(active && MotionPreview.IsPlaying ? "Pause" : "▶  Play", GUILayout.Height(24)))
            {
                if (active && MotionPreview.IsPlaying)
                    MotionPreview.Pause();
                else
                    MotionPreview.Play(sequence);
            }

            if (GUILayout.Button("■  Reset", GUILayout.Height(24), GUILayout.Width(80)))
                MotionPreview.Stop();
            EditorGUILayout.EndHorizontal();

            float total = Mathf.Max(0.05f, sequence.TotalDuration);
            float shown = active ? MotionPreview.Time : scrub;
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider("Time", shown, 0f, total);
            if (EditorGUI.EndChangeCheck())
            {
                scrub = value;
                MotionPreview.Scrub(sequence, value);
            }

            EditorGUILayout.EndVertical();
            if (MotionPreview.IsPlaying && active)
                Repaint();
        }

        void OnDisable()
        {
            MotionSequence sequence = target as MotionSequence;
            if (sequence != null && MotionPreview.IsActive(sequence))
                MotionPreview.Stop();
        }
    }
}
