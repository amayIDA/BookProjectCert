using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScreenKit
{
    /// <summary>
    /// Main canvas, page 4: three column card with a header row, a body and a strip of formats.
    /// This file is the template for new screens: copy it, change the words and the numbers.
    /// </summary>
    public static class MainPage4
    {
        [MenuItem("Book/Screens/Build Main Page 4")]
        public static void Build()
        {
            Transform page = Find("MainCanvas", "Page 4");
            if (page == null)
            {
                Debug.LogError("MainCanvas/PagePanels/Page 4 was not found.");
                return;
            }

            Screen s = Screen.Begin(page);
            Style st = s.style;
            TMP_FontAsset light = st.light, regular = st.regular;

            s.Header("AT A GLANCE",
                "THE BEST IDEA IN A ROOM IS WORTH\nNOTHING IF IT CANNOT BE <color=#8C9BFF>EXPLAINED.</color>",
                "This is how we make sure yours can be.",
                0.9f);

            RectTransform card = s.Card("Card", 0.07f, 0.31f, 0.93f, 0.745f, 0.79f, 0.14f);
            s.Glow(card, 0.5f, 0.5f, 0.79f, 1f, Beat.Glow);

            // ---- header row ----
            string[] numbers = { "01", "02", "03" };
            string[] titles = { "THE GAP", "HOW THE ACADEMY FILLS IT", "THE RESULT" };
            string[] icons = { "alert", "cap", "trend" };
            for (int i = 0; i < 3; i++)
            {
                float x = i / 3f;
                float at = Beat.Content + i * 0.3f;
                TMP_Text n = s.Text(card, "Number " + numbers[i], numbers[i], light, 15, st.dim, x + 0.025f, 0.885f, x + 0.12f, 0.965f, TextAlignmentOptions.MidlineLeft);
                n.characterSpacing = 14f;
                Fx.Words(n.gameObject, MotionDirection.Bottom, 24f, at, 0.05f, 0.5f, true);
                TMP_Text t = s.Text(card, "Title " + numbers[i], titles[i], regular, 24, st.white, x + 0.025f, 0.8f, x + 0.3f, 0.885f, TextAlignmentOptions.MidlineLeft);
                t.characterSpacing = 12f;
                Fx.Words(t.gameObject, MotionDirection.Bottom, 30f, at + 0.1f, 0.07f, 0.5f, true);
                s.Icon(card, "Icon " + numbers[i], icons[i], x + 0.3333f - 0.03f, 0.89f, 44f, st.white, at);
            }

            s.VRule(card, "Divider 1", 1f / 3f, 0.14f, 0.79f, 3.35f);
            s.VRule(card, "Divider 2", 2f / 3f, 0.14f, 0.79f, 3.45f);

            // ---- body: three lanes read at the same time, each a little later ----
            Lane l1 = new Lane(Beat.Content + 0.55f);
            Lane l2 = new Lane(Beat.Content + 0.9f);
            Lane l3 = new Lane(Beat.Content + 1.25f);
            System.Func<float, float> by = f => 0.14f + f * 0.65f;

            s.Read(card, l1, "Gaps",
                "•  Capable teams held back by language.\n•  Good ideas that never land with clients or leaders.\n•  Standard courses that don't match the job.\n•  No starting point, so no proof of progress.",
                light, 26, st.white, 0.03f, by(0.04f), 0.31f, by(0.97f));
            TMP_Text gaps = card.Find("Gaps").GetComponent<TMP_Text>();
            gaps.paragraphSpacing = 36f; gaps.lineSpacing = 10f;

            string[] labels = { "DISCOVER", "DESIGN", "DELIVER", "EVALUATE", "SUSTAIN" };
            string[] steps =
            {
                "We test where each group sits on the CEFR scale.",
                "A bespoke curriculum mapped to real job roles.",
                "Sector-experienced trainers teaching from your own working environment.",
                "Progress tracked against the baseline.",
                "Proficiency gains, workplace indicators and ROI reported."
            };
            for (int i = 0; i < 5; i++)
            {
                float top = 0.98f - i * 0.195f;
                string value = "<size=62%><color=#8C9BFF><cspace=0.2em>" + labels[i] + "</cspace></color></size>\n" + steps[i];
                s.Read(card, l2, "Step " + labels[i], value, light, 24, st.white,
                    1f / 3f + 0.03f, by(top - 0.185f), 2f / 3f - 0.02f, by(top));
            }

            float c3 = 2f / 3f + 0.03f;
            s.Read(card, l3, "Quote", "“Confident communicators\nfor every workplace”", regular, 32, st.white, c3, by(0.76f), 0.97f, by(0.97f));
            s.Read(card, l3, "Paragraph",
                "From first words to executive presence, through General English, Business English, sector English and IELTS, all CEFR aligned.",
                light, 23, st.mute, c3, by(0.45f), 0.96f, by(0.72f));
            s.HRule(card, "StatsRule", c3, 0.97f, by(0.38f), l3.time, 0.8f);
            s.StatBlock(card, l3, "Years", "30", "YEARS", c3, by(0.05f), by(0.36f));
            s.StatBlock(card, l3, "Programmes", "21", "PROGRAMMES", 2f / 3f + 0.5f * (1f / 3f), by(0.05f), by(0.36f));

            // ---- strip ----
            float stripAt = Mathf.Max(l1.time, Mathf.Max(l2.time, l3.time)) - 0.6f;
            TMP_Text label = s.Text(card, "Formats", "DELIVERY FORMATS", light, 14, st.dim, 0.05f, 0.02f, 0.3f, 0.12f, TextAlignmentOptions.MidlineLeft);
            label.characterSpacing = 22f;
            Fx.Words(label.gameObject, MotionDirection.Bottom, 20f, stripAt + 0.1f, 0.08f, 0.5f, true);
            string[] formats = { "English or Arabic", "Half-day to multi-week", "Adapt or build new" };
            string[] formatIcons = { "translate", "clock", "layers" };
            float[] xs = { 0.36f, 0.55f, 0.74f };
            for (int i = 0; i < 3; i++)
            {
                float at = stripAt + 0.25f + i * 0.2f;
                s.Icon(card, "Format icon " + (i + 1), formatIcons[i], xs[i], 0.07f, 34f, st.white, at);
                TMP_Text f = s.Text(card, "Format " + (i + 1), formats[i], light, 22, st.white, xs[i] + 0.02f, 0.02f, xs[i] + 0.19f, 0.12f, TextAlignmentOptions.MidlineLeft);
                Fx.Words(f.gameObject, MotionDirection.Bottom, 24f, at + 0.1f, 0.07f, 0.5f, true);
            }

            s.Footer(stripAt + 0.6f);
            s.Finish();

            EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
            EditorSceneManager.SaveScene(page.gameObject.scene);
        }

        static Transform Find(string canvasName, string pageName)
        {
            GameObject canvas = GameObject.Find(canvasName);
            return canvas != null ? canvas.transform.Find("PagePanels/" + pageName) : null;
        }
    }
}
