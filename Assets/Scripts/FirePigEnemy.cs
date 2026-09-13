using System;
using UnityEngine;

public sealed class FirePigEnemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.15f;
    [SerializeField] private float maxHealth = 60f;
    [SerializeField] private float turnSpeed = 10f;

    private Transform[] waypoints;
    private int waypointIndex;
    private float health;
    private Animator animator;
    private Action<FirePigEnemy, bool> onFinished;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsAlive => health > 0f;

    public void Initialize(Transform[] path, float speed, float hitPoints, Action<FirePigEnemy, bool> finished)
    {
        waypoints = path;
        moveSpeed = speed;
        maxHealth = hitPoints;
        health = maxHealth;
        onFinished = finished;
        waypointIndex = 1;
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.CrossFade("Move", 0.08f);
        }
    }

    private void Update()
    {
        if (!IsAlive || waypoints == null || waypoints.Length < 2) return;
        if (waypointIndex >= waypoints.Length)
        {
            Finish(true);
            return;
        }

        Vector3 target = waypoints[waypointIndex].position;
        target.y = transform.position.y;
        Vector3 offset = target - transform.position;
        if (offset.sqrMagnitude <= 0.01f)
        {
            waypointIndex++;
            return;
        }

        Vector3 direction = offset.normalized;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        health = Mathf.Max(0f, health - amount);
        if (health > 0f)
        {
            if (animator != null) animator.CrossFade("Damage", 0.04f);
            return;
        }

        if (animator != null) animator.CrossFade("Die", 0.05f);
        enabled = false;
        onFinished?.Invoke(this, false);
        Destroy(gameObject, 1.5f);
    }

    private void Finish(bool reachedGoal)
    {
        health = 0f;
        enabled = false;
        onFinished?.Invoke(this, reachedGoal);
        Destroy(gameObject);
    }
}
