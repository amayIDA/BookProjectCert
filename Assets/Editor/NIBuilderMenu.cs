using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Menu entries to build the Talent Wheel screens into the scene and to restyle them.</summary>
public static class NIBuilderMenu
{
    const string Folder = "Assets/Resources/NationalInitiative";

    [MenuItem("Tools/National Initiative/Build Scene UI")]
    public static void Build()
    {
        EnsureMaterial();
        string result = NIBuild.BuildAll();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("National Initiative: " + result);
    }

    public static string BuildForTool()
    {
        EnsureMaterial();
        string result = NIBuild.BuildAll();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return result;
    }

    static void EnsureMaterial()
    {
        string path = Folder + "/ParticleAdditive.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            return;

        Shader shader = Shader.Find("NI/UIAdditive");
        if (shader == null)
        {
            Debug.LogWarning("NI/UIAdditive shader not found yet. Run Build Scene UI again after the import finishes.");
            return;
        }

        AssetDatabase.CreateAsset(new Material(shader), path);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/National Initiative/Apply Selected Font To All Text")]
    public static void ApplyFont()
    {
        Font font = Selection.activeObject as Font;
        if (font == null)
        {
            EditorUtility.DisplayDialog("Apply font", "Select a Font asset in the Project window first.", "OK");
            return;
        }

        int count = 0;
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            Transform t = texts[i].transform;
            bool inside = false;
            while (t != null)
            {
                if (t.name == "NIScreen" || t.name == "NITouchUi" || t.name == "NILedFx")
                    inside = true;
                t = t.parent;
            }

            CircularMenu menu = texts[i].GetComponentInParent<CircularMenu>(true);
            if (inside || menu != null)
            {
                Undo.RecordObject(texts[i], "Apply font");
                texts[i].font = font;
                EditorUtility.SetDirty(texts[i]);
                count++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("National Initiative: font applied to " + count + " texts.");
    }
}
