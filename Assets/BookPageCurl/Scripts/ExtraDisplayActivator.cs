using UnityEngine;

/// <summary>
/// Turns on every display after the first one. Used by the split-display scene.
/// </summary>
public class ExtraDisplayActivator : MonoBehaviour
{
    void Start()
    {
        for (int i = 1; i < Display.displays.Length; i++)
            Display.displays[i].Activate();
    }
}
