using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Level01FirePigInstaller
{
    private const string ScenePath = "Assets/Scenes/Level01_TowerDefense.unity";
    private const string PrefabPath = "Assets/Suriyun/Monster Pack Fire/Prefab/Fire Pig/Fire_Pig_B.prefab";
    private const string TurretPath = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Models/Accessories/turret_base.fbx";

    static Level01FirePigInstaller() => EditorApplication.delayCall += Install;

    [MenuItem("Tools/Mobile Garden/Install Fire Pig Wave 01")]
    private static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return;

        TowerDefenseLevelLayout layout = Object.FindFirstObjectByType<TowerDefenseLevelLayout>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject turret = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPath);
        if (layout == null || prefab == null || turret == null)
        {
            Debug.LogError("无法安装第一关火猪波次：缺少关卡布局或 Fire Pig B Prefab。");
            return;
        }

        Level01WaveController waves = Object.FindFirstObjectByType<Level01WaveController>();
        GameObject manager = waves != null ? waves.gameObject : new GameObject("Level01_WaveController");
        manager.transform.SetParent(layout.transform);
        if (waves == null) manager.AddComponent<Level01WaveController>().Configure(layout, prefab);
        Level01TowerBuildController builder = manager.GetComponent<Level01TowerBuildController>();
        if (builder == null) builder = manager.AddComponent<Level01TowerBuildController>();
        builder.Configure(layout, turret);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("第一关已安装：3 波火猪、3 座可点击建造的自动炮塔。按 Play 开始。");
    }
}
