using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MotionStudio
{
    /// <summary>
    /// Plays a list of animations in the order you give, with a controllable interval between them.
    /// Put it on a parent object, press "Collect Children" and choose the interval.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Motion Studio/Motion Sequence")]
    public class MotionSequence : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The animation to play. Any Motion Studio component works.")]
            public MotionBehaviour target;

            [Tooltip("Extra seconds added to this entry only. Negative values start it earlier.")]
            public float offset;

            [Tooltip("Turn off to skip this entry without removing it.")]
            public bool enabled = true;
        }

        [Header("Order and spacing")]
        public List<Entry> entries = new List<Entry>();

        [Tooltip("Seconds between items. See Spacing for what it is measured from.")]
        [Min(0f)] public float interval = 0.15f;

        public SequenceSpacing spacing = SequenceSpacing.StartToStart;

        [Tooltip("Items that have the same Order number start at the same moment.")]
        public bool groupSameOrder = true;

        [Header("Playback")]
        public bool playOnStart = true;

        [Tooltip("Seconds to wait before the sequence begins.")]
        [Min(0f)] public float startDelay;

        [Tooltip("Hide every item until it is its turn. Prevents a flash of the finished layout.")]
        public bool hideUntilPlayed = true;

        public bool loop;

        [Min(0f)] public float loopPause = 1f;

        [Range(0.1f, 4f)] public float speed = 1f;

        public bool useUnscaledTime = true;

        [Header("Events")]
        public UnityEvent onStarted = new UnityEvent();
        public UnityEvent onCompleted = new UnityEvent();

        float time;
        float direction = 1f;
        bool playing;
        bool paused;
        float waiting;
        float total;
        bool dirty = true;
        readonly List<float> starts = new List<float>();
        readonly List<float> lastLocal = new List<float>();

        // ---------- timeline ----------

        public float TotalDuration
        {
            get
            {
                Rebuild();
                return total;
            }
        }

        public float Time
        {
            get { return time; }
        }

        public bool IsPlaying
        {
            get { return playing && !paused; }
        }

        public bool IsPaused
        {
            get { return paused; }
        }

        public float StartOf(int index)
        {
            Rebuild();
            return index >= 0 && index < starts.Count ? starts[index] : 0f;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        /// <summary>Compute when every entry starts.</summary>
        public void Rebuild()
        {
            if (!dirty && starts.Count == entries.Count)
                return;

            dirty = false;
            starts.Clear();
            lastLocal.Clear();
            total = 0f;

            float cursor = 0f;
            float groupStart = 0f;
            int previousOrder = int.MinValue;
            bool first = true;
            float groupEnd = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.target == null || !entry.enabled)
                {
                    starts.Add(0f);
                    lastLocal.Add(float.NaN);
                    continue;
                }

                MotionBehaviour target = entry.target;
                bool together = groupSameOrder && !first && target.order == previousOrder;
                float start;
                if (together)
                    start = groupStart;
                else
                {
                    start = first ? 0f : cursor;
                    groupStart = start;
                    groupEnd = start;
                }

                float begin = start + entry.offset;
                starts.Add(begin);
                lastLocal.Add(float.NaN);

                float end = begin + target.Duration;
                total = Mathf.Max(total, end);
                groupEnd = Mathf.Max(groupEnd, end);

                // the cursor for the next group
                cursor = spacing == SequenceSpacing.StartToStart ? groupStart + interval : groupEnd + interval;
                previousOrder = target.order;
                first = false;
            }
        }

        // ---------- building the list ----------

        /// <summary>Fill the list with every Motion Studio component below this object, in hierarchy order or sorted by Order.</summary>
        public void Collect(bool sortByOrder = true)
        {
            MotionBehaviour[] found = GetComponentsInChildren<MotionBehaviour>(true);
            List<MotionBehaviour> list = new List<MotionBehaviour>(found);
            if (sortByOrder)
            {
                // stable sort: equal Order keeps hierarchy order
                List<KeyValuePair<int, MotionBehaviour>> indexed = new List<KeyValuePair<int, MotionBehaviour>>();
                for (int i = 0; i < list.Count; i++)
                    indexed.Add(new KeyValuePair<int, MotionBehaviour>(i, list[i]));
                indexed.Sort((a, b) =>
                {
                    int c = a.Value.order.CompareTo(b.Value.order);
                    return c != 0 ? c : a.Key.CompareTo(b.Key);
                });
                list.Clear();
                for (int i = 0; i < indexed.Count; i++)
                    list.Add(indexed[i].Value);
            }

            entries.Clear();
            for (int i = 0; i < list.Count; i++)
                entries.Add(new Entry { target = list[i] });
            dirty = true;
        }

        public Entry Add(MotionBehaviour target, float offset = 0f)
        {
            Entry entry = new Entry { target = target, offset = offset };
            entries.Add(entry);
            dirty = true;
            return entry;
        }

        public void Clear()
        {
            entries.Clear();
            dirty = true;
        }

        /// <summary>Sort the list by each item's Order number (stable).</summary>
        public void SortByOrder()
        {
            List<KeyValuePair<int, Entry>> indexed = new List<KeyValuePair<int, Entry>>();
            for (int i = 0; i < entries.Count; i++)
                indexed.Add(new KeyValuePair<int, Entry>(i, entries[i]));
            indexed.Sort((a, b) =>
            {
                int oa = a.Value != null && a.Value.target != null ? a.Value.target.order : int.MaxValue;
                int ob = b.Value != null && b.Value.target != null ? b.Value.target.order : int.MaxValue;
                int c = oa.CompareTo(ob);
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            entries.Clear();
            for (int i = 0; i < indexed.Count; i++)
                entries.Add(indexed[i].Value);
            dirty = true;
        }

        /// <summary>Give the items the numbers 0, 1, 2 ... in the current list order.</summary>
        public void NumberEntriesByListOrder()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].target != null)
                    entries[i].target.order = i;
            }

            dirty = true;
        }

        // ---------- playback ----------

        void OnEnable()
        {
            dirty = true;
            if (Application.isPlaying && hideUntilPlayed)
                PrepareAll(true);
        }

        void Start()
        {
            if (playOnStart)
                Play();
        }

        void OnValidate()
        {
            dirty = true;
        }

        /// <summary>Make every item ready: finds its components and records its designed state.</summary>
        public void Prepare()
        {
            PrepareAll(false);
        }

        void PrepareAll(bool applyStart)
        {
            Rebuild();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null || entries[i].target == null)
                    continue;
                entries[i].target.Prepare();
                if (applyStart && entries[i].enabled)
                    entries[i].target.ApplyStartState();
            }
        }

        /// <summary>Start from the beginning.</summary>
        public void Play()
        {
            PrepareAll(false);
            time = 0f;
            direction = 1f;
            waiting = startDelay;
            playing = true;
            paused = false;
            ForceEvaluate(0f);
            onStarted.Invoke();
        }

        /// <summary>Play the sequence backwards, from the end to the start (an exit animation).</summary>
        public void PlayReverse()
        {
            PrepareAll(false);
            Rebuild();
            time = total;
            direction = -1f;
            waiting = 0f;
            playing = true;
            paused = false;
            ForceEvaluate(time);
            onStarted.Invoke();
        }

        public void Restart()
        {
            Play();
        }

        public void Pause()
        {
            paused = true;
        }

        public void Resume()
        {
            paused = false;
        }

        public void Stop()
        {
            playing = false;
            paused = false;
        }

        /// <summary>Jump to the end state of every item.</summary>
        public void Complete()
        {
            PrepareAll(false);
            Rebuild();
            playing = false;
            time = total;
            ForceEvaluate(total);
        }

        /// <summary>Jump to the start state of every item (everything hidden).</summary>
        public void ResetToStart()
        {
            PrepareAll(false);
            playing = false;
            time = 0f;
            ForceEvaluate(0f);
        }

        /// <summary>Put every item back to the state the scene was designed in.</summary>
        public void ResetToRest()
        {
            playing = false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].target != null)
                    entries[i].target.ApplyRestState();
            }
        }

        /// <summary>Show the sequence at an exact time. Used by the editor preview.</summary>
        public void Scrub(float seconds)
        {
            Rebuild();
            for (int i = 0; i < entries.Count; i++)
                lastLocal[i] = float.NaN;
            time = Mathf.Clamp(seconds, 0f, total);
            ForceEvaluate(time);
        }

        void ForceEvaluate(float seconds)
        {
            Rebuild();
            for (int i = 0; i < lastLocal.Count; i++)
                lastLocal[i] = float.NaN;
            EvaluateAt(seconds);
        }

        void EvaluateAt(float seconds)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.target == null || !entry.enabled)
                    continue;

                float duration = entry.target.Duration;
                float local = Mathf.Clamp(seconds - starts[i], 0f, duration);
                if (!float.IsNaN(lastLocal[i]) && Mathf.Approximately(lastLocal[i], local))
                    continue;

                lastLocal[i] = local;
                entry.target.Evaluate(local);
            }
        }

        void Update()
        {
            if (!playing || paused)
                return;

            float dt = useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
            if (waiting > 0f)
            {
                waiting -= dt;
                return;
            }

            Rebuild();
            time += dt * speed * direction;
            if (direction > 0f && time >= total)
            {
                time = total;
                EvaluateAt(time);
                Finish();
                return;
            }

            if (direction < 0f && time <= 0f)
            {
                time = 0f;
                EvaluateAt(time);
                Finish();
                return;
            }

            EvaluateAt(time);
        }

        void Finish()
        {
            playing = false;
            onCompleted.Invoke();
            if (loop)
                StartCoroutineLoop();
        }

        void StartCoroutineLoop()
        {
            StartCoroutine(LoopRoutine());
        }

        System.Collections.IEnumerator LoopRoutine()
        {
            float wait = loopPause;
            while (wait > 0f)
            {
                wait -= useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
                yield return null;
            }

            if (loop && isActiveAndEnabled)
            {
                ResetToStart();
                Play();
            }
        }
    }
}
