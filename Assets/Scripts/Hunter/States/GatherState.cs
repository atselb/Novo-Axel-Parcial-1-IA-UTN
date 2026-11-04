
using UnityEngine;

public class GatherState : HunterState
{
    private HunterPathNavigator navigator;
    private float gatherTimer;

    public GatherState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        gatherTimer = 0f;

        navigator = hunter.GetComponent<HunterPathNavigator>();

        if (navigator == null)
        {
            Debug.LogError(
                "Hunter Gather: HunterPathNavigator component is missing.",
                hunter
            );

            hunter.StopMovement();
            return;
        }

        navigator.ResetNavigation();

        Debug.Log("Hunter entered Gather state.");
    }

    public override void Update()
    {
        BoidAgent target = hunter.CurrentTarget;

        if (!IsTargetAvailable(target))
        {
            ReturnToSearch();
            return;
        }

        if (navigator == null)
        {
            hunter.StopMovement();
            return;
        }

        float distance = Vector3.Distance(
            hunter.transform.position,
            target.transform.position
        );

        bool hasLineOfSight =
            navigator.HasLineOfSight(target.transform.position);

        if (distance <= hunter.GatherRadius && hasLineOfSight)
        {
            hunter.StopMovement();
            UpdateGathering(target);
            return;
        }

        gatherTimer = 0f;

        navigator.MoveTo(
            target.transform.position,
            hunter.GatherRadius + 2f
        );
    }

    private void UpdateGathering(BoidAgent target)
    {
        gatherTimer += Time.deltaTime;

        if (gatherTimer < hunter.GatherDuration)
        {
            return;
        }

        target.Collect();

        Debug.Log($"Hunter collected {target.name}");

        hunter.ClearTarget();
        hunter.ChangeState(hunter.SearchState);
    }

    private bool IsTargetAvailable(BoidAgent target)
    {
        if (target == null) return false;
        if (target.IsActive) return false;
        if (target.IsCollected) return false;

        return true;
    }

    private void ReturnToSearch()
    {
        hunter.ClearTarget();
        hunter.ChangeState(hunter.SearchState);
    }

    public override void Exit()
    {
        if (navigator != null)
        {
            navigator.StopNavigation();
        }

        gatherTimer = 0f;

        Debug.Log("Hunter exited Gather state.");
    }
}
