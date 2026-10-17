using UnityEngine;

namespace MotionStudio
{
    /// <summary>Where an element comes from when it enters.</summary>
    public enum MotionDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom
    }

    /// <summary>How a text is cut into pieces that animate one after another.</summary>
    public enum TextSplit
    {
        Whole,
        Words,
        Characters,
        Lines
    }

    /// <summary>In which order the pieces of a text appear.</summary>
    public enum TextOrder
    {
        Forward,
        Reverse,
        FromCenter,
        ToCenter,
        Random
    }

    /// <summary>Easing curves. The Apple... curves are the long, soft deceleration used by system animations.</summary>
    public enum MotionEase
    {
        Linear,
        SineIn,
        SineOut,
        SineInOut,
        QuadOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        QuartOut,
        QuintOut,
        ExpoOut,
        ExpoInOut,
        CircOut,
        BackOut,
        BackInOut,
        ElasticOut,
        BounceOut,
        AppleOut,
        AppleInOut,
        AppleSoft,
        SpringOut
    }

    /// <summary>How the sequence spaces its items.</summary>
    public enum SequenceSpacing
    {
        /// <summary>The next item starts "interval" seconds after the previous one STARTED. Overlapping, flowing.</summary>
        StartToStart,

        /// <summary>The next item starts "interval" seconds after the previous one FINISHED. Strict one after another.</summary>
        AfterPrevious
    }

    /// <summary>State of one piece of a text (a word, a character or a line).</summary>
    public struct TextUnitState
    {
        public float alpha;
        public Vector2 offset;
        public float scale;
        public bool useColor;
        public Color color;
    }

    /// <summary>Anything that can be placed on the timeline of a MotionSequence.</summary>
    public interface IMotionPlayable
    {
        /// <summary>Total length in seconds including its own delay.</summary>
        float Duration { get; }

        /// <summary>Position in the sequence. Lower plays first, equal numbers play together.</summary>
        int Order { get; }

        /// <summary>Set the state at a time since the item started. Values outside 0..Duration are clamped.</summary>
        void Evaluate(float time);

        /// <summary>Hidden or collapsed state before the item starts.</summary>
        void ApplyStartState();

        /// <summary>The natural state the scene was designed in (what you see in the editor).</summary>
        void ApplyRestState();

        GameObject gameObject { get; }
    }

    /// <summary>Contract for the pieces of a text that can be animated separately. Implement it to support another text component.</summary>
    public interface IMotionTextAdapter
    {
        /// <summary>Number of pieces for the split mode.</summary>
        int UnitCount(TextSplit split);

        /// <summary>Capture the current text. Called before the first animation frame and whenever the text changed.</summary>
        void Begin(TextSplit split);

        void SetUnit(int index, TextUnitState state);

        /// <summary>Write all pieces to the text component in one go.</summary>
        void Commit();

        /// <summary>Put the plain text back.</summary>
        void Restore();

        /// <summary>True when the text changed since Begin and Begin must run again.</summary>
        bool NeedsBegin(TextSplit split);

        /// <summary>True when offsets and scale per piece are rendered (TextMeshPro). Legacy Text can only fade and tint.</summary>
        bool SupportsTransform { get; }
    }

    /// <summary>Base class for adapters. One instance of a subclass is created per text component.</summary>
    public abstract class MotionTextAdapterFactory
    {
        public abstract IMotionTextAdapter Create(Component component);
    }
}
