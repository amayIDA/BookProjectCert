using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MotionStudio.Editor
{
    /// <summary>Saves a picture of a canvas, also an overlay canvas, at the current preview time. Handy for documentation and reviews.</summary>
    public static class MotionCapture
    {
        public static string Save(Canvas canvas, string path, int width = 1920, int height = 1080)
        {
            if (canvas == null)
                return "no canvas";

            Camera camera = Camera.main;
            GameObject temporary = null;
            if (camera == null)
            {
                temporary = new GameObject("~MotionCaptureCamera", typeof(Camera));
                temporary.hideFlags = HideFlags.HideAndDontSave;
                camera = temporary.GetComponent<Camera>();
            }

            RenderMode mode = canvas.renderMode;
            Camera previousCamera = canvas.worldCamera;
            float plane = canvas.planeDistance;
            CameraClearFlags flags = camera.clearFlags;
            Color background = camera.backgroundColor;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.targetTexture = previousTarget;

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = active;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);

            canvas.renderMode = mode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = plane;
            camera.clearFlags = flags;
            camera.backgroundColor = background;
            if (temporary != null)
                Object.DestroyImmediate(temporary);
            return path;
        }
    }
}
