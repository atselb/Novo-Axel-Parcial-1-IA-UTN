using UnityEngine;

public class TestCohesion : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private CohesionBehaviour cohesion;

    private void Update()
    {
        Vector3 desiredVelocity = cohesion.Calculate();

        agent.ApplySteering(desiredVelocity);
    }
}