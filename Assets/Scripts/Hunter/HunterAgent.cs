
using UnityEngine;

public class HunterAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float maxAcceleration = 8f;
    [SerializeField] private float rotationSpeed = 180f;

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

    public float MaxSpeed => maxSpeed;
    public Vector3 CurrentVelocity => currentVelocity;
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

    public PatrolState PatrolState { get; private set; }
    public AttackState AttackState { get; private set; }
    public GatherState GatherState { get; private set; }

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

        PatrolState = new PatrolState(this);
        AttackState = new AttackState(this);
        GatherState = new GatherState(this);

        attackCooldownTimer = tba;

        CacheObstacleColliders();
    }

    private void Start()
    {
        ChangeState(PatrolState);
    }

    private void Update()
    {
        UpdateAttackCooldown();

        stateMachine?.Update();
    }

    public void ChangeState(HunterState newState)
    {
        stateMachine?.ChangeState(newState);
    }

    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f) return;

        Vector3 desiredVelocity = direction.normalized * maxSpeed;

        Vector3 steering = desiredVelocity - currentVelocity;

        steering = Vector3.ClampMagnitude(
            steering,
            maxAcceleration
        );

        currentVelocity += steering * Time.deltaTime;

        currentVelocity = Vector3.ClampMagnitude(
            currentVelocity,
            maxSpeed
        );

        transform.position += currentVelocity * Time.deltaTime;

        RotateTowardsVelocity();
    }

    private void RotateTowardsVelocity()
    {
        if (currentVelocity.sqrMagnitude <= 0.001f) return;

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
                $"{name}: No obstacle colliders were found.",
                this
            );
        }
    }

    public void SpawnInterestObject()
    {
        if (interestObjectPrefab == null) return;

        if (huntingGroundRenderer == null)
        {
            Debug.LogError(
                $"{name}: Hunting Ground Renderer is not assigned.",
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

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle * interestSpawnRadius;

            Vector3 spawnPosition = new Vector3(
                spawnCenter.x + randomCircle.x,
                interestSpawnHeight,
                spawnCenter.z + randomCircle.y
            );

            if (!IsInsideHuntingGround(spawnPosition))
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
            $"{name}: Could not find a valid position for an InterestObject.",
            this
        );
    }

    private bool IsInsideHuntingGround(Vector3 position)
    {
        if (huntingGroundRenderer == null)
        {
            return false;
        }

        Bounds bounds = huntingGroundRenderer.bounds;

        float minX = bounds.min.x + spawnBoundsPadding;
        float maxX = bounds.max.x - spawnBoundsPadding;

        float minZ = bounds.min.z + spawnBoundsPadding;
        float maxZ = bounds.max.z - spawnBoundsPadding;

        if (minX >= maxX || minZ >= maxZ)
        {
            return false;
        }

        return
            position.x >= minX &&
            position.x <= maxX &&
            position.z >= minZ &&
            position.z <= maxZ;
    }

    private bool IsClearOfObstacles(Vector3 position)
    {
        float clearanceSqr =
            spawnObstacleClearance * spawnObstacleClearance;

        foreach (Collider obstacle in obstacleColliders)
        {
            if (obstacle == null) continue;
            if (!obstacle.enabled) continue;
            if (!obstacle.gameObject.activeInHierarchy) continue;
            if (obstacle.isTrigger) continue;

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
        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxAcceleration = Mathf.Max(0f, maxAcceleration);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);

        perceptionRadius = Mathf.Max(0f, perceptionRadius);

        maxSpawnAttempts = Mathf.Max(1, maxSpawnAttempts);
        interestSpawnRadius = Mathf.Max(0f, interestSpawnRadius);
        spawnBoundsPadding = Mathf.Max(0f, spawnBoundsPadding);
        spawnObstacleClearance = Mathf.Max(0f, spawnObstacleClearance);

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
        Gizmos.DrawWireSphere(
            transform.position,
            perceptionRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            rangeAttackRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            meleeAttackRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            gatherRadius
        );
    }
}
