using UnityEngine;

public class ArriveBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;

    [Header("Arrive")]
    [SerializeField] private float slowRadius = 4f;
    [SerializeField] private float stopRadius = 0.8f;

    public Vector3 Calculate(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= stopRadius) return Vector3.zero;

        float targetSpeed = agent.MaxSpeed;

        if (distance < slowRadius)
        {   
            float normalizedDistance = (distance - stopRadius) / (slowRadius - stopRadius);

            targetSpeed = agent.MaxSpeed * Mathf.Clamp01(normalizedDistance);
        }

        Vector3 desiredVelocity = toTarget.normalized * targetSpeed;

        return desiredVelocity;
    }

    private void OnValidate()
    {
        slowRadius = Mathf.Max(0.1f, slowRadius);

        stopRadius = Mathf.Clamp(stopRadius, 0f, slowRadius);
    }
}