using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the Talent Wheel art and audio in Assets/Resources/NationalInitiative.
/// Pictures become readable sprites (the hologram samples the white shapes), the rounded and glass plates get
/// nine-slice borders, and the audio is kept uncompressed so the music loops have no gap.
/// </summary>
public class NIAssetImport : AssetPostprocessor
{
    const string Folder = "Assets/Resources/NationalInitiative";

    static float BorderFor(string file)
    {
        switch (file)
        {
            case "ui_round_6": return 6f;
            case "ui_round_16": return 16f;
            case "ui_round_31": return 31f;
            case "ui_glass": return 22f;
            case "ui_glass_soft": return 22f;
            case "ui_glass_pill": return 31f;
            default: return 0f;
        }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        string file = Path.GetFileNameWithoutExtension(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = (file == "pattern_tile" || file == "ui_scan") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        float border = BorderFor(file);
        if (border > 0f)
            importer.spriteBorder = new Vector4(border, border, border, border);

        // full rect meshes keep sliced and tiled images clean
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Folder))
            return;

        AudioImporter importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = false;
    }

    [InitializeOnLoadMethod]
    static void CheckOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            TextureImporter probe = AssetImporter.GetAtPath(Folder + "/ui_glass.png") as TextureImporter;
            if (probe != null && probe.spriteBorder == Vector4.zero)
                Reimport();
        };
    }

    [MenuItem("Tools/National Initiative/Reimport Art And Audio")]
    public static void Reimport()
    {
        AssetDatabase.ImportAsset(Folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
    }
}
