using System.Collections.Generic;
using UnityEngine;

public class ExitState : HunterState
{
    private readonly List<NavigationNode> currentPath = new();
    private int currentPathIndex;
    private float nextPathRetryTime;

    public IReadOnlyList<NavigationNode> CurrentPath => currentPath;
    public int CurrentPathIndex => currentPathIndex;

    public ExitState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        hunter.StopMovement();
        hunter.ClearTarget();
        currentPath.Clear();
        currentPathIndex = 0;
        nextPathRetryTime = 0f;

        Debug.Log("[Hunter Exit] Entered Exit state.");
        TryCreateExitPath();
    }

    public override void Update()
    {
        // Higher priority: Flee interrupts Exit, then resumes it.
        if (hunter.TryEnterFlee()) return;

        if (hunter.IsInsideExitZone(hunter.transform.position))
        {
            hunter.CompleteEscapeAndRestart();
            return;
        }

        if (currentPath.Count == 0 ||
            currentPathIndex >= currentPath.Count)
        {
            hunter.StopMovement();

            if (Time.time >= nextPathRetryTime)
            {
                TryCreateExitPath();
            }

            return;
        }

        FollowPath();
    }

    private void TryCreateExitPath()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        nextPathRetryTime = Time.time + 1f;

        if (hunter.NavigationGraph == null ||
            !hunter.NavigationGraph.IsReady ||
            hunter.ExitZoneRenderer == null)
        {
            Debug.LogError("[Hunter Exit] Navigation Graph or Exit Zone is missing.");
            return;
        }

        // Find actual walkable graph nodes INSIDE the Exit floor.
        // Compare candidate paths to avoid choosing an unreachable border node.
        Bounds area = hunter.ExitZoneRenderer.bounds;
        List<NavigationNode> best = null;
        float bestLength = float.PositiveInfinity;
        HashSet<NavigationNode> seen = new();

        const int SamplesPerAxis = 5;
        for (int x = 0; x < SamplesPerAxis; x++)
        {
            for (int z = 0; z < SamplesPerAxis; z++)
            {
                Vector3 sample = new Vector3(
                    Mathf.Lerp(area.min.x, area.max.x, (x + 0.5f) / SamplesPerAxis),
                    hunter.transform.position.y,
                    Mathf.Lerp(area.min.z, area.max.z, (z + 0.5f) / SamplesPerAxis)
                );

                NavigationNode destination =
                    hunter.NavigationGraph.GetClosestWalkableNode(sample, false);

                if (destination == null ||
                    !destination.Walkable ||
                    !hunter.IsInsideExitZone(destination.Position) ||
                    !seen.Add(destination))
                {
                    continue;
                }

                List<NavigationNode> candidate =
                    AStarPathfinder.FindPath(
                        hunter.NavigationGraph,
                        hunter.transform.position,
                        destination.Position,
                        false,
                        out int exploredCount
                    );

                if (candidate == null || candidate.Count == 0) continue;

                float length = 0f;
                Vector3 previous = hunter.transform.position;
                foreach (NavigationNode node in candidate)
                {
                    Vector3 delta = node.Position - previous;
                    delta.y = 0f;
                    length += delta.magnitude;
                    previous = node.Position;
                }

                if (length < bestLength)
                {
                    bestLength = length;
                    best = candidate;
                }
            }
        }

        if (best == null)
        {
            Debug.LogWarning(
                "[Hunter Exit] No reachable A* node inside Zone_Exit. " +
                "Check the Exit floor Renderer and NavigationGraph."
            );
            return;
        }

        currentPath.AddRange(best);
        Debug.Log($"[Hunter Exit] A* path found: {currentPath.Count} nodes.");
    }

    private void FollowPath()
    {
        float reachSqr =
            hunter.PathNodeReachDistance * hunter.PathNodeReachDistance;

        while (currentPathIndex < currentPath.Count)
        {
            NavigationNode node = currentPath[currentPathIndex];
            Vector3 destination = new Vector3(
                node.Position.x,
                hunter.transform.position.y,
                node.Position.z
            );

            Vector3 difference = destination - hunter.transform.position;
            difference.y = 0f;

            if (difference.sqrMagnitude <= reachSqr)
            {
                currentPathIndex++;
                continue;
            }

            hunter.MoveAlongPath(destination);
            return;
        }

        hunter.StopMovement();
    }

    public override void Exit()
    {
        hunter.StopMovement();
        currentPath.Clear();
        currentPathIndex = 0;
    }
}
