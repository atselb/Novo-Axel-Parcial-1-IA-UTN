
using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Renderer floorRenderer;
    [SerializeField] private Transform obstaclesRoot;
    [SerializeField] private Renderer safeZone01;
    [SerializeField] private Renderer safeZone02;

    [Header("Grid Settings")]
    [SerializeField, Min(0.5f)] private float cellSize = 2f;
    [SerializeField, Min(0f)] private float floorMargin = 1.5f;
    [SerializeField, Min(0f)] private float agentClearance = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool drawGrid = true;

    private NavigationNode[,] nodes;
    private Collider[] obstacleColliders;

    private int width;
    private int height;

    public float CellSize => cellSize;
    public bool IsReady => nodes != null;

    public int TotalNodes => width * height;

    private void Awake()
    {
        BuildGraph();
    }

    [ContextMenu("Rebuild Navigation Graph")]
    public void BuildGraph()
    {
        nodes = null;

        if (floorRenderer == null ||
            obstaclesRoot == null ||
            safeZone01 == null ||
            safeZone02 == null)
        {
            Debug.LogError(
                "NavigationGraph: Missing scene references.",
                this
            );
            return;
        }

        obstacleColliders =
            obstaclesRoot.GetComponentsInChildren<Collider>();

        if (obstacleColliders.Length == 0)
        {
            Debug.LogError(
                "NavigationGraph: No obstacle colliders found.",
                this
            );
            return;
        }

        Bounds floorBounds = floorRenderer.bounds;

        float availableWidth =
            floorBounds.size.x - floorMargin * 2f;

        float availableHeight =
            floorBounds.size.z - floorMargin * 2f;

        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            Debug.LogError(
                "NavigationGraph: Invalid floor dimensions.",
                this
            );
            return;
        }

        width = Mathf.FloorToInt(
            availableWidth / cellSize
        ) + 1;

        height = Mathf.FloorToInt(
            availableHeight / cellSize
        ) + 1;

        nodes = new NavigationNode[width, height];

        float startX = floorBounds.center.x -
            (width - 1) * cellSize * 0.5f;

        float startZ = floorBounds.center.z -
            (height - 1) * cellSize * 0.5f;

        float nodeY = floorBounds.max.y + 0.5f;

        int blockedCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 position = new Vector3(
                    startX + x * cellSize,
                    nodeY,
                    startZ + z * cellSize
                );

                bool walkable = !IsBlocked(position);
                bool safeZone = IsInsideSafeZone(position);

                nodes[x, z] = new NavigationNode(
                    x,
                    z,
                    position,
                    walkable,
                    safeZone
                );

                if (!walkable)
                {
                    blockedCount++;
                }
            }
        }

        Debug.Log(
            $"NavigationGraph generated: {TotalNodes} nodes, " +
            $"{blockedCount} blocked, " +
            $"{TotalNodes - blockedCount} walkable.",
            this
        );
    }

    private bool IsBlocked(Vector3 position)
    {
        foreach (Collider obstacle in obstacleColliders)
        {
            if (obstacle == null ||
                !obstacle.enabled ||
                obstacle.isTrigger ||
                !obstacle.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = obstacle.bounds;

            float closestX = Mathf.Clamp(
                position.x,
                bounds.min.x,
                bounds.max.x
            );

            float closestZ = Mathf.Clamp(
                position.z,
                bounds.min.z,
                bounds.max.z
            );

            float dx = position.x - closestX;
            float dz = position.z - closestZ;

            if (dx * dx + dz * dz <=
                agentClearance * agentClearance)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInsideSafeZone(Vector3 position)
    {
        return IsInsideBounds(position, safeZone01.bounds) ||
               IsInsideBounds(position, safeZone02.bounds);
    }

    private bool IsInsideBounds(Vector3 position, Bounds bounds)
    {
        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    public NavigationNode GetNode(int x, int z)
    {
        if (!IsReady) return null;

        if (x < 0 || x >= width ||
            z < 0 || z >= height)
        {
            return null;
        }

        return nodes[x, z];
    }

    public IEnumerable<NavigationNode> GetNeighbors(
        NavigationNode current,
        bool excludeSafeZones = false)
    {
        if (current == null) yield break;

        NavigationNode[] candidates =
        {
            GetNode(current.X + 1, current.Z),
            GetNode(current.X - 1, current.Z),
            GetNode(current.X, current.Z + 1),
            GetNode(current.X, current.Z - 1)
        };

        foreach (NavigationNode neighbor in candidates)
        {
            if (neighbor == null || !neighbor.Walkable)
            {
                continue;
            }

            if (excludeSafeZones && neighbor.IsSafeZone)
            {
                continue;
            }

            yield return neighbor;
        }
    }

    public NavigationNode GetClosestWalkableNode(
        Vector3 position,
        bool excludeSafeZones = false)
    {
        if (!IsReady) return null;

        NavigationNode closest = null;
        float closestDistance = float.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                NavigationNode node = nodes[x, z];

                if (!node.Walkable) continue;

                if (excludeSafeZones && node.IsSafeZone)
                {
                    continue;
                }

                float dx = node.Position.x - position.x;
                float dz = node.Position.z - position.z;
                float distance = dx * dx + dz * dz;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = node;
                }
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGrid || !IsReady) return;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                NavigationNode node = nodes[x, z];

                if (!node.Walkable)
                {
                    Gizmos.color = Color.red;
                }
                else if (node.IsSafeZone)
                {
                    Gizmos.color = Color.magenta;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Gizmos.DrawSphere(
                    node.Position,
                    0.15f
                );
            }
        }
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.5f, cellSize);
        floorMargin = Mathf.Max(0f, floorMargin);
        agentClearance = Mathf.Max(0f, agentClearance);
    }
}
