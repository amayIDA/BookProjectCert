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
    /// The Begin Here scenario lists: page 8 on the left canvas (three scenarios) and the right canvas (two more, with Home and
    /// Turn the page). They are shown when the page is turned after Begin Here and play the same flow as Page 2 on the left canvas.
    /// </summary>
    public static class BeginHerePages
    {
        const string PageName = "Page 8";
        static readonly Color Navy = new Color(0.12f, 0.16f, 0.38f, 1f);

        [MenuItem("Book/Screens/Build Begin Here Lists")]
        public static void Build()
        {
            Transform left = Fresh("LeftCanvas", "Page 2");
            Transform right = Fresh("RightCanvas", "Page 3");
            ScreenPages.Ensure("MainCanvas", PageName, "Page 5");
            if (left == null || right == null)
                return;

            // ---- left: title and three scenarios ----
            Transform source = left; // rows are copied from the left page, so keep the originals until the right page is made
            string[] names = { "Entry Level", "Frontline", "Functional" };
            string[] labels = { "ENTRY LEVEL AND NEW JOINERS", "FRONTLINE AND CLIENT-FACING TEAMS", "FUNCTIONAL SPECIALITIES" };
            string[] icons = { "user", "users", "briefcase" };
            string[] numbers = { "01", "02", "03" };
            string[] oldRows = { "At A Glance", "Begin Here", "Seal Of Approval", "Get In Touch" };

            Transform title = left.Find("Text");
            title.GetComponent<TMP_Text>().text = "BEGIN HERE";
            ((RectTransform)title).anchoredPosition = new Vector2(-21f, -156f);
            RecordPose(title);

            for (int i = 3; i < oldRows.Length; i++)
                Object.DestroyImmediate(left.Find(oldRows[i]).gameObject);

            Transform[] rows = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                rows[i] = left.Find(oldRows[i]);
                Fill(rows[i], names[i], numbers[i], labels[i], icons[i], 95f - i * 160f, true);
            }

            FixSequence(left.GetComponent<MotionSequence>());

            // ---- right: two more scenarios, Home and Turn the page ----
            string[] rightLabels = { "MANAGERS AND EMERGING LEADERS", "C-LEVEL AND ORGANIZATION-WIDE STRATEGY" };
            string[] rightNames = { "Managers", "Executives" };
            string[] rightIcons = { "target", "crown" };
            string[] rightNumbers = { "04", "05" };
            float[] rightY = { 75f, -90f };

            Transform home = right.Find("Home");
            Transform turn = right.Find("Turn the page");
            for (int i = right.childCount - 1; i >= 0; i--)
            {
                Transform child = right.GetChild(i);
                if (child != home && child != turn)
                    Object.DestroyImmediate(child.gameObject);
            }

            Transform[] rightRows = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject copy = Object.Instantiate(rows[0].gameObject, right);
                copy.name = rightNames[i];
                rightRows[i] = copy.transform;
                Fill(copy.transform, rightNames[i], rightNumbers[i], rightLabels[i], rightIcons[i], rightY[i], false);
            }

            StyleButton(home, "Assets/UI/Pages/Page2/Icon6 1.png", 0.055f, 0.095f, 0.225f, 0.122f);
            StyleButton(turn, "Assets/UI/Pages/Page2/Icon6-2.png", 0.665f, 0.095f, 0.88f, 0.122f);

            // same flow as Page 2: everything starts together, each item with its own small motion
            MotionSequence reference = left.GetComponent<MotionSequence>();
            MotionSequence seq = right.GetComponent<MotionSequence>();
            if (seq == null)
                seq = right.gameObject.AddComponent<MotionSequence>();
            if (right.GetComponent<PageIntro>() == null)
                right.gameObject.AddComponent<PageIntro>();
            seq.interval = reference.interval; seq.spacing = reference.spacing; seq.speed = reference.speed;
            seq.groupSameOrder = reference.groupSameOrder; seq.hideUntilPlayed = reference.hideUntilPlayed; seq.playOnStart = reference.playOnStart;
            seq.Clear();
            for (int i = 0; i < 2; i++)
            {
                seq.Add(rightRows[i].Find("Line").GetComponent<AxisReveal>(), 0f);
                seq.Add(rightRows[i].Find("Text").GetComponent<MotionItem>(), 0f);
                seq.Add(rightRows[i].Find("Icon").GetComponent<MotionItem>(), 0f);
            }

            MotionItem homeFade = Fx.Fade(home.gameObject, MotionDirection.None, 0f, 0.15f, 0.4f);
            MotionItem turnFade = Fx.Fade(turn.gameObject, MotionDirection.None, 0f, 0.25f, 0.4f);
            homeFade.order = 0; turnFade.order = 0;
            seq.Add(homeFade, 0f);
            seq.Add(turnFade, 0f);
            EditorUtility.SetDirty(seq);

            Debug.Log("Begin Here lists built: left " + left.GetComponent<MotionSequence>().entries.Count + " items, right " + seq.entries.Count + " items.");
            EditorSceneManager.MarkSceneDirty(left.gameObject.scene);
            EditorSceneManager.SaveScene(left.gameObject.scene);
        }

        static Transform Fresh(string canvasName, string copyFrom)
        {
            GameObject canvas = GameObject.Find(canvasName);
            Transform panels = canvas != null ? canvas.transform.Find("PagePanels") : null;
            if (panels == null)
                return null;
            Transform old = panels.Find(PageName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            return ScreenPages.Ensure(canvasName, PageName, copyFrom);
        }

        static void Fill(Transform row, string name, string number, string label, string icon, float y, bool shiftText)
        {
            row.name = name;
            RectTransform rect = (RectTransform)row;
            rect.anchoredPosition = new Vector2(22f, y);

            // these rows only show their highlight, the old page buttons are removed
            Button button = row.GetComponent<Button>();
            BookControl control = row.GetComponent<BookControl>();
            if (button != null && control != null)
            {
                for (int c = button.onClick.GetPersistentEventCount() - 1; c >= 0; c--)
                {
                    if (button.onClick.GetPersistentMethodName(c) == "Click")
                        UnityEventTools.RemovePersistentListener(button.onClick, c);
                }
            }

            if (control != null)
                Object.DestroyImmediate(control);

            TMP_Text text = row.Find("Text").GetComponent<TMP_Text>();
            text.text = label; text.fontSize = 26; text.characterSpacing = 6f; text.enableWordWrapping = false;
            RectTransform textRect = (RectTransform)text.transform;
            if (shiftText)
                textRect.anchoredPosition += new Vector2(0f, 40f);
            TMP_Text digits = row.Find("Number").GetComponent<TMP_Text>();
            digits.text = number; digits.fontSize = 22;

            Image picture = row.Find("Icon").GetComponent<Image>();
            picture.sprite = ScreenIcons.Get(icon);
            picture.color = Navy;
            picture.preserveAspect = true;
            ((RectTransform)picture.transform).sizeDelta = new Vector2(42f, 42f);

            // the copied animation items remember the old positions, so record the new ones
            RecordPose(text.transform);
            RecordPose(digits.transform);
            RecordPose(picture.transform);
        }

        static void RecordPose(Transform target)
        {
            MotionItem item = target.GetComponent<MotionItem>();
            if (item != null)
                item.Record();
        }

        static void FixSequence(MotionSequence seq)
        {
            seq.entries.RemoveAll(e => e.target == null);
            EditorUtility.SetDirty(seq);
        }

        static void StyleButton(Transform button, string spritePath, float x0, float y0, float x1, float y1)
        {
            if (button == null)
                return;
            for (int i = button.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(button.GetChild(i).gameObject);
            RectTransform rect = (RectTransform)button;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image face = button.GetComponent<Image>();
            face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            face.color = Color.white;
            Button b = button.GetComponent<Button>();
            if (b != null)
                b.transition = Selectable.Transition.None;
        }
    }
}
