using UnityEngine;

public class FlockingBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private BoidPerception perception;
    
    [Header("Behaviours")]
    [SerializeField] private SeparationBehaviour separation;
    [SerializeField] private AlignmentBehaviour alignment;
    [SerializeField] private CohesionBehaviour cohesion;
    [SerializeField] private ArriveBehaviour arrive;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;
    [SerializeField] private float arriveWeight = 1.5f;

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
        Vector3 arriveVelocity = Vector3.zero;

        InterestObject target = perception.GetClosestInterestObject();

        if (target != null)
        {
            arriveVelocity = arrive.Calculate(target.transform.position) * arriveWeight;
        }

        Vector3 flockingVelocity = separationVelocity + alignmentVelocity + cohesionVelocity + arriveVelocity;

        return Vector3.ClampMagnitude(flockingVelocity, agent.MaxSpeed);
    }
}