using UnityEngine;

public sealed class PolarBearCombatEffects : MonoBehaviour
{
    [SerializeField] private Transform staff;
    [SerializeField] private GameObject swingEffect;
    [SerializeField] private GameObject impactEffect;

    public void Configure(Transform equippedStaff, GameObject swing, GameObject impact)
    {
        staff = equippedStaff;
        swingEffect = swing;
        impactEffect = impact;
    }

    public void Play(FirePigEnemy target)
    {
        Vector3 staffTip = staff != null
            ? FindTop(staff)
            : transform.position + Vector3.up * 1.1f;
        TurretProjectile.SpawnEffect(swingEffect, staffTip, staff != null ? staff.rotation : transform.rotation);
        if (target != null)
            TurretProjectile.SpawnEffect(impactEffect, target.transform.position + Vector3.up * 0.5f, transform.rotation);
    }

    private static Vector3 FindTop(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return root.position + root.up;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}
