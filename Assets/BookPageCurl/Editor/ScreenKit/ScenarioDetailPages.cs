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
    /// The five scenario detail spreads (Page 9 to 13 on the left and right canvas). Clicking a scenario on the Begin Here lists
    /// (Page 8) turns the spread to its detail page: title and three questions on the left, a big icon with Home and Turn the page on the right.
    /// </summary>
    public static class ScenarioDetailPages
    {
        static readonly Color Navy = new Color(0.17f, 0.22f, 0.42f, 1f);

        static readonly string[] Names = { "Entry Level", "Frontline", "Functional", "Managers", "Executives" };
        static readonly string[] Titles =
        {
            "ENTRY LEVEL AND\nNEW JOINERS", "FRONTLINE AND\nCLIENT FACING TEAMS", "FUNCTIONAL\nSPECIALITIES",
            "MANAGERS AND\nEMERGING LEADERS", "C-LEVEL AND ORGANIZATION\nWIDE STRATEGY"
        };
        static readonly string[] Icons = { "user", "users", "briefcase", "target", "crown" };
        static readonly string[][] Questions =
        {
            new[] { "Do new hires struggle to communicate at a basic level?", "Do your staff need professional English communication skills in the workplace?", "Do employees in your organization need training in professional email writing skills?" },
            new[] { "Do customers complain about phone manner or inconsistent service?", "Do sales staff struggle to explain products or close sales in English?", "Do your sites or sectors need one shared service-language standard?" },
            new[] { "Does HR struggle with English documentation, interviews or employee-relations conversations?", "Do admin staff find meetings, office systems or written coordination difficult?", "Do candidates or promotion hopefuls fail at the English interview stage?" },
            new[] { "Do technically strong professionals stall at promotion because of their English?", "Do managers face challenges when presenting or negotiating?", "Will your emerging leaders need to brief senior executives?" },
            new[] { "Does your leadership team present to boards, investors or regulators in English?", "Do you need measurable language gains reported as ROI?", "Do you sponsor staff for licensing, studying overseas or promotion within the organization?" }
        };

        [MenuItem("Book/Screens/Build Scenario Detail Pages")]
        public static void Build()
        {
            Transform leftLists = Find("LeftCanvas", "Page 8");
            Transform rightLists = Find("RightCanvas", "Page 8");
            if (leftLists == null || rightLists == null)
            {
                Debug.LogError("Build the Begin Here lists first.");
                return;
            }

            for (int i = 0; i < Names.Length; i++)
            {
                string pageName = "Page " + (9 + i);
                BuildLeft(pageName, i, leftLists);
                BuildRight(pageName, i, rightLists);
                Transform row = (i < 3 ? leftLists : rightLists).Find(Names[i]);
                Wire(row);
            }

            Debug.Log("Scenario detail pages built (Page 9 to 13).");
            EditorSceneManager.MarkSceneDirty(leftLists.gameObject.scene);
            EditorSceneManager.SaveScene(leftLists.gameObject.scene);
        }

        /// <summary>
        /// Page 9 on the left canvas is the hand-tuned design. This rebuilds Pages 10 to 13 as exact copies of it
        /// (positions, sizes, fonts, highlights, animation) and only swaps the title and the three questions.
        /// Run this instead of Build when Page 9 has been edited by hand.
        /// </summary>
        [MenuItem("Book/Screens/Copy Left Page 9 Layout To Pages 10-13")]
        public static void CopyLayout()
        {
            Transform source = Find("LeftCanvas", "Page 9");
            if (source == null)
            {
                Debug.LogError("Left Page 9 was not found.");
                return;
            }

            for (int i = 1; i < Names.Length; i++)
            {
                Transform page = Fresh("LeftCanvas", "Page " + (9 + i), "Page 9");
                TMP_Text title = page.Find("Text").GetComponent<TMP_Text>();
                title.text = Titles[i];
                for (int r = 0; r < 3; r++)
                {
                    Transform row = page.Find("Question " + (r + 1));
                    row.Find("Text").GetComponent<TMP_Text>().text = Questions[i][r];
                }

                EditorUtility.SetDirty(page.gameObject);
            }

            Debug.Log("Left Pages 10 to 13 now copy Page 9.");
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
            EditorSceneManager.SaveScene(source.gameObject.scene);
        }

        static Transform Find(string canvas, string page)
        {
            GameObject go = GameObject.Find(canvas);
            return go == null ? null : go.transform.Find("PagePanels/" + page);
        }

        static Transform Fresh(string canvas, string pageName, string copyFrom)
        {
            Transform panels = GameObject.Find(canvas).transform.Find("PagePanels");
            Transform old = panels.Find(pageName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            return ScreenPages.Ensure(canvas, pageName, copyFrom);
        }

        static void BuildLeft(string pageName, int index, Transform lists)
        {
            Transform page = Fresh("LeftCanvas", pageName, "Page 8");
            // keep exactly the three row slots of the left list and reuse them for the questions
            string[] rowNames = { "Entry Level", "Frontline", "Functional" };
            Transform title = page.Find("Text");
            TMP_Text heading = title.GetComponent<TMP_Text>();
            heading.text = Titles[index];
            heading.fontStyle = FontStyles.Normal;
            heading.alignment = TextAlignmentOptions.Left;
            Record(title);

            MotionSequence seq = page.GetComponent<MotionSequence>();
            seq.Clear();
            seq.Add(title.GetComponent<MotionItem>(), 0f);

            float[] y = { 232f, -74f, -380f };
            for (int r = 0; r < 3; r++)
            {
                Transform row = page.Find(rowNames[r]);
                row.name = "Question " + (r + 1);
                ((RectTransform)row).anchoredPosition = new Vector2(22f, y[r]);
                Object.DestroyImmediate(row.GetComponent<PageRowHighlight>());
                Object.DestroyImmediate(row.GetComponent<Button>());
                Image back = row.GetComponent<Image>();
                if (back != null)
                {
                    back.color = Color.clear;
                    back.raycastTarget = false;
                }

                Object.DestroyImmediate(row.Find("Icon").gameObject);

                TMP_Text text = row.Find("Text").GetComponent<TMP_Text>();
                text.text = Questions[index][r];
                text.fontSize = 26; text.characterSpacing = 6f; text.enableWordWrapping = true;
                text.alignment = TextAlignmentOptions.TopLeft;
                text.fontStyle = FontStyles.Normal;
                RectTransform tr = (RectTransform)text.transform;
                tr.anchorMin = new Vector2(0f, 0f); tr.anchorMax = new Vector2(1f, 0f);
                tr.pivot = new Vector2(0f, 1f);
                tr.offsetMin = new Vector2(70f, 0f); tr.offsetMax = new Vector2(-60f, 0f);
                tr.anchoredPosition = new Vector2(70f, 85f);
                tr.sizeDelta = new Vector2(-130f, 100f);
                row.Find("Number").GetComponent<TMP_Text>().text = "0" + (r + 1);
                Record(tr); Record(row.Find("Number"));

                Transform line = row.Find("Line");
                Transform glow = row.Find("ImagetoHighlight");
                Transform edge = row.Find("ImagetoHighlight2");
                // the rows start un-clicked, as on the lists: no highlight
                glow.gameObject.SetActive(false);
                edge.gameObject.SetActive(false);
                seq.Add(row.Find("Number").GetComponent<MotionItem>(), 0f);
                seq.Add(line.GetComponent<AxisReveal>(), 0f);
                seq.Add(text.GetComponent<MotionItem>(), 0f);
            }

            // all three questions come in together: every item shares one order
            foreach (MotionBehaviour b in page.GetComponentsInChildren<MotionBehaviour>(true))
                b.order = 0;
            EditorUtility.SetDirty(seq);
        }

        static void BuildRight(string pageName, int index, Transform lists)
        {
            Transform page = Fresh("RightCanvas", pageName, "Page 8");
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform child = page.GetChild(i);
                if (child.name != "Home" && child.name != "Turn the page")
                    Object.DestroyImmediate(child.gameObject);
            }

            RectTransform icon = Screen.Stretch("BigIcon", page);
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(360f, 360f);
            icon.anchoredPosition = new Vector2(0f, 60f);
            Image picture = icon.gameObject.AddComponent<Image>();
            picture.sprite = ScreenIcons.Get(Icons[index]);
            picture.color = Navy;
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            // a large animated icon: loops and drifts slowly up and down
            AnimatedIconTools.Convert(icon, AnimatedIconTools.ScenarioVideos[index], true, Navy, new Vector2(360f, 360f), true);
            MotionItem iconFade = Fx.Fade(icon.gameObject, MotionDirection.Bottom, 40f, 0.3f, 0.9f);
            iconFade.order = 0;

            MotionSequence seq = page.GetComponent<MotionSequence>();
            seq.Clear();
            seq.Add(iconFade, 0.4f);
            Transform home = page.Find("Home");
            Transform turn = page.Find("Turn the page");
            seq.Add(home.GetComponent<MotionItem>(), 0.9f);
            seq.Add(turn.GetComponent<MotionItem>(), 1.0f);
            EditorUtility.SetDirty(seq);
        }

        static void Wire(Transform row)
        {
            if (row == null)
                return;
            BookControl control = row.GetComponent<BookControl>();
            if (control == null)
                control = row.gameObject.AddComponent<BookControl>();
            control.action = BookScreens.ButtonAction.OpenPage;
            Button button = row.GetComponent<Button>();
            if (button == null)
                return;
            for (int c = button.onClick.GetPersistentEventCount() - 1; c >= 0; c--)
            {
                if (button.onClick.GetPersistentMethodName(c) == "Click")
                    UnityEventTools.RemovePersistentListener(button.onClick, c);
            }

            UnityEventTools.AddPersistentListener(button.onClick, control.Click);
            Image face = row.GetComponent<Image>();
            if (face != null)
                face.raycastTarget = true;
            EditorUtility.SetDirty(row.gameObject);
        }

        static void Record(Transform target)
        {
            MotionItem item = target.GetComponent<MotionItem>();
            if (item != null)
                item.Record();
        }
    }
}
