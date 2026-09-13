using UnityEngine;

public sealed class TurretProjectile : MonoBehaviour
{
    private FirePigEnemy target;
    private GameObject hitEffect;
    private float damage;
    private float speed;

    public void Initialize(FirePigEnemy enemy, float projectileDamage, float projectileSpeed, GameObject impactEffect)
    {
        target = enemy;
        damage = projectileDamage;
        speed = projectileSpeed;
        hitEffect = impactEffect;

        ProjectileMover originalMover = GetComponent<ProjectileMover>();
        if (originalMover != null) originalMover.enabled = false;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        Destroy(gameObject, 4f);
    }

    private void Update()
    {
        if (target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 destination = target.transform.position + Vector3.up * 0.55f;
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude <= 0.025f)
        {
            target.TakeDamage(damage);
            SpawnEffect(hitEffect, destination, transform.rotation);
            Destroy(gameObject);
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    }

    public static void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, position, rotation);
        float lifetime = 2f;
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }
        Destroy(effect, lifetime + 0.2f);
    }
}
