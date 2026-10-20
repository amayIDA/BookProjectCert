using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenKit
{
    /// <summary>
    /// The fifteen question pages on the main canvas. Each scenario question on the left canvas (pages 9 to 13) opens one:
    /// a full-bleed photograph, the scenario eyebrow and "START HERE", and a card with the recommended programme.
    /// Pages are named "Question N-M" (scenario N, question M). The words live in the table below.
    /// </summary>
    public static class MainQuestionPages
    {
        const string BackgroundFolder = "Assets/UI/QuestionBackground/Asset ";
        const int FirstAsset = 114;

        static readonly string[] Scenarios = { "ENTRY LEVEL AND NEW JOINERS", "FRONTLINE AND CLIENT FACING TEAMS", "FUNCTIONAL SPECIALITIES", "MANAGERS AND EMERGING LEADERS", "C-LEVEL AND ORGANIZATION WIDE STRATEGY" };
        static readonly string[] ScenarioIcons = { "user", "users", "briefcase", "target", "crown" };

        // programme (white), level or focus (accent), description
        static readonly string[][] Content =
        {
            new[] { "GENERAL ENGLISH", "PRE-BEGINNER/BEGINNER", "Literacy and workplace readiness from day one, with a clear pathway upward." },
            new[] { "PROFESSIONAL ENGLISH", "ELEMENTARY TO INTERMEDIATE", "Everyday workplace communication that builds confidence in meetings, calls and teamwork." },
            new[] { "BUSINESS WRITING", "EMAIL ESSENTIALS", "Clear, professional emails and messages, from the first draft to the final send." },
            new[] { "SERVICE COMMUNICATION", "PHONE AND FRONT DESK", "Consistent, courteous phone manner and customer handling that people notice." },
            new[] { "SALES ENGLISH", "PRODUCTS AND CLOSING", "Explain products with confidence, handle objections and close the sale in English." },
            new[] { "SERVICE LANGUAGE STANDARD", "SITES AND SECTORS", "One shared language framework, so every team serves customers the same way." },
            new[] { "HR ENGLISH", "DOCUMENTATION AND INTERVIEWS", "Precise written records, structured interviews and sensitive employee conversations." },
            new[] { "OFFICE ENGLISH", "MEETINGS AND COORDINATION", "Run meetings, work with systems and coordinate in writing without friction." },
            new[] { "INTERVIEW READINESS", "CANDIDATES AND PROMOTION", "Targeted practice that turns interview-stage nerves into a confident performance." },
            new[] { "LEADERSHIP ENGLISH", "PROMOTION PATHWAY", "Technical strength matched with the language to lead, so skill is never held back." },
            new[] { "PRESENTING AND NEGOTIATING", "MANAGERS", "Structure, persuade and negotiate clearly in front of clients and stakeholders." },
            new[] { "EXECUTIVE BRIEFING", "EMERGING LEADERS", "Concise, confident briefings that earn the attention of senior executives." },
            new[] { "BOARDROOM ENGLISH", "BOARDS, INVESTORS, REGULATORS", "Authoritative presentation skills for the audiences that matter most." },
            new[] { "MEASURABLE OUTCOMES", "LANGUAGE ROI REPORTING", "Baselines, progress tracking and proficiency gains reported as clear ROI." },
            new[] { "SPONSORED PROGRAMMES", "LICENSING AND STUDY ABROAD", "Structured pathways for staff licensing, overseas study and internal promotion." }
        };

        [MenuItem("Book/Screens/Build Question Pages")]
        public static void BuildAll()
        {
            Sprite scrim = Scrim();
            for (int i = 0; i < Content.Length; i++)
                Build(i, scrim);
            ScenarioQuestionButtons.Apply();
            Debug.Log("Built " + Content.Length + " question pages on the main canvas.");
        }

        static void Build(int index, Sprite scrim)
        {
            int scenario = index / 3, question = index % 3;
            string pageName = "Question " + (scenario + 1) + "-" + (question + 1);
            GameObject canvas = GameObject.Find("MainCanvas");
            Transform panels = canvas.transform.Find("PagePanels");
            Transform old = panels.Find(pageName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            Transform page = ScreenPages.Ensure("MainCanvas", pageName, "Page 4");
            if (page == null)
                return;
            // the page is a plain full-screen stage: nothing from the page it was copied from stays
            for (int c = page.childCount - 1; c >= 0; c--)
                Object.DestroyImmediate(page.GetChild(c).gameObject);
            foreach (MotionSequence leftover in page.GetComponents<MotionSequence>())
                Object.DestroyImmediate(leftover);
            foreach (PageIntro leftover in page.GetComponents<PageIntro>())
                Object.DestroyImmediate(leftover);

            Screen s = Screen.Begin(page);
            Style st = s.style;

            // ---- the photograph: fades in and settles with a slow push ----
            Sprite photo = ScreenPages.LoadSprite(BackgroundFolder + (FirstAsset + index) + ".png");
            RectTransform photoRect = Screen.Stretch("Photo", s.sheet);
            Image photoImage = photoRect.gameObject.AddComponent<Image>();
            photoImage.sprite = photo; photoImage.raycastTarget = false; photoImage.preserveAspect = false;
            MotionItem push = Fx.Fade(photoRect.gameObject, MotionDirection.None, 0f, 0f, 7f);
            push.fadeDuration = 1.4f; push.scale = true; push.scaleFrom = 1.12f;
            RectTransform shade = Screen.Stretch("Shade", s.sheet);
            Image shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.sprite = scrim; shadeImage.raycastTarget = false;

            // ---- header: eyebrow rule + label and "START HERE", aligned with the card ----
            const float left = 0.345f;
            s.HRule(s.sheet, "EyebrowRule", left, left + 0.022f, 0.9f, Beat.EyebrowRule, 0.7f);
            TMP_Text eyebrow = s.Text(s.sheet, "Eyebrow", Scenarios[scenario], st.regular, 22, st.mute, left + 0.03f, 0.88f, 0.95f, 0.92f, TextAlignmentOptions.MidlineLeft);
            eyebrow.characterSpacing = 18f;
            Fx.Eyebrow(eyebrow.gameObject, Beat.Eyebrow);
            TMP_Text headline = s.Text(s.sheet, "Headline", "START HERE", st.regular, 76, st.white, left, 0.805f, 0.95f, 0.88f, TextAlignmentOptions.MidlineLeft);
            headline.characterSpacing = 4f;
            Fx.Headline(headline.gameObject, Beat.Headline);
            s.Logo();

            // ---- card: header row (programme + level) over a divider, description under it ----
            RectTransform card = s.Card("Card", left, 0.515f, 0.955f, 0.795f, 0.51f);
            s.Glow(card, 0.6f, 0.55f, 0.51f, 1f, Beat.Glow);

            string[] words = Content[index];
            TMP_Text programme = s.Text(card, "Programme", words[0] + "\n<color=#8C9BFF>" + words[1] + "</color>", st.regular, 66, st.white, 0.035f, 0.56f, 0.88f, 0.95f, TextAlignmentOptions.MidlineLeft);
            programme.characterSpacing = 8f; programme.lineSpacing = -6f;
            Fx.Words(programme.gameObject, MotionDirection.Bottom, 40f, Beat.Content, 0.09f, 0.6f, true);
            s.Icon(card, "Scenario icon", ScenarioIcons[scenario], 0.962f, 0.86f, 64f, st.white, Beat.Content + 0.2f);
            Lane lane = new Lane(Beat.Content + 0.7f);
            s.Read(card, lane, "Description", words[2], st.light, 40, st.white, 0.035f, 0.08f, 0.95f, 0.46f, TextAlignmentOptions.MidlineLeft, 0.05f);

            s.Finish();
            EditorUtility.SetDirty(page.gameObject);
        }

        /// <summary>A soft dark gradient along the top, so the eyebrow and headline read on any photograph.</summary>
        static Sprite Scrim()
        {
            const string path = "Assets/BookPageCurl/Sprites/ScrimTop.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                Texture2D tex = new Texture2D(4, 256, TextureFormat.RGBA32, false);
                for (int y = 0; y < 256; y++)
                {
                    float t = y / 255f;                       // 0 bottom, 1 top
                    float a = Mathf.Clamp01((t - 0.55f) / 0.45f);
                    a = a * a * 0.78f;
                    // a faint foot too, for the bottom edge
                    a = Mathf.Max(a, Mathf.Clamp01((0.18f - t) / 0.18f) * 0.45f);
                    for (int x = 0; x < 4; x++)
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }

                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }

            return ScreenPages.LoadSprite(path);
        }
    }

    /// <summary>Makes the questions on the left detail pages (9 to 13) real buttons. Layout and animation are not touched.</summary>
    public static class ScenarioQuestionButtons
    {
        [MenuItem("Book/Screens/Make Scenario Questions Clickable")]
        public static void Apply()
        {
            for (int scenario = 0; scenario < 5; scenario++)
            {
                Transform page = GameObject.Find("LeftCanvas").transform.Find("PagePanels/Page " + (9 + scenario));
                if (page == null)
                    continue;
                for (int q = 0; q < 3; q++)
                {
                    Transform row = page.Find("Question " + (q + 1));
                    if (row != null)
                        Wire(row, scenario * 3 + q);
                }

                EditorUtility.SetDirty(page.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(GameObject.Find("LeftCanvas").scene);
            EditorSceneManager.SaveScene(GameObject.Find("LeftCanvas").scene);
        }

        static void Wire(Transform row, int questionIndex)
        {
            Image face = row.GetComponent<Image>();
            if (face != null)
            {
                face.color = Color.clear;
                face.raycastTarget = true;
            }

            BookControl control = row.GetComponent<BookControl>();
            if (control == null)
                control = row.gameObject.AddComponent<BookControl>();
            control.action = BookScreens.ButtonAction.OpenPage;
            control.page = questionIndex;

            PageRowHighlight highlight = row.GetComponent<PageRowHighlight>();
            if (highlight == null)
                highlight = row.gameObject.AddComponent<PageRowHighlight>();
            Transform glow = row.Find("ImagetoHighlight");
            Transform edge = row.Find("ImagetoHighlight2");
            highlight.glow = glow != null ? glow.GetComponent<MotionGlow>() : null;
            highlight.vertical = edge != null ? edge.GetComponent<AxisReveal>() : null;

            Button button = row.GetComponent<Button>();
            if (button == null)
                button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            for (int c = button.onClick.GetPersistentEventCount() - 1; c >= 0; c--)
                UnityEventTools.RemovePersistentListener(button.onClick, c);
            UnityEventTools.AddPersistentListener(button.onClick, control.Click);
            UnityEventTools.AddPersistentListener(button.onClick, highlight.Show);
            EditorUtility.SetDirty(row.gameObject);
        }
    }
}
