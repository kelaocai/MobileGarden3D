using System.Collections;
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
    private Coroutine hitReaction;
    private bool isReacting;
    private Vector3 normalScale;
    private bool isDying;
    private float hitReactionStrength = 1f;

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
        normalScale = transform.localScale;
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.CrossFade("Move", 0.08f);
        }
        FirePigHealthBar healthBar = GetComponent<FirePigHealthBar>() ?? gameObject.AddComponent<FirePigHealthBar>();
        healthBar.Initialize(this);
    }

    private void Update()
    {
        if (!IsAlive || isDying || isReacting || waypoints == null || waypoints.Length < 2) return;
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

    public void TakeDamage(float amount, float reactionStrength = 1f)
    {
        if (!IsAlive || amount <= 0f) return;
        health = Mathf.Max(0f, health - amount);
        hitReactionStrength = Mathf.Clamp(reactionStrength, 0.6f, 2f);
        if (health > 0f)
        {
            if (hitReaction != null) StopCoroutine(hitReaction);
            hitReaction = StartCoroutine(PlayHitReaction());
            return;
        }

        if (hitReaction != null) StopCoroutine(hitReaction);
        isReacting = false;
        transform.localScale = normalScale;
        isDying = true;
        onFinished?.Invoke(this, false);
        onFinished = null;
        StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayHitReaction()
    {
        isReacting = true;
        if (animator != null) animator.CrossFade("Damage", 0.035f);

        const float duration = 0.24f;
        float recoilDistance = 0.16f * hitReactionStrength;
        float hopHeight = 0.13f * hitReactionStrength;
        Vector3 start = transform.position;
        Vector3 end = start - transform.forward * recoilDistance;
        float elapsed = 0f;

        while (elapsed < duration && IsAlive)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += punch * hopHeight;
            transform.position = position;
            transform.localScale = Vector3.Scale(normalScale, new Vector3(1f + punch * 0.08f, 1f - punch * 0.07f, 1f + punch * 0.08f));
            yield return null;
        }

        transform.localScale = normalScale;
        isReacting = false;
        hitReaction = null;
        if (IsAlive && animator != null) animator.CrossFade("Move", 0.06f);
    }

    private IEnumerator PlayDeathSequence()
    {
        if (animator != null) animator.CrossFade("Die", 0.04f);

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 backward = -transform.forward * 0.12f;
        const float duration = 1.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float hop = Mathf.Sin(Mathf.Clamp01(t / 0.28f) * Mathf.PI) * 0.12f * (1f - t);
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.12f) / 0.58f));
            float sink = Mathf.SmoothStep(0f, 0.22f, Mathf.Clamp01((t - 0.7f) / 0.3f));
            float shrink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.62f) / 0.38f));

            transform.position = startPosition + backward * fall + Vector3.up * (hop - sink);
            transform.rotation = startRotation * Quaternion.Euler(-72f * fall, 0f, 8f * Mathf.Sin(t * Mathf.PI));
            transform.localScale = normalScale * Mathf.Max(0.04f, shrink);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Finish(bool reachedGoal)
    {
        health = 0f;
        enabled = false;
        onFinished?.Invoke(this, reachedGoal);
        Destroy(gameObject);
    }
}
