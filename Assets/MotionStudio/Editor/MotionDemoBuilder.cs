using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MotionStudio.Editor
{
    /// <summary>Builds the demo canvas in the open scene. Every object it creates is a normal scene object you can change.</summary>
    public static class MotionDemoBuilder
    {
        const string CanvasName = "Motion Studio Demo";

        static Sprite LoadSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return null;
        }

        static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static Image MakeImage(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            RectTransform rt = MakeRect(name, parent, position, size);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text MakeText(string name, Transform parent, Vector2 position, Vector2 size, string text, int fontSize, FontStyle style, Color color, TextAnchor anchor)
        {
            RectTransform rt = MakeRect(name, parent, position, size);
            Text label = rt.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            return label;
        }

        // ---------- configure ----------

        static MotionItem Item(GameObject go, int order, MotionDirection direction, float distance, float duration = 0.9f)
        {
            MotionItem item = go.AddComponent<MotionItem>();
            item.order = order;
            item.direction = direction;
            item.distance = distance;
            item.duration = duration;
            item.Record();
            return item;
        }

        static MotionItem WordItem(Text text, int order, MotionDirection direction, float distance, float stagger)
        {
            MotionItem item = Item(text.gameObject, order, direction, distance, 0.8f);
            item.split = TextSplit.Words;
            item.pieceStagger = stagger;
            return item;
        }

        // ---------- build ----------

        public static void Build()
        {
            MotionPreview.Stop();

            GameObject old = GameObject.Find(CanvasName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old);

            GameObject canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasObject.transform;
            RectTransform rootRect = (RectTransform)root;

            EnsureEventSystem();

            Color navy = new Color(0.024f, 0.035f, 0.1f, 1f);
            Color white = Color.white;
            Color muted = new Color(0.72f, 0.77f, 0.87f, 1f);
            Color gold = new Color(0.93f, 0.84f, 0.6f, 1f);
            Sprite card = LoadSprite("card_panel");
            Sprite blob = LoadSprite("shape_blob");
            Sprite ring = LoadSprite("shape_ring");

            // ---- background ----
            Image background = MakeImage("Background", root, Vector2.zero, Vector2.zero, null, navy);
            Stretch(background.rectTransform);

            // big moving light, grows in first
            RectTransform glowRect = MakeRect("Background Light", root, new Vector2(-560f, -330f), new Vector2(1300f, 1300f));
            MotionGlowGraphic bgGlow = glowRect.gameObject.AddComponent<MotionGlowGraphic>();
            bgGlow.shape = blob;
            bgGlow.blur = 0.85f;
            bgGlow.brightness = 0.7f;
            bgGlow.colorA = new Color(0.2f, 0.4f, 1f, 1f);
            bgGlow.colorB = new Color(0.65f, 0.3f, 0.95f, 1f);
            bgGlow.scrollSpeed = 0.05f;
            MotionGlow bgMotion = glowRect.gameObject.AddComponent<MotionGlow>();
            bgMotion.order = 0;
            bgMotion.fadeDuration = 1.2f;
            bgMotion.duration = 2.4f;
            bgMotion.growFrom = 0.35f;
            bgMotion.cycleColors = true;
            bgMotion.breathe = 0.05f;
            bgMotion.drift = 40f;
            bgMotion.Record();

            RectTransform glow2 = MakeRect("Ring Light", root, new Vector2(500f, -60f), new Vector2(1000f, 1000f));
            MotionGlowGraphic g2 = glow2.gameObject.AddComponent<MotionGlowGraphic>();
            g2.shape = ring;
            g2.blur = 0.3f;
            g2.brightness = 0.6f;
            g2.colorA = new Color(0.98f, 0.8f, 0.45f, 1f);
            g2.colorB = new Color(1f, 0.55f, 0.55f, 1f);
            MotionGlow m2 = glow2.gameObject.AddComponent<MotionGlow>();
            m2.order = 0;
            m2.duration = 2.2f;
            m2.growFrom = 0.4f;
            m2.spin = 6f;
            m2.Record();

            // ---- headline ----
            Text title = MakeText("Title", root, new Vector2(-110f, 330f), new Vector2(1500f, 150f), "Motion Studio", 120, FontStyle.Bold, white, TextAnchor.MiddleLeft);
            WordItem(title, 1, MotionDirection.Bottom, 46f, 0.12f).label = "Title: words rise";

            Text subtitle = MakeText("Subtitle", root, new Vector2(-110f, 215f), new Vector2(1500f, 120f), "Ordered, soft and precise motion for any UI. Words, cards, light and icons.", 38, FontStyle.Normal, muted, TextAnchor.UpperLeft);
            MotionItem subItem = WordItem(subtitle, 2, MotionDirection.Left, 70f, 0.05f);
            subItem.changeColor = true;
            subItem.colorFrom = gold;
            subItem.colorTo = muted;
            subItem.label = "Subtitle: words, color change";

            // ---- card A : line to card, masked light inside, icon and texts ----
            Image cardA = MakeImage("Card A", root, new Vector2(-450f, -110f), new Vector2(820f, 430f), card, new Color(0.1f, 0.14f, 0.28f, 0.96f));
            cardA.type = UnityEngine.UI.Image.Type.Sliced;
            MotionCard motionA = cardA.gameObject.AddComponent<MotionCard>();
            motionA.order = 3;
            motionA.changeColor = true;
            motionA.colorFrom = new Color(0.25f, 0.45f, 0.95f, 0.96f);
            motionA.colorTo = new Color(0.1f, 0.14f, 0.28f, 0.96f);
            motionA.colorDuration = 1.6f;
            motionA.label = "Card A: line opens";

            // light that stays inside the card (the sprite masks it)
            Image clip = MakeImage("Light Mask", cardA.transform, Vector2.zero, Vector2.zero, card, Color.white);
            clip.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(clip.rectTransform);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform innerGlow = MakeRect("Inner Light", clip.transform, new Vector2(-250f, -170f), new Vector2(560f, 560f));
            MotionGlowGraphic ig = innerGlow.gameObject.AddComponent<MotionGlowGraphic>();
            ig.shape = blob;
            ig.blur = 0.8f;
            ig.brightness = 0.8f;
            ig.colorA = new Color(0.3f, 0.75f, 1f, 1f);
            ig.colorB = new Color(0.9f, 0.4f, 1f, 1f);
            MotionGlow im = innerGlow.gameObject.AddComponent<MotionGlow>();
            im.order = 3;
            im.delay = 0.9f;
            im.duration = 1.8f;
            im.growFrom = 0.1f;
            im.cycleColors = true;
            im.drift = 60f;
            im.Record();

            Image iconA = MakeImage("Icon", cardA.transform, new Vector2(-250f, 20f), new Vector2(210f, 210f), LoadSprite("demo_icon_star"), gold);
            iconA.preserveAspect = true;
            MotionIconComposer composeA = iconA.gameObject.AddComponent<MotionIconComposer>();
            composeA.order = 4;
            composeA.changeColor = true;
            composeA.colorFrom = Color.white;
            composeA.colorTo = gold;
            composeA.fadeDuration = 0.25f;
            composeA.style = ComposeStyle.Assemble;
            composeA.startFrom = ComposeStart.Scatter;
            composeA.wave = ComposeWave.FromCenter;
            composeA.columns = 12;
            composeA.rows = 12;
            composeA.duration = 1.9f;
            composeA.label = "Icon: assembles";

            Text cardTitle = MakeText("Card Title", cardA.transform, new Vector2(110f, 60f), new Vector2(430f, 80f), "Built from a line", 54, FontStyle.Bold, white, TextAnchor.MiddleLeft);
            Item(cardTitle.gameObject, 4, MotionDirection.Right, 90f, 0.9f).label = "Card title: from right";

            Text cardBody = MakeText("Card Body", cardA.transform, new Vector2(110f, -60f), new Vector2(430f, 150f), "The width of the sprite becomes a line, the line opens in both directions.", 30, FontStyle.Normal, muted, TextAnchor.UpperLeft);
            WordItem(cardBody, 5, MotionDirection.Bottom, 24f, 0.04f).label = "Card text: words";

            // ---- card B : grows out of an object ----
            Image seed = MakeImage("Seed", root, new Vector2(500f, -250f), new Vector2(86f, 86f), ring, gold);
            MotionItem seedItem = Item(seed.gameObject, 6, MotionDirection.None, 0f, 0.7f);
            seedItem.scale = true;
            seedItem.scaleFrom = 0.2f;
            seedItem.label = "Seed object";

            Image cardB = MakeImage("Card B", root, new Vector2(500f, -60f), new Vector2(720f, 520f), card, new Color(0.14f, 0.1f, 0.26f, 0.96f));
            cardB.type = UnityEngine.UI.Image.Type.Sliced;
            MotionCard motionB = cardB.gameObject.AddComponent<MotionCard>();
            motionB.order = 7;
            motionB.fade = true;
            motionB.fadeFrom = 0.2f;
            motionB.fadeDuration = 0.6f;
            motionB.origin = CardOrigin.FromObject;
            motionB.fromObject = seed.rectTransform;
            motionB.fromObjectDuration = 1.1f;
            motionB.label = "Card B: grows from the seed";

            Image iconB = MakeImage("Icon", cardB.transform, new Vector2(0f, 100f), new Vector2(190f, 190f), LoadSprite("demo_icon_shield"), white);
            iconB.preserveAspect = true;
            MotionIconComposer composeB = iconB.gameObject.AddComponent<MotionIconComposer>();
            composeB.order = 8;
            composeB.style = ComposeStyle.Pixel;
            composeB.wave = ComposeWave.Diagonal;
            composeB.columns = 16;
            composeB.rows = 16;
            composeB.duration = 1.5f;
            composeB.label = "Icon: pixels";

            Text titleB = MakeText("Card Title", cardB.transform, new Vector2(0f, -70f), new Vector2(620f, 80f), "Grown from an object", 52, FontStyle.Bold, white, TextAnchor.MiddleCenter);
            MotionItem titleBItem = WordItem(titleB, 8, MotionDirection.Bottom, 30f, 0.1f);
            titleBItem.changeColor = true;
            titleBItem.colorFrom = gold;
            titleBItem.colorTo = white;

            Text bodyB = MakeText("Card Body", cardB.transform, new Vector2(0f, -170f), new Vector2(560f, 120f), "Any object can be the start of a card, a button, an icon or a dot.", 30, FontStyle.Normal, muted, TextAnchor.UpperCenter);
            WordItem(bodyB, 9, MotionDirection.None, 0f, 0.05f);

            // ---- icon row ----
            string[] names = { "demo_icon_star", "demo_icon_shield", "demo_icon_globe" };
            ComposeStyle[] styles = { ComposeStyle.Slide, ComposeStyle.Assemble, ComposeStyle.Dissolve };
            ComposeStart[] starts = { ComposeStart.Left, ComposeStart.Top, ComposeStart.Radial };
            ComposeWave[] waves = { ComposeWave.LeftToRight, ComposeWave.Random, ComposeWave.FromCenter };
            for (int i = 0; i < 3; i++)
            {
                Image icon = MakeImage("Row Icon " + (i + 1), root, new Vector2(-690f + i * 170f, -440f), new Vector2(120f, 120f), LoadSprite(names[i]), i == 1 ? gold : white);
                icon.preserveAspect = true;
                MotionIconComposer c = icon.gameObject.AddComponent<MotionIconComposer>();
                c.order = 10 + i;
                c.style = styles[i];
                c.startFrom = starts[i];
                c.wave = waves[i];
                c.columns = 10;
                c.rows = 10;
                c.duration = 1.2f;
                c.distance = 200f;
            }

            // ---- replay ----
            Image button = MakeImage("Replay Button", root, new Vector2(760f, -470f), new Vector2(240f, 70f), card, new Color(1f, 1f, 1f, 0.14f));
            button.type = UnityEngine.UI.Image.Type.Sliced;
            button.raycastTarget = true;
            Button replay = button.gameObject.AddComponent<Button>();
            replay.targetGraphic = button;
            Text replayText = MakeText("Label", button.transform, Vector2.zero, new Vector2(240f, 70f), "Replay", 32, FontStyle.Bold, white, TextAnchor.MiddleCenter);
            Stretch(replayText.rectTransform);
            MotionItem replayItem = Item(button.gameObject, 13, MotionDirection.Bottom, 40f, 0.7f);
            replayItem.label = "Replay button";

            // ---- the sequence: it lives on the canvas and plays everything below it ----
            MotionSequence sequence = canvasObject.AddComponent<MotionSequence>();
            sequence.interval = 0.34f;
            sequence.spacing = SequenceSpacing.StartToStart;
            sequence.playOnStart = true;
            sequence.groupSameOrder = true;
            sequence.Collect(true);
            UnityEventTools.AddVoidPersistentListener(replay.onClick, sequence.Play);
            background.transform.SetAsFirstSibling();

            rootRect.localScale = Vector3.one;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasObject;
            Debug.Log("Motion Studio: demo built. Select the canvas and use the Preview in the Motion Sequence component, or press Play.");
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null)
            {
                Component module = go.AddComponent(inputModule);
                System.Reflection.MethodInfo assign = inputModule.GetMethod("AssignDefaultActions");
                if (assign != null)
                    assign.Invoke(module, null);
            }
            else
                go.AddComponent<StandaloneInputModule>();
        }
    }
}
