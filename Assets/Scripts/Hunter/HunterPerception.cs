
using System.Collections.Generic;
using UnityEngine;

public class HunterPerception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HunterAgent hunter;

    [Header("Detection Settings")]
    [SerializeField, Min(0.02f)]
    private float detectionInterval = 0.2f;

    private float nextDetectionTime;

    private Collider[] overlapResults = new Collider[32];

    private readonly List<BoidAgent> perceivedAliveBoids = new();
    private readonly List<BoidAgent> perceivedEliminatedBoids = new();

    public IReadOnlyList<BoidAgent> PerceivedAliveBoids => perceivedAliveBoids;
    public IReadOnlyList<BoidAgent> PerceivedEliminatedBoids => perceivedEliminatedBoids;

    public bool HasAliveBoids => perceivedAliveBoids.Count > 0;
    public bool HasEliminatedBoids => perceivedEliminatedBoids.Count > 0;

    private void OnEnable()
    {
        nextDetectionTime = Time.time;
    }

    private void Update()
    {
        if (Time.time < nextDetectionTime) return;

        nextDetectionTime = Time.time + detectionInterval;

        DetectBoids();
    }

    private void DetectBoids()
    {
        perceivedAliveBoids.Clear();
        perceivedEliminatedBoids.Clear();

        int hitCount = GetNearbyColliders();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null) continue;

            BoidAgent boid = hit.GetComponentInParent<BoidAgent>();

            if (boid == null) continue;

            if (boid.IsActive)
            {
                if (!perceivedAliveBoids.Contains(boid))
                {
                    perceivedAliveBoids.Add(boid);
                }
            }
            else
            {
                if (!perceivedEliminatedBoids.Contains(boid))
                {
                    perceivedEliminatedBoids.Add(boid);
                }
            }
        }
    }

    private int GetNearbyColliders()
    {
        int hitCount;

        while (true)
        {
            hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                hunter.PerceptionRadius,
                overlapResults
            );

            if (hitCount < overlapResults.Length)
            {
                return hitCount;
            }

            System.Array.Resize(
                ref overlapResults,
                overlapResults.Length * 2
            );
        }
    }

    public BoidAgent GetClosestAliveBoid()
    {
        BoidAgent closest = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (BoidAgent boid in perceivedAliveBoids)
        {
            if (boid == null) continue;

            float distanceSqr =
                (boid.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = boid;
            }
        }

        return closest;
    }

    public BoidAgent GetClosestEliminatedBoid()
    {
        BoidAgent closest = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (BoidAgent boid in perceivedEliminatedBoids)
        {
            if (boid == null) continue;

            float distanceSqr =
                (boid.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = boid;
            }
        }

        return closest;
    }

    private void OnValidate()
    {
        detectionInterval = Mathf.Max(0.02f, detectionInterval);
    }
}
