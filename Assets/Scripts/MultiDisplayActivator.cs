using UnityEngine;
using System.Collections.Generic;

public class MultiDisplayActivator : MonoBehaviour
{
    [System.Serializable]
    public class DisplayCamera
    {
        [Header("Camera")]
        public Camera camera;

        [Header("Display")]
        [Min(0)]
        public int displayIndex = 0;

        [Header("Camera Settings")]
        public bool enableCamera = true;
    }

    [Header("CAMERAS / DISPLAYS")]
    public List<DisplayCamera> displays = new List<DisplayCamera>();

    [Header("SETTINGS")]
    public bool activateDisplays = true;
    public bool showDebugLogs = true;

    private void Awake()
    {
        SetupDisplays();
    }

    private void SetupDisplays()
    {
        Debug.Log($"Unity detected {Display.displays.Length} display(s).");

        foreach (DisplayCamera entry in displays)
        {
            // Make sure a camera was assigned
            if (entry.camera == null)
            {
                Debug.LogWarning("No camera assigned to one of the display entries.");
                continue;
            }

            int displayIndex = entry.displayIndex;

            // Check display exists
            if (displayIndex < 0 || displayIndex >= Display.displays.Length)
            {
                Debug.LogWarning(
                    $"Display {displayIndex} does not exist. " +
                    $"Unity detected {Display.displays.Length} display(s)."
                );

                continue;
            }

            // Activate secondary displays
            if (activateDisplays && displayIndex > 0)
            {
                Display.displays[displayIndex].Activate();
            }

            // Display 1 in the Game view is targetDisplay 0. That camera receives the swipe.
            entry.camera.targetDisplay = displayIndex;

            // Enable / disable camera
            entry.camera.enabled = entry.enableCamera;

            if (showDebugLogs)
            {
                Debug.Log(
                    $"Camera '{entry.camera.name}' → Display {displayIndex + 1}"
                );
            }
        }
    }
}   