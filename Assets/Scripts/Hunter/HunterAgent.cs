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
    [SerializeField] private Vector2 arenaXBounds = new Vector2(-24f, 24f);
    [SerializeField] private Vector2 arenaZBounds = new Vector2(-24f, 24f);
    [SerializeField] private float interestSpawnHeight = 1f;
    [SerializeField] private int maxSpawnAttempts = 10;

    private HunterState currentState;
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

    public HunterState CurrentState => currentState;
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
        currentState != null
        ? currentState.GetType().Name
        : "None";

    public string CurrentTargetName =>
        currentTarget != null
        ? currentTarget.name
        : "None";

    private void Start()
    {
        ChangeState(PatrolState);
    }

    private void Awake()
    {
        PatrolState = new PatrolState(this);
        AttackState = new AttackState(this);
        GatherState = new GatherState(this);

        attackCooldownTimer = tba;
    }

    private void Update()
    {   
        UpdateAttackCooldown();

        currentState?.Update();        
    }

    public void ChangeState(HunterState newState)
    {
        if (newState == null)
        {
            return;
        }

        currentState?.Exit();

        currentState = newState;

        currentState.Enter();
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

        currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxSpeed);

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

    public void SpawnInterestObject()
    {
        if (interestObjectPrefab == null) return;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * interestSpawnRadius;

            Vector3 spawnPosition = new Vector3(
                transform.position.x + randomCircle.x,
                interestSpawnHeight,
                transform.position.z + randomCircle.y
            );

            if (!IsInsideArena(spawnPosition)) continue;

            Instantiate(
                interestObjectPrefab,
                spawnPosition,
                Quaternion.identity
            );

            return;
        }

        Debug.LogWarning("Hunter could not find a valid position to spawn a InterestObject.");
    }

    private bool IsInsideArena(Vector3 position)
    {
        return
            position.x >= arenaXBounds.x &&
            position.x <= arenaXBounds.y &&
            position.z >= arenaZBounds.x &&
            position.z <= arenaZBounds.y;
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

        tba = Mathf.Max(0f, tba);
        rangeAttackRadius = Mathf.Max(0f, rangeAttackRadius);
        meleeAttackRadius = Mathf.Clamp(meleeAttackRadius, 0f, rangeAttackRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        Gizmos.DrawWireSphere(transform.position, gatherRadius);
    }
}
