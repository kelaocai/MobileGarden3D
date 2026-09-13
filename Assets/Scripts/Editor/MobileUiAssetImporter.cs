using UnityEditor;

[InitializeOnLoad]
internal static class MobileUiAssetImporter
{
    private static readonly string[] Paths =
    {
        "Assets/Resources/UI/joystick_base.png",
        "Assets/Resources/UI/joystick_handle.png",
        "Assets/Resources/UI/attack_button.png"
    };

    static MobileUiAssetImporter() => EditorApplication.delayCall += Configure;

    private static void Configure()
    {
        foreach (string path in Paths)
        {
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer ||
                importer.textureType == TextureImporterType.Sprite)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
    }
}
