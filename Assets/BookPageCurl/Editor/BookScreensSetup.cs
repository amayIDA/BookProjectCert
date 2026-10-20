using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BookScreensSetup
{
    [MenuItem("Book/Build Screen Buttons")]
    public static void Build()
    {
        Canvas left = Find("LeftCanvas");
        Canvas right = Find("RightCanvas");
        Canvas single = Find("SingleCanvas");
        if (left == null || right == null || single == null)
        {
            Debug.LogError("BookScreensSetup needs LeftCanvas, RightCanvas, and SingleCanvas in the open scene.");
            return;
        }

        HideChildren(left.transform);
        HideChildren(right.transform);
        HideChildren(single.transform);

        BookScreens screens = Object.FindAnyObjectByType<BookScreens>();
        if (screens == null)
        {
            var host = GameObject.Find("DisplaySetup");
            if (host == null)
                host = new GameObject("DisplaySetup");
            screens = host.AddComponent<BookScreens>();
        }

        screens.EnsureContent();
        EditorUtility.SetDirty(screens);
        EditorSceneManager.MarkSceneDirty(screens.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Display 1 and display 2 are button screens. Display 3 is the book. Press Play to draw the pages.");
    }

    static Canvas Find(string canvasName)
    {
        GameObject go = GameObject.Find(canvasName);
        return go != null ? go.GetComponent<Canvas>() : null;
    }

    static void HideChildren(Transform canvas)
    {
        for (int i = 0; i < canvas.childCount; i++)
        {
            Transform child = canvas.GetChild(i);
            if (child.name != "Panel" && child.name != "PagePanels")
                child.gameObject.SetActive(false);
        }
    }
}
