using System.Collections.Generic;
using UnityEngine;

public class BoidPerception : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;

    private readonly List<BoidAgent> perceivedNeighbors = new();
    private readonly List<BoidAgent> separationNeighbors = new();

    public IReadOnlyList<BoidAgent> PerceivedNeighbors => perceivedNeighbors;
    public IReadOnlyList<BoidAgent> SeparationNeighbors => separationNeighbors;

    public int PerceivedCount => perceivedNeighbors.Count;
    public int SeparationCount => separationNeighbors.Count;

    void Update()
    {
        DetectNeighbors();
    }

    private void DetectNeighbors()
    {
        perceivedNeighbors.Clear();
        separationNeighbors.Clear();

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            agent.PerceptionRadius
        );

        foreach (Collider hit in hits)
        {
            BoidAgent otherAgent = hit.GetComponent<BoidAgent>();

            if (otherAgent == null) continue;
            if (otherAgent == agent) continue;
            if (!otherAgent.IsActive) continue;

            float distance = Vector3.Distance(
                transform.position,
                otherAgent.transform.position
            );

            perceivedNeighbors.Add(otherAgent);

            if (distance <= agent.SeparationRadius)
            {
                separationNeighbors.Add(otherAgent);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (agent == null)
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            agent.PerceptionRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            agent.SeparationRadius
        );
    }
}
