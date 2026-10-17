using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MotionStudio.Editor
{
    /// <summary>
    /// Plays and scrubs animations in the editor without entering Play mode. When it stops, every object is put back
    /// to the state the scene was designed in, so nothing is left half animated in the scene file.
    /// </summary>
    [InitializeOnLoad]
    public static class MotionPreview
    {
        static MotionSequence sequence;
        static MotionBehaviour single;
        static bool playing;
        static double last;
        static float time;
        static float speed = 1f;

        static MotionPreview()
        {
            EditorApplication.update += Tick;
            EditorSceneManager.sceneSaving += (scene, path) => Stop();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    Stop();
            };
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }

        public static bool IsPlaying
        {
            get { return playing; }
        }

        public static float Time
        {
            get { return time; }
        }

        public static bool IsActive(MotionSequence s)
        {
            return sequence == s;
        }

        public static bool IsActive(MotionBehaviour b)
        {
            return single == b;
        }

        public static void Play(MotionSequence s, float playbackSpeed = 1f)
        {
            Stop();
            sequence = s;
            speed = playbackSpeed;
            time = 0f;
            s.Prepare();
            s.Scrub(0f);
            last = EditorApplication.timeSinceStartup;
            playing = true;
        }

        public static void Play(MotionBehaviour b, float playbackSpeed = 1f)
        {
            Stop();
            single = b;
            speed = playbackSpeed;
            time = 0f;
            b.Prepare();
            b.Evaluate(0f);
            last = EditorApplication.timeSinceStartup;
            playing = true;
        }

        public static void Pause()
        {
            playing = false;
        }

        public static void Scrub(MotionSequence s, float seconds)
        {
            if (sequence != s)
            {
                Stop();
                sequence = s;
            }

            playing = false;
            time = seconds;
            s.Scrub(seconds);
            Redraw();
        }

        public static void Scrub(MotionBehaviour b, float seconds)
        {
            if (single != b)
            {
                Stop();
                single = b;
                b.Prepare();
            }

            playing = false;
            time = seconds;
            b.Evaluate(seconds);
            Redraw();
        }

        public static void Stop()
        {
            playing = false;
            if (sequence != null)
            {
                sequence.ResetToRest();
                sequence = null;
            }

            if (single != null)
            {
                single.ApplyRestState();
                single = null;
            }

            time = 0f;
            Redraw();
        }

        static void Tick()
        {
            if (!playing)
                return;

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - last);
            last = now;
            time += dt * speed;

            if (sequence != null)
            {
                float total = sequence.TotalDuration;
                if (time >= total)
                {
                    time = total;
                    playing = false;
                }

                sequence.Scrub(time);
            }
            else if (single != null)
            {
                float total = single.Duration;
                if (time >= total)
                {
                    time = total;
                    playing = false;
                }

                single.Evaluate(time);
            }
            else
                playing = false;

            Redraw();
        }

        static void Redraw()
        {
            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
