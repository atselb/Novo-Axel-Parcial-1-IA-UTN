
using UnityEngine;

public class WolfPerception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform obstaclesRoot;

    [Header("Vision")]
    [SerializeField, Min(0.1f)]
    private float visionRadius = 12f;

    [SerializeField, Range(1f, 360f)]
    private float fieldOfView = 120f;

    [SerializeField, Min(0f)]
    private float eyeHeight = 0.7f;

    [SerializeField, Min(0.02f)]
    private float detectionInterval = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool drawVision = true;

    private WolfAgent wolf;
    private Collider[] obstacleColliders;
    private float nextDetectionTime;

    public bool CanSeeHunter { get; private set; }

    private void Awake()
    {
        wolf = GetComponent<WolfAgent>();

        if (obstaclesRoot == null)
        {
            Debug.LogError(
                $"{name}: Wolf obstacles root is missing.",
                this
            );
            return;
        }

        obstacleColliders =
            obstaclesRoot.GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        nextDetectionTime = 0f;
        CanSeeHunter = false;
    }

    private void Update()
    {
        if (Time.time < nextDetectionTime)
        {
            return;
        }

        nextDetectionTime = Time.time + detectionInterval;
        CanSeeHunter = DetectHunter();
    }

    private bool DetectHunter()
    {
        if (wolf == null ||
            wolf.HunterTarget == null ||
            obstacleColliders == null)
        {
            return false;
        }

        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Vector3 target =
            wolf.HunterTarget.transform.position +
            Vector3.up * eyeHeight;

        Vector3 direction = target - origin;

        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude >
            visionRadius * visionRadius)
        {
            return false;
        }

        if (horizontalDirection.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.Angle(
                transform.forward,
                horizontalDirection
            );

            if (angle > fieldOfView * 0.5f)
            {
                return false;
            }
        }

        float distance = direction.magnitude;

        if (distance < 0.001f)
        {
            return true;
        }

        Ray ray = new Ray(
            origin,
            direction / distance
        );

        foreach (Collider obstacle in obstacleColliders)
        {
            if (obstacle == null ||
                !obstacle.enabled ||
                obstacle.isTrigger ||
                !obstacle.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (obstacle.Raycast(ray, out RaycastHit hit, distance))
            {
                return false;
            }
        }

        return true;
    }

    private void OnValidate()
    {
        visionRadius = Mathf.Max(0.1f, visionRadius);
        eyeHeight = Mathf.Max(0f, eyeHeight);
        detectionInterval = Mathf.Max(0.02f, detectionInterval);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawVision) return;

        Gizmos.color = CanSeeHunter ? Color.red : Color.cyan;

        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Gizmos.DrawWireSphere(origin, visionRadius);

        Vector3 left = Quaternion.Euler(
            0f,
            -fieldOfView * 0.5f,
            0f
        ) * transform.forward;

        Vector3 right = Quaternion.Euler(
            0f,
            fieldOfView * 0.5f,
            0f
        ) * transform.forward;

        Gizmos.DrawRay(origin, left * visionRadius);
        Gizmos.DrawRay(origin, right * visionRadius);

        if (CanSeeHunter &&
            wolf != null &&
            wolf.HunterTarget != null)
        {
            Gizmos.DrawLine(
                origin,
                wolf.HunterTarget.transform.position +
                Vector3.up * eyeHeight
            );
        }
    }
}
