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
            Debug.LogError("Hunter Gather: HunterPathNavigator is missing.", hunter);
            hunter.StopMovement();
            return;
        }

        navigator.ResetNavigation();
        Debug.Log("Hunter entered Gather state.");
    }

    public override void Update()
    {
        // Flee has priority over Gather and cancels any partial gathering.
        if (hunter.TryEnterFlee()) return;

        BoidAgent target = hunter.CurrentTarget;
        if (!IsTargetAvailable(target))
        {
            ReturnToSearchOrExit();
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
        if (gatherTimer < hunter.GatherDuration) return;

        // Only a completed collection increases the objective counter.
        target.Collect();
        Debug.Log($"Hunter collected {target.name}");

        hunter.RegisterSuccessfulGather();
        hunter.ClearTarget();
        ReturnToSearchOrExit();
    }

    private bool IsTargetAvailable(BoidAgent target)
    {
        return target != null && !target.IsActive && !target.IsCollected;
    }

    private void ReturnToSearchOrExit()
    {
        hunter.ClearTarget();
        hunter.ChangeState(
            hunter.HasRequiredGathers ? (HunterState)hunter.ExitState : hunter.SearchState
        );
    }

    public override void Exit()
    {
        if (navigator != null) navigator.StopNavigation();
        gatherTimer = 0f;
        Debug.Log("Hunter exited Gather state.");
    }
}
