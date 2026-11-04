
using System.Collections.Generic;
using UnityEngine;

public class HunterAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float maxAcceleration = 8f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Navigation")]
    [SerializeField] private NavigationGraph navigationGraph;
    [SerializeField] private Renderer startZoneRenderer;
    [SerializeField] private float pathNodeReachDistance = 0.1f;

    [Header("Perception")]
    [SerializeField] private float perceptionRadius = 12f;
    [SerializeField] private HunterPerception perception;

    [Header("Attack")]
    [SerializeField] private float tba = 3f;
    [SerializeField] private float rangeAttackRadius = 8f;
    [SerializeField] private float meleeAttackRadius = 2f;

    [Header("Damage")]
    [SerializeField] private float meleeDamage = 50f;
    [SerializeField] private float rangeDamage = 25f;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Header("Objective - Parcial 2")]
    [SerializeField, Min(1)] private int requiredGathers = 3;
    [SerializeField] private Renderer exitZoneRenderer;

    [Header("Gather")]
    [SerializeField] private float gatherRadius = 1.5f;
    [SerializeField] private float gatherDuration = 2f;

    [Header("Interest Objects")]
    [SerializeField] private InterestObject interestObjectPrefab;
    [SerializeField] private float interestSpawnInterval = 5f;
    [SerializeField] private int maxActiveInterestObjects = 5;
    [SerializeField] private float interestSpawnRadius = 10f;

    [Header("Interest Object Spawn Bounds")]
    [SerializeField] private Renderer huntingGroundRenderer;
    [SerializeField] private Transform obstaclesRoot;
    [SerializeField] private float interestSpawnHeight = 1f;
    [SerializeField] private float spawnBoundsPadding = 1f;
    [SerializeField] private float spawnObstacleClearance = 1f;
    [SerializeField] private int maxSpawnAttempts = 10;

    private Collider[] obstacleColliders;
    private StateMachine stateMachine;
    private float attackCooldownTimer;
    private Vector3 currentVelocity;
    private BoidAgent currentTarget;
    private int gatherCount;

    public float MaxSpeed => maxSpeed;
    public Vector3 CurrentVelocity => currentVelocity;

    public NavigationGraph NavigationGraph => navigationGraph;
    public Renderer HuntingGroundRenderer => huntingGroundRenderer;
    public float PathNodeReachDistance => pathNodeReachDistance;

    public float PerceptionRadius => perceptionRadius;
    public HunterPerception Perception => perception;

    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;

    public float MeleeDamage => meleeDamage;
    public float RangeDamage => rangeDamage;

    public HunterState CurrentState =>
        stateMachine?.CurrentState as HunterState;

    public Transform[] Waypoints => waypoints;
    public float WaypointReachDistance => waypointReachDistance;

    public InterestObject InterestObjectPrefab => interestObjectPrefab;
    public float InterestSpawnInterval => interestSpawnInterval;
    public int MaxActiveInterestObjects => maxActiveInterestObjects;
    public float InterestSpawnRadius => interestSpawnRadius;

    public float AttackCooldownTimer => attackCooldownTimer;
    public bool IsAttackReady => attackCooldownTimer >= tba;
    public BoidAgent CurrentTarget => currentTarget;

    public float GatherRadius => gatherRadius;
    public float GatherDuration => gatherDuration;

    public int RequiredGathers => requiredGathers;
    public int GatherCount => gatherCount;
    public bool HasRequiredGathers => gatherCount >= requiredGathers;
    public Renderer ExitZoneRenderer => exitZoneRenderer;

    public SearchState SearchState { get; private set; }
    public AttackState AttackState { get; private set; }
    public GatherState GatherState { get; private set; }
    public ExitState ExitState { get; private set; }

    public HunterFleeContext FleeContext { get; private set; }
    public FleeState FleeState { get; private set; }

    // Compatibilidad con los scripts anteriores.
    public HunterState PatrolState => SearchState;

    public string CurrentStateName =>
        CurrentState != null
            ? CurrentState.GetType().Name
            : "None";

    public string CurrentTargetName =>
        currentTarget != null
            ? currentTarget.name
            : "None";

    private void Awake()
    {
        stateMachine = new StateMachine();

        SearchState = new SearchState(this);
        AttackState = new AttackState(this);
        GatherState = new GatherState(this);
        ExitState = new ExitState(this);

        FleeContext = GetComponent<HunterFleeContext>();
        FleeState = new FleeState(
            this,
            SearchState,
            FleeContext
        );

        attackCooldownTimer = tba;
        gatherCount = 0;

        CacheObstacleColliders();
    }

    private void Start()
    {
        if (navigationGraph == null ||
            !navigationGraph.IsReady ||
            startZoneRenderer == null ||
            huntingGroundRenderer == null ||
            exitZoneRenderer == null ||
            perception == null)
        {
            Debug.LogError(
                $"{name}: Missing Hunter navigation or " +
                "perception references.",
                this
            );

            enabled = false;
            return;
        }

        if (FleeContext == null || !FleeContext.IsReady)
        {
            Debug.LogError(
                $"{name}: HunterFleeContext is missing " +
                "or has unassigned references.",
                this
            );

            enabled = false;
            return;
        }

        Vector3 startPosition =
            startZoneRenderer.bounds.center;

        startPosition.y = transform.position.y;

        transform.position = startPosition;

        StopMovement();
        ChangeState(SearchState);
    }

    private void Update()
    {
        UpdateAttackCooldown();
        stateMachine?.Update();
    }

    public void ChangeState(HunterState newState)
    {
        if (newState == null) return;
        stateMachine?.ChangeState(newState);
    }

    // Invocado desde los estados de la FSM.
    // Flee tiene prioridad sobre Search, Attack y Gather.
    public bool TryEnterFlee()
    {
        if (CurrentState == FleeState ||
            FleeContext == null ||
            !FleeContext.CanSeeWolf())
        {
            return false;
        }

        StopMovement();
        ClearTarget();
        ChangeState(FleeState);

        return true;
    }

    public void RegisterSuccessfulGather()
    {
        gatherCount++;
        Debug.Log($"[Hunter Objective] {gatherCount}/{requiredGathers} Boids collected.");
    }

    public bool IsInsideExitZone(Vector3 position)
    {
        if (exitZoneRenderer == null) return false;

        Bounds bounds = exitZoneRenderer.bounds;
        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    // Called by ExitState after reaching the Exit zone.
    public void CompleteEscapeAndRestart()
    {
        if (CurrentState != ExitState) return;

        StopMovement();
        ClearTarget();
        gatherCount = 0;

        // Instant respawn at Start, restarting Search via the FSM.
        Vector3 startPosition = startZoneRenderer.bounds.center;
        startPosition.y = transform.position.y;
        transform.position = startPosition;

        Debug.Log("[Hunter Exit] Escaped! Respawned at Start. Gathers: 0.");
        ChangeState(SearchState);
    }

    public bool IsInsideHuntingGround(Vector3 position)
    {
        if (huntingGroundRenderer == null)
        {
            return false;
        }

        Bounds bounds = huntingGroundRenderer.bounds;

        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    // Movimiento por los nodos de A*.
    public void MoveAlongPath(Vector3 targetPosition)
    {
        Vector3 target = new Vector3(
            targetPosition.x,
            transform.position.y,
            targetPosition.z
        );

        Vector3 previousPosition = transform.position;

        Vector3 nextPosition = Vector3.MoveTowards(
            previousPosition,
            target,
            maxSpeed * Time.deltaTime
        );

        transform.position = nextPosition;

        if (Time.deltaTime > 0f)
        {
            currentVelocity =
                (nextPosition - previousPosition) /
                Time.deltaTime;
        }
        else
        {
            currentVelocity = Vector3.zero;
        }

        RotateTowardsVelocity();
    }

    // Steering del Parcial 1, conservado.
    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 desiredVelocity =
            direction.normalized * maxSpeed;

        Vector3 steering =
            desiredVelocity - currentVelocity;

        steering = Vector3.ClampMagnitude(
            steering,
            maxAcceleration
        );

        currentVelocity += steering * Time.deltaTime;

        currentVelocity = Vector3.ClampMagnitude(
            currentVelocity,
            maxSpeed
        );

        transform.position +=
            currentVelocity * Time.deltaTime;

        RotateTowardsVelocity();
    }

    private void RotateTowardsVelocity()
    {
        if (currentVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                currentVelocity.normalized,
                Vector3.up
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    public void StopMovement()
    {
        currentVelocity = Vector3.zero;
    }

    public int GetActiveInterestObjectCount()
    {
        return FindObjectsByType<InterestObject>(
            FindObjectsSortMode.None
        ).Length;
    }

    private void CacheObstacleColliders()
    {
        if (obstaclesRoot == null)
        {
            Debug.LogError(
                $"{name}: Obstacles Root is not assigned.",
                this
            );

            return;
        }

        obstacleColliders =
            obstaclesRoot.GetComponentsInChildren<Collider>();

        if (obstacleColliders.Length == 0)
        {
            Debug.LogError(
                $"{name}: No obstacle colliders found.",
                this
            );
        }
    }

    public void SpawnInterestObject()
    {
        if (interestObjectPrefab == null)
        {
            return;
        }

        if (huntingGroundRenderer == null)
        {
            Debug.LogError(
                $"{name}: Hunting Ground Renderer " +
                "is not assigned.",
                this
            );
            return;
        }

        if (obstacleColliders == null ||
            obstacleColliders.Length == 0)
        {
            return;
        }

        Bounds bounds = huntingGroundRenderer.bounds;
        Vector3 spawnCenter = bounds.center;

        for (int attempt = 0;
             attempt < maxSpawnAttempts;
             attempt++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle *
                interestSpawnRadius;

            Vector3 spawnPosition = new Vector3(
                spawnCenter.x + randomCircle.x,
                interestSpawnHeight,
                spawnCenter.z + randomCircle.y
            );

            if (!IsInsideArena(spawnPosition))
            {
                continue;
            }

            if (!IsClearOfObstacles(spawnPosition))
            {
                continue;
            }

            Instantiate(
                interestObjectPrefab,
                spawnPosition,
                Quaternion.identity
            );

            return;
        }

        Debug.LogWarning(
            $"{name}: Could not find a valid position " +
            "for an InterestObject.",
            this
        );
    }

    private bool IsInsideArena(Vector3 position)
    {
        if (huntingGroundRenderer == null)
        {
            return false;
        }

        Bounds bounds = huntingGroundRenderer.bounds;

        float minX =
            bounds.min.x + spawnBoundsPadding;

        float maxX =
            bounds.max.x - spawnBoundsPadding;

        float minZ =
            bounds.min.z + spawnBoundsPadding;

        float maxZ =
            bounds.max.z - spawnBoundsPadding;

        if (minX >= maxX || minZ >= maxZ)
        {
            return false;
        }

        return position.x >= minX &&
               position.x <= maxX &&
               position.z >= minZ &&
               position.z <= maxZ;
    }

    private bool IsClearOfObstacles(Vector3 position)
    {
        float clearanceSqr =
            spawnObstacleClearance *
            spawnObstacleClearance;

        foreach (Collider obstacle in obstacleColliders)
        {
            if (obstacle == null ||
                !obstacle.enabled ||
                !obstacle.gameObject.activeInHierarchy ||
                obstacle.isTrigger)
            {
                continue;
            }

            Vector3 closestPoint =
                obstacle.ClosestPoint(position);

            float distanceSqr =
                (closestPoint - position).sqrMagnitude;

            if (distanceSqr <= clearanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateAttackCooldown()
    {
        if (attackCooldownTimer < tba)
        {
            attackCooldownTimer += Time.deltaTime;
        }
    }

    public void ResetAttackCooldown()
    {
        attackCooldownTimer = 0f;
    }

    public void SetTarget(BoidAgent target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    public void RegisterSuccessfulAttack()
    {
        ResetAttackCooldown();
    }

    private void OnValidate()
    {
        requiredGathers = Mathf.Max(1, requiredGathers);
        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxAcceleration = Mathf.Max(0f, maxAcceleration);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);

        pathNodeReachDistance = Mathf.Max(
            0.05f,
            pathNodeReachDistance
        );

        perceptionRadius = Mathf.Max(0f, perceptionRadius);

        maxSpawnAttempts = Mathf.Max(1, maxSpawnAttempts);
        interestSpawnRadius = Mathf.Max(0f, interestSpawnRadius);
        spawnBoundsPadding = Mathf.Max(0f, spawnBoundsPadding);
        spawnObstacleClearance =
            Mathf.Max(0f, spawnObstacleClearance);

        tba = Mathf.Max(0f, tba);
        rangeAttackRadius = Mathf.Max(0f, rangeAttackRadius);

        meleeAttackRadius = Mathf.Clamp(
            meleeAttackRadius,
            0f,
            rangeAttackRadius
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(
            transform.position,
            perceptionRadius
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            rangeAttackRadius
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            meleeAttackRadius
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            transform.position,
            gatherRadius
        );

        IReadOnlyList<NavigationNode> path;
        int pathIndex;
        if (CurrentState == ExitState && ExitState != null)
        {
            path = ExitState.CurrentPath;
            pathIndex = ExitState.CurrentPathIndex;
            Gizmos.color = Color.blue;
        }
        else
        {
            if (SearchState == null) return;
            path = SearchState.CurrentPath;
            pathIndex = SearchState.CurrentPathIndex;
            Gizmos.color = Color.cyan;
        }

        if (path == null || path.Count == 0) return;

        for (int i = pathIndex;
             i < path.Count;
             i++)
        {
            Vector3 position =
                path[i].Position + Vector3.up * 0.5f;

            Gizmos.DrawSphere(position, 0.2f);

            if (i > pathIndex)
            {
                Vector3 previousPosition =
                    path[i - 1].Position + Vector3.up * 0.5f;

                Gizmos.DrawLine(
                    previousPosition,
                    position
                );
            }
        }
    }
}
