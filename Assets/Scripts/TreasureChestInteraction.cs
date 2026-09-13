using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class TreasureChestInteraction : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float interactionDistance = 0.75f;
    [SerializeField, Min(0.1f)] private float openingDuration = 0.65f;
    [SerializeField] private float openAngle = -110f;

    private Transform lid;
    private Quaternion closedRotation;
    private bool isOpen;
    private bool isOpening;
    private Bounds worldBounds;

    public bool IsOpen => isOpen || isOpening;

    public static void InstallAll()
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (!sceneTransform.name.Equals("ammo_crate_withLid", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (sceneTransform.GetComponent<TreasureChestInteraction>() == null)
            {
                sceneTransform.gameObject.AddComponent<TreasureChestInteraction>();
            }
        }
    }

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains("lid", System.StringComparison.OrdinalIgnoreCase) && child != transform)
            {
                lid = child;
                break;
            }
        }

        if (lid != null)
        {
            closedRotation = lid.localRotation;
        }

        EnsureCollisionAndNavigationObstacle();
    }

    private void EnsureCollisionAndNavigationObstacle()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds localBounds = CalculateLocalBounds(renderers);
        worldBounds = renderers[0].bounds;
        foreach (Renderer chestRenderer in renderers)
        {
            worldBounds.Encapsulate(chestRenderer.bounds);
        }

        if (GetComponent<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = localBounds.center;
            box.size = localBounds.size;
        }

        if (GetComponent<NavMeshObstacle>() == null)
        {
            NavMeshObstacle obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = localBounds.center;
            obstacle.size = new Vector3(
                localBounds.size.x + 0.3f,
                localBounds.size.y,
                localBounds.size.z + 0.3f);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.carvingTimeToStationary = 0f;
        }
    }

    public bool TryGetApproachPoint(Vector3 knightPosition, out Vector3 approachPoint)
    {
        Vector3 awayFromChest = knightPosition - transform.position;
        awayFromChest.y = 0f;
        if (awayFromChest.sqrMagnitude < 0.01f)
        {
            awayFromChest = -transform.forward;
        }

        Vector3 direction = awayFromChest.normalized;
        float boundsRadiusInDirection =
            Mathf.Abs(direction.x) * worldBounds.extents.x +
            Mathf.Abs(direction.z) * worldBounds.extents.z;
        Vector3 desiredPoint = worldBounds.center +
                               direction * (boundsRadiusInDirection + interactionDistance);
        desiredPoint.y = worldBounds.min.y;

        if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, 1.25f, NavMesh.AllAreas))
        {
            approachPoint = hit.position;
            return true;
        }

        approachPoint = default;
        return false;
    }

    public void Open()
    {
        if (!IsOpen && lid != null)
        {
            StartCoroutine(OpenRoutine());
        }
    }

    private IEnumerator OpenRoutine()
    {
        isOpening = true;
        Quaternion openRotation = closedRotation * Quaternion.Euler(openAngle, 0f, 0f);
        float elapsed = 0f;

        while (elapsed < openingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / openingDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            lid.localRotation = Quaternion.Slerp(closedRotation, openRotation, easedProgress);
            yield return null;
        }

        lid.localRotation = openRotation;
        isOpening = false;
        isOpen = true;
    }

    private Bounds CalculateLocalBounds(Renderer[] renderers)
    {
        Bounds localBounds = new(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (Renderer chestRenderer in renderers)
        {
            Bounds worldBounds = chestRenderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = worldBounds.center + Vector3.Scale(
                            worldBounds.extents, new Vector3(x, y, z));
                        localBounds.Encapsulate(transform.InverseTransformPoint(corner));
                    }
                }
            }
        }

        return localBounds;
    }
}
