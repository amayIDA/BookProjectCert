using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScreenKit
{
    /// <summary>
    /// Main canvas, page 5: "How this work?", opened by the Begin Here button.
    /// A single box with a glow, two lines of text and the settings icon.
    /// </summary>
    public static class MainPage5
    {
        const int PageNumber = 5;

        [MenuItem("Book/Screens/Build Main Page 5")]
        public static void Build()
        {
            Transform page = ScreenPages.Ensure("MainCanvas", "Page " + PageNumber, "Page 4");
            ScreenPages.Ensure("LeftCanvas", "Page " + PageNumber, "Page 3");
            ScreenPages.Ensure("RightCanvas", "Page " + PageNumber, "Page 3");
            if (page == null)
                return;

            Screen s = Screen.Begin(page);
            Style st = s.style;

            s.Header("BEGIN HERE", "HOW <color=#8C9BFF>THIS WORK?</color>", null, 0.9f);

            RectTransform card = s.Card("Card", 0.07f, 0.575f, 0.93f, 0.745f);
            s.Glow(card, 0.42f, 0.62f, 0f, 0.55f, Beat.Glow);
            s.Icon(card, "Icon settings", "settings", 0.955f, 0.72f, 110f, st.white, Beat.Content);

            Lane lane = new Lane(Beat.Content + 0.2f);
            s.Read(card, lane, "Intro",
                "Each question describes a different real workplace situation.\nSelect your scenario and be directed to the right program.",
                st.regular, 28, st.white, 0.03f, 0.25f, 0.8f, 0.7f, TextAlignmentOptions.MidlineLeft, 0.05f);
            TMP_Text intro = card.Find("Intro").GetComponent<TMP_Text>();
            intro.lineSpacing = 22f;

            s.Footer(lane.time + 0.3f);
            s.Finish();

            // the Begin Here row on the left canvas opens this page
            ScreenPages.PointButtonAt("LeftCanvas", "Page 2", "Begin Here", PageNumber - 1);

            EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
            EditorSceneManager.SaveScene(page.gameObject.scene);
        }
    }

    /// <summary>Keeps the three canvases in step: page N on the main canvas needs page N on the left and right canvas too.</summary>
    public static class ScreenPages
    {
        /// <summary>Returns canvas/PagePanels/pageName, creating it as a copy of copyFrom when it does not exist yet.</summary>
        public static Transform Ensure(string canvasName, string pageName, string copyFrom)
        {
            GameObject canvas = GameObject.Find(canvasName);
            Transform panels = canvas != null ? canvas.transform.Find("PagePanels") : null;
            if (panels == null)
            {
                Debug.LogError(canvasName + "/PagePanels was not found.");
                return null;
            }

            Transform page = panels.Find(pageName);
            if (page != null)
                return page;

            Transform source = panels.Find(copyFrom);
            if (source == null)
            {
                Debug.LogError(canvasName + "/PagePanels/" + copyFrom + " was not found to copy.");
                return null;
            }

            GameObject copy = Object.Instantiate(source.gameObject, panels);
            copy.name = pageName;
            copy.SetActive(false);
            copy.transform.SetAsLastSibling();
            return copy.transform;
        }

        /// <summary>Loads a picture as a sprite, switching its import settings to Sprite when needed.</summary>
        public static Sprite LoadSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Makes a button in a scene page open the given page (0 is the first page).</summary>
        public static void PointButtonAt(string canvasName, string pageName, string buttonName, int pageIndex)
        {
            GameObject canvas = GameObject.Find(canvasName);
            Transform button = canvas != null ? canvas.transform.Find("PagePanels/" + pageName + "/" + buttonName) : null;
            BookControl control = button != null ? button.GetComponent<BookControl>() : null;
            if (control == null)
            {
                Debug.LogWarning("No BookControl on " + canvasName + "/" + pageName + "/" + buttonName);
                return;
            }

            control.page = pageIndex;
            EditorUtility.SetDirty(control);
        }
    }
}
