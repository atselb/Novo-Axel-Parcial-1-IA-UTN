using UnityEngine;

public class BoidInterestInteraction : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private BoidPerception perception;

    private InterestObject currentTarget;
    private float damageTimer;

    private void Update()
    {
        UpdateTarget();
        UpdateInteraction();
    }

    private void UpdateTarget()
    {
        InterestObject perceivedTarget = perception.GetClosestInterestObject();

        if (perceivedTarget != currentTarget)
        {
            currentTarget = perceivedTarget;
            damageTimer = 0f;
        }
    }

    private void UpdateInteraction()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > agent.InteractionRadius)
        {
            damageTimer = 0f;
            return;
        }

        damageTimer += Time.deltaTime;

        if (damageTimer < agent.InterestDamageInterval) return;

        damageTimer = 0f;

        currentTarget.TakeDamage(agent.InterestDamage);
    }
}