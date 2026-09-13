using UnityEngine;

public class TestSteering : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;

    private void Update()
    {
        Vector3 desiredVelocity =
            Vector3.forward * agent.MaxSpeed;

        agent.ApplySteering(desiredVelocity);
    }
}