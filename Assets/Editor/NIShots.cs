using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Renders the three display canvases to PNG files in Temp/NIShots. Works in Edit and Play mode.</summary>
public static class NIShots
{
    [MenuItem("Tools/National Initiative/Capture Displays")]
    public static string Capture(bool blackBackground = false)
    {
        string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "NIShots");
        Directory.CreateDirectory(dir);
        string report = string.Empty;
        string[] names = { "TouchScreen_Canvas", "LargeLEDCircle_Canvas", "ContentDisplay_Canvas" };
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int n = 0; n < names.Length; n++)
        {
            Canvas canvas = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].name == names[n])
                    canvas = canvases[i];
            }

            if (canvas == null)
            {
                report += names[n] + ": missing\n";
                continue;
            }

            Camera cam = canvas.worldCamera;
            if (cam == null)
            {
                report += names[n] + ": no camera\n";
                continue;
            }

            int w = 1080;
            int h = 1080;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                w = Mathf.RoundToInt(scaler.referenceResolution.x);
                h = Mathf.RoundToInt(scaler.referenceResolution.y);
            }

            CameraClearFlags oldFlags = cam.clearFlags;
            Color oldBackground = cam.backgroundColor;
            if (blackBackground)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            RenderTexture previous = cam.targetTexture;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.targetTexture = previous;
            cam.clearFlags = oldFlags;
            cam.backgroundColor = oldBackground;

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(Path.Combine(dir, names[n] + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            report += names[n] + ": " + w + "x" + h + "\n";
        }

        return report;
    }
}
