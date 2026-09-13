using UnityEngine;

public class SeparationBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private BoidPerception perception;

    public Vector3 Calculate()
    {
        if (perception.SeparationNeighbors.Count == 0)
            return Vector3.zero;
        
        Vector3 separation = Vector3.zero;

        foreach (BoidAgent neighbor in perception.SeparationNeighbors)
        {
            Vector3 awayFromNeighbor = transform.position - neighbor.transform.position;
            float distance = awayFromNeighbor.magnitude;

            if (distance > 0f)
            {
                separation += awayFromNeighbor.normalized / distance;
            }
        }

        separation /= perception.SeparationNeighbors.Count;

        if (separation.sqrMagnitude > 0f)
        {
            separation = separation.normalized * agent.MaxSpeed;
        }

        return separation;
    }
}
