using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class TurretTracker : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float detectionRadius = 6f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;
    [SerializeField] private float minimumPitch = -10f;
    [SerializeField] private float maximumPitch = 35f;
    [SerializeField, Min(0f)] private float targetHeight = 0.8f;

    private Transform gun;
    private Transform knight;
    private Quaternion restingWorldRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnTurrets()
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (!sceneTransform.name.Equals("turret_base", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (sceneTransform.GetComponent<TurretTracker>() == null)
            {
                sceneTransform.gameObject.AddComponent<TurretTracker>();
            }
        }
    }

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals("turret_gun", System.StringComparison.OrdinalIgnoreCase))
            {
                gun = child;
                restingWorldRotation = gun.rotation;
                break;
            }
        }

        EnsureCollisionAndNavigationObstacle();
        FindKnight();
    }

    private void LateUpdate()
    {
        if (gun == null)
        {
            return;
        }

        if (knight == null)
        {
            FindKnight();
        }

        Quaternion desiredRotation = restingWorldRotation;
        if (knight != null)
        {
            Vector3 targetPoint = knight.position + Vector3.up * targetHeight;
            Vector3 toTarget = targetPoint - gun.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(toTarget, Vector3.up);

            if (horizontal.sqrMagnitude <= detectionRadius * detectionRadius && horizontal.sqrMagnitude > 0.001f)
            {
                float pitch = Mathf.Clamp(
                    Mathf.Atan2(toTarget.y, horizontal.magnitude) * Mathf.Rad2Deg,
                    minimumPitch,
                    maximumPitch);
                Vector3 aimDirection = horizontal.normalized * Mathf.Cos(pitch * Mathf.Deg2Rad) +
                                       Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad);
                desiredRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }
        }

        gun.rotation = Quaternion.RotateTowards(
            gun.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void FindKnight()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude);
        foreach (Animator candidate in animators)
        {
            if (candidate.name.Equals("Knight", System.StringComparison.OrdinalIgnoreCase))
            {
                knight = candidate.transform;
                return;
            }
        }
    }

    private void EnsureCollisionAndNavigationObstacle()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds localBounds = CalculateLocalBounds(renderers);
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
            obstacle.size = new Vector3(localBounds.size.x + 0.25f, localBounds.size.y, localBounds.size.z + 0.25f);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.carvingTimeToStationary = 0f;
        }
    }

    private Bounds CalculateLocalBounds(Renderer[] renderers)
    {
        Bounds localBounds = new(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (Renderer turretRenderer in renderers)
        {
            Bounds worldBounds = turretRenderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                        localBounds.Encapsulate(transform.InverseTransformPoint(corner));
                    }
                }
            }
        }

        return localBounds;
    }
}
