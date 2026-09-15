using System.ComponentModel;
using UnityEngine;

public class GatherState : HunterState
{
    private float gatherTimer;

    public GatherState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        gatherTimer = 0f;

        Debug.Log("Hunter entered Gather state.");
    }

    public override void Update()
    {
        BoidAgent target = hunter.CurrentTarget;

        if (!IsTargetAvailable(target))
        {
            ReturnToPatrol();
            return;
        }

        float distance = Vector3.Distance(hunter.transform.position, target.transform.position);

        if (distance > hunter.GatherRadius)
        {
            gatherTimer = 0f;

            hunter.MoveTowards(target.transform.position);
            
            return;
        }

        UpdateGathering(target);
    }


    public override void Exit()
    {
        Debug.Log("Hunter exited Gather state.");
    }

    private void UpdateGathering(BoidAgent target)
    {
        gatherTimer += Time.deltaTime;

        if (gatherTimer < hunter.GatherDuration) return;

        target.Collect();

        hunter.ClearTarget();
        hunter.ChangeState(hunter.PatrolState);
    }

    private bool IsTargetAvailable(BoidAgent target)
    {
        if (target == null) return false;

        if (target.IsActive) return false;

        if (target.IsCollected) return false;

        return true; 
    }

    private void ReturnToPatrol()
    {
        hunter.ClearTarget();
        hunter.ChangeState(hunter.PatrolState);
    }
}