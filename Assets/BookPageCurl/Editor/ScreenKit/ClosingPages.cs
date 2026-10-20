using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenKit
{
    /// <summary>
    /// The closing screens, shown when the page is turned on any scenario (Pages 9 to 13):
    /// left canvas Page 14 has the big animated check, right canvas Page 14 says "THAT'S ALL FOR NOW." with Home and Turn the page,
    /// and the main canvas shows the "Closing" page over the video background.
    /// </summary>
    public static class ClosingPages
    {
        static readonly Color Navy = new Color(0.17f, 0.22f, 0.42f, 1f);

        [MenuItem("Book/Screens/Build Closing Pages")]
        public static void Build()
        {
            BuildLeft();
            BuildRight();
            BuildMain();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Closing pages built: left Page 14, right Page 14 and the main canvas Closing page.");
        }

        static Transform Fresh(string canvas, string pageName, string copyFrom)
        {
            Transform panels = GameObject.Find(canvas).transform.Find("PagePanels");
            Transform old = panels.Find(pageName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            return ScreenPages.Ensure(canvas, pageName, copyFrom);
        }

        static void BuildLeft()
        {
            Transform page = Fresh("LeftCanvas", "Page 14", "Page 9");
            for (int i = page.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(page.GetChild(i).gameObject);
            MotionSequence seq = page.GetComponent<MotionSequence>();
            if (seq != null)
                seq.Clear();
            // the big animated check sits in the middle of the page and drifts slowly
            AnimatedIconTools.AddLarge("LeftCanvas", "Page 14", "Thats_all", new Vector2(0.5f, 0.5f), 520f, 0.3f);
        }

        static void BuildRight()
        {
            Transform page = Fresh("RightCanvas", "Page 14", "Page 9");
            Transform pageOne = GameObject.Find("RightCanvas").transform.Find("PagePanels/Page 1");
            Transform home = page.Find("Home");
            Transform turn = page.Find("Turn the page");
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform child = page.GetChild(i);
                if (child != home && child != turn)
                    Object.DestroyImmediate(child.gameObject);
            }

            Style st = new Style();

            // the soft light under the words, the same light as the first page and the thank you page
            Transform glowSource = pageOne.Find("Page1Button/GradientImage");
            Transform glowObject = Object.Instantiate(glowSource.gameObject, page).transform;
            glowObject.name = "Glow";
            RectTransform glowRect = (RectTransform)glowObject;
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = new Vector2(0f, -168f);
            MotionGlow glow = glowObject.GetComponent<MotionGlow>();
            MotionGlowGraphic light = glowObject.GetComponent<MotionGlowGraphic>();
            light.brightness = Mathf.Max(light.brightness, 1.9f);
            light.PushProperties();
            glow.order = 0; glow.delay = 0.55f; glow.growFrom = 1f;
            glow.Record();

            RectTransform textRect = Screen.Stretch("Closing text", page);
            textRect.anchorMin = new Vector2(0.1f, 0.44f);
            textRect.anchorMax = new Vector2(0.9f, 0.56f);
            TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "THAT’S ALL\nFOR NOW."; text.font = st.regular; text.fontSize = 60; text.color = Navy; text.lineSpacing = -6f;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            Fx.Words(textRect.gameObject, MotionDirection.Bottom, 36f, 0.3f, 0.12f, 0.6f, true);

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
        }

        static void BuildMain()
        {
            Transform panels = GameObject.Find("MainCanvas").transform.Find("PagePanels");
            Transform old = panels.Find("Closing");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            Transform page = ScreenPages.Ensure("MainCanvas", "Closing", "Page 4");
            for (int c = page.childCount - 1; c >= 0; c--)
                Object.DestroyImmediate(page.GetChild(c).gameObject);
            foreach (MotionSequence leftover in page.GetComponents<MotionSequence>())
                Object.DestroyImmediate(leftover);
            foreach (PageIntro leftover in page.GetComponents<PageIntro>())
                Object.DestroyImmediate(leftover);

            Screen s = Screen.Begin(page);
            Style st = s.style;

            // eyebrow: rule, then the label
            s.HRule(s.sheet, "EyebrowRule", 0.04f, 0.062f, 0.898f, Beat.EyebrowRule, 0.7f);
            TMP_Text eyebrow = s.Text(s.sheet, "Eyebrow", "BEGIN HERE", st.regular, 22, st.mute, 0.07f, 0.88f, 0.5f, 0.916f, TextAlignmentOptions.MidlineLeft);
            eyebrow.characterSpacing = 18f;
            Fx.Eyebrow(eyebrow.gameObject, Beat.Eyebrow);
            s.Logo();

            // the card: one box with the light rising from its lower edge
            RectTransform card = s.Card("Card", 0.04f, 0.675f, 0.96f, 0.868f);
            s.Glow(card, 0.63f, 0.5f, 0f, 0.95f, Beat.Glow);
            TMP_Text title = s.Text(card, "Title", "THAT’S ALL FOR NOW.", st.regular, 66, st.white, 0.03f, 0.5f, 0.9f, 0.92f, TextAlignmentOptions.BottomLeft);
            title.characterSpacing = 4f;
            Fx.Words(title.gameObject, MotionDirection.Bottom, 40f, Beat.Content, 0.09f, 0.6f, true);
            Lane lane = new Lane(Beat.Content + 0.5f);
            s.Read(card, lane, "Subtitle", "More programmes coming soon.", st.regular, 44, st.white, 0.03f, 0.1f, 0.9f, 0.5f, TextAlignmentOptions.TopLeft, 0.06f);
            s.Finish();
        }
    }
}
