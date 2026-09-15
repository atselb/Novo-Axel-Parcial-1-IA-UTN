using UnityEngine;

public class TestArrive : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private BoidPerception perception;
    [SerializeField] private ArriveBehaviour arrive;

    private void Update()
    {
        InterestObject target = perception.GetClosestInterestObject();

        if (target == null) return;

        Vector3 desiredVelocity = arrive.Calculate(target.transform.position);

        agent.ApplySteering(desiredVelocity);
    }
}