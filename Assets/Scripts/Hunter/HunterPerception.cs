using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class HunterPerception : MonoBehaviour
{
    [SerializeField] private HunterAgent hunter;

    private readonly List<BoidAgent> perceivedAliveBoids = new();
    private readonly List<BoidAgent> perceivedEliminatedBoids = new();

    public IReadOnlyList<BoidAgent> PerceivedAliveBoids => perceivedAliveBoids;
    public IReadOnlyList<BoidAgent> PerceivedEliminatedBoids => perceivedEliminatedBoids;
    
    public bool HasaliveBoids => perceivedAliveBoids.Count > 0;
    public bool HaseliminatedBoids => perceivedEliminatedBoids.Count > 0;

    private void Update()
    {
        DetectBoids();

        Debug.Log(
            $"Alive: {perceivedAliveBoids.Count} | " +
            $"Eliminated: {perceivedEliminatedBoids.Count}"
        );
    }

    private void DetectBoids()
    {
        perceivedAliveBoids.Clear();
        perceivedEliminatedBoids.Clear();

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hunter.PerceptionRadius
        );

        foreach (Collider hit in hits)
        {
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
                if (!PerceivedEliminatedBoids.Contains(boid))
                {
                    perceivedEliminatedBoids.Add(boid);
                }
            }
        }
    }

    public BoidAgent GetClosestAliveBoid()
    {
        BoidAgent closest = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (BoidAgent boid in perceivedAliveBoids)
        {
            if (boid == null) continue;

            float distanceSqr = (boid.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr > closestDistanceSqr)
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

            float distanceSqr = (boid.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = boid;
            }
        }

        return closest;
    } 
}