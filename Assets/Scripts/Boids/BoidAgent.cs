
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

    [Header("Hunting Ground Bounds")]
    [SerializeField] private Renderer huntingGroundRenderer;
    [SerializeField, Min(0f)] private float boundaryPadding = 1f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;

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

    public bool IsCollected { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (huntingGroundRenderer == null)
        {
            Debug.LogError(
                $"{name}: Hunting Ground Renderer is not assigned.",
                this
            );

            return;
        }

        KeepInsideHuntingGround();
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

        SetVelocity(
            Vector3.ClampMagnitude(currentVelocity, maxSpeed)
        );

        Move();
    }

    private void Move()
    {
        if (!isActive) return;

        transform.position += currentVelocity * Time.deltaTime;

        KeepInsideHuntingGround();

        if (currentVelocity.sqrMagnitude > 0.001f)
        {
            transform.forward = currentVelocity.normalized;
        }
    }

    private void KeepInsideHuntingGround()
    {
        if (!TryGetHuntingBounds(
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ))
        {
            return;
        }

        Vector3 position = transform.position;

        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(position.z, minZ, maxZ);

        if (!Mathf.Approximately(position.x, clampedX))
        {
            currentVelocity.x = 0f;
        }

        if (!Mathf.Approximately(position.z, clampedZ))
        {
            currentVelocity.z = 0f;
        }

        position.x = clampedX;
        position.z = clampedZ;

        transform.position = position;
    }

    private bool TryGetHuntingBounds(
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ)
    {
        minX = 0f;
        maxX = 0f;
        minZ = 0f;
        maxZ = 0f;

        if (huntingGroundRenderer == null)
        {
            return false;
        }

        Bounds bounds = huntingGroundRenderer.bounds;

        if (bounds.extents.x <= boundaryPadding ||
            bounds.extents.z <= boundaryPadding)
        {
            return false;
        }

        minX = bounds.min.x + boundaryPadding;
        maxX = bounds.max.x - boundaryPadding;

        minZ = bounds.min.z + boundaryPadding;
        maxZ = bounds.max.z - boundaryPadding;

        return true;
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

        if (currentHealth <= 0f)
        {
            Die();
        }
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
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = value;
        }

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = value;
        }
    }

    private void Respawn()
    {
        if (!TryGetHuntingBounds(
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ))
        {
            Debug.LogError(
                $"{name}: Cannot respawn without valid hunting ground bounds.",
                this
            );

            return;
        }

        Vector3 randomPosition = new Vector3(
            Random.Range(minX, maxX),
            transform.position.y,
            Random.Range(minZ, maxZ)
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
        separationRadius = Mathf.Min(
            separationRadius,
            perceptionRadius
        );

        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxAcceleration = Mathf.Max(0f, maxAcceleration);
        boundaryPadding = Mathf.Max(0f, boundaryPadding);
    }
}
