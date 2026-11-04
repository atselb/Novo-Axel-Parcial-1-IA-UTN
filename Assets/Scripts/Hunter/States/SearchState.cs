
using System.Collections.Generic;
using UnityEngine;

public class SearchState : HunterState
{
    private readonly List<NavigationNode> currentPath = new();

    private int currentPathIndex;
    private float interestSpawnTimer;
    private bool reachedHuntingGround;

    private const int MaxSearchAttempts = 20;

    public IReadOnlyList<NavigationNode> CurrentPath => currentPath;
    public int CurrentPathIndex => currentPathIndex;

    public SearchState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Debug.Log("Hunter entered Search state.");

        currentPath.Clear();
        currentPathIndex = 0;

        reachedHuntingGround =
            hunter.IsInsideHuntingGround(
                hunter.transform.position
            );

        if (reachedHuntingGround)
        {
            TryCreateSearchPath();
        }
        else
        {
            TryCreatePath(
                hunter.HuntingGroundRenderer.bounds.center
            );
        }
    }

    public override void Update()
    {
        // Flee has priority over every Search action.
        if (hunter.TryEnterFlee())
        {
            return;
        }

        if (hunter.NavigationGraph == null ||
            !hunter.NavigationGraph.IsReady)
        {
            hunter.StopMovement();
            return;
        }

        if (hunter.IsInsideHuntingGround(
            hunter.transform.position))
        {
            reachedHuntingGround = true;
        }

        if (reachedHuntingGround)
        {
            // Preserve Parcial 1 priorities:
            // Gather before Attack.
            if (TryEnterGather())
            {
                return;
            }

            if (TryEnterAttack())
            {
                return;
            }

            UpdateInterestObjectSpawn();
        }

        if (currentPath.Count == 0 ||
            currentPathIndex >= currentPath.Count)
        {
            if (reachedHuntingGround)
            {
                if (!TryCreateSearchPath())
                {
                    hunter.StopMovement();
                }
            }
            else
            {
                if (!TryCreatePath(
                    hunter.HuntingGroundRenderer.bounds.center))
                {
                    hunter.StopMovement();
                }
            }

            return;
        }

        FollowPath();
    }

    private void FollowPath()
    {
        float reachDistanceSqr =
            hunter.PathNodeReachDistance *
            hunter.PathNodeReachDistance;

        while (currentPathIndex < currentPath.Count)
        {
            Vector3 targetPosition =
                currentPath[currentPathIndex].Position;

            Vector3 difference =
                targetPosition - hunter.transform.position;

            difference.y = 0f;

            if (difference.sqrMagnitude <= reachDistanceSqr)
            {
                currentPathIndex++;
                continue;
            }

            hunter.MoveAlongPath(targetPosition);
            return;
        }

        hunter.StopMovement();
        currentPath.Clear();
        currentPathIndex = 0;
    }

    private bool TryCreatePath(Vector3 destination)
    {
        if (hunter.NavigationGraph == null ||
            !hunter.NavigationGraph.IsReady)
        {
            return false;
        }

        List<NavigationNode> calculatedPath =
            AStarPathfinder.FindPath(
                hunter.NavigationGraph,
                hunter.transform.position,
                destination,
                false,
                out int exploredCount
            );

        if (calculatedPath == null ||
            calculatedPath.Count == 0)
        {
            return false;
        }

        currentPath.Clear();
        currentPath.AddRange(calculatedPath);
        currentPathIndex = 0;

        Debug.Log(
            $"Hunter A*: {currentPath.Count} nodes, " +
            $"{exploredCount} explored."
        );

        return true;
    }

    private bool TryCreateSearchPath()
    {
        Bounds bounds =
            hunter.HuntingGroundRenderer.bounds;

        const float margin = 1.5f;

        float minX = bounds.min.x + margin;
        float maxX = bounds.max.x - margin;
        float minZ = bounds.min.z + margin;
        float maxZ = bounds.max.z - margin;

        if (minX >= maxX || minZ >= maxZ)
        {
            return false;
        }

        for (int attempt = 0;
             attempt < MaxSearchAttempts;
             attempt++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(minX, maxX),
                hunter.transform.position.y,
                Random.Range(minZ, maxZ)
            );

            NavigationNode destinationNode =
                hunter.NavigationGraph.GetClosestWalkableNode(
                    randomPosition,
                    false
                );

            if (destinationNode == null)
            {
                continue;
            }

            if (!hunter.IsInsideHuntingGround(
                destinationNode.Position))
            {
                continue;
            }

            Vector3 difference =
                destinationNode.Position -
                hunter.transform.position;

            difference.y = 0f;

            if (difference.sqrMagnitude < 9f)
            {
                continue;
            }

            if (TryCreatePath(destinationNode.Position))
            {
                return true;
            }
        }

        Debug.LogWarning(
            "Hunter Search: No reachable hunting " +
            "ground node found."
        );

        return false;
    }

    private bool TryEnterGather()
    {
        if (!hunter.Perception.HasEliminatedBoids)
        {
            return false;
        }

        BoidAgent target =
            hunter.Perception.GetClosestEliminatedBoid();

        if (target == null)
        {
            return false;
        }

        hunter.SetTarget(target);
        hunter.ChangeState(hunter.GatherState);

        return true;
    }

    private bool TryEnterAttack()
    {
        if (!hunter.IsAttackReady)
        {
            return false;
        }

        if (!hunter.Perception.HasAliveBoids)
        {
            return false;
        }

        BoidAgent target =
            hunter.Perception.GetClosestAliveBoid();

        if (target == null)
        {
            return false;
        }

        hunter.SetTarget(target);
        hunter.ChangeState(hunter.AttackState);

        return true;
    }

    private void UpdateInterestObjectSpawn()
    {
        interestSpawnTimer += Time.deltaTime;

        if (interestSpawnTimer <
            hunter.InterestSpawnInterval)
        {
            return;
        }

        interestSpawnTimer = 0f;

        if (hunter.GetActiveInterestObjectCount() >=
            hunter.MaxActiveInterestObjects)
        {
            return;
        }

        hunter.SpawnInterestObject();
    }

    public override void Exit()
    {
        hunter.StopMovement();

        currentPath.Clear();
        currentPathIndex = 0;

        Debug.Log("Hunter exited Search state.");
    }
}
