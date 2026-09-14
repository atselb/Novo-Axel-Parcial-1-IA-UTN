using UnityEngine;

public class PatrolState : HunterState
{
    public PatrolState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Debug.Log("Hunter entered Patrol state.");
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        Debug.Log("Hunter exited Patrol state.");
    }
}