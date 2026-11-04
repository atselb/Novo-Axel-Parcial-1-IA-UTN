
using UnityEngine;

public class DeadState : HunterState
{
    private float deathStartTime;

    public float RemainingTime =>
        Mathf.Max(
            0f,
            hunter.RespawnTime - (Time.time - deathStartTime)
        );

    public DeadState(HunterAgent hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        deathStartTime = Time.time;

        hunter.StopMovement();
        hunter.ClearTarget();

        HunterPathNavigator navigator =
            hunter.GetComponent<HunterPathNavigator>();

        if (navigator != null)
        {
            navigator.StopNavigation();
        }

        hunter.SetDeathPresentation(false);
    }

    public override void Update()
    {
        hunter.StopMovement();

        if (Time.time - deathStartTime < hunter.RespawnTime)
        {
            return;
        }

        hunter.CompleteDeathRespawn();
    }

    public override void Exit()
    {
        hunter.StopMovement();
        hunter.SetDeathPresentation(true);
    }
}
