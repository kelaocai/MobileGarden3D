using UnityEngine;

public sealed class BasicDefenseTurret : MonoBehaviour
{
    [SerializeField] private float attackRange = 3.0f;
    [SerializeField] private float damage = 18f;
    [SerializeField] private float shotsPerSecond = 1.5f;
    [SerializeField] private float rotationSpeed = 300f;
    [SerializeField] private float projectileSpeed = 7f;

    private GameObject projectilePrefab;
    private GameObject muzzleFlashPrefab;
    private GameObject hitEffectPrefab;

    private Transform gun;
    private FirePigEnemy target;
    private float nextTargetSearch;
    private float nextShot;

    public void ConfigureEffects(GameObject projectile, GameObject muzzleFlash, GameObject hitEffect)
    {
        projectilePrefab = projectile;
        muzzleFlashPrefab = muzzleFlash;
        hitEffectPrefab = hitEffect;
    }

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals("turret_gun", System.StringComparison.OrdinalIgnoreCase))
            {
                gun = child;
                break;
            }
        }
        if (gun == null) gun = transform;
    }

    private void Update()
    {
        if (Time.time >= nextTargetSearch)
        {
            nextTargetSearch = Time.time + 0.18f;
            FindTarget();
        }
        if (target == null || !target.IsAlive) return;

        Vector3 toTarget = target.transform.position - gun.position;
        Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion desired = Quaternion.LookRotation(flatDirection, Vector3.up);
            gun.rotation = Quaternion.RotateTowards(gun.rotation, desired, rotationSpeed * Time.deltaTime);
        }

        if (toTarget.sqrMagnitude <= attackRange * attackRange && Time.time >= nextShot)
        {
            nextShot = Time.time + 1f / shotsPerSecond;
            Fire();
        }
    }

    private void Fire()
    {
        Vector3 muzzlePosition = gun.position + gun.forward * 0.38f + Vector3.up * 0.08f;
        TurretProjectile.SpawnEffect(muzzleFlashPrefab, muzzlePosition, gun.rotation);
        if (projectilePrefab == null)
        {
            target.TakeDamage(damage);
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, muzzlePosition, gun.rotation);
        TurretProjectile controller = projectile.GetComponent<TurretProjectile>() ?? projectile.AddComponent<TurretProjectile>();
        controller.Initialize(target, damage, projectileSpeed, hitEffectPrefab);
    }

    private void FindTarget()
    {
        target = null;
        float nearestDistance = attackRange * attackRange;
        foreach (FirePigEnemy enemy in FindObjectsByType<FirePigEnemy>(FindObjectsInactive.Exclude))
        {
            if (!enemy.IsAlive) continue;
            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                target = enemy;
            }
        }
    }
}
