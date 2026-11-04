
using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    private struct QueueEntry
    {
        public NavigationNode Node;
        public float Priority;

        public QueueEntry(NavigationNode node, float priority)
        {
            Node = node;
            Priority = priority;
        }
    }

    private class MinHeap
    {
        private readonly List<QueueEntry> heap = new();

        public int Count => heap.Count;

        public void Enqueue(NavigationNode node, float priority)
        {
            heap.Add(new QueueEntry(node, priority));

            int index = heap.Count - 1;

            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (heap[parent].Priority <= heap[index].Priority)
                {
                    break;
                }

                (heap[parent], heap[index]) =
                    (heap[index], heap[parent]);

                index = parent;
            }
        }

        public NavigationNode Dequeue()
        {
            NavigationNode result = heap[0].Node;

            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int index = 0;

            while (index < heap.Count)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left < heap.Count &&
                    heap[left].Priority < heap[smallest].Priority)
                {
                    smallest = left;
                }

                if (right < heap.Count &&
                    heap[right].Priority < heap[smallest].Priority)
                {
                    smallest = right;
                }

                if (smallest == index) break;

                (heap[index], heap[smallest]) =
                    (heap[smallest], heap[index]);

                index = smallest;
            }

            return result;
        }
    }

    public static List<NavigationNode> FindPath(
        NavigationGraph graph,
        Vector3 startPosition,
        Vector3 targetPosition,
        bool excludeSafeZones,
        out int exploredCount)
    {
        exploredCount = 0;

        List<NavigationNode> emptyPath = new();

        if (graph == null || !graph.IsReady)
        {
            return emptyPath;
        }

        NavigationNode start = graph.GetClosestWalkableNode(
            startPosition,
            excludeSafeZones
        );

        NavigationNode goal = graph.GetClosestWalkableNode(
            targetPosition,
            excludeSafeZones
        );

        if (start == null || goal == null)
        {
            return emptyPath;
        }

        MinHeap openSet = new();

        Dictionary<NavigationNode, NavigationNode> cameFrom = new();
        Dictionary<NavigationNode, float> gScore = new();
        HashSet<NavigationNode> closedSet = new();

        gScore[start] = 0f;
        openSet.Enqueue(start, Heuristic(start, goal));

        while (openSet.Count > 0)
        {
            NavigationNode current = openSet.Dequeue();

            if (!closedSet.Add(current))
            {
                continue;
            }

            exploredCount++;

            if (current == goal)
            {
                return ReconstructPath(
                    cameFrom,
                    current
                );
            }

            foreach (NavigationNode neighbor in
                graph.GetNeighbors(current, excludeSafeZones))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                float tentativeG =
                    gScore[current] + graph.CellSize;

                if (gScore.TryGetValue(
                    neighbor,
                    out float previousG) &&
                    tentativeG >= previousG)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;

                float h = Heuristic(neighbor, goal);
                float f = tentativeG + h;

                openSet.Enqueue(neighbor, f);
            }
        }

        return emptyPath;
    }

    private static float Heuristic(
        NavigationNode from,
        NavigationNode to)
    {
        return Mathf.Abs(from.Position.x - to.Position.x) +
               Mathf.Abs(from.Position.z - to.Position.z);
    }

    private static List<NavigationNode> ReconstructPath(
        Dictionary<NavigationNode, NavigationNode> cameFrom,
        NavigationNode current)
    {
        List<NavigationNode> path = new()
        {
            current
        };

        while (cameFrom.TryGetValue(
            current,
            out NavigationNode previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
