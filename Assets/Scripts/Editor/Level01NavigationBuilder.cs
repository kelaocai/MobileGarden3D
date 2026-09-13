using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class Level01NavigationBuilder
{
    private const string ScenePath = "Assets/Scenes/Level01_TowerDefense.unity";
    private const string NavigationRootName = "Navigation_MainRoad";
    private const float RoadWidth = 1.65f;
    private const float RoadHeight = 0.08f;
    private const float RoadY = 0.01f;
    private const float SegmentOverlap = 0.18f;

    static Level01NavigationBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("Tools/Mobile Garden/Rebuild Level 01 Navigation")]
    private static void RebuildFromMenu()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorUtility.DisplayDialog(
                "Level 01 Navigation",
                "Open Level01_TowerDefense before rebuilding its navigation.",
                "OK");
            return;
        }

        GameObject existing = GameObject.Find(NavigationRootName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        BuildNavigation();
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath ||
            GameObject.Find(NavigationRootName) != null)
        {
            return;
        }

        BuildNavigation();
    }

    private static void BuildNavigation()
    {
        Transform waypointRoot = GameObject.Find("EnemyPath_Waypoints")?.transform;
        if (waypointRoot == null || waypointRoot.childCount < 2)
        {
            Debug.LogWarning("Level 01 navigation needs at least two EnemyPath_Waypoints.");
            return;
        }

        List<Transform> waypoints = new();
        foreach (Transform child in waypointRoot)
        {
            waypoints.Add(child);
        }
        waypoints.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        GameObject navigationRoot = new(NavigationRootName);
        GameObject roadRoot = new("WalkableRoad_Proxy");
        roadRoot.transform.SetParent(navigationRoot.transform, false);

        for (int index = 0; index < waypoints.Count; index++)
        {
            CreateJunction(roadRoot.transform, waypoints[index].position, index + 1);
            if (index > 0)
            {
                CreateSegment(
                    roadRoot.transform,
                    waypoints[index - 1].position,
                    waypoints[index].position,
                    index);
            }
        }

        NavMeshSurface surface = navigationRoot.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = Physics.AllLayers;
        surface.overrideTileSize = true;
        surface.tileSize = 64;
        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = navigationRoot;
        Debug.Log("Built Level 01 main-road navigation from 13 path waypoints.");
    }

    private static void CreateSegment(Transform parent, Vector3 start, Vector3 end, int index)
    {
        Vector3 flatStart = new(start.x, RoadY, start.z);
        Vector3 flatEnd = new(end.x, RoadY, end.z);
        Vector3 direction = flatEnd - flatStart;
        float length = direction.magnitude;
        if (length < 0.01f)
        {
            return;
        }

        GameObject segment = new($"RoadSegment_{index:00}");
        segment.transform.SetParent(parent, false);
        segment.transform.position = (flatStart + flatEnd) * 0.5f;
        segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        BoxCollider collider = segment.AddComponent<BoxCollider>();
        collider.size = new Vector3(RoadWidth, RoadHeight, length + SegmentOverlap);
    }

    private static void CreateJunction(Transform parent, Vector3 position, int index)
    {
        GameObject junction = new($"RoadJunction_{index:00}");
        junction.transform.SetParent(parent, false);
        junction.transform.position = new Vector3(position.x, RoadY, position.z);

        BoxCollider collider = junction.AddComponent<BoxCollider>();
        collider.size = new Vector3(RoadWidth, RoadHeight, RoadWidth);
    }
}
