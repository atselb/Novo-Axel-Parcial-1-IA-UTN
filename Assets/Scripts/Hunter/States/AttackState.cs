using System.Linq;
using UnityEngine;

public class AttackState : HunterState
{
    public AttackState(HunterAgent hunter) : base(hunter){  }

    public override void Enter()
    {
        Debug.Log("Hunter entered Attack state.");
    }

    public override void Update()
    {
        BoidAgent target = hunter.CurrentTarget;

        if (target == null)
        {
            ReturnToPatrol();
            return;
        }

        if (!IsTargetStillPerceived(target))
        {
            ReturnToPatrol();
            return;
        }

        float distance = Vector3.Distance(hunter.transform.position, target.transform.position);

        if (distance <= hunter.MeleeAttackRadius)
        {
            PerformMeleeAttack(target);
            return;
        }

        if (distance <= hunter.RangeAttackRadius)
        {
            PerformRangeAttack(target);
            return;
        }

        hunter.MoveTowards(target.transform.position);
    }

    private bool IsTargetStillPerceived(BoidAgent target)
    {
        return hunter.Perception.PerceivedAliveBoids.Contains(target);
    }

    private void ReturnToPatrol()
    {
        hunter.ClearTarget();
        hunter.ChangeState(hunter.PatrolState);
    }

    public override void Exit()
    {
        Debug.Log("Hunter exited Attack state.");
    }

    private void PerformMeleeAttack(BoidAgent target)
    {
        Debug.Log($"Hunter performs melee attakc on {target.name}");

        hunter.RegisterSuccessfulAttack();
        ReturnToPatrol();
    }

    private void PerformRangeAttack(BoidAgent target)
    {
        Debug.Log($"Hunter performs ranged attack on {target.name}");

        hunter.RegisterSuccessfulAttack();
        ReturnToPatrol();
    }
}