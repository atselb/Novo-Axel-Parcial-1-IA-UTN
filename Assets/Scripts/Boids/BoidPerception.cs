using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BoidPerception : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    private HunterAgent perceivedHunter;

    private readonly List<BoidAgent> perceivedNeighbors = new();
    private readonly List<BoidAgent> separationNeighbors = new();
    private readonly List<InterestObject> perceivedInterestObjects = new();

    public IReadOnlyList<BoidAgent> PerceivedNeighbors => perceivedNeighbors;
    public IReadOnlyList<BoidAgent> SeparationNeighbors => separationNeighbors;
    public IReadOnlyList<InterestObject> PerceivedInterestObjects => perceivedInterestObjects;

    public int PerceivedCount => perceivedNeighbors.Count;
    public int SeparationCount => separationNeighbors.Count;
    public int InterestObjectCount => perceivedInterestObjects.Count;
    public HunterAgent PerceivedHunter => perceivedHunter;
    public bool HasHunterThreat => perceivedHunter != null;

    private void Update()
    {
        DetectNearbyObjects();

        if (perceivedInterestObjects.Count > 0)
        {
            Debug.Log(
                $"{name} detected {perceivedInterestObjects.Count} interest objects."
            );
        }
    }

    private void DetectNearbyObjects()
    {
        perceivedNeighbors.Clear();
        separationNeighbors.Clear();
        perceivedInterestObjects.Clear();

        perceivedHunter = null;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            agent.PerceptionRadius
        );

        foreach (Collider hit in hits)
        {
            DetectBoid(hit);
            DetectInterestObject(hit);
            DetectHunter(hit);
        }
    }

    private void DetectBoid(Collider hit)
    {
        BoidAgent otherAgent = hit.GetComponent<BoidAgent>();

        if (otherAgent == null) return;
        if (otherAgent == agent) return;
        if (!otherAgent.IsActive) return;

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

    private void DetectInterestObject(Collider hit)
    {
        InterestObject interestObject = hit.GetComponent<InterestObject>();

        if (interestObject == null) return;

        if (interestObject.IsDestroyed) return;

        perceivedInterestObjects.Add(interestObject);
    }

    private void DetectHunter(Collider hit)
    {
        HunterAgent hunter = hit.GetComponent<HunterAgent>();

        if (hunter == null) return;

        perceivedHunter = hunter;

        if (perceivedHunter != null)
        {
            Debug.Log($"{name} detected the Hunter");
        }
    }

    public InterestObject GetClosestInterestObject()
    {
        InterestObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (InterestObject interestObject in perceivedInterestObjects)
        {
            if (interestObject == null) continue;

            float distance = Vector3.SqrMagnitude(interestObject.transform.position - transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interestObject;
            }
        }

        return closest;
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




    // DEPRECATED: This method is no longer used, but kept for reference.
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
}
