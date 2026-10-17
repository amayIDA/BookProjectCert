using System.Collections;
using UnityEngine;

namespace MotionStudio
{
    /// <summary>
    /// Base of every animation component. It has an order (position in a sequence), a delay and a duration,
    /// and can be played alone or as part of a <see cref="MotionSequence"/>.
    /// </summary>
    public abstract class MotionBehaviour : MonoBehaviour, IMotionPlayable
    {
        [Header("Sequence")]
        [Tooltip("Position in the sequence. Lower numbers play first. Items with the same number play together.")]
        public int order;

        [Tooltip("Extra seconds to wait before this item starts, on top of the sequence spacing.")]
        [Min(0f)] public float delay;

        [Tooltip("Optional name shown in the sequence timeline.")]
        public string label;

        Coroutine standalone;

        /// <summary>Length of the animation itself, without the delay.</summary>
        public abstract float ContentDuration { get; }

        /// <summary>Set the state at a time since the animation started (before the delay is removed).</summary>
        protected abstract void EvaluateContent(float time);

        /// <summary>Return everything to the state of the scene as it was designed.</summary>
        public abstract void ApplyRestState();

        /// <summary>Make sure the animation can run: find components and record the designed state.</summary>
        public virtual void Prepare()
        {
        }

        public float Duration
        {
            get { return delay + ContentDuration; }
        }

        public int Order
        {
            get { return order; }
        }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(label) ? name : label; }
        }

        public void Evaluate(float time)
        {
            EvaluateContent(time - delay);
        }

        public virtual void ApplyStartState()
        {
            Evaluate(0f);
        }

        /// <summary>The state at the end of the animation.</summary>
        public virtual void ApplyEndState()
        {
            Evaluate(Duration);
        }

        public bool IsPlayingStandalone
        {
            get { return standalone != null; }
        }

        /// <summary>Play this item alone, without a sequence.</summary>
        public void Play(float speed = 1f)
        {
            Stop();
            Prepare();
            standalone = StartCoroutine(RunStandalone(speed));
        }

        public void PlayReverse(float speed = 1f)
        {
            Stop();
            Prepare();
            standalone = StartCoroutine(RunStandalone(-Mathf.Abs(speed)));
        }

        public void Stop()
        {
            if (standalone != null)
            {
                StopCoroutine(standalone);
                standalone = null;
            }
        }

        IEnumerator RunStandalone(float speed)
        {
            float total = Duration;
            float time = speed >= 0f ? 0f : total;
            Evaluate(time);
            while (speed >= 0f ? time < total : time > 0f)
            {
                yield return null;
                time += Time.unscaledDeltaTime * speed;
                time = Mathf.Clamp(time, 0f, total);
                Evaluate(time);
            }

            standalone = null;
        }

        protected virtual void OnDisable()
        {
            Stop();
        }
    }
}
