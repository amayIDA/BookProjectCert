using MotionStudio;
using UnityEngine;

/// <summary>
/// Plays the page's Motion Sequence whenever the page is shown.
/// </summary>
public class PageIntro : MonoBehaviour
{
    MotionSequence sequence;

    void Awake()
    {
        sequence = GetComponent<MotionSequence>();
    }

    void OnEnable()
    {
        if (sequence == null)
            sequence = GetComponent<MotionSequence>();
        if (!Application.isPlaying || sequence == null)
            return;
        Canvas.ForceUpdateCanvases();
        RememberScenePoses();
        sequence.Play();
    }

    /// <summary>
    /// Layout has settled, so the pose in the scene is the one every animation ends on.
    /// </summary>
    void RememberScenePoses()
    {
        MotionItem[] items = GetComponentsInChildren<MotionItem>(true);
        for (int i = 0; i < items.Length; i++)
        {
            MotionItem item = items[i];
            if (item == null)
                continue;
            // the sequence has already moved it to its start pose, so go back to the saved rest pose before recording
            if (item.recorded)
                item.ApplyRestState();
            CanvasGroup group = item.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = 1f;
            item.Record();
        }

        AxisReveal[] lines = GetComponentsInChildren<AxisReveal>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null)
                continue;
            // the sequence has already moved everything to its start pose, so go back to the saved rest pose first
            if (lines[i].recorded)
                lines[i].ApplyRestState();
            lines[i].recorded = false;
            lines[i].Prepare();
        }

        CardLineToBox[] cards = GetComponentsInChildren<CardLineToBox>(true);
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;
            if (cards[i].recorded)
                cards[i].ApplyRestState();
            cards[i].recorded = false;
            cards[i].Prepare();
        }
    }
}
