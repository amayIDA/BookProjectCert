using System;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio
{
    /// <summary>
    /// One line helpers for code and for AI agents. Every method adds the right component, records the current position as the
    /// final state and returns the component so it can be tuned further.
    /// <code>
    /// MotionSequence seq = Motion.Sequence(canvasObject, interval: 0.15f);
    /// seq.Add(Motion.FadeIn(title, MotionDirection.Left, 100f, 0.8f));
    /// seq.Add(Motion.Words(subtitle, MotionDirection.Bottom));
    /// seq.Add(Motion.Card(panel));
    /// seq.Play();
    /// </code>
    /// </summary>
    public static class Motion
    {
        static T Get<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        /// <summary>Fade (and optionally slide) an object in. direction None only fades.</summary>
        public static MotionItem FadeIn(GameObject go, MotionDirection direction = MotionDirection.None, float distance = 80f, float duration = 0.8f, float delay = 0f, MotionEase ease = MotionEase.AppleOut)
        {
            MotionItem item = Get<MotionItem>(go);
            item.Prepare();
            item.Record();
            item.fade = true;
            item.direction = direction;
            item.distance = distance;
            item.duration = duration;
            item.delay = delay;
            item.ease = ease;
            return item;
        }

        /// <summary>Animate a text word by word (or by characters or lines).</summary>
        public static MotionItem Words(GameObject text, MotionDirection direction = MotionDirection.Bottom, float distance = 30f, float stagger = 0.07f, TextSplit split = TextSplit.Words, float delay = 0f)
        {
            MotionItem item = FadeIn(text, direction, distance, 0.7f, delay);
            item.split = split;
            item.pieceStagger = stagger;
            return item;
        }

        /// <summary>Change color while arriving.</summary>
        public static MotionItem ColorChange(GameObject go, Color from, Color to)
        {
            MotionItem item = Get<MotionItem>(go);
            item.Prepare();
            item.changeColor = true;
            item.colorFrom = from;
            item.colorTo = to;
            return item;
        }

        /// <summary>Card reveal: a line grows from the centre and opens into the card.</summary>
        public static MotionCard Card(GameObject go, float delay = 0f)
        {
            MotionCard card = Get<MotionCard>(go);
            card.Prepare();
            card.delay = delay;
            return card;
        }

        /// <summary>Card that grows out of another UI object.</summary>
        public static MotionCard CardFrom(GameObject go, RectTransform from, float delay = 0f)
        {
            MotionCard card = Card(go, delay);
            card.origin = CardOrigin.FromObject;
            card.fromObject = from;
            return card;
        }

        /// <summary>Build an icon out of pieces.</summary>
        public static MotionIconComposer Compose(GameObject icon, ComposeStyle style = ComposeStyle.Assemble, float duration = 1.8f, float delay = 0f)
        {
            MotionIconComposer composer = Get<MotionIconComposer>(icon);
            composer.style = style;
            composer.duration = duration;
            composer.delay = delay;
            composer.Prepare();
            return composer;
        }

        /// <summary>Animate a MotionGlowGraphic: it grows to the size set in the scene.</summary>
        public static MotionGlow Glow(GameObject go, float duration = 1.6f, float delay = 0f)
        {
            MotionGlow glow = Get<MotionGlow>(go);
            glow.duration = duration;
            glow.delay = delay;
            glow.Prepare();
            return glow;
        }

        /// <summary>Get or create a sequence on an object. Collects nothing, use Add or Collect.</summary>
        public static MotionSequence Sequence(GameObject host, float interval = 0.15f, SequenceSpacing spacing = SequenceSpacing.StartToStart)
        {
            MotionSequence sequence = Get<MotionSequence>(host);
            sequence.interval = interval;
            sequence.spacing = spacing;
            sequence.playOnStart = false;
            return sequence;
        }

        /// <summary>Add an animation to a sequence and give it the next order number.</summary>
        public static T AddTo<T>(this MotionSequence sequence, T behaviour, float offset = 0f) where T : MotionBehaviour
        {
            behaviour.order = sequence.entries.Count;
            sequence.Add(behaviour, offset);
            return behaviour;
        }
    }
}
