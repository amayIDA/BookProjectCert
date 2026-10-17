using UnityEngine;

/// <summary>
/// Turns on display 2 in a player build. The editor Game view selects a display from its toolbar.
/// </summary>
public class SecondDisplayActivator : MonoBehaviour
{
    void Start()
    {
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();
    }
}
