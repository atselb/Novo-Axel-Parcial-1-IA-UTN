using UnityEngine;

public class PatrolState : HunterState
{
    private int currentWaypointIndex;
    
    public PatrolState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Debug.Log("Hunter entered Patrol state.");
    }

    public override void Update()
    {
        Transform[] waypoints = hunter.Waypoints;

        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        hunter.MoveTowards(targetWaypoint.position);

        float distance = Vector3.Distance(hunter.transform.position, targetWaypoint.position);

        if (distance <= hunter.WaypointReachDistance)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
    }

    public override void Exit()
    {
        Debug.Log("Hunter exited Patrol state.");
    }
}