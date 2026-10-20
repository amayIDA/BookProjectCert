using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScreenKit
{
    /// <summary>
    /// Plain line icons drawn in code so a screen can be built without any artwork.
    /// Each icon is a white PNG on transparent, written once to Assets/BookPageCurl/Sprites/Icons.
    /// Replace a PNG with the real artwork (same name) and every screen that uses it updates.
    /// </summary>
    public static class ScreenIcons
    {
        const string Folder = "Assets/BookPageCurl/Sprites/Icons";
        const int Size = 192;
        const float Stroke = 0.055f;

        public static Sprite Get(string name)
        {
            string path = Folder + "/" + name + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            List<Vector2[]> strokes = Shape(name);
            if (strokes == null)
            {
                Debug.LogWarning("ScreenIcons: no icon called " + name);
                return null;
            }

            Directory.CreateDirectory(Folder);
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[Size * Size];
            float aa = 1.5f / Size;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float d = 10f;
                    for (int s = 0; s < strokes.Count; s++)
                    {
                        Vector2[] line = strokes[s];
                        for (int i = 0; i + 1 < line.Length; i++)
                            d = Mathf.Min(d, Distance(p, line[i], line[i + 1]));
                    }

                    float a = Mathf.Clamp01((Stroke * 0.5f + aa - d) / (2f * aa));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static float Distance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len = ab.sqrMagnitude;
            float t = len < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len);
            return Vector2.Distance(p, a + ab * t);
        }

        static Vector2[] Circle(float cx, float cy, float r)
        {
            Vector2[] points = new Vector2[49];
            for (int i = 0; i < points.Length; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                points[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }

            return points;
        }

        static Vector2[] Line(params float[] xy)
        {
            Vector2[] points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }

        static List<Vector2[]> Shape(string name)
        {
            List<Vector2[]> s = new List<Vector2[]>();
            switch (name)
            {
                case "alert":
                    s.Add(Circle(0.5f, 0.5f, 0.4f));
                    s.Add(Line(0.5f, 0.42f, 0.5f, 0.7f));
                    s.Add(Line(0.5f, 0.29f, 0.5f, 0.295f));
                    break;
                case "user":
                    s.Add(Circle(0.5f, 0.7f, 0.2f));
                    s.Add(Line(0.18f, 0.1f, 0.18f, 0.26f, 0.27f, 0.38f, 0.5f, 0.44f, 0.73f, 0.38f, 0.82f, 0.26f, 0.82f, 0.1f));
                    break;
                case "users":
                    s.Add(Circle(0.36f, 0.7f, 0.17f));
                    s.Add(Line(0.08f, 0.1f, 0.08f, 0.26f, 0.17f, 0.37f, 0.36f, 0.43f, 0.55f, 0.37f, 0.64f, 0.26f, 0.64f, 0.1f));
                    s.Add(Line(0.62f, 0.86f, 0.7f, 0.88f, 0.78f, 0.84f, 0.8f, 0.72f, 0.74f, 0.62f, 0.66f, 0.58f));
                    s.Add(Line(0.74f, 0.42f, 0.86f, 0.34f, 0.92f, 0.22f, 0.92f, 0.1f));
                    break;
                case "briefcase":
                    s.Add(Line(0.08f, 0.12f, 0.92f, 0.12f, 0.92f, 0.7f, 0.08f, 0.7f, 0.08f, 0.12f));
                    s.Add(Line(0.34f, 0.7f, 0.34f, 0.86f, 0.66f, 0.86f, 0.66f, 0.7f));
                    s.Add(Line(0.3f, 0.12f, 0.3f, 0.7f));
                    s.Add(Line(0.7f, 0.12f, 0.7f, 0.7f));
                    break;
                case "target":
                    s.Add(Circle(0.5f, 0.5f, 0.46f));
                    s.Add(Circle(0.5f, 0.5f, 0.3f));
                    s.Add(Circle(0.5f, 0.5f, 0.12f));
                    break;
                case "crown":
                    s.Add(Line(0.08f, 0.28f, 0.08f, 0.74f, 0.29f, 0.52f, 0.5f, 0.82f, 0.71f, 0.52f, 0.92f, 0.74f, 0.92f, 0.28f, 0.08f, 0.28f));
                    s.Add(Line(0.08f, 0.14f, 0.92f, 0.14f));
                    break;
                case "ring":
                    s.Add(Circle(0.5f, 0.5f, 0.47f));
                    break;
                case "clock":
                    s.Add(Circle(0.5f, 0.5f, 0.4f));
                    s.Add(Line(0.5f, 0.5f, 0.5f, 0.74f));
                    s.Add(Line(0.5f, 0.5f, 0.68f, 0.38f));
                    break;
                case "cap":
                    s.Add(Line(0.5f, 0.82f, 0.94f, 0.6f, 0.5f, 0.38f, 0.06f, 0.6f, 0.5f, 0.82f));
                    s.Add(Line(0.24f, 0.5f, 0.24f, 0.3f, 0.5f, 0.18f, 0.76f, 0.3f, 0.76f, 0.5f));
                    s.Add(Line(0.94f, 0.6f, 0.94f, 0.34f));
                    break;
                case "trend":
                    s.Add(Line(0.06f, 0.24f, 0.36f, 0.54f, 0.52f, 0.4f, 0.92f, 0.78f));
                    s.Add(Line(0.68f, 0.78f, 0.92f, 0.78f, 0.92f, 0.54f));
                    break;
                case "translate":
                    s.Add(Line(0.06f, 0.72f, 0.5f, 0.72f));
                    s.Add(Line(0.28f, 0.82f, 0.28f, 0.72f));
                    s.Add(Line(0.12f, 0.46f, 0.3f, 0.6f, 0.46f, 0.72f));
                    s.Add(Line(0.46f, 0.46f, 0.28f, 0.6f, 0.14f, 0.72f));
                    s.Add(Line(0.46f, 0.2f, 0.68f, 0.8f, 0.9f, 0.2f));
                    s.Add(Line(0.54f, 0.4f, 0.82f, 0.4f));
                    break;
                case "layers":
                    s.Add(Line(0.5f, 0.86f, 0.94f, 0.66f, 0.5f, 0.46f, 0.06f, 0.66f, 0.5f, 0.86f));
                    s.Add(Line(0.06f, 0.5f, 0.5f, 0.3f, 0.94f, 0.5f));
                    s.Add(Line(0.06f, 0.34f, 0.5f, 0.14f, 0.94f, 0.34f));
                    break;
                default:
                    return null;
            }

            return s;
        }
    }
}
