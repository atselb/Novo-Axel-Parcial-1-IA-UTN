
using System.Collections.Generic;
using UnityEngine;

public class HunterFleeContext : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavigationGraph navigationGraph;

    [Header("Wolves")]
    [SerializeField] private WolfAgent[] wolves;
    [SerializeField, Min(0.1f)]
    private float wolfDetectionRadius = 12f;
    [SerializeField, Min(0f)]
    private float eyeHeight = 0.6f;

    [Header("Safe Zones")]
    [SerializeField] private Renderer safeZone01;
    [SerializeField] private Renderer safeZone02;
    [SerializeField, Min(0f)]
    private float safeZoneWaitTime = 3f;

    [Header("Obstacles")]
    [SerializeField] private Transform obstaclesRoot;

    [Header("Debug")]
    [SerializeField] private bool drawDetectionRadius = true;

    private Collider[] obstacleColliders;

    public float SafeZoneWaitTime => safeZoneWaitTime;

    public bool IsReady =>
        navigationGraph != null &&
        navigationGraph.IsReady &&
        wolves != null &&
        wolves.Length >= 4 &&
        safeZone01 != null &&
        safeZone02 != null &&
        obstacleColliders != null &&
        obstacleColliders.Length > 0;

    private void Awake()
    {
        if (obstaclesRoot != null)
        {
            obstacleColliders =
                obstaclesRoot.GetComponentsInChildren<Collider>(true);
        }
    }

    public bool CanSeeWolf()
    {
        if (!IsReady ||
            IsInsideSafeZone(transform.position))
        {
            return false;
        }

        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        foreach (WolfAgent wolf in wolves)
        {
            if (wolf == null || !wolf.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 target =
                wolf.transform.position + Vector3.up * eyeHeight;

            Vector3 difference = target - origin;
            float distance = difference.magnitude;

            if (distance <= 0.001f ||
                distance > wolfDetectionRadius)
            {
                continue;
            }

            Ray ray = new Ray(
                origin,
                difference / distance
            );

            bool blocked = false;

            foreach (Collider obstacle in obstacleColliders)
            {
                if (obstacle == null ||
                    !obstacle.enabled ||
                    !obstacle.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (obstacle.Raycast(
                    ray,
                    out RaycastHit hit,
                    distance
                ))
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsInsideSafeZone(Vector3 position)
    {
        return IsInsideZone(safeZone01, position) ||
               IsInsideZone(safeZone02, position);
    }

    private static bool IsInsideZone(
        Renderer zone,
        Vector3 position)
    {
        if (zone == null)
        {
            return false;
        }

        Bounds bounds = zone.bounds;

        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    public bool TryFindShortestSafeZonePath(
        Vector3 start,
        out List<NavigationNode> bestPath,
        out string selectedZone)
    {
        bestPath = null;
        selectedZone = string.Empty;

        if (!IsReady)
        {
            return false;
        }

        float shortestLength =
            float.PositiveInfinity;

        Renderer[] zones =
        {
            safeZone01,
            safeZone02
        };

        foreach (Renderer zone in zones)
        {
            NavigationNode destination =
                navigationGraph.GetClosestWalkableNode(
                    zone.bounds.center,
                    false
                );

            if (destination == null ||
                !destination.Walkable ||
                !IsInsideZone(zone, destination.Position))
            {
                continue;
            }

            // false allows the Hunter to enter
            // nodes belonging to Safe Zones.
            List<NavigationNode> path =
                AStarPathfinder.FindPath(
                    navigationGraph,
                    start,
                    destination.Position,
                    false,
                    out int exploredCount
                );

            if (path == null || path.Count == 0)
            {
                continue;
            }

            float pathLength = 0f;
            Vector3 previous = start;

            foreach (NavigationNode node in path)
            {
                Vector3 difference =
                    node.Position - previous;

                difference.y = 0f;
                pathLength += difference.magnitude;

                previous = node.Position;
            }

            if (pathLength < shortestLength)
            {
                shortestLength = pathLength;
                bestPath = path;
                selectedZone = zone.name;
            }
        }

        return bestPath != null;
    }

    private void OnValidate()
    {
        wolfDetectionRadius =
            Mathf.Max(0.1f, wolfDetectionRadius);

        eyeHeight = Mathf.Max(0f, eyeHeight);
        safeZoneWaitTime = Mathf.Max(0f, safeZoneWaitTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDetectionRadius)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            wolfDetectionRadius
        );
    }
}
