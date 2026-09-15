using UnityEngine;

public class AlignmentBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private BoidPerception perception;

    public Vector3 Calculate()
    {
        if (perception.PerceivedNeighbors.Count == 0)
            return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;

        foreach (BoidAgent neighbor in perception.PerceivedNeighbors)
        {
            averageVelocity += neighbor.CurrentVelocity;
        }

        averageVelocity /= perception.PerceivedNeighbors.Count;

        averageVelocity = Vector3.ClampMagnitude(averageVelocity, agent.MaxSpeed);

        return averageVelocity;
    }
}