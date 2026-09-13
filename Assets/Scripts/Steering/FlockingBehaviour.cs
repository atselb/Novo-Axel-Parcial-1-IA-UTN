using UnityEngine;

public class FlockingBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    
    [Header("Behaviours")]
    [SerializeField] private SeparationBehaviour separation;
    [SerializeField] private AlignmentBehaviour alignment;
    [SerializeField] private CohesionBehaviour cohesion;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    private void Update()
    {
        Vector3 desiredVelocity = CalculateFlocking();

        agent.ApplySteering(desiredVelocity);
    }

    private Vector3 CalculateFlocking()
    {
        Vector3 separationVelocity = separation.Calculate() * separationWeight;
        Vector3 alignmentVelocity = alignment.Calculate() * alignmentWeight;
        Vector3 cohesionVelocity = cohesion.Calculate() * cohesionWeight;

        Vector3 flockingVelocity = separationVelocity + alignmentVelocity + cohesionVelocity;

        return Vector3.ClampMagnitude(flockingVelocity, agent.MaxSpeed);
    }
}