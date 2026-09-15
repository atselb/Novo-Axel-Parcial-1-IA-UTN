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

            awayFromNeighbor.y = 0f;

            float distance = awayFromNeighbor.magnitude;
            
            if (distance <= 0.001f) continue;

            float strength = Mathf.Clamp01(
                (agent.SeparationRadius - distance) /
                agent.SeparationRadius
            );

            separation +=
                awayFromNeighbor.normalized *
                strength *
                agent.MaxSpeed;
        }
        
        return Vector3.ClampMagnitude(separation, agent.MaxSpeed);
    }
}
