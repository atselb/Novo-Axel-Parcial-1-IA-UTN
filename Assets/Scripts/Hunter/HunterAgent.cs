using UnityEngine;
using UnityEngine.InputSystem;

public class HunterAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;

    [Header("Perception")]
    [SerializeField] private float perceptionRadius = 12f;
    [SerializeField] private HunterPerception perception;

    [Header("Attack")]
    [SerializeField] private float tba = 3f;
    [SerializeField] private float rangeAttackRadius = 8f;
    [SerializeField] private float meleeAttackRadius = 2f;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Header("Interest Objects")]
    [SerializeField] private InterestObject interestObjectPrefab;
    [SerializeField] private float interestSpawnInterval = 5f;
    [SerializeField] private int maxActiveInterestObjects = 5;
    [SerializeField] private float interestSpawnRadius = 10f;

    private HunterState currentState;
    private float attackCooldownTimer;

    private BoidAgent currentTarget;

    public float MaxSpeed => maxSpeed;
    public float PerceptionRadius => perceptionRadius;
    public HunterPerception Perception => perception;

    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;

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

    public PatrolState PatrolState { get; private set; }
    public AttackState AttackState { get; private set; }
    public GatherState GatherState { get; private set; }

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

        // Debugging state changes using keyboard input (commented out)
        // if (Keyboard.current == null) return;

        // if (Keyboard.current.digit1Key.wasPressedThisFrame)
        // {
        //     ChangeState(PatrolState);
        // }
        // else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        // {
        //     ChangeState(AttackState);
        // }
        // else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        // {
        //     ChangeState(GatherState);
        // }
        
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
        
        Vector3 velocity = direction.normalized * maxSpeed;

        transform.position += velocity * Time.deltaTime;
        transform.forward = direction.normalized;
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

        Vector2 randomCircle = Random.insideUnitCircle * interestSpawnRadius;

        Vector3 spawnPosition = new Vector3(
            transform.position.x + randomCircle.x,
            1f,
            transform.position.z + randomCircle.y
        );

        Instantiate(
            interestObjectPrefab,
            spawnPosition,
            Quaternion.identity
        );
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



    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        perceptionRadius = Mathf.Max(0f, perceptionRadius);

        tba = Mathf.Max(0f, tba);
        rangeAttackRadius = Mathf.Max(0f, rangeAttackRadius);
        meleeAttackRadius = Mathf.Clamp(meleeAttackRadius, 0f, rangeAttackRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
    }
}
