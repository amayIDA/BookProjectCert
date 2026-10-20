using UnityEditor;
using UnityEngine;

namespace MotionStudio.Editor
{
    /// <summary>
    /// Keeps a selected item's recorded pose matched to where it sits in the scene,
    /// so moving it does not leave the animation aimed at the old position.
    /// </summary>
    [InitializeOnLoad]
    static class ScenePoseSync
    {
        static ScenePoseSync()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (Application.isPlaying || MotionPreview.HasTarget)
                return;

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return;

            RectTransform rect = selected.transform as RectTransform;
            if (rect == null)
                return;

            MotionGlow glow = selected.GetComponent<MotionGlow>();
            if (glow != null && glow.recorded)
            {
                if (Close(rect.anchoredPosition, glow.restPosition) && Close(rect.localScale, glow.restScale))
                    return;
                glow.restPosition = rect.anchoredPosition;
                glow.restScale = rect.localScale;
                EditorUtility.SetDirty(glow);
                return;
            }

            MotionItem item = selected.GetComponent<MotionItem>();
            if (item == null || !item.recorded)
                return;

            if (Close(rect.anchoredPosition, item.restPosition) && Close(rect.localScale, item.restScale))
                return;

            item.restPosition = rect.anchoredPosition;
            item.restScale = rect.localScale;
            EditorUtility.SetDirty(item);
        }

        static bool Close(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude < 0.0001f;
        }

        static bool Close(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < 0.0001f;
        }
    }
}
