using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KnightAttackEffects : MonoBehaviour
{
    [SerializeField, Min(0f)] private float swingDelay = 0.12f;
    [SerializeField, Min(0f)] private float impactDelay = 0.30f;
    [SerializeField, Min(0.1f)] private float effectLifetime = 3f;
    [SerializeField] private Vector3 impactOffset = new(0f, 0.65f, 1.05f);
    [SerializeField, Min(0.01f)] private float flashScale = 0.36f;
    [SerializeField, Min(0.01f)] private float hitScale = 0.42f;

    private GameObject flashPrefab;
    private GameObject hitPrefab;
    private Transform sword;
    private Coroutine sequence;

    private void Awake()
    {
        flashPrefab = Resources.Load<GameObject>("CombatFX/SwordFlash");
        hitPrefab = Resources.Load<GameObject>("CombatFX/SwordHit");
        FindSword();
    }

    public void PlayAttackEffects()
    {
        if (sequence != null) StopCoroutine(sequence);
        sequence = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        yield return new WaitForSeconds(swingDelay);
        FindSword();

        Vector3 flashPosition = sword != null
            ? sword.position + sword.up * 0.35f
            : transform.position + transform.forward * 0.65f + Vector3.up * 0.8f;
        Spawn(flashPrefab, flashPosition, transform.rotation, flashScale);

        yield return new WaitForSeconds(Mathf.Max(0f, impactDelay - swingDelay));
        Vector3 worldImpactOffset = transform.TransformDirection(impactOffset);
        Spawn(hitPrefab, transform.position + worldImpactOffset, transform.rotation, hitScale);
        sequence = null;
    }

    private void FindSword()
    {
        if (sword != null) return;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals("sword_2handed", System.StringComparison.OrdinalIgnoreCase))
            {
                sword = child;
                return;
            }
        }
    }

    private void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale)
    {
        if (prefab == null) return;
        GameObject instance = Instantiate(prefab, position, rotation);
        instance.transform.localScale *= scale;
        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particle.Play(true);
        }
        Destroy(instance, effectLifetime);
    }
}
