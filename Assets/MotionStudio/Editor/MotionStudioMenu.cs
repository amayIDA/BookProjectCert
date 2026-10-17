using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio.Editor
{
    /// <summary>Menu entries: create objects, build the demo, export the plugin, open the documentation.</summary>
    public static class MotionStudioMenu
    {
        const string PluginName = "MotionStudio";

        // ---------- helpers ----------

        public static string PluginRoot()
        {
            string[] guids = AssetDatabase.FindAssets("MotionStudio.Runtime t:asmdef");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("MotionStudio.Runtime.asmdef"))
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
            }

            return "Assets/MotionStudio";
        }

        public static Canvas EnsureCanvas()
        {
            Canvas canvas = null;
            if (Selection.activeGameObject != null)
                canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                return canvas;

            GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            return canvas;
        }

        static Transform ParentForNew()
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<Canvas>() != null)
                return Selection.activeGameObject.transform;
            return EnsureCanvas().transform;
        }

        static GameObject CreateUi(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Selection.activeGameObject = go;
            return go;
        }

        // ---------- create ----------

        [MenuItem("GameObject/Motion Studio/Sequence", false, 10)]
        static void CreateSequence()
        {
            Transform parent = Selection.activeGameObject != null ? Selection.activeGameObject.transform : EnsureCanvas().transform;
            GameObject go = CreateUi("Motion Sequence", parent);
            Undo.AddComponent<MotionSequence>(go);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        [MenuItem("GameObject/Motion Studio/Glow", false, 11)]
        static void CreateGlow()
        {
            GameObject go = CreateUi("Motion Glow", ParentForNew());
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(600f, 600f);
            MotionGlowGraphic graphic = Undo.AddComponent<MotionGlowGraphic>(go);
            graphic.shape = FindSample("shape_blob");
            Undo.AddComponent<MotionGlow>(go);
        }

        [MenuItem("GameObject/Motion Studio/Card", false, 12)]
        static void CreateCard()
        {
            GameObject go = CreateUi("Motion Card", ParentForNew());
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(640f, 360f);
            Image image = Undo.AddComponent<Image>(go);
            image.sprite = FindSample("card_panel");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.16f, 0.2f, 0.32f, 1f);
            Undo.AddComponent<MotionCard>(go);
        }

        [MenuItem("GameObject/Motion Studio/Icon Composer", false, 13)]
        static void CreateIcon()
        {
            GameObject go = CreateUi("Motion Icon", ParentForNew());
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(256f, 256f);
            Image image = Undo.AddComponent<Image>(go);
            image.sprite = FindSample("demo_icon_star");
            image.preserveAspect = true;
            Undo.AddComponent<MotionIconComposer>(go);
        }

        [MenuItem("GameObject/Motion Studio/Add Motion Item To Selection", false, 30)]
        static void AddItemToSelection()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<RectTransform>() != null && go.GetComponent<MotionItem>() == null)
                    Undo.AddComponent<MotionItem>(go);
            }
        }

        [MenuItem("GameObject/Motion Studio/Add Motion Item To Selection", true)]
        static bool AddItemValidate()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<RectTransform>() != null;
        }

        static Sprite FindSample(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return null;
        }

        // ---------- tools ----------

        [MenuItem("Tools/Motion Studio/Create Demo In Current Scene", false, 1)]
        static void CreateDemo()
        {
            MotionDemoBuilder.Build();
        }

        [MenuItem("Tools/Motion Studio/Open Documentation", false, 20)]
        static void OpenDocumentation()
        {
            string path = PluginRoot() + "/Documentation/README.md";
            Object doc = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (doc != null)
            {
                Selection.activeObject = doc;
                EditorGUIUtility.PingObject(doc);
                AssetDatabase.OpenAsset(doc);
            }
            else
                Debug.LogWarning("Motion Studio: documentation not found at " + path);
        }

        [MenuItem("Tools/Motion Studio/Open Agent Guide", false, 21)]
        static void OpenAgentGuide()
        {
            string path = PluginRoot() + "/Documentation/AGENT_GUIDE.md";
            Object doc = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (doc != null)
            {
                Selection.activeObject = doc;
                EditorGUIUtility.PingObject(doc);
                AssetDatabase.OpenAsset(doc);
            }
        }

        [MenuItem("Tools/Motion Studio/Stop Editor Preview", false, 40)]
        static void StopPreview()
        {
            MotionPreview.Stop();
        }

        [MenuItem("Tools/Motion Studio/Export Package (with demo)", false, 60)]
        static void ExportWithDemo()
        {
            Export(true);
        }

        [MenuItem("Tools/Motion Studio/Export Package (plugin only)", false, 61)]
        static void ExportPluginOnly()
        {
            Export(false);
        }

        static void Export(bool withDemo)
        {
            string root = PluginRoot();
            string target = EditorUtility.SaveFilePanel("Export Motion Studio", "", PluginName + (withDemo ? "_with_demo" : string.Empty) + ".unitypackage", "unitypackage");
            if (string.IsNullOrEmpty(target))
                return;

            string[] folders = withDemo
                ? new[] { root }
                : new[] { root + "/Runtime", root + "/Editor", root + "/Documentation", root + "/package.json" };
            AssetDatabase.ExportPackage(folders, target, ExportPackageOptions.Recurse);
            Debug.Log("Motion Studio exported to " + target);
            EditorUtility.RevealInFinder(target);
        }

        [MenuItem("Tools/Motion Studio/Repair Resources", false, 80)]
        static void Repair()
        {
            MotionStudioSetup.EnsureResources(true);
            AssetDatabase.Refresh();
        }
    }
}
