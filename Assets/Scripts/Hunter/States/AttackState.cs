
using UnityEngine;

public class AttackState : HunterState
{
    private HunterPathNavigator navigator;
    private HunterCombatFeedback feedback;

    public AttackState(HunterAgent hunter) : base(hunter) { }

    public override void Enter()
    {
        navigator = hunter.GetComponent<HunterPathNavigator>();
        feedback = hunter.GetComponent<HunterCombatFeedback>();

        // Runtime fallback: the visual feedback works even if the
        // component was not yet added in the Inspector.
        if (feedback == null)
        {
            feedback = hunter.gameObject.AddComponent<HunterCombatFeedback>();
        }

        feedback.ClearPrediction();

        if (navigator == null)
        {
            Debug.LogError(
                "Hunter Attack: HunterPathNavigator component is missing.",
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
        // Flee must interrupt attacking immediately.
        if (hunter.TryEnterFlee()) return;

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

        Vector3 difference =
            target.transform.position - hunter.transform.position;
        difference.y = 0f;
        float distance = difference.magnitude;

        bool hasLineOfSight =
            navigator.HasLineOfSight(target.transform.position);

        if (distance <= hunter.MeleeAttackRadius && hasLineOfSight)
        {
            PerformMeleeAttack(target);
            return;
        }

        if (distance <= hunter.RangeAttackRadius && hasLineOfSight)
        {
            PerformRangeAttack(target);
            return;
        }

        // Predict where the moving Boid will be, instead of
        // pathfinding to its position in the current frame.
        Vector3 predictedPosition = PredictBoidPosition(target, distance);
        feedback?.SetPredictedPosition(predictedPosition);

        // Keep A* while there is no LoS to the actual Boid.
        // Steering is only allowed for the final visible approach.
        float directApproachDistance = hasLineOfSight
            ? hunter.RangeAttackRadius + 1f
            : 0f;

        navigator.MoveTo(predictedPosition, directApproachDistance);
    }

    private Vector3 PredictBoidPosition(BoidAgent target, float distance)
    {
        float predictionLimit = feedback != null
            ? feedback.MaxPredictionTime
            : 1.5f;

        float predictionTime = Mathf.Clamp(
            distance / Mathf.Max(0.1f, hunter.MaxSpeed),
            0f,
            predictionLimit
        );

        Vector3 boidVelocity = target.CurrentVelocity;
        boidVelocity.y = 0f;

        Vector3 prediction =
            target.transform.position + boidVelocity * predictionTime;

        // Boids are confined to the hunting ground.
        // Their predicted destination must remain inside it too.
        Renderer ground = hunter.HuntingGroundRenderer;
        if (ground != null)
        {
            Bounds bounds = ground.bounds;
            const float padding = 1f;

            if (bounds.size.x > padding * 2f &&
                bounds.size.z > padding * 2f)
            {
                prediction.x = Mathf.Clamp(
                    prediction.x,
                    bounds.min.x + padding,
                    bounds.max.x - padding
                );
                prediction.z = Mathf.Clamp(
                    prediction.z,
                    bounds.min.z + padding,
                    bounds.max.z - padding
                );
            }
        }

        prediction.y = target.transform.position.y;
        return prediction;
    }

    private bool IsTargetStillPerceived(BoidAgent target)
    {
        var perceivedBoids = hunter.Perception.PerceivedAliveBoids;

        for (int i = 0; i < perceivedBoids.Count; i++)
        {
            if (perceivedBoids[i] == target) return true;
        }

        return false;
    }

    private void PerformMeleeAttack(BoidAgent target)
    {
        feedback?.PlayAttack(
            hunter.transform.position,
            target.transform.position,
            true
        );

        target.TakeDamage(hunter.MeleeDamage);
        Debug.Log($"Hunter performs melee attack on {target.name}");

        hunter.RegisterSuccessfulAttack();
        ReturnToSearch();
    }

    private void PerformRangeAttack(BoidAgent target)
    {
        feedback?.PlayAttack(
            hunter.transform.position,
            target.transform.position,
            false
        );

        target.TakeDamage(hunter.RangeDamage);
        Debug.Log($"Hunter performs ranged attack on {target.name}");

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
        if (navigator != null) navigator.StopNavigation();
        if (feedback != null) feedback.ClearPrediction();

        Debug.Log("Hunter exited Attack state.");
    }
}
