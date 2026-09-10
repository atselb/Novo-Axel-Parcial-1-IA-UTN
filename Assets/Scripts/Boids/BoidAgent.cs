using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxAcceleration = 10f;

    [Header("Perception")]
    [SerializeField] private float perceptionRadius = 8f;
    [SerializeField] private float separationRadius = 3f;

    [Header("Agent")]
    [SerializeField] private float health = 100f;

    private Vector3 currentVelocity;
    private bool isActive = true;

    public float MaxSpeed => maxSpeed;
    public float MaxAcceleration => maxAcceleration;
    public float PerceptionRadius => perceptionRadius;
    public float SeparationRadius => separationRadius;
    public float Health => health;
    public Vector3 CurrentVelocity => currentVelocity;
    public bool IsActive => isActive;

    public void ApplySteering(Vector3 desiredVelocity)
    {
        if (!isActive) return;

        Vector3 steering = desiredVelocity - currentVelocity;

        steering = Vector3.ClampMagnitude(
            steering,
            maxAcceleration
        );

        currentVelocity += steering * Time.deltaTime;

        SetVelocity(Vector3.ClampMagnitude(currentVelocity, maxSpeed));

        Move();
    }

    private void Move()
    {
        transform.position += currentVelocity * Time.deltaTime;

        if (CurrentVelocity.sqrMagnitude > 0.001f)
        {
            transform.forward = currentVelocity.normalized;
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        currentVelocity = velocity;
    }
}
