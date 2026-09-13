using UnityEngine;

public class TestAlignment : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private AlignmentBehaviour alignment;

    private void Update()
    {
        Vector3 desiredVelocity = alignment.Calculate();

        agent.ApplySteering(desiredVelocity);
    }
}