using System.Collections.Generic;
using UnityEngine;

public class FleeState : HunterState
{
    private readonly HunterState searchState;
    private readonly HunterFleeContext context;

    private List<NavigationNode> path;
    private int pathIndex;
    private bool isWaiting;
    private float waitTimer;
    private float nextPathRetryTime;

    private const float NodeReachDistance = 0.25f;

    public FleeState(
        HunterAgent hunter,
        HunterState searchState,
        HunterFleeContext context
    ) : base(hunter)
    {
        this.searchState = searchState;
        this.context = context;
    }

    public override void Enter()
    {
        path = null;
        pathIndex = 0;
        isWaiting = false;
        waitTimer = 0f;
        nextPathRetryTime = 0f;

        hunter.StopMovement();
        hunter.ClearTarget();

        if (context == null || !context.IsReady)
        {
            Debug.LogError("[Hunter Flee] Missing navigation context.");
            return;
        }

        PlanEscapePath();
    }

    private void PlanEscapePath()
    {
        nextPathRetryTime = Time.time + 1f;

        if (!context.TryFindShortestSafeZonePath(
            hunter.transform.position,
            out path,
            out string selectedZone
        ))
        {
            path = null;
            Debug.LogWarning("[Hunter Flee] No A* path to any Safe Zone.");
            return;
        }

        pathIndex = 0;
        Debug.Log(
            $"[Hunter Flee] Wolf detected. Selected Safe Zone: {selectedZone}. " +
            $"A* nodes: {path.Count}."
        );
    }

    public override void Update()
    {
        if (context == null || !context.IsReady)
        {
            hunter.StopMovement();
            return;
        }

        if (context.IsInsideSafeZone(hunter.transform.position))
        {
            UpdateSafeZoneWait();
            return;
        }

        if (path == null)
        {
            hunter.StopMovement();
            if (Time.time >= nextPathRetryTime) PlanEscapePath();
            return;
        }

        FollowEscapePath();
    }

    private void FollowEscapePath()
    {
        float reachSqr = NodeReachDistance * NodeReachDistance;

        while (pathIndex < path.Count)
        {
            NavigationNode node = path[pathIndex];
            if (node == null || !node.Walkable)
            {
                path = null;
                hunter.StopMovement();
                return;
            }

            Vector3 destination = new Vector3(
                node.Position.x,
                hunter.transform.position.y,
                node.Position.z
            );

            Vector3 difference = destination - hunter.transform.position;
            difference.y = 0f;

            if (difference.sqrMagnitude <= reachSqr)
            {
                pathIndex++;
                hunter.StopMovement();
                continue;
            }

            hunter.MoveAlongPath(destination);
            return;
        }

        hunter.StopMovement();
        if (!context.IsInsideSafeZone(hunter.transform.position))
        {
            path = null;
            nextPathRetryTime = Time.time;
        }
    }

    private void UpdateSafeZoneWait()
    {
        hunter.StopMovement();

        if (!isWaiting)
        {
            isWaiting = true;
            waitTimer = 0f;
            Debug.Log("[Hunter Flee] Entered Safe Zone.");
        }

        waitTimer += Time.deltaTime;
        if (waitTimer < context.SafeZoneWaitTime) return;

        HunterState resumeState = hunter.HasRequiredGathers
            ? (HunterState)hunter.ExitState
            : searchState;

        Debug.Log(
            $"[Hunter Flee] Safe Zone wait completed. Returning to " +
            $"{(hunter.HasRequiredGathers ? "Exit" : "Search")}."
        );
        hunter.ChangeState(resumeState);
    }

    public override void Exit()
    {
        hunter.StopMovement();
        path = null;
        isWaiting = false;
    }
}
