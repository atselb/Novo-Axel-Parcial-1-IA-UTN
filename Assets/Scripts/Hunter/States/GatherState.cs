using UnityEngine;

public class GatherState : HunterState
{
    public GatherState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        Debug.Log("Hunter entered Gather state.");
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        Debug.Log("Hunter exited Gather state.");
    }
}