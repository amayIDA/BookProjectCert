using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ScreenKit
{
    /// <summary>
    /// Right canvas, page 3: shown when Begin Here is pressed. The book animation plays once where the icon used to be,
    /// with "Turn the pages to reveal more" below it. That text is the button that turns to the scenario lists.
    /// </summary>
    public static class RightPage3
    {
        const string VideoPath = "Assets/UI/Cert_book animation.webm";
        static readonly Color Navy = new Color(0.17f, 0.22f, 0.42f, 1f);

        [MenuItem("Book/Screens/Build Right Page 3 (Begin Here video)")]
        public static void Build()
        {
            GameObject canvas = GameObject.Find("RightCanvas");
            Transform page = canvas != null ? canvas.transform.Find("PagePanels/Page 3") : null;
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
            if (page == null || clip == null)
            {
                Debug.LogError("Right Page 3 or " + VideoPath + " was not found.");
                return;
            }

            for (int i = page.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(page.GetChild(i).gameObject);

            Style st = new Style();

            // the book animation, centred in the upper middle of the page
            RectTransform videoRect = Screen.Stretch("BookVideo", page);
            videoRect.anchorMin = videoRect.anchorMax = new Vector2(0.5f, 0.57f);
            videoRect.sizeDelta = new Vector2(560f, 560f);
            videoRect.anchoredPosition = Vector2.zero;
            RawImage raw = videoRect.gameObject.AddComponent<RawImage>();
            raw.color = new Color(1f, 1f, 1f, 0f);
            raw.raycastTarget = false;
            PageVideo video = videoRect.gameObject.AddComponent<PageVideo>();
            video.clip = clip;
            video.startDelay = 0.3f;

            // the words under it are the button that turns the page
            RectTransform textRect = Screen.Stretch("Turn the page", page);
            textRect.anchorMin = new Vector2(0.08f, 0.33f);
            textRect.anchorMax = new Vector2(0.92f, 0.43f);
            TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "TURN THE PAGES\nTO REVEAL MORE";
            text.font = st.regular; text.fontSize = 54; text.color = Navy; text.lineSpacing = 6f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = true;
            Button button = textRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            BookControl control = textRect.gameObject.AddComponent<BookControl>();
            control.action = BookScreens.ButtonAction.TurnPage;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, control.Click);
            Fx.Words(textRect.gameObject, MotionDirection.Bottom, 36f, 1.5f, 0.1f, 0.6f, true);

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

            Debug.Log("Right page 3 built: video " + clip.width + "x" + clip.height + ", " + clip.length.ToString("0.0") + "s, " + seq.entries.Count + " animated item.");
            EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
            EditorSceneManager.SaveScene(page.gameObject.scene);
        }
    }
}
