using UnityEngine;

public sealed class TowerDefenseLevelLayout : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Transform[] buildSlots;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform goalPoint;

    public Transform[] Waypoints => waypoints;
    public Transform[] BuildSlots => buildSlots;
    public Transform SpawnPoint => spawnPoint;
    public Transform GoalPoint => goalPoint;

    public void Configure(Transform[] path, Transform[] slots, Transform spawn, Transform goal)
    {
        waypoints = path;
        buildSlots = slots;
        spawnPoint = spawn;
        goalPoint = goal;
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.9f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position + Vector3.up * 0.15f, 0.13f);
                if (i > 0 && waypoints[i - 1] != null)
                    Gizmos.DrawLine(waypoints[i - 1].position + Vector3.up * 0.15f, waypoints[i].position + Vector3.up * 0.15f);
            }
        }

        if (buildSlots != null)
        {
            Gizmos.color = new Color(1f, 0.72f, 0.1f, 0.75f);
            foreach (Transform slot in buildSlots)
                if (slot != null) Gizmos.DrawWireSphere(slot.position + Vector3.up * 0.1f, 0.55f);
        }
    }
}
