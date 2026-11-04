
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HunterAgent))]
public class HunterPathNavigator : MonoBehaviour
{
    [Header("Obstacle Detection")]
    [SerializeField] private Transform obstaclesRoot;
    [SerializeField, Min(0f)] private float visionHeight = 0.7f;

    [Header("Pathfinding")]
    [SerializeField, Min(0.1f)]
    private float repathInterval = 0.4f;

    [SerializeField, Min(0.1f)]
    private float targetMovementThreshold = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawPath = true;

    private HunterAgent hunter;
    private Collider[] obstacleColliders;

    private readonly List<NavigationNode> currentPath = new();

    private int currentPathIndex;
    private Vector3 lastTargetPosition;
    private float nextRepathTime;
    private bool hasPlannedTarget;

    public IReadOnlyList<NavigationNode> CurrentPath => currentPath;

    private void Awake()
    {
        hunter = GetComponent<HunterAgent>();

        if (obstaclesRoot == null)
        {
            Debug.LogError(
                "HunterPathNavigator: Obstacles Root is missing.",
                this
            );

            return;
        }

        obstacleColliders =
            obstaclesRoot.GetComponentsInChildren<Collider>(true);

        if (obstacleColliders.Length == 0)
        {
            Debug.LogError(
                "HunterPathNavigator: No obstacle colliders found.",
                this
            );
        }
    }

    public bool HasLineOfSight(Vector3 targetPosition)
    {
        if (hunter == null ||
            obstacleColliders == null ||
            obstacleColliders.Length == 0)
        {
            return false;
        }

        Vector3 origin =
            hunter.transform.position + Vector3.up * visionHeight;

        Vector3 destination =
            targetPosition + Vector3.up * visionHeight;

        Vector3 direction = destination - origin;

        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            return true;
        }

        Ray ray = new Ray(
            origin,
            direction / distance
        );

        foreach (Collider obstacle in obstacleColliders)
        {
            if (obstacle == null ||
                !obstacle.enabled ||
                obstacle.isTrigger ||
                !obstacle.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (obstacle.Raycast(
                ray,
                out RaycastHit hit,
                distance))
            {
                return false;
            }
        }

        return true;
    }

    public void MoveTo(
        Vector3 targetPosition,
        float directApproachDistance)
    {
        if (hunter == null ||
            hunter.NavigationGraph == null ||
            !hunter.NavigationGraph.IsReady ||
            obstacleColliders == null ||
            obstacleColliders.Length == 0)
        {
            hunter?.StopMovement();
            return;
        }

        Vector3 difference =
            targetPosition - hunter.transform.position;

        difference.y = 0f;

        float distance = difference.magnitude;

        if (distance <= directApproachDistance &&
            HasLineOfSight(targetPosition))
        {
            ClearPath();
            hunter.MoveTowards(targetPosition);
            return;
        }

        UpdatePathIfNeeded(targetPosition);
        FollowPath();
    }

    private void UpdatePathIfNeeded(Vector3 targetPosition)
    {
        if (Time.time < nextRepathTime)
        {
            return;
        }

        Vector3 targetDifference =
            targetPosition - lastTargetPosition;

        targetDifference.y = 0f;

        bool targetMoved =
            !hasPlannedTarget ||
            targetDifference.sqrMagnitude >=
            targetMovementThreshold * targetMovementThreshold;

        bool pathFinished =
            currentPathIndex >= currentPath.Count;

        if (!targetMoved && !pathFinished)
        {
            return;
        }

        nextRepathTime = Time.time + repathInterval;

        lastTargetPosition = targetPosition;
        hasPlannedTarget = true;

        List<NavigationNode> calculatedPath =
            AStarPathfinder.FindPath(
                hunter.NavigationGraph,
                hunter.transform.position,
                targetPosition,
                false,
                out int exploredCount
            );

        currentPath.Clear();
        currentPathIndex = 0;

        if (calculatedPath == null ||
            calculatedPath.Count == 0)
        {
            return;
        }

        currentPath.AddRange(calculatedPath);
    }

    private void FollowPath()
    {
        if (currentPath.Count == 0)
        {
            hunter.StopMovement();
            return;
        }

        float reachDistance = Mathf.Max(
            0.15f,
            hunter.PathNodeReachDistance
        );

        float reachDistanceSqr =
            reachDistance * reachDistance;

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

            if (!HasLineOfSight(targetPosition))
            {
                currentPath.Clear();
                currentPathIndex = 0;

                nextRepathTime = Time.time + repathInterval;

                hunter.StopMovement();
                return;
            }

            hunter.MoveAlongPath(targetPosition);
            return;
        }

        hunter.StopMovement();
    }

    private void ClearPath()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        hasPlannedTarget = false;
        nextRepathTime = 0f;
    }

    public void ResetNavigation()
    {
        ClearPath();
    }

    public void StopNavigation()
    {
        ClearPath();

        if (hunter != null)
        {
            hunter.StopMovement();
        }
    }

    private void OnValidate()
    {
        visionHeight = Mathf.Max(0f, visionHeight);
        repathInterval = Mathf.Max(0.1f, repathInterval);

        targetMovementThreshold = Mathf.Max(
            0.1f,
            targetMovementThreshold
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPath || currentPath.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Vector3 previous =
            transform.position + Vector3.up * 1.2f;

        for (int i = currentPathIndex;
             i < currentPath.Count;
             i++)
        {
            Vector3 current =
                currentPath[i].Position + Vector3.up * 1.2f;

            Gizmos.DrawSphere(current, 0.2f);
            Gizmos.DrawLine(previous, current);

            previous = current;
        }
    }
}
