
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

    [Header("Visual Feedback")]
    [SerializeField] private Color damageFlashColor =
        new Color(1f, 0.18f, 0.08f, 1f);
    [SerializeField] private Color eliminatedColor =
        new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField, Min(0.02f)]
    private float damageFlashDuration = 0.18f;

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

    private Renderer[] visualRenderers;
    private MaterialPropertyBlock colorBlock;
    private Coroutine damageFlashRoutine;

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
        visualRenderers = GetComponentsInChildren<Renderer>(true);
        colorBlock = new MaterialPropertyBlock();
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
        steering = Vector3.ClampMagnitude(steering, maxAcceleration);

        currentVelocity += steering * Time.deltaTime;
        SetVelocity(Vector3.ClampMagnitude(currentVelocity, maxSpeed));
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
            currentVelocity.x = 0f;

        if (!Mathf.Approximately(position.z, clampedZ))
            currentVelocity.z = 0f;

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

        if (huntingGroundRenderer == null) return false;

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
        if (!isActive || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
        {
            Die();
        }

        ShowDamageFlash();
    }

    private void Die()
    {
        isActive = false;
        currentVelocity = Vector3.zero;
        Debug.Log($"{name} was eliminated.");
    }

    private void ShowDamageFlash()
    {
        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(DamageFlash());
    }

    private IEnumerator DamageFlash()
    {
        SetVisualTint(damageFlashColor);
        yield return new WaitForSeconds(damageFlashDuration);

        if (IsCollected)
        {
            damageFlashRoutine = null;
            yield break;
        }

        if (isActive)
            RestoreOriginalColors();
        else
            SetVisualTint(eliminatedColor);

        damageFlashRoutine = null;
    }

    private void SetVisualTint(Color color)
    {
        if (visualRenderers == null) return;

        foreach (Renderer renderer in visualRenderers)
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            colorBlock.Clear();
            renderer.GetPropertyBlock(colorBlock);

            // Works with URP Lit (_BaseColor) and Standard (_Color).
            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                colorBlock.SetColor("_BaseColor", color);

            if (renderer.sharedMaterial.HasProperty("_Color"))
                colorBlock.SetColor("_Color", color);

            renderer.SetPropertyBlock(colorBlock);
        }
    }

    private void RestoreOriginalColors()
    {
        if (visualRenderers == null) return;

        foreach (Renderer renderer in visualRenderers)
        {
            if (renderer != null) renderer.SetPropertyBlock(null);
        }
    }

    public void Collect()
    {
        if (isActive || IsCollected) return;

        IsCollected = true;
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }

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
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            renderer.enabled = value;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = value;
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

        RestoreOriginalColors();
        SetVisualsActive(true);
        Debug.Log($"{name} respawned.");
    }

    private void OnValidate()
    {
        separationRadius = Mathf.Min(separationRadius, perceptionRadius);
        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxAcceleration = Mathf.Max(0f, maxAcceleration);
        boundaryPadding = Mathf.Max(0f, boundaryPadding);
        damageFlashDuration = Mathf.Max(0.02f, damageFlashDuration);
    }
}
