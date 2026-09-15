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

    [Header("Interest Interaction")]
    [SerializeField] private float interactionRadius = 1f;
    [SerializeField] private float interestDamage = 10f;
    [SerializeField] private float interestDamageInterval = 1f;

    private Vector3 currentVelocity;
    private bool isActive = true;

    public float MaxSpeed => maxSpeed;
    public float MaxAcceleration => maxAcceleration;
    public float PerceptionRadius => perceptionRadius;
    public float SeparationRadius => separationRadius;
    public float Health => health;
    public Vector3 CurrentVelocity => currentVelocity;
    public bool IsActive => isActive;
    public float InteractionRadius => interactionRadius;
    public float InterestDamage => interestDamage;
    public float InterestDamageInterval => interestDamageInterval;

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
        if (!isActive) return;

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

    public void TakeDamage(float amount)
    {
        if (!isActive) return;

        health -= amount;
        health = Mathf.Max(0f, health);

        if (health <= 0f) Die();
    }

    private void Die()
    {
        isActive = false;
        currentVelocity = Vector3.zero;

        Debug.Log($"{name} was eliminated.");
    }

    private void OnValidate()
    {
        separationRadius = Mathf.Min(separationRadius, perceptionRadius);

        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxAcceleration = Mathf.Max(0f, maxAcceleration);
    }
}
