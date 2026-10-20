using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenKit
{
    /// <summary>
    /// Right canvas, page 7: the thank you screen shown beside the Get In Touch page.
    /// "THANK YOU!" with the soft glow under it and a Home button, animated like the other screens.
    /// </summary>
    public static class RightPage7
    {
        [MenuItem("Book/Screens/Build Right Page 7")]
        public static void Build()
        {
            Transform page = ScreenPages.Ensure("RightCanvas", "Page 7", "Page 2");
            Transform pageOne = GameObject.Find("RightCanvas").transform.Find("PagePanels/Page 1");
            if (page == null || pageOne == null)
                return;

            // keep the Home button (it already goes home) and rebuild everything else
            Transform home = page.Find("Home");
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform child = page.GetChild(i);
                if (child != home)
                    Object.DestroyImmediate(child.gameObject);
            }

            Style st = new Style();
            Color navy = new Color(0.17f, 0.22f, 0.42f, 1f);

            // glow: the same light as the right canvas Page 1 button
            Transform glowSource = pageOne.Find("Page1Button/GradientImage");
            Transform glowObject = Object.Instantiate(glowSource.gameObject, page).transform;
            glowObject.name = "Glow";
            RectTransform glowRect = (RectTransform)glowObject;
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = new Vector2(0f, -112f);
            MotionGlow glow = glowObject.GetComponent<MotionGlow>();
            MotionGlowGraphic light = glowObject.GetComponent<MotionGlowGraphic>();
            light.brightness = Mathf.Max(light.brightness, 1.9f);
            light.PushProperties();
            glow.order = 0; glow.delay = 0.55f; glow.growFrom = 1f;
            glow.Record();

            // the words
            RectTransform textRect = Screen.Stretch("Thanks", page);
            textRect.anchorMin = new Vector2(0.1f, 0.455f);
            textRect.anchorMax = new Vector2(0.9f, 0.525f);
            TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "THANK YOU!"; text.font = st.regular; text.fontSize = 60; text.color = navy;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            Fx.Words(textRect.gameObject, MotionDirection.Bottom, 36f, 0.3f, 0.12f, 0.6f, true);

            // Home button: the HOME picture, bottom left
            if (home != null)
            {
                for (int i = home.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(home.GetChild(i).gameObject);
                RectTransform homeRect = (RectTransform)home;
                homeRect.anchorMin = new Vector2(0.055f, 0.088f);
                homeRect.anchorMax = new Vector2(0.231f, 0.114f);
                homeRect.offsetMin = Vector2.zero;
                homeRect.offsetMax = Vector2.zero;
                Image face = home.GetComponent<Image>();
                face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Pages/Page2/Icon6 1.png");
                face.color = Color.white; face.preserveAspect = false;
                Button button = home.GetComponent<Button>();
                if (button != null)
                    button.transition = Selectable.Transition.None;
                Fx.Fade(home.gameObject, MotionDirection.Bottom, 24f, 1.3f, 0.6f);
            }

            MotionSequence seq = page.GetComponent<MotionSequence>();
            if (seq == null)
                seq = page.gameObject.AddComponent<MotionSequence>();
            if (page.GetComponent<PageIntro>() == null)
                page.gameObject.AddComponent<PageIntro>();
            seq.groupSameOrder = true; seq.interval = 0f; seq.startDelay = 0f; seq.playOnStart = false;
            foreach (MotionBehaviour b in page.GetComponentsInChildren<MotionBehaviour>(true))
                b.order = 0;
            seq.Collect(true);
            EditorUtility.SetDirty(seq);

            ScreenPages.PointButtonAt("LeftCanvas", "Page 2", "Get In Touch", 6);
            Debug.Log("Right page 7 built: " + seq.entries.Count + " animated items, " + seq.TotalDuration.ToString("0.0") + "s.");

            EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
            EditorSceneManager.SaveScene(page.gameObject.scene);
        }
    }
}
