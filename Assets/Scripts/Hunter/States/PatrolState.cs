using UnityEngine;

public class PatrolState : HunterState
{
    private int currentWaypointIndex;
    private float interestSpawnTimer;
    
    public PatrolState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Debug.Log("Hunter entered Patrol state.");
        interestSpawnTimer = 0f;
    }

    public override void Update()
    {
        if (TryEnterGather()) return;

        if (TryEnterAttack()) return;

        UpdatePatrol();
        UpdateInterestObjectSpawn();
    }

    private void UpdatePatrol()
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

    private void UpdateInterestObjectSpawn()
    {
        interestSpawnTimer += Time.deltaTime;

        if (interestSpawnTimer < hunter.InterestSpawnInterval) return;

        interestSpawnTimer = 0f;

        if (hunter.GetActiveInterestObjectCount() >= hunter.MaxActiveInterestObjects) return;

        hunter.SpawnInterestObject();
    }

    private bool TryEnterAttack()
    {
        if (!hunter.IsAttackReady) return false;

        if (!hunter.Perception.HasAliveBoids) return false;

        BoidAgent target = hunter.Perception.GetClosestAliveBoid();

        if (target == null) return false;

        hunter.SetTarget(target);
        hunter.ChangeState(hunter.AttackState);

        return true;
    }

    private bool TryEnterGather()
    {
        if (!hunter.Perception.HasEliminatedBoids) return false;

        BoidAgent target = hunter.Perception.GetClosestEliminatedBoid();

        if (target == null) return false;

        hunter.SetTarget(target);
        hunter.ChangeState(hunter.GatherState);

        return true;
    }

    public override void Exit()
    {
        Debug.Log("Hunter exited Patrol state.");
    }
}