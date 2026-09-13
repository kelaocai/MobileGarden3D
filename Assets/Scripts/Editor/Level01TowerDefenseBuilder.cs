using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Level01TowerDefenseBuilder
{
    private const string ScenePath = "Assets/Scenes/Level01_TowerDefense.unity";
    private const string MapPath = "Assets/Art/Maps/zaogao_level_01_bakery_garden_gate_v2.png";
    private const string MaterialPath = "Assets/Art/Maps/Level01_Map_Unlit.mat";

    static Level01TowerDefenseBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    private static void BuildIfNeeded()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;
        if (GameObject.Find("Level01_Configured") != null)
        {
            GameObject existingGround = GameObject.Find("VisualGround_Level01");
            if (existingGround != null && existingGround.transform.rotation != Quaternion.Euler(90f, 0f, 0f))
            {
                existingGround.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Corrected Level01 map plane facing direction.");
            }
            return;
        }

        ConfigureMapTexture();
        foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);

        GameObject marker = new("Level01_Configured");
        marker.hideFlags = HideFlags.HideInHierarchy;

        GameObject environment = new("Environment");
        CreateVisualGround(environment.transform);

        GameObject gameplay = new("Gameplay");
        Transform spawn = CreateMarker("SpawnPoint_Enemy", gameplay.transform, new Vector3(0f, 0.08f, -8.8f));
        Transform goal = CreateMarker("GoalPoint_Bakery", gameplay.transform, new Vector3(0.75f, 0.08f, 8.25f));
        Transform[] path = CreatePath(gameplay.transform);
        Transform[] slots = CreateBuildSlots(gameplay.transform);
        gameplay.AddComponent<TowerDefenseLevelLayout>().Configure(path, slots, spawn, goal);

        new GameObject("Units");
        new GameObject("Towers");
        new GameObject("Effects");
        CreateCamera();
        CreateLight();
        new GameObject("UI");

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured Level01 tower-defense scene with map, path and six build build slots.");
    }

    private static void ConfigureMapTexture()
    {
        AssetDatabase.ImportAsset(MapPath, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(MapPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static void CreateVisualGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ground.name = "VisualGround_Level01";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ground.transform.localScale = new Vector3(10f, 20.34f, 1f);

        Object.DestroyImmediate(ground.GetComponent<MeshCollider>());
        BoxCollider collider = ground.AddComponent<BoxCollider>();
        collider.size = new Vector3(1f, 1f, 0.01f);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            material = new Material(shader) { name = "Level01 Map Unlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        ground.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Transform[] CreatePath(Transform parent)
    {
        GameObject root = new("EnemyPath_Waypoints");
        root.transform.SetParent(parent);
        Vector3[] positions =
        {
            new(0f, .08f, -8.8f), new(0f, .08f, -7.3f), new(.35f, .08f, -5.9f),
            new(1.05f, .08f, -4.5f), new(1.35f, .08f, -3.2f), new(.9f, .08f, -1.8f),
            new(.05f, .08f, -.45f), new(-.55f, .08f, .85f), new(-.95f, .08f, 2.05f),
            new(-1.55f, .08f, 3.55f), new(-1.25f, .08f, 5.1f), new(-.35f, .08f, 6.55f),
            new(.75f, .08f, 8.25f)
        };
        List<Transform> result = new();
        for (int i = 0; i < positions.Length; i++)
            result.Add(CreateMarker($"Waypoint_{i + 1:00}", root.transform, positions[i]));
        return result.ToArray();
    }

    private static Transform[] CreateBuildSlots(Transform parent)
    {
        GameObject root = new("BuildSlots");
        root.transform.SetParent(parent);
        Vector3[] positions =
        {
            new(-1.50f, .09f, 5.40f), new(2.10f, .09f, 4.30f),
            new(-1.75f, .09f, -.90f), new(1.90f, .09f, -.45f),
            new(-1.40f, .09f, -3.45f), new(3.25f, .09f, -3.55f)
        };
        Transform[] result = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
            result[i] = CreateMarker($"BuildSlot_{i + 1:00}", root.transform, positions[i]);
        return result;
    }

    private static Transform CreateMarker(string name, Transform parent, Vector3 position)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9.1f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.27f, .58f, .83f);
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position = new Vector3(0f, 18f, -10f);
        cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.zero - cameraObject.transform.position, Vector3.up);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<LockedArenaCamera>();
    }

    private static void CreateLight()
    {
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }
}
