using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class PolarBearWander : MonoBehaviour
{
    [Header("Wandering")]
    [SerializeField, Min(0.5f)] private float wanderRadius = 8f;
    [SerializeField, Min(0f)] private float minimumIdleTime = 2f;
    [SerializeField, Min(0f)] private float maximumIdleTime = 5f;
    [SerializeField, Min(0.1f)] private float destinationSearchRadius = 3f;
    [SerializeField, Min(1)] private int destinationAttempts = 12;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.2f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.15f;

    [Header("Defense")]
    [SerializeField, Min(1f)] private float detectionRadius = 4.2f;
    [SerializeField, Min(0.2f)] private float attackRange = 0.75f;
    [SerializeField, Min(1f)] private float attackDamage = 24f;
    [SerializeField, Min(0.2f)] private float attackCooldown = 1.15f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 2.25f;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int[] IdleStates =
    {
        Animator.StringToHash("Base Layer.idle1"),
        Animator.StringToHash("Base Layer.idle2"),
        Animator.StringToHash("Base Layer.idle3")
    };

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 homePosition;
    private FirePigEnemy combatTarget;
    private float nextTargetSearch;
    private float nextAttack;
    private bool performingAttack;
    private PolarBearCombatEffects combatEffects;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InstallOnScenePolars()
    {
        EnsureNavMesh();

        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude);
        foreach (Animator candidate in animators)
        {
            if (!candidate.gameObject.name.Equals("Polar", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            RuntimeAnimatorController locomotionController =
                Resources.Load<RuntimeAnimatorController>("PolarBearLocomotion");
            if (locomotionController != null)
            {
                candidate.runtimeAnimatorController = locomotionController;
            }

            NavMeshAgent navAgent = candidate.GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = candidate.gameObject.AddComponent<NavMeshAgent>();
            }

            ConfigureAgent(navAgent);

            if (candidate.GetComponent<PolarBearWander>() == null)
            {
                candidate.gameObject.AddComponent<PolarBearWander>();
            }
        }
    }

    private static void EnsureNavMesh()
    {
        NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject surfaceObject = new("Runtime NavMesh Surface");
            surface = surfaceObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = Physics.DefaultRaycastLayers;
            surface.overrideTileSize = true;
            surface.tileSize = 128;
        }

        if (surface.navMeshData == null)
        {
            surface.BuildNavMesh();
        }
    }

    private static void ConfigureAgent(NavMeshAgent navAgent)
    {
        navAgent.speed = 1.2f;
        navAgent.angularSpeed = 360f;
        navAgent.acceleration = 4f;
        navAgent.stoppingDistance = 0.15f;
        navAgent.radius = 0.35f;
        navAgent.height = 1.2f;
        navAgent.autoBraking = true;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        combatEffects = GetComponent<PolarBearCombatEffects>();
        combatTarget = null;
        performingAttack = false;
        nextAttack = 0f;
        nextTargetSearch = 0f;
        RuntimeAnimatorController locomotionController =
            Resources.Load<RuntimeAnimatorController>("PolarBearLocomotion");
        if (animator != null && locomotionController != null)
        {
            animator.runtimeAnimatorController = locomotionController;
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.Update(0f);
            animator.Play("Base Layer.idle1", 0, 0f);
        }
        ConfigureAgent(agent);
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (!TryPlaceOnNavMesh())
        {
            Debug.LogWarning($"{name} could not find a nearby NavMesh and cannot wander.", this);
            enabled = false;
            yield break;
        }

        homePosition = transform.position;
        PlayRandomIdle();

        while (enabled)
        {
            yield return new WaitForSeconds(Random.Range(minimumIdleTime, maximumIdleTime));

            if (combatTarget != null) continue;

            if (!TryChooseDestination(out Vector3 destination))
            {
                yield return null;
                continue;
            }

            agent.SetDestination(destination);
            PlayWalk();

            while (enabled && agent.isOnNavMesh &&
                   (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
            {
                if (combatTarget != null) break;
                if (!agent.pathPending &&
                    (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete))
                {
                    break;
                }

                PlayWalk();
                yield return null;
            }

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            PlayRandomIdle();
        }
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (Time.time >= nextTargetSearch)
        {
            nextTargetSearch = Time.time + 0.2f;
            FindCombatTarget();
        }
        if (combatTarget == null || performingAttack) return;
        agent.speed = chaseSpeed;

        Vector3 offset = combatTarget.transform.position - transform.position;
        float distance = Vector3.ProjectOnPlane(offset, Vector3.up).magnitude;
        if (distance > attackRange)
        {
            if (NavMesh.SamplePosition(combatTarget.transform.position, out NavMeshHit hit, 1.2f, NavMesh.AllAreas))
            {
                agent.stoppingDistance = attackRange * 0.8f;
                agent.SetDestination(hit.position);
                PlayWalk();
            }
            return;
        }

        agent.ResetPath();
        Vector3 look = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 12f * Time.deltaTime);
        if (Time.time >= nextAttack) StartCoroutine(PerformAttack());
    }

    private void FindCombatTarget()
    {
        if (combatTarget != null && combatTarget.IsAlive &&
            (combatTarget.transform.position - transform.position).sqrMagnitude <= detectionRadius * detectionRadius)
            return;

        combatTarget = null;
        float bestDistance = detectionRadius * detectionRadius;
        foreach (FirePigEnemy enemy in FindObjectsByType<FirePigEnemy>(FindObjectsInactive.Exclude))
        {
            if (!enemy.IsAlive) continue;
            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                combatTarget = enemy;
            }
        }

        if (combatTarget == null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = stoppingDistance;
        }
    }

    private IEnumerator PerformAttack()
    {
        performingAttack = true;
        nextAttack = Time.time + attackCooldown;
        if (animator != null) animator.CrossFadeInFixedTime("Base Layer.attack", 0.08f);

        Vector3 baseScale = transform.localScale;
        const float duration = 0.38f;
        float elapsed = 0f;
        bool dealtDamage = false;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + punch * 0.06f, 1f - punch * 0.04f, 1f + punch * 0.1f));
            if (!dealtDamage && t >= 0.42f)
            {
                dealtDamage = true;
                if (combatTarget != null && combatTarget.IsAlive &&
                    (combatTarget.transform.position - transform.position).sqrMagnitude <= attackRange * attackRange * 1.5f)
                {
                    combatEffects?.Play(combatTarget);
                    combatTarget.TakeDamage(attackDamage, 1.55f);
                }
            }
            yield return null;
        }

        transform.localScale = baseScale;
        performingAttack = false;
        if (animator != null)
            animator.CrossFadeInFixedTime(combatTarget != null && combatTarget.IsAlive ? "Base Layer.walk" : "Base Layer.idle1", 0.1f);
    }

    private bool TryPlaceOnNavMesh()
    {
        if (agent.isOnNavMesh)
        {
            return true;
        }

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            return false;
        }

        return agent.Warp(hit.position);
    }

    private bool TryChooseDestination(out Vector3 destination)
    {
        NavMeshPath path = new();
        for (int attempt = 0; attempt < destinationAttempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSearchRadius, NavMesh.AllAreas))
            {
                continue;
            }

            if ((hit.position - transform.position).sqrMagnitude < 1f)
            {
                continue;
            }

            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                destination = hit.position;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }

    private void PlayWalk()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(SpeedParameter, Mathf.Max(0.2f, agent.velocity.magnitude));
    }

    private void PlayRandomIdle()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(SpeedParameter, 0f);

        int firstChoice = Random.Range(0, IdleStates.Length);
        for (int offset = 0; offset < IdleStates.Length; offset++)
        {
            int choice = (firstChoice + offset) % IdleStates.Length;
            if (!animator.HasState(0, IdleStates[choice]))
            {
                continue;
            }

            animator.CrossFadeInFixedTime(IdleStates[choice], 0.2f);
            return;
        }
    }

    private void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }
}
