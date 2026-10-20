using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenKit
{
    /// <summary>
    /// Main canvas, page 6: "Seal of approval", opened by the Seal Of Approval button.
    /// A card with the partner logos on a white band (Assets/UI/icons8.png) and four names underneath.
    /// </summary>
    public static class MainPage6
    {
        const int PageNumber = 6;
        const string LogosPath = "Assets/UI/icons8.png";

        [MenuItem("Book/Screens/Build Main Page 6")]
        public static void Build()
        {
            Transform page = ScreenPages.Ensure("MainCanvas", "Page " + PageNumber, "Page 4");
            ScreenPages.Ensure("LeftCanvas", "Page " + PageNumber, "Page 3");
            ScreenPages.Ensure("RightCanvas", "Page " + PageNumber, "Page 3");
            if (page == null)
                return;

            Sprite logos = ScreenPages.LoadSprite(LogosPath);
            if (logos == null)
            {
                Debug.LogError(LogosPath + " was not found.");
                return;
            }

            Screen s = Screen.Begin(page);
            Style st = s.style;
            s.Header("SEAL OF APPROVAL", null, null, 0.9f);

            // the logo band keeps the picture's own proportions, the names sit in a strip under it
            float pageWidth = 2560f * 0.86f;
            float bandHeight = pageWidth * logos.rect.height / logos.rect.width;
            float stripHeight = 190f;
            float cardHeight = (bandHeight + stripHeight) / 1920f;
            float top = 0.845f;
            float split = stripHeight / (bandHeight + stripHeight);

            RectTransform card = s.Card("Card", 0.07f, top - cardHeight, 0.93f, top, split);
            s.Glow(card, 0.5f, 0.62f, 0f, split, Beat.Glow);

            // the box opens first, then the white band expands from the middle out to both sides
            // an empty container clips the picture, so it really grows out from the middle
            RectTransform band = Screen.Stretch("Logos", card);
            Screen.Place(band, 0f, split, 1f, 1f);
            RectTransform pictureRect = Screen.Stretch("Picture", band);
            Image picture = pictureRect.gameObject.AddComponent<Image>();
            picture.sprite = logos; picture.raycastTarget = false;
            Fx.WipeOut(band.gameObject, Beat.CardOpen + 0.45f, 1.1f);

            string[] names = { "Amideast", "Cambridge English", "Educational\nTesting Service", "Education First" };
            for (int i = 0; i < 4; i++)
            {
                float x = i * 0.25f;
                TMP_Text label = s.Text(card, "Name " + (i + 1), names[i], st.light, 26, st.white, x + 0.01f, 0.02f, x + 0.24f, split - 0.02f, TextAlignmentOptions.Center);
                Fx.Words(label.gameObject, MotionDirection.Bottom, 30f, Beat.Content + 0.2f + i * 0.18f, 0.07f, 0.5f, true);
                if (i > 0)
                    s.VRule(card, "Divider " + i, x, 0.04f, split - 0.04f, Beat.Content + i * 0.18f);
            }

            s.Footer(Beat.Content + 1.6f);
            s.Finish();

            // the Seal Of Approval row on the left canvas opens this page
            ScreenPages.PointButtonAt("LeftCanvas", "Page 2", "Seal Of Approval", PageNumber - 1);

            EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
            EditorSceneManager.SaveScene(page.gameObject.scene);
        }
    }
}
