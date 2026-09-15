using UnityEngine;

public class EvadeBehaviour : MonoBehaviour
{
    [SerializeField] private BoidAgent agent;
    [SerializeField] private float predictionTime = 1f;

    public Vector3 Calculate(HunterAgent hunter)
    {
        if (hunter == null) return Vector3.zero;

        Vector3 predictedPosition = 
            hunter.transform.position +
            hunter.transform.forward *
            hunter.MaxSpeed *
            predictionTime;

        Vector3 awayFromHunter = transform.position - predictedPosition;

        awayFromHunter.y = 0f;

        if (awayFromHunter.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        return awayFromHunter.normalized * agent.MaxSpeed;
    }
}