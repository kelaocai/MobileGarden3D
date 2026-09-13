using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.AI.Navigation;

[DisallowMultipleComponent]
public sealed class KnightClickMover : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.2f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.08f;
    [SerializeField, Min(1f)] private float maximumTapMovement = 22f;
    [SerializeField, Min(0.05f)] private float maximumTapDuration = 0.5f;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int ActionId = Animator.StringToHash("Action");
    private static readonly int AttackId = Animator.StringToHash("Attack");

    private Animator animator;
    private NavMeshAgent agent;
    private Camera mainCamera;
    private Vector2 pressPosition;
    private float pressTime;
    private TreasureChestInteraction pendingChest;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnKnight()
    {
        TreasureChestInteraction.InstallAll();
        EnsureRuntimeNavMesh();

        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude);
        foreach (Animator candidate in animators)
        {
            if (!candidate.name.Equals("Knight", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidate.runtimeAnimatorController == null)
            {
                candidate.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("KnightLocomotion");
            }

            KnightEquipment.AttachSceneSword(candidate);

            if (candidate.GetComponent<KnightAttackEffects>() == null)
            {
                candidate.gameObject.AddComponent<KnightAttackEffects>();
            }

            if (candidate.GetComponent<KnightClickMover>() == null)
            {
                NavMeshAgent navAgent = candidate.GetComponent<NavMeshAgent>();
                if (navAgent == null)
                {
                    navAgent = candidate.gameObject.AddComponent<NavMeshAgent>();
                }

                ConfigureAgent(navAgent);
                candidate.gameObject.AddComponent<KnightClickMover>();
            }

            if (candidate.GetComponentInChildren<Collider>() == null)
            {
                CapsuleCollider capsule = candidate.gameObject.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 0.8f, 0f);
                capsule.height = 1.6f;
                capsule.radius = 0.32f;
            }
        }
    }

    private static void EnsureRuntimeNavMesh()
    {
        AddHouseCarvingObstacle();

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

    private static void AddHouseCarvingObstacle()
    {
        GameObject house = GameObject.Find("Baker_house");
        if (house == null || house.GetComponent<NavMeshObstacle>() != null)
        {
            return;
        }

        Renderer[] renderers = house.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds localBounds = new(house.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (Renderer houseRenderer in renderers)
        {
            Bounds worldBounds = houseRenderer.bounds;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        localBounds.Encapsulate(house.transform.InverseTransformPoint(corner));
                    }
                }
            }
        }

        NavMeshObstacle obstacle = house.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = localBounds.center;
        obstacle.size = new Vector3(localBounds.size.x + 0.6f, localBounds.size.y, localBounds.size.z + 0.6f);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.carvingTimeToStationary = 0f;
    }

    private static void ConfigureAgent(NavMeshAgent navAgent)
    {
        navAgent.speed = 3.2f;
        navAgent.angularSpeed = 720f;
        navAgent.acceleration = 18f;
        navAgent.stoppingDistance = 0.08f;
        navAgent.radius = 0.28f;
        navAgent.height = 1.5f;
        navAgent.autoBraking = true;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        ConfigureAgent(agent);
        mainCamera = Camera.main;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(navHit.position);
        }
    }

    private void Update()
    {
        ReadAttackInput();
        ReadDirectMovementInput();
        MoveKnight();
    }

    private void ReadDirectMovementInput()
    {
        Vector2 input = MobileMobaControls.MoveInput;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            Vector2 keyboardInput = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardInput.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardInput.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardInput.y += 1f;
            if (keyboardInput.sqrMagnitude > input.sqrMagnitude) input = keyboardInput.normalized;
        }

        if (input.sqrMagnitude < 0.01f || agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        pendingChest = null;
        if (agent.hasPath) agent.ResetPath();

        Vector3 direction = new(input.x, 0f, input.y);
        Vector3 displacement = direction.normalized * (moveSpeed * input.magnitude * Time.deltaTime);
        agent.Move(displacement);

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 14f * Time.deltaTime);
    }

    private void ReadAttackInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            Attack();
        }
    }

    public void Attack()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        pendingChest = null;
        animator.SetFloat(SpeedId, 0f);
        animator.SetTrigger(AttackId);
        GetComponent<KnightAttackEffects>()?.PlayAttackEffects();
    }

    private void ReadPointer()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                BeginPress(touch.position.ReadValue());
            }
            else if (touch.press.wasReleasedThisFrame)
            {
                EndPress(touch.position.ReadValue());
            }

            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginPress(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndPress(mouse.position.ReadValue());
        }
    }

    private void BeginPress(Vector2 screenPosition)
    {
        pressPosition = screenPosition;
        pressTime = Time.unscaledTime;
    }

    private void EndPress(Vector2 screenPosition)
    {
        if (Vector2.Distance(pressPosition, screenPosition) > maximumTapMovement ||
            Time.unscaledTime - pressTime > maximumTapDuration || mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Animator clickedAnimator = hit.collider.GetComponentInParent<Animator>();
        if (clickedAnimator == animator)
        {
            agent.ResetPath();
            animator.SetFloat(SpeedId, 0f);
            animator.SetTrigger(ActionId);
            return;
        }

        TreasureChestInteraction clickedChest = hit.collider.GetComponentInParent<TreasureChestInteraction>();
        if (clickedChest != null && !clickedChest.IsOpen)
        {
            if (clickedChest.TryGetApproachPoint(transform.position, out Vector3 approachPoint))
            {
                TrySetDestination(approachPoint);
                pendingChest = clickedChest;
            }

            return;
        }

        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit targetHit, 2f, NavMesh.AllAreas))
        {
            return;
        }

        pendingChest = null;
        TrySetDestination(targetHit.position);
    }

    private bool TrySetDestination(Vector3 targetPosition)
    {
        NavMeshPath path = new();
        if (!agent.CalculatePath(targetPosition, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        agent.SetPath(path);
        return true;
    }

    private void MoveKnight()
    {
        if (!agent.isOnNavMesh)
        {
            animator.SetFloat(SpeedId, 0f, 0.12f, Time.deltaTime);
            return;
        }

        bool arrived = !agent.pathPending && agent.hasPath &&
                       agent.remainingDistance <= Mathf.Max(stoppingDistance, agent.stoppingDistance);
        if (arrived)
        {
            agent.ResetPath();

            if (pendingChest != null)
            {
                Vector3 lookDirection = pendingChest.transform.position - transform.position;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                }

                pendingChest.Open();
                animator.SetTrigger(ActionId);
                pendingChest = null;
            }
        }

        float joystickSpeed = MobileMobaControls.MoveInput.magnitude;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.wKey.isPressed || keyboard.aKey.isPressed ||
            keyboard.sKey.isPressed || keyboard.dKey.isPressed || keyboard.upArrowKey.isPressed ||
            keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed))
        {
            joystickSpeed = 1f;
        }
        float normalizedSpeed = Mathf.Max(agent.velocity.magnitude / Mathf.Max(0.01f, moveSpeed), joystickSpeed);
        animator.SetFloat(SpeedId, normalizedSpeed, 0.08f, Time.deltaTime);
    }
}
