using MotionStudio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenKit
{
    /// <summary>
    /// Main canvas, page 7: "Get in touch", opened by the Get In Touch button.
    /// A header box with the message, and a body split in two: the contact details and the QR code.
    /// </summary>
    public static class MainPage7
    {
        const int PageNumber = 7;

        [MenuItem("Book/Screens/Build Main Page 7")]
        public static void Build()
        {
            Transform page = ScreenPages.Ensure("MainCanvas", "Page " + PageNumber, "Page 4");
            ScreenPages.Ensure("LeftCanvas", "Page " + PageNumber, "Page 3");
            ScreenPages.Ensure("RightCanvas", "Page " + PageNumber, "Page 3");
            if (page == null)
                return;

            Screen s = Screen.Begin(page);
            Style st = s.style;
            s.Header("GET IN TOUCH", null, null, 0.9f);

            // header box (message) on top, body below: the first line is drawn between them
            const float split = 0.73f;
            RectTransform card = s.Card("Card", 0.07f, 0.245f, 0.93f, 0.745f, split);
            s.Glow(card, 0.67f, 0.5f, split, 1f, Beat.Glow);

            Lane head = new Lane(Beat.Content);
            TMP_Text message = s.Read(card, head, "Message", "For further information, please find Mr. Ian\nor the Academy's team.",
                st.regular, 34, st.white, 0.025f, split + 0.02f, 0.62f, 0.97f, TextAlignmentOptions.MidlineLeft, 0.06f);
            message.characterSpacing = 8f;
            message.lineSpacing = 12f;

            // ---- left: contact details ----
            Lane left = new Lane(Beat.Content + 0.6f);
            TMP_Text lead = s.Read(card, left, "Lead", "<color=#8C9BFF>YOU CAN GET IN TOUCH DIRECTLY AT</color>", st.light, 21, st.white,
                0.025f, 0.6f, 0.45f, 0.69f, TextAlignmentOptions.MidlineLeft, 0.08f);
            lead.characterSpacing = 22f;

            Sprite globe = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Pages/Page2/footer.png");
            Sprite phone = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Pages/Page2/footer-2.png");
            Sprite mail = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Pages/Page2/footer-3.png");
            Sprite[] icons = { globe, phone, mail };
            string[] rows = { "inxeducation.ac.ae", "+971 2 206 5700", "info@inxeducation.ae" };
            float[] ys = { 0.47f, 0.335f, 0.2f };
            for (int i = 0; i < 3; i++)
            {
                float at = left.time;
                s.IconSprite(card, "Row icon " + (i + 1), icons[i], 0.034f, ys[i], 36f, st.white, at);
                s.Read(card, left, "Row " + (i + 1), rows[i], st.light, 27, st.white, 0.06f, ys[i] - 0.05f, 0.45f, ys[i] + 0.05f, TextAlignmentOptions.MidlineLeft, 0.05f);
                if (i < 2)
                    s.HRule(card, "Row rule " + (i + 1), 0.025f, 0.45f, (ys[i] + ys[i + 1]) * 0.5f, left.time - 0.1f, 0.7f);
            }

            // ---- centre: divider with OR ----
            s.VRule(card, "Divider", 0.5f, 0.06f, 0.67f, Beat.Content + 0.45f, 0.9f);
            // a black patch under the ring leaves a gap in the divider, as in the design
            RectTransform backing = s.Bar(card, "Or backing", 0.5f, 0.305f, 0.5f, 0.425f, Color.black, 132f);
            Fx.Fade(backing.gameObject, MotionDirection.None, 0f, Beat.Content + 1.1f, 0.4f);
            RectTransform ring = s.Icon(card, "Or ring", "ring", 0.5f, 0.365f, 118f, st.white, Beat.Content + 1.2f);
            TMP_Text or = s.Text(card, "Or", "OR", st.light, 20, st.white, 0.476f, 0.33f, 0.516f, 0.4f, TextAlignmentOptions.Center);
            or.characterSpacing = 20f;
            Fx.Fade(or.gameObject, MotionDirection.None, 0f, Beat.Content + 1.4f, 0.6f);

            // ---- right: scan the code ----
            Lane right = new Lane(Beat.Content + 1.0f);
            s.Read(card, right, "Scan", "Scan the code below\nto book your consultation.", st.light, 23, st.white,
                0.55f, 0.56f, 0.95f, 0.69f, TextAlignmentOptions.Center, 0.05f);

            RectTransform qr = Screen.Stretch("Qr", card);
            qr.anchorMin = qr.anchorMax = new Vector2(0.75f, 0.28f);
            qr.sizeDelta = new Vector2(440f, 440f);
            qr.anchoredPosition = Vector2.zero;
            float t0 = right.time + 0.1f;
            Color hair = new Color(0.62f, 0.66f, 0.78f, 0.6f);
            s.HRule(qr, "Qr top", 0f, 1f, 1f, t0, 0.8f).GetComponent<Image>().color = hair;
            s.HRule(qr, "Qr bottom", 0f, 1f, 0f, t0 + 0.1f, 0.8f).GetComponent<Image>().color = hair;
            s.VRule(qr, "Qr left", 0f, 0f, 1f, t0 + 0.2f, 0.8f).GetComponent<Image>().color = hair;
            s.VRule(qr, "Qr right", 1f, 0f, 1f, t0 + 0.3f, 0.8f).GetComponent<Image>().color = hair;

            // the four corner brackets
            float arm = 0.17f;
            float thick = 6f;
            for (int c = 0; c < 4; c++)
            {
                bool right2 = (c & 1) == 1;
                bool top = (c & 2) == 0;
                float x = right2 ? 1f : 0f;
                float y = top ? 1f : 0f;
                float dx = right2 ? -arm : arm;
                float dy = top ? -arm : arm;
                RectTransform bh = s.Bar(qr, "Corner " + c + " h", Mathf.Min(x, x + dx), y, Mathf.Max(x, x + dx), y, Color.white, thick);
                RectTransform bv = s.Bar(qr, "Corner " + c + " v", x, Mathf.Min(y, y + dy), x, Mathf.Max(y, y + dy), Color.white, thick);
                Fx.Fade(bh.gameObject, MotionDirection.None, 0f, t0 + 0.5f + c * 0.08f, 0.5f);
                Fx.Fade(bv.gameObject, MotionDirection.None, 0f, t0 + 0.5f + c * 0.08f, 0.5f);
            }

            TMP_Text label = s.Text(qr, "Qr label", "QR CODE", st.light, 16, st.white, 0.2f, 0.45f, 0.8f, 0.55f, TextAlignmentOptions.Center);
            label.characterSpacing = 24f;
            Fx.Fade(label.gameObject, MotionDirection.None, 0f, t0 + 0.9f, 0.6f);

            s.Finish();

            ScreenPages.PointButtonAt("LeftCanvas", "Page 2", "Get In Touch", PageNumber - 1);

            EditorSceneManager.MarkSceneDirty(page.gameObject.scene);
            EditorSceneManager.SaveScene(page.gameObject.scene);
        }
    }
}
