using UnityEngine;

public class BoundaryAvoidanceBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;

    [Header("Boundary Detection")]
    [SerializeField] private LayerMask boundaryMask;
    [SerializeField] private float lookAheadDistance = 5f;
    [SerializeField] private float sphereRadius = 0.4f;
    [SerializeField] private float whiskerAngle = 35f;

    public Vector3 Calculate()
    {
        Vector3 forward = GetMovementDirection();

        if (forward.sqrMagnitude <= 0.001f) return Vector3.zero;

        Vector3 left = 
            Quaternion.AngleAxis(
                -whiskerAngle,
                Vector3.up
            ) * forward;
        
        Vector3 right =
            Quaternion.AngleAxis(
                whiskerAngle,
                Vector3.up
            ) * forward;

        Vector3 avoidance =
            CastWhisker(forward) +
            CastWhisker(left) +
            CastWhisker(right);
        
        avoidance.y = 0f;

        if (avoidance.sqrMagnitude <= 0.001f) return Vector3.zero;

        return avoidance.normalized * agent.MaxSpeed;
    }

    private Vector3 GetMovementDirection()
    {
        Vector3 direction = 
            agent.CurrentVelocity.sqrMagnitude > 0.001f
            ? agent.CurrentVelocity.normalized
            : transform.forward;

        direction.y = 0f;

        return direction.normalized;
    }

    private Vector3 CastWhisker(Vector3 direction)
    {
        if (!Physics.SphereCast(
            transform.position,
            sphereRadius,
            direction,
            out RaycastHit hit,
            lookAheadDistance,
            boundaryMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            return Vector3.zero;
        }

        float proximity = 
            1f - Mathf.Clamp01(
                hit.distance / lookAheadDistance
            );

        Vector3 normal = hit.normal;
        normal.y = 0f;

        return normal * (1f + proximity);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 forward = 
            Application.isPlaying &&
            agent != null &&
            agent.CurrentVelocity.sqrMagnitude > 0.001f
                ? agent.CurrentVelocity.normalized
                : transform.forward;
        
        forward.y = 0f;

        Vector3 left =
            Quaternion.AngleAxis(
                -whiskerAngle,
                Vector3.up
            ) * forward;
        
        Vector3 right =
            Quaternion.AngleAxis(
                whiskerAngle,
                Vector3.up
            ) * forward;

        Gizmos.DrawLine(
            transform.position,
            transform.position + forward * lookAheadDistance
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + left * lookAheadDistance
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + right * lookAheadDistance
        );
    }
}