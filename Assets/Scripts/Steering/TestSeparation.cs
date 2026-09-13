using UnityEngine;

public class TestSeparation : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private SeparationBehaviour separation;

    private void Update()
    {
        Vector3 desiredVelocity = separation.Calculate();

        agent.ApplySteering(desiredVelocity);
    }
}