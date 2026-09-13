using UnityEngine;

public class CohesionBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private BoidPerception perception;

    public Vector3 Calculate()
    {
        if (perception.PerceivedNeighbors.Count == 0)
            return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;

        foreach (BoidAgent neighbor in perception.PerceivedNeighbors)
        {
            centerOfMass += neighbor.transform.position;
        }

        centerOfMass /= perception.PerceivedNeighbors.Count;

        Vector3 directionToCenter = centerOfMass - transform.position;

        if (directionToCenter.sqrMagnitude > 0f)
        {
            directionToCenter = directionToCenter.normalized * agent.MaxSpeed;
        }

        return directionToCenter;
    }
}