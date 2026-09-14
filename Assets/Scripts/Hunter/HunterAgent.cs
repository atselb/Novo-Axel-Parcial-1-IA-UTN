using UnityEngine;

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

    public float MaxSpeed => maxSpeed;
    public float PerceptionRadius => perceptionRadius;

    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;

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
