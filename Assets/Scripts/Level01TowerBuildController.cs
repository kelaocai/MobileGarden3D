using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private int startingCoins = 300;
    [SerializeField] private int buildCost = 100;
    [SerializeField] private int levelTwoCost = 80;
    [SerializeField] private int levelThreeCost = 150;
    [SerializeField] private float selectionRadius = 0.75f;
    [SerializeField] private float turretScale = 0.85f;

    private readonly HashSet<Transform> occupiedSlots = new();
    private readonly Dictionary<Transform, BasicDefenseTurret> towers = new();
    private Camera mainCamera;
    private TowerActionMenu actionMenu;

    public int Coins { get; private set; }

    public void Configure(TowerDefenseLevelLayout levelLayout, GameObject model, GameObject projectile, GameObject muzzleFlash, GameObject hitEffect)
    {
        layout = levelLayout;
        turretModel = model;
        projectilePrefab = projectile;
        muzzleFlashPrefab = muzzleFlash;
        hitEffectPrefab = hitEffect;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        Coins = startingCoins;
        actionMenu = GetComponent<TowerActionMenu>() ?? gameObject.AddComponent<TowerActionMenu>();
        actionMenu.Initialize(this);
    }

    private void Update()
    {
        if (layout == null || turretModel == null || mainCamera == null) return;

        if (!TryReadPointerDown(out Vector2 screenPosition)) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TrySelectTower(screenPosition)) return;
        actionMenu.Hide();
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
        if (Coins < buildCost) return;
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
        BasicDefenseTurret turret = tower.AddComponent<BasicDefenseTurret>();
        turret.ConfigureEffects(projectilePrefab, muzzleFlashPrefab, hitEffectPrefab);
        turret.InitializeBuild(nearest, buildCost);
        occupiedSlots.Add(nearest);
        towers[nearest] = turret;
        Coins -= buildCost;
    }

    private bool TrySelectTower(Vector2 screenPosition)
    {
        BasicDefenseTurret nearest = null;
        float best = selectionRadius * selectionRadius;
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane ground = new(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float distance)) return false;
        Vector3 point = ray.GetPoint(distance);
        foreach (BasicDefenseTurret turret in towers.Values)
        {
            if (turret == null) continue;
            float sqr = Vector2.SqrMagnitude(new Vector2(point.x - turret.transform.position.x, point.z - turret.transform.position.z));
            if (sqr <= best) { best = sqr; nearest = turret; }
        }
        if (nearest == null) return false;
        actionMenu.Show(nearest);
        return true;
    }

    public int GetUpgradeCost(BasicDefenseTurret turret) => turret.Level == 1 ? levelTwoCost : levelThreeCost;

    public bool TryUpgrade(BasicDefenseTurret turret)
    {
        if (turret == null || !turret.CanUpgrade) return false;
        int cost = GetUpgradeCost(turret);
        if (Coins < cost || !turret.Upgrade(cost)) return false;
        Coins -= cost;
        return true;
    }

    public void Sell(BasicDefenseTurret turret)
    {
        if (turret == null) return;
        Transform slot = turret.BuildSlot;
        Coins += Mathf.RoundToInt(turret.TotalInvestment * 0.6f);
        if (slot != null)
        {
            occupiedSlots.Remove(slot);
            towers.Remove(slot);
        }
        actionMenu.Hide();
        Destroy(turret.gameObject);
    }

    public void AddCoins(int amount) => Coins = Mathf.Max(0, Coins + amount);

}
