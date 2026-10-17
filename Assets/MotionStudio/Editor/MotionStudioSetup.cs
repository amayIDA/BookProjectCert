using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotionStudio.Editor
{
    /// <summary>
    /// Keeps the plugin ready: makes sure the glow material exists next to the shader (so the shader is always part of a build),
    /// and imports the demo pictures as sprites.
    /// </summary>
    [InitializeOnLoad]
    public class MotionStudioSetup : AssetPostprocessor
    {
        static MotionStudioSetup()
        {
            EditorApplication.delayCall += () => EnsureResources(false);
        }

        public static void EnsureResources(bool log)
        {
            string[] guids = AssetDatabase.FindAssets("MotionGlow t:Shader");
            for (int i = 0; i < guids.Length; i++)
            {
                string shaderPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!shaderPath.EndsWith("MotionGlow.shader"))
                    continue;

                string materialPath = Path.Combine(Path.GetDirectoryName(shaderPath), "MotionGlow.mat").Replace('\\', '/');
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
                {
                    if (log)
                        Debug.Log("Motion Studio: glow material is fine.");
                    return;
                }

                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if (shader == null)
                    return;

                AssetDatabase.CreateAsset(new Material(shader), materialPath);
                AssetDatabase.SaveAssets();
                if (log)
                    Debug.Log("Motion Studio: created " + materialPath);
                return;
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/MotionStudio/Demo/Sprites/"))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = file.StartsWith("demo_icon");
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;

            if (file == "card_panel")
                importer.spriteBorder = new Vector4(44f, 44f, 44f, 44f);
        }
    }
}
