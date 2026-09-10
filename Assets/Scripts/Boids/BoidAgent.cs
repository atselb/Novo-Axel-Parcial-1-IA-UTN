using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;

    [Header("Perception")]
    [SerializeField] private float perceptionRadius = 8f;

    [Header("Agent")]
    [SerializeField] private float health = 100f;

    private Vector3 currentVelocity;
    private bool isActive = true;

    public float MaxSpeed => maxSpeed;
    public float PerceptionRadius => perceptionRadius;
    public float Health => health;
    public Vector3 CurrentVelocity => currentVelocity;
    public bool IsActive => isActive;

    public void SetVelocity(Vector3 velocity)
    {
        currentVelocity = velocity;
    }
}
