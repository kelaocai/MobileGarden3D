using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class BattleArenaSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/BattleArena.unity";
    private const string BackgroundPath = "Assets/Art/Backgrounds/bakery_garden_arena_v1.png";
    private const string KnightPath = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Characters/Knight.prefab";
    private const string SwordPath = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Accessories/sword_2handed.prefab";

    static BattleArenaSceneBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    private static void BuildOnce()
    {
        if (File.Exists(ScenePath))
        {
            return;
        }

        ConfigureBackgroundImport();
        EditorSceneManager.SaveOpenScenes();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BattleArena";

        Camera camera = CreateCamera();
        CreateBackground(camera);
        CreateWalkableGround();
        CreateLighting();
        CreateKnightAndSword();

        EditorSceneManager.SaveScene(scene, ScenePath);
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Created fixed-camera BattleArena scene.");
    }

    private static void ConfigureBackgroundImport()
    {
        AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 8.365f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.14f, 0.11f);
        cameraObject.transform.position = new Vector3(0f, 12f, -10f);
        cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.zero - cameraObject.transform.position, Vector3.up);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<LockedArenaCamera>();
        return camera;
    }

    private static void CreateBackground(Camera camera)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        GameObject background = new("Static Arena Background");
        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;

        Transform cameraTransform = camera.transform;
        background.transform.position = cameraTransform.position + cameraTransform.forward * 30f;
        background.transform.rotation = cameraTransform.rotation;

        float desiredHeight = camera.orthographicSize * 2f;
        float scale = desiredHeight / sprite.bounds.size.y;
        background.transform.localScale = Vector3.one * scale;
    }

    private static void CreateWalkableGround()
    {
        GameObject ground = new("Walkable Arena");
        ground.transform.position = Vector3.zero;
        BoxCollider collider = ground.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, -0.1f, 0f);
        collider.size = new Vector3(9.2f, 0.2f, 18f);
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.68f);
    }

    private static void CreateKnightAndSword()
    {
        GameObject knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
        GameObject knight = PrefabUtility.InstantiatePrefab(knightPrefab) as GameObject;
        knight.name = "Knight";
        knight.transform.position = new Vector3(0f, 0f, -5.5f);
        knight.transform.rotation = Quaternion.identity;

        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
        GameObject sword = PrefabUtility.InstantiatePrefab(swordPrefab) as GameObject;
        sword.name = "sword_2handed";
        sword.transform.position = knight.transform.position + Vector3.right;
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }
}
