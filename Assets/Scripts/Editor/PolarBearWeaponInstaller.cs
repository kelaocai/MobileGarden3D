using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
internal static class PolarBearWeaponInstaller
{
    private const string ScenePath = "Assets/Scenes/Level01_TowerDefense.unity";
    private const string StaffPath = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Models/Accessories/druid_staff.fbx";
    private const string StaffName = "Polar_DruidStaff";
    private const string SwingEffectPath = "Assets/Hovl Studio/Toon Projectiles 2/Prefabs/Flash 5.prefab";
    private const string ImpactEffectPath = "Assets/Hovl Studio/Toon Projectiles 2/Prefabs/Hit 5.prefab";

    static PolarBearWeaponInstaller() => EditorApplication.delayCall += Install;

    [MenuItem("Tools/Mobile Garden/Equip Polar Druid Staff")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject polarObject = GameObject.Find("Polar");
        Transform polar = polarObject != null ? polarObject.transform : null;
        if (polar == null)
        {
            Debug.LogError("无法配置 Polar：当前场景中没有名为 Polar 的节点。");
            return;
        }

        Transform rightHand = null;
        List<Transform> existingStaffs = new();
        foreach (Transform bone in polar.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == StaffName) existingStaffs.Add(bone);
            if (bone.name.Equals("Teddy R Hand", System.StringComparison.OrdinalIgnoreCase))
                rightHand = bone;
        }

        GameObject staffAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StaffPath);
        GameObject swingEffect = AssetDatabase.LoadAssetAtPath<GameObject>(SwingEffectPath);
        GameObject impactEffect = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactEffectPath);
        if (rightHand == null || staffAsset == null)
        {
            Debug.LogError("无法给 Polar 装备法杖：右手骨骼或 druid_staff.fbx 不存在。");
            return;
        }

        NavMeshAgent navAgent = polar.GetComponent<NavMeshAgent>();
        if (navAgent == null) navAgent = polar.gameObject.AddComponent<NavMeshAgent>();
        navAgent.speed = 1.2f;
        navAgent.angularSpeed = 360f;
        navAgent.acceleration = 4f;
        navAgent.stoppingDistance = 0.15f;
        navAgent.radius = 0.35f;
        navAgent.height = 1.2f;
        navAgent.autoBraking = true;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        PolarBearWander wander = polar.GetComponent<PolarBearWander>();
        if (wander == null) wander = polar.gameObject.AddComponent<PolarBearWander>();
        EditorUtility.SetDirty(navAgent);
        EditorUtility.SetDirty(wander);

        GameObject staff = existingStaffs.Count > 0
            ? existingStaffs[0].gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(staffAsset, rightHand);
        for (int i = 1; i < existingStaffs.Count; i++)
            Object.DestroyImmediate(existingStaffs[i].gameObject);
        staff.name = StaffName;
        staff.transform.SetParent(rightHand, false);
        staff.transform.localScale = Vector3.one * 0.42f;
        staff.transform.rotation = Quaternion.identity;
        staff.transform.position = rightHand.position;
        Renderer[] staffRenderers = staff.GetComponentsInChildren<Renderer>(true);
        if (staffRenderers.Length > 0)
        {
            Bounds bounds = staffRenderers[0].bounds;
            for (int i = 1; i < staffRenderers.Length; i++) bounds.Encapsulate(staffRenderers[i].bounds);
            Vector3 bottomCenter = new(bounds.center.x, bounds.min.y, bounds.center.z);
            staff.transform.position += rightHand.position - bottomCenter;
        }

        PolarBearCombatEffects effects = polar.GetComponent<PolarBearCombatEffects>();
        if (effects == null) effects = polar.gameObject.AddComponent<PolarBearCombatEffects>();
        effects.Configure(staff.transform, swingEffect, impactEffect);
        EditorUtility.SetDirty(effects);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Polar 已装备 druid_staff，法杖会跟随右手动画。");
    }
}
