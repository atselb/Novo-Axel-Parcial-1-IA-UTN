using System.Collections;
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
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Interest Interaction")]
    [SerializeField] private float interactionRadius = 1f;
    [SerializeField] private float interestDamage = 10f;
    [SerializeField] private float interestDamageInterval = 1f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private Vector2 respawnXRange = new Vector2(-20f, 20f);
    [SerializeField] private Vector2 respawnZRange = new Vector2(-20f, 20f);

    private Vector3 currentVelocity;
    private bool isActive = true;

    public float MaxSpeed => maxSpeed;
    public float MaxAcceleration => maxAcceleration;
    public float PerceptionRadius => perceptionRadius;
    public float SeparationRadius => separationRadius;
    public float MaxHealth => maxHealth;
    public float Health => currentHealth;
    public Vector3 CurrentVelocity => currentVelocity;
    public bool IsActive => isActive;
    public float InteractionRadius => interactionRadius;
    public float InterestDamage => interestDamage;
    public float InterestDamageInterval => interestDamageInterval;
    public float RespawnDelay => respawnDelay;

    // public bool IsEvading => GetComponent<BoidPerception>()?.HasHunterThreat ?? false;

    public bool IsCollected {get; private set;}

    private void Awake()
    {
        currentHealth = maxHealth;
    }

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

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);

        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        isActive = false;
        currentVelocity = Vector3.zero;

        Debug.Log($"{name} was eliminated.");
    }

    public void Collect()
    {
        if (isActive) return;

        if (IsCollected) return;

        IsCollected = true;
        
        StartCoroutine(RespawnRoutine());

        Debug.Log($"{name} was collected.");
    }

    private IEnumerator RespawnRoutine()
    {
        SetVisualsActive(false);

        yield return new WaitForSeconds(respawnDelay);

        Respawn();
    }

    private void SetVisualsActive(bool value)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = value;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = value;
        }
    }

    private void Respawn()
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(respawnXRange.x, respawnXRange.y),
            transform.position.y,
            Random.Range(respawnZRange.x, respawnZRange.y)
        );

        transform.position = randomPosition;

        currentHealth = maxHealth;
        currentVelocity = Vector3.zero;

        isActive = true;
        IsCollected = false;

        SetVisualsActive(true);

        Debug.Log($"{name} respawned.");
    }

    private void OnValidate()
    {
        separationRadius = Mathf.Min(separationRadius, perceptionRadius);

        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxAcceleration = Mathf.Max(0f, maxAcceleration);
    }
}
