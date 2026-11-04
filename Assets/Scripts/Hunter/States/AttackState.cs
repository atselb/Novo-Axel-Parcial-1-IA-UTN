
using UnityEngine;

public class AttackState : HunterState
{
    private HunterPathNavigator navigator;

    public AttackState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        navigator =
            hunter.GetComponent<HunterPathNavigator>();

        if (navigator == null)
        {
            Debug.LogError(
                "Hunter Attack: HunterPathNavigator " +
                "component is missing.",
                hunter
            );

            hunter.StopMovement();
            return;
        }

        navigator.ResetNavigation();

        Debug.Log("Hunter entered Attack state.");
    }

    public override void Update()
    {
        // Detecting a wolf interrupts the attack.
        if (hunter.TryEnterFlee())
        {
            return;
        }

        BoidAgent target = hunter.CurrentTarget;

        if (target == null || !target.IsActive)
        {
            ReturnToSearch();
            return;
        }

        if (!IsTargetStillPerceived(target))
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
            navigator.HasLineOfSight(
                target.transform.position
            );

        if (distance <= hunter.MeleeAttackRadius &&
            hasLineOfSight)
        {
            PerformMeleeAttack(target);
            return;
        }

        if (distance <= hunter.RangeAttackRadius &&
            hasLineOfSight)
        {
            PerformRangeAttack(target);
            return;
        }

        navigator.MoveTo(
            target.transform.position,
            hunter.RangeAttackRadius + 1f
        );
    }

    private bool IsTargetStillPerceived(BoidAgent target)
    {
        var perceivedBoids =
            hunter.Perception.PerceivedAliveBoids;

        for (int i = 0; i < perceivedBoids.Count; i++)
        {
            if (perceivedBoids[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private void PerformMeleeAttack(BoidAgent target)
    {
        target.TakeDamage(hunter.MeleeDamage);

        Debug.Log(
            $"Hunter performs melee attack on {target.name}"
        );

        hunter.RegisterSuccessfulAttack();
        ReturnToSearch();
    }

    private void PerformRangeAttack(BoidAgent target)
    {
        target.TakeDamage(hunter.RangeDamage);

        Debug.Log(
            $"Hunter performs ranged attack on {target.name}"
        );

        hunter.RegisterSuccessfulAttack();
        ReturnToSearch();
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

        Debug.Log("Hunter exited Attack state.");
    }
}
