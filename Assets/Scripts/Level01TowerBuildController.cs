using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class Level01TowerBuildController : MonoBehaviour
{
    [SerializeField] private TowerDefenseLevelLayout layout;
    [SerializeField] private GameObject turretModel;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private int availableTurrets = 3;
    [SerializeField] private float selectionRadius = 0.75f;
    [SerializeField] private float turretScale = 0.85f;

    private readonly HashSet<Transform> occupiedSlots = new();
    private Camera mainCamera;

    public void Configure(TowerDefenseLevelLayout levelLayout, GameObject model, GameObject projectile, GameObject muzzleFlash, GameObject hitEffect)
    {
        layout = levelLayout;
        turretModel = model;
        projectilePrefab = projectile;
        muzzleFlashPrefab = muzzleFlash;
        hitEffectPrefab = hitEffect;
    }

    private void Awake() => mainCamera = Camera.main;

    private void Update()
    {
        if (availableTurrets <= 0 || layout == null || turretModel == null || mainCamera == null) return;

        if (!TryReadPointerDown(out Vector2 screenPosition)) return;

        TryBuild(screenPosition);
    }

    private static bool TryReadPointerDown(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPosition = Input.GetTouch(0).position;
            return true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
#endif
        screenPosition = default;
        return false;
    }

    private void TryBuild(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane ground = new(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float distance)) return;
        Vector3 worldPoint = ray.GetPoint(distance);

        Transform nearest = null;
        float bestDistance = selectionRadius * selectionRadius;
        foreach (Transform slot in layout.BuildSlots)
        {
            if (slot == null || occupiedSlots.Contains(slot)) continue;
            float sqrDistance = Vector2.SqrMagnitude(new Vector2(worldPoint.x - slot.position.x, worldPoint.z - slot.position.z));
            if (sqrDistance <= bestDistance)
            {
                bestDistance = sqrDistance;
                nearest = slot;
            }
        }

        if (nearest == null) return;
        GameObject tower = Instantiate(turretModel, nearest.position, Quaternion.identity, transform);
        tower.name = $"Defense Turret · {nearest.name}";
        tower.transform.localScale = Vector3.one * turretScale;
        tower.AddComponent<BasicDefenseTurret>().ConfigureEffects(projectilePrefab, muzzleFlashPrefab, hitEffectPrefab);
        occupiedSlots.Add(nearest);
        availableTurrets--;
    }

    private void OnGUI()
    {
        GUIStyle style = new(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.021f), alignment = TextAnchor.MiddleLeft };
        GUI.Box(new Rect(16f, 70f, Mathf.Min(360f, Screen.width * 0.58f), 44f), $"点击黄色建造点放置炮塔  剩余 {availableTurrets}", style);
    }
}
