
using System.Collections.Generic;
using UnityEngine;

public class NavigationPathTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavigationGraph graph;

    [Header("Zones")]
    [SerializeField] private Transform startZone;
    [SerializeField] private Transform huntingGround;
    [SerializeField] private Transform safeZone01;
    [SerializeField] private Transform safeZone02;
    [SerializeField] private Transform exitZone;

    [Header("Visualization")]
    [SerializeField, Range(0, 4)]
    private int displayedRoute = 0;

    [SerializeField] private bool showScreenFeedback = true;

    private readonly List<NavigationNode>[] routes =
        new List<NavigationNode>[5];

    private readonly int[] exploredCounts = new int[5];

    private readonly string[] routeNames =
    {
        "Start -> Hunting Ground",
        "Hunting Ground -> Safe 01",
        "Hunting Ground -> Safe 02",
        "Safe 02 -> Exit",
        "Wolf: Start -> Exit"
    };

    private bool testsExecuted;

    private void Start()
    {
        RunTests();
    }

    [ContextMenu("Run Navigation Tests")]
    public void RunTests()
    {
        testsExecuted = false;

        if (graph == null || !graph.IsReady ||
            startZone == null ||
            huntingGround == null ||
            safeZone01 == null ||
            safeZone02 == null ||
            exitZone == null)
        {
            Debug.LogError(
                "NavigationPathTester: Missing references.",
                this
            );
            return;
        }

        routes[0] = AStarPathfinder.FindPath(
            graph,
            startZone.position,
            huntingGround.position,
            false,
            out exploredCounts[0]
        );

        routes[1] = AStarPathfinder.FindPath(
            graph,
            huntingGround.position,
            safeZone01.position,
            false,
            out exploredCounts[1]
        );

        routes[2] = AStarPathfinder.FindPath(
            graph,
            huntingGround.position,
            safeZone02.position,
            false,
            out exploredCounts[2]
        );

        routes[3] = AStarPathfinder.FindPath(
            graph,
            safeZone02.position,
            exitZone.position,
            false,
            out exploredCounts[3]
        );

        routes[4] = AStarPathfinder.FindPath(
            graph,
            startZone.position,
            exitZone.position,
            true,
            out exploredCounts[4]
        );

        testsExecuted = true;

        for (int i = 0; i < routes.Length; i++)
        {
            bool found = routes[i] != null &&
                         routes[i].Count > 0;

            Debug.Log(
                $"[A*] {routeNames[i]} | " +
                $"Path: {(found ? "FOUND" : "NOT FOUND")} | " +
                $"Nodes: {(found ? routes[i].Count : 0)} | " +
                $"Explored: {exploredCounts[i]}",
                this
            );
        }
    }

    private void OnDrawGizmos()
    {
        if (!testsExecuted) return;

        List<NavigationNode> path = routes[displayedRoute];

        if (path == null || path.Count == 0) return;

        Color[] colors =
        {
            Color.cyan,
            Color.magenta,
            Color.yellow,
            Color.green,
            Color.blue
        };

        Gizmos.color = colors[displayedRoute];

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position =
                path[i].Position + Vector3.up * 0.5f;

            Gizmos.DrawSphere(position, 0.3f);

            if (i == 0) continue;

            Vector3 previous =
                path[i - 1].Position + Vector3.up * 0.5f;

            Gizmos.DrawLine(previous, position);
        }
    }

    private void OnGUI()
    {
        if (!showScreenFeedback || !testsExecuted)
        {
            return;
        }

        Rect panel = new Rect(
            Mathf.Max(10f, Screen.width - 430f),
            10f,
            420f,
            175f
        );

        GUILayout.BeginArea(panel, GUI.skin.box);

        GUILayout.Label("A* NAVIGATION TESTS");

        int successful = 0;

        for (int i = 0; i < routes.Length; i++)
        {
            bool found = routes[i] != null &&
                         routes[i].Count > 0;

            if (found) successful++;

            GUILayout.Label(
                $"{routeNames[i]}: " +
                $"{(found ? "OK" : "FAILED")}"
            );
        }

        GUILayout.Label(
            $"RESULT: {successful}/{routes.Length} routes"
        );

        GUILayout.EndArea();
    }
}
