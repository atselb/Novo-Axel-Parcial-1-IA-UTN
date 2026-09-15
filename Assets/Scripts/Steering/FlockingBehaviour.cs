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
    [SerializeField] private EvadeBehaviour evade;
    [SerializeField] private BoundaryAvoidanceBehaviour boundary;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;
    [SerializeField] private float arriveWeight = 1.5f;

    [Header("Priority Weights")]
    [SerializeField] private float evadeSeparationWeight = 2.5f;
    [SerializeField] private float boundaryWeight = 3f;

    public bool IsEvading {get; private set;}

    private void Update()
    {
        if (!agent.IsActive) return;

        Vector3 desiredVelocity = CalculateFlocking();

        agent.ApplySteering(desiredVelocity);
    }

    private Vector3 CalculateFlocking()
    {
        HunterAgent hunter = perception.PerceivedHunter;

        Vector3 separationVelocity = separation.Calculate();
        Vector3 boundaryVelocity = boundary.Calculate();

        // 1. Boundary safety
        if (boundaryVelocity.sqrMagnitude > 0.001f)
        {
            Vector3 emergencyVelocity = 
                boundaryVelocity * boundaryWeight +
                separationVelocity * evadeSeparationWeight;
            
            if (hunter != null)
            {
                IsEvading = true;
                emergencyVelocity += evade.Calculate(hunter);
            }
            else
            {
                IsEvading = false;
            }

            return Vector3.ClampMagnitude(emergencyVelocity, agent.MaxSpeed);
        }

        // 2. Hunter threat
        if (hunter != null)
        {
            IsEvading = true;

            Vector3 escapeVelocity = 
                evade.Calculate(hunter) + 
                separationVelocity * evadeSeparationWeight;
            
            return Vector3.ClampMagnitude(escapeVelocity, agent.MaxSpeed);
        }

        IsEvading = false;

        // 3. Interest object

        InterestObject target = perception.GetClosestInterestObject();

        if (target != null)
        {
            Vector3 arriveVelocity =
                arrive.Calculate(target.transform.position);

            Vector3 interestVelocity =
                arriveVelocity * arriveWeight +
                separationVelocity * separationWeight;
            
            return Vector3.ClampMagnitude(
                interestVelocity,
                agent.MaxSpeed
            );

        }

        // 4. Normal flocking

        Vector3 alignmentVelocity = alignment.Calculate() * alignmentWeight;

        Vector3 cohesionVelocity = cohesion.Calculate() * cohesionWeight;

        Vector3 flockingVelocity = 
            separationVelocity * separationWeight +
            alignmentVelocity +
            cohesionVelocity;

        return Vector3.ClampMagnitude(
            flockingVelocity,
            agent.MaxSpeed
        );
    }

    
}