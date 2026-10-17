using UnityEngine;

namespace MotionStudio
{
    /// <summary>Easing functions. Input t is 0..1. Overshooting curves (Back, Elastic, Spring) may return values outside 0..1.</summary>
    public static class MotionEasing
    {
        public static float Evaluate(MotionEase ease, float t)
        {
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;

            switch (ease)
            {
                case MotionEase.Linear: return t;
                case MotionEase.SineIn: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case MotionEase.SineOut: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case MotionEase.SineInOut: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case MotionEase.QuadOut: return 1f - (1f - t) * (1f - t);
                case MotionEase.CubicIn: return t * t * t;
                case MotionEase.CubicOut: return 1f - Mathf.Pow(1f - t, 3f);
                case MotionEase.CubicInOut: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case MotionEase.QuartOut: return 1f - Mathf.Pow(1f - t, 4f);
                case MotionEase.QuintOut: return 1f - Mathf.Pow(1f - t, 5f);
                case MotionEase.ExpoOut: return 1f - Mathf.Pow(2f, -10f * t);
                case MotionEase.ExpoInOut:
                    return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
                case MotionEase.CircOut: return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
                case MotionEase.BackOut:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case MotionEase.BackInOut:
                {
                    const float c1 = 1.70158f;
                    const float c2 = c1 * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
                }
                case MotionEase.ElasticOut:
                {
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case MotionEase.BounceOut: return BounceOut(t);
                case MotionEase.AppleOut: return Bezier(0.22f, 1f, 0.36f, 1f, t);
                case MotionEase.AppleInOut: return Bezier(0.65f, 0f, 0.35f, 1f, t);
                case MotionEase.AppleSoft: return Bezier(0.25f, 0.1f, 0.25f, 1f, t);
                case MotionEase.SpringOut: return 1f - Mathf.Exp(-7.5f * t) * Mathf.Cos(10.5f * t) + (1f - t) * 0.0004f;
                default: return t;
            }
        }

        static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1)
                return n1 * t * t;
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }

            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        /// <summary>CSS cubic-bezier(x1, y1, x2, y2).</summary>
        public static float Bezier(float x1, float y1, float x2, float y2, float t)
        {
            float lo = 0f;
            float hi = 1f;
            float u = t;
            for (int i = 0; i < 20; i++)
            {
                float x = Cubic(x1, x2, u);
                if (Mathf.Abs(x - t) < 0.0003f)
                    break;
                if (x < t)
                    lo = u;
                else
                    hi = u;
                u = (lo + hi) * 0.5f;
            }

            return Cubic(y1, y2, u);
        }

        static float Cubic(float a, float b, float t)
        {
            float s = 1f - t;
            return 3f * s * s * t * a + 3f * s * t * t * b + t * t * t;
        }

        public static float Remap01(float time, float start, float length)
        {
            if (length <= 0.00001f)
                return time >= start ? 1f : 0f;
            return Mathf.Clamp01((time - start) / length);
        }

        public static Vector2 DirectionVector(MotionDirection direction, Vector2 custom)
        {
            switch (direction)
            {
                // an element that comes from the left starts at negative x
                case MotionDirection.Left: return new Vector2(-1f, 0f);
                case MotionDirection.Right: return new Vector2(1f, 0f);
                case MotionDirection.Top: return new Vector2(0f, 1f);
                case MotionDirection.Bottom: return new Vector2(0f, -1f);
                case MotionDirection.TopLeft: return new Vector2(-0.7071f, 0.7071f);
                case MotionDirection.TopRight: return new Vector2(0.7071f, 0.7071f);
                case MotionDirection.BottomLeft: return new Vector2(-0.7071f, -0.7071f);
                case MotionDirection.BottomRight: return new Vector2(0.7071f, -0.7071f);
                case MotionDirection.Custom: return custom;
                default: return Vector2.zero;
            }
        }
    }
}
