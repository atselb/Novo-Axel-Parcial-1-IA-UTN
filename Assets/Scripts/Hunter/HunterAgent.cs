using UnityEngine;
using UnityEngine.InputSystem;

public class HunterAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;

    [Header("Perception")]
    [SerializeField] private float perceptionRadius = 12f;

    [Header("Attack")]
    [SerializeField] private float tba = 3f;
    [SerializeField] private float rangeAttackRadius = 8f;
    [SerializeField] private float meleeAttackRadius = 2f;

    private HunterState currentState;

    public float MaxSpeed => maxSpeed;
    public float PerceptionRadius => perceptionRadius;

    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;

    public HunterState CurrentState => currentState;

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
    }

    private void Update()
    {
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
