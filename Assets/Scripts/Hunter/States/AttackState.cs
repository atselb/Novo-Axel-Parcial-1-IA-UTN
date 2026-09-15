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
            hunter.ClearTarget();
            hunter.ChangeState(hunter.PatrolState);
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

    public override void Exit()
    {
        Debug.Log("Hunter exited Attack state.");
    }

    private void PerformMeleeAttack(BoidAgent target)
    {
        Debug.Log($"Hunter performs melee attakc on {target.name}");
    }

    private void PerformRangeAttack(BoidAgent target)
    {
        Debug.Log($"Hunter performs ranged attack on {target.name}");
    }
}