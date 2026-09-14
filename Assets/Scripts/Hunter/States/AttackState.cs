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
        
    }

    public override void Exit()
    {
        Debug.Log("Hunter exited Attack state.");
    }
}