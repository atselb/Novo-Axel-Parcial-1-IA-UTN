
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WolfPerception))]
public class WolfAgent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavigationGraph navigationGraph;
    [SerializeField] private HunterAgent hunterTarget;
    [SerializeField] private WolfPackCoordinator packCoordinator;
    [SerializeField] private Transform obstaclesRoot;

    [Header("Safe Zones")]
    [SerializeField] private Renderer safeZone01;
    [SerializeField] private Renderer safeZone02;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
    [SerializeField, Min(1f)] private float rotationSpeed = 270f;
    [SerializeField, Min(0.05f)] private float nodeReachDistance = 0.2f;

    [Header("Patrol - Waypoints")]
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField, Min(0.1f)]
    private float waypointReachDistance = 0.65f;

    [Header("Pursuit and Search")]
    [SerializeField, Min(0.1f)] private float repathInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float repathDistance = 1f;
    [SerializeField, Min(0.1f)] private float alertMemoryDuration = 5f;
    [SerializeField, Min(0.1f)] private float alertInterval = 2f;
    [SerializeField, Min(1f)] private float searchRadius = 3f;

    [Header("Debug")]
    [SerializeField] private bool drawPath = true;
    [SerializeField] private bool logPaths = false;

    private readonly List<NavigationNode> currentPath = new();

    private StateMachine stateMachine;
    private WolfPerception perception;
    private WolfPatrolState patrolState;
    private WolfPursueState pursueState;
    private WolfSearchState searchState;

    private Collider[] obstacleColliders;
    private Vector3[] patrolPositions;

    private int currentPathIndex;
    private int currentWaypointIndex;
    private int lastLoggedWaypointIndex = -1;
    private int searchPointIndex;

    private bool isReady;
    private bool hasCalculatedTarget;
    private bool hasLastKnownPosition;
    private bool previouslySawHunter;

    private Vector3 lastKnownHunterPosition;
    private Vector3 lastCalculatedTarget;
    private Vector3 lastBroadcastPosition;

    private float lastAlertTime = float.NegativeInfinity;
    private float nextAlertTime;
    private float nextRepathTime;
    private float nextPatrolRetryTime;
    private float nextSearchRetryTime;

    public HunterAgent HunterTarget => hunterTarget;

    public bool CanSeeHunter =>
        perception != null &&
        perception.CanSeeHunter &&
        !IsHunterInsideSafeZone();

    public string CurrentStateName =>
        stateMachine?.CurrentState?.GetType().Name ?? "None";

    public int CurrentPathNodeCount => currentPath.Count;
    public int CurrentWaypointIndex => currentWaypointIndex;

    private void Awake()
    {
        perception = GetComponent<WolfPerception>();

        stateMachine = new StateMachine();

        patrolState = new WolfPatrolState(this);
        pursueState = new WolfPursueState(this);
        searchState = new WolfSearchState(this);
    }

    private void OnEnable()
    {
        if (packCoordinator != null)
        {
            packCoordinator.Register(this);
        }
    }

    private void OnDisable()
    {
        if (packCoordinator != null)
        {
            packCoordinator.Unregister(this);
        }
    }

    private void Start()
    {
        if (navigationGraph == null ||
            !navigationGraph.IsReady ||
            hunterTarget == null ||
            packCoordinator == null ||
            perception == null ||
            obstaclesRoot == null ||
            safeZone01 == null ||
            safeZone02 == null)
        {
            DisableWithError(
                "Missing required references. " +
                "Check Safe Zones and other Inspector fields."
            );
            return;
        }

        if (safeZone01 == safeZone02)
        {
            DisableWithError(
                "Safe Zone 01 and 02 must be different."
            );
            return;
        }

        obstacleColliders =
            obstaclesRoot.GetComponentsInChildren<Collider>(true);

        if (obstacleColliders.Length == 0)
        {
            DisableWithError("No obstacle colliders found.");
            return;
        }

        if (patrolWaypoints == null ||
            patrolWaypoints.Length < 2)
        {
            DisableWithError(
                "At least 2 patrol waypoints are required."
            );
            return;
        }

        patrolPositions = new Vector3[patrolWaypoints.Length];

        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            Transform waypoint = patrolWaypoints[i];

            if (waypoint == null)
            {
                DisableWithError(
                    $"Waypoint {i} is not assigned."
                );
                return;
            }

            NavigationNode node =
                navigationGraph.GetClosestWalkableNode(
                    waypoint.position,
                    true
                );

            if (node == null ||
                node.IsSafeZone ||
                IsInsideAnySafeZone(node.Position))
            {
                DisableWithError(
                    $"Waypoint {waypoint.name} has no valid node."
                );
                return;
            }

            Vector3 difference =
                node.Position - waypoint.position;

            difference.y = 0f;

            if (difference.sqrMagnitude > 1.6f * 1.6f)
            {
                DisableWithError(
                    $"Waypoint {waypoint.name} is too far " +
                    "from a walkable node."
                );
                return;
            }

            patrolPositions[i] = node.Position;
        }

        currentWaypointIndex = 0;
        isReady = true;

        stateMachine.ChangeState(patrolState);
    }

    private void DisableWithError(string message)
    {
        Debug.LogError($"{name}: {message}", this);
        enabled = false;
    }

    // -------------------------------------------------
    // UPDATE / PERCEPTION
    // -------------------------------------------------

    private void Update()
    {
        if (!isReady) return;

        // Safe zones immediately cancel every wolf's
        // detection and pursuit of the Hunter.
        if (IsHunterInsideSafeZone())
        {
            if (stateMachine.CurrentState != patrolState)
            {
                ForgetHunter();

                stateMachine.ChangeState(patrolState);

                Debug.Log(
                    $"[Wolf Safe] {name}: Hunter entered " +
                    "a Safe Zone. Returning to patrol."
                );
            }
            else
            {
                ForgetHunter();
            }

            previouslySawHunter = false;
            stateMachine.Update();
            return;
        }

        bool seesHunter = CanSeeHunter;

        if (seesHunter)
        {
            Vector3 position =
                hunterTarget.transform.position;

            RememberHunter(position);

            if (stateMachine.CurrentState != pursueState)
            {
                stateMachine.ChangeState(pursueState);

                Debug.Log($"{name} spotted the Hunter.");
            }

            Vector3 alertDifference =
                position - lastBroadcastPosition;

            alertDifference.y = 0f;

            bool shouldBroadcast =
                !previouslySawHunter ||
                (
                    Time.time >= nextAlertTime &&
                    alertDifference.sqrMagnitude >= 9f
                );

            if (shouldBroadcast)
            {
                nextAlertTime = Time.time + alertInterval;
                lastBroadcastPosition = position;

                packCoordinator.BroadcastAlert(this, position);
            }
        }

        previouslySawHunter = seesHunter;

        stateMachine.Update();
    }

    public void ReceiveAlert(Vector3 hunterPosition)
    {
        if (!isReady || IsHunterInsideSafeZone())
        {
            return;
        }

        RememberHunter(hunterPosition);

        if (stateMachine.CurrentState != pursueState)
        {
            stateMachine.ChangeState(pursueState);

            Debug.Log(
                $"{name} received a pack alert."
            );
        }
    }

    private void RememberHunter(Vector3 position)
    {
        lastKnownHunterPosition = position;
        hasLastKnownPosition = true;
        lastAlertTime = Time.time;
    }

    private void ForgetHunter()
    {
        lastKnownHunterPosition = Vector3.zero;
        lastAlertTime = float.NegativeInfinity;
        hasLastKnownPosition = false;
        previouslySawHunter = false;

        ClearPath();
    }

    private bool IsAlertRemembered()
    {
        return hasLastKnownPosition &&
               Time.time - lastAlertTime <=
               alertMemoryDuration;
    }

    // -------------------------------------------------
    // SAFE ZONES
    // -------------------------------------------------

    private bool IsHunterInsideSafeZone()
    {
        if (hunterTarget == null)
        {
            return false;
        }

        return IsInsideAnySafeZone(
            hunterTarget.transform.position
        );
    }

    private bool IsInsideAnySafeZone(Vector3 position)
    {
        return IsInsideZone(safeZone01, position) ||
               IsInsideZone(safeZone02, position);
    }

    private static bool IsInsideZone(
        Renderer zone,
        Vector3 position)
    {
        if (zone == null)
        {
            return false;
        }

        Bounds bounds = zone.bounds;

        // The zones are floor areas. Check X/Z;
        // vertical height is not relevant.
        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    // -------------------------------------------------
    // PATROL - ORDERED WAYPOINTS
    // -------------------------------------------------

    private Vector3 GetPatrolDestination()
    {
        return patrolPositions[currentWaypointIndex];
    }

    private bool HasReachedWaypoint()
    {
        return HasReachedPosition(
            GetPatrolDestination(),
            waypointReachDistance
        );
    }

    private bool HasReachedPosition(
        Vector3 position,
        float distance)
    {
        Vector3 difference =
            position - transform.position;

        difference.y = 0f;

        return difference.sqrMagnitude <=
               distance * distance;
    }

    private void AdvancePatrolWaypoint()
    {
        currentWaypointIndex =
            (currentWaypointIndex + 1) %
            patrolWaypoints.Length;

        ClearPath();
        nextPatrolRetryTime = 0f;
    }

    private void ReportPatrolWaypoint()
    {
        if (!logPaths ||
            lastLoggedWaypointIndex == currentWaypointIndex)
        {
            return;
        }

        lastLoggedWaypointIndex = currentWaypointIndex;

        Debug.Log(
            $"[Wolf Patrol] {name}: heading to " +
            $"{patrolWaypoints[currentWaypointIndex].name} " +
            $"({currentWaypointIndex + 1}/" +
            $"{patrolWaypoints.Length})."
        );
    }

    // -------------------------------------------------
    // DIRECT NAVIGATION / LINE OF SIGHT
    // -------------------------------------------------

    private bool CanMoveDirectlyTo(Vector3 destination)
    {
        Vector3 origin = transform.position;

        Vector3 target = new Vector3(
            destination.x,
            origin.y,
            destination.z
        );

        Vector3 difference = target - origin;
        float distance = difference.magnitude;

        if (distance <= 0.01f)
        {
            return true;
        }

        Ray ray = new Ray(
            origin,
            difference / distance
        );

        foreach (Collider obstacle in obstacleColliders)
        {
            if (obstacle == null ||
                !obstacle.enabled ||
                !obstacle.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (obstacle.Raycast(
                ray,
                out RaycastHit hit,
                distance
            ))
            {
                return false;
            }
        }

        // A direct route must never cross Safe Zones.
        int samples =
            Mathf.Max(1, Mathf.CeilToInt(distance / 0.5f));

        for (int i = 0; i <= samples; i++)
        {
            Vector3 sample = Vector3.Lerp(
                origin,
                target,
                (float)i / samples
            );

            if (IsInsideAnySafeZone(sample))
            {
                return false;
            }

            NavigationNode node =
                navigationGraph.GetClosestWalkableNode(
                    sample,
                    false
                );

            if (node == null || node.IsSafeZone)
            {
                return false;
            }
        }

        return true;
    }

    private void TryPlanPatrolPath()
    {
        if (Time.time < nextPatrolRetryTime)
        {
            return;
        }

        nextPatrolRetryTime = Time.time + 1f;

        CalculatePath(
            GetPatrolDestination(),
            true
        );
    }

    // -------------------------------------------------
    // A* PATHFINDING
    // -------------------------------------------------

    private bool CalculatePath(
        Vector3 destination,
        bool forceRecalculation)
    {
        if (navigationGraph == null ||
            !navigationGraph.IsReady)
        {
            return false;
        }

        if (!forceRecalculation &&
            Time.time < nextRepathTime)
        {
            return HasPendingPath();
        }

        NavigationNode targetNode =
            navigationGraph.GetClosestWalkableNode(
                destination,
                true
            );

        if (targetNode == null ||
            targetNode.IsSafeZone ||
            IsInsideAnySafeZone(targetNode.Position))
        {
            ClearPath();
            return false;
        }

        Vector3 targetDifference =
            targetNode.Position - lastCalculatedTarget;

        targetDifference.y = 0f;

        bool targetMoved =
            !hasCalculatedTarget ||
            targetDifference.sqrMagnitude >=
            repathDistance * repathDistance;

        bool pathFinished = !HasPendingPath();

        if (!forceRecalculation &&
            !targetMoved &&
            !pathFinished)
        {
            return true;
        }

        nextRepathTime = Time.time + repathInterval;

        List<NavigationNode> calculatedPath =
            AStarPathfinder.FindPath(
                navigationGraph,
                transform.position,
                targetNode.Position,
                true,
                out int exploredCount
            );

        lastCalculatedTarget = targetNode.Position;
        hasCalculatedTarget = true;

        currentPath.Clear();
        currentPathIndex = 0;

        if (calculatedPath == null ||
            calculatedPath.Count == 0)
        {
            return false;
        }

        foreach (NavigationNode node in calculatedPath)
        {
            if (node == null ||
                !node.Walkable ||
                node.IsSafeZone ||
                IsInsideAnySafeZone(node.Position))
            {
                ClearPath();
                return false;
            }
        }

        currentPath.AddRange(calculatedPath);

        if (logPaths)
        {
            Debug.Log(
                $"[Wolf A*] {name}: " +
                $"{currentPath.Count} nodes, " +
                $"{exploredCount} explored."
            );
        }

        return true;
    }

    private bool HasPendingPath()
    {
        return currentPathIndex < currentPath.Count;
    }

    private void FollowPath()
    {
        float reachSqr =
            nodeReachDistance * nodeReachDistance;

        while (currentPathIndex < currentPath.Count)
        {
            NavigationNode node =
                currentPath[currentPathIndex];

            Vector3 destination = new Vector3(
                node.Position.x,
                transform.position.y,
                node.Position.z
            );

            Vector3 difference =
                destination - transform.position;

            if (difference.sqrMagnitude <= reachSqr)
            {
                currentPathIndex++;
                continue;
            }

            MoveTo(destination);
            return;
        }
    }

    private void MoveTo(Vector3 destination)
    {
        Vector3 previousPosition = transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );

        Vector3 movement =
            transform.position - previousPosition;

        if (movement.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(
            movement.normalized,
            Vector3.up
        );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ClearPath()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        hasCalculatedTarget = false;
        nextRepathTime = 0f;
    }

    // -------------------------------------------------
    // SEARCH AROUND LAST KNOWN POSITION
    // -------------------------------------------------

    private void TryPlanSearchPath()
    {
        if (!IsAlertRemembered() ||
            Time.time < nextSearchRetryTime)
        {
            return;
        }

        nextSearchRetryTime = Time.time + 1f;

        // Four search directions around the last
        // detected position, using our navigation graph.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            int index = searchPointIndex % 4;
            searchPointIndex++;

            Vector3 offset;

            switch (index)
            {
                case 0:
                    offset = Vector3.right * searchRadius;
                    break;

                case 1:
                    offset = Vector3.forward * searchRadius;
                    break;

                case 2:
                    offset = Vector3.left * searchRadius;
                    break;

                default:
                    offset = Vector3.back * searchRadius;
                    break;
            }

            Vector3 candidate =
                lastKnownHunterPosition + offset;

            NavigationNode node =
                navigationGraph.GetClosestWalkableNode(
                    candidate,
                    true
                );

            if (node == null ||
                node.IsSafeZone ||
                IsInsideAnySafeZone(node.Position))
            {
                continue;
            }

            Vector3 projectionDifference =
                node.Position - candidate;

            projectionDifference.y = 0f;

            if (projectionDifference.sqrMagnitude > 9f)
            {
                continue;
            }

            if (HasReachedPosition(
                node.Position,
                waypointReachDistance
            ))
            {
                continue;
            }

            if (CalculatePath(node.Position, true))
            {
                if (logPaths)
                {
                    Debug.Log(
                        $"[Wolf Search] {name}: " +
                        $"searching near {candidate}."
                    );
                }

                return;
            }
        }
    }

    // -------------------------------------------------
    // VALIDATION AND GIZMOS
    // -------------------------------------------------

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        rotationSpeed = Mathf.Max(1f, rotationSpeed);
        nodeReachDistance =
            Mathf.Max(0.05f, nodeReachDistance);

        waypointReachDistance =
            Mathf.Max(0.1f, waypointReachDistance);

        repathInterval = Mathf.Max(0.1f, repathInterval);
        repathDistance = Mathf.Max(0.1f, repathDistance);
        alertMemoryDuration =
            Mathf.Max(0.1f, alertMemoryDuration);
        alertInterval = Mathf.Max(0.1f, alertInterval);
        searchRadius = Mathf.Max(1f, searchRadius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPath) return;

        // Patrol waypoint circuit.
        if (patrolWaypoints != null &&
            patrolWaypoints.Length >= 2)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < patrolWaypoints.Length; i++)
            {
                Transform current = patrolWaypoints[i];
                Transform next = patrolWaypoints[
                    (i + 1) % patrolWaypoints.Length
                ];

                if (current == null || next == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(
                    current.position,
                    0.3f
                );

                Gizmos.DrawLine(
                    current.position,
                    next.position
                );
            }
        }

        // Last known Hunter position.
        if (hasLastKnownPosition &&
            stateMachine?.CurrentState != patrolState)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(
                lastKnownHunterPosition + Vector3.up * 0.5f,
                0.5f
            );
        }

        if (currentPath.Count == 0) return;

        Gizmos.color =
            stateMachine?.CurrentState == pursueState
                ? Color.red
                : stateMachine?.CurrentState == searchState
                    ? Color.magenta
                    : Color.yellow;

        Vector3 previous =
            transform.position + Vector3.up * 0.6f;

        for (int i = currentPathIndex;
             i < currentPath.Count;
             i++)
        {
            Vector3 current =
                currentPath[i].Position + Vector3.up * 0.6f;

            Gizmos.DrawSphere(current, 0.15f);
            Gizmos.DrawLine(previous, current);

            previous = current;
        }
    }

    // -------------------------------------------------
    // PATROL STATE
    // -------------------------------------------------

    private sealed class WolfPatrolState : IState
    {
        private readonly WolfAgent wolf;

        public WolfPatrolState(WolfAgent wolf)
        {
            this.wolf = wolf;
        }

        public void Enter()
        {
            wolf.ClearPath();
        }

        public void Update()
        {
            if (wolf.HasReachedWaypoint())
            {
                wolf.AdvancePatrolWaypoint();
            }

            wolf.ReportPatrolWaypoint();

            Vector3 destination =
                wolf.GetPatrolDestination();

            if (wolf.CanMoveDirectlyTo(destination))
            {
                if (wolf.currentPath.Count > 0)
                {
                    wolf.ClearPath();
                }

                wolf.MoveTo(new Vector3(
                    destination.x,
                    wolf.transform.position.y,
                    destination.z
                ));

                return;
            }

            if (!wolf.HasPendingPath())
            {
                wolf.TryPlanPatrolPath();
            }

            wolf.FollowPath();
        }

        public void Exit()
        {
            wolf.ClearPath();
        }
    }

    // -------------------------------------------------
    // PURSUE STATE
    // -------------------------------------------------

    private sealed class WolfPursueState : IState
    {
        private readonly WolfAgent wolf;

        public WolfPursueState(WolfAgent wolf)
        {
            this.wolf = wolf;
        }

        public void Enter()
        {
            wolf.ClearPath();

            wolf.CalculatePath(
                wolf.lastKnownHunterPosition,
                true
            );
        }

        public void Update()
        {
            if (!wolf.IsAlertRemembered())
            {
                wolf.stateMachine.ChangeState(
                    wolf.patrolState
                );

                return;
            }

            if (!wolf.CanSeeHunter &&
                wolf.HasReachedPosition(
                    wolf.lastKnownHunterPosition,
                    wolf.waypointReachDistance
                ))
            {
                wolf.stateMachine.ChangeState(
                    wolf.searchState
                );

                return;
            }

            Vector3 destination =
                wolf.CanSeeHunter
                    ? wolf.hunterTarget.transform.position
                    : wolf.lastKnownHunterPosition;

            wolf.CalculatePath(destination, false);
            wolf.FollowPath();

            if (!wolf.CanSeeHunter &&
                !wolf.HasPendingPath())
            {
                wolf.stateMachine.ChangeState(
                    wolf.searchState
                );
            }
        }

        public void Exit()
        {
            wolf.ClearPath();
        }
    }

    // -------------------------------------------------
    // SEARCH STATE
    // -------------------------------------------------

    private sealed class WolfSearchState : IState
    {
        private readonly WolfAgent wolf;

        public WolfSearchState(WolfAgent wolf)
        {
            this.wolf = wolf;
        }

        public void Enter()
        {
            wolf.ClearPath();
            wolf.searchPointIndex = 0;
            wolf.nextSearchRetryTime = 0f;

            Debug.Log(
                $"[Wolf Search] {wolf.name}: " +
                "reached last known Hunter position. " +
                $"Searching near {wolf.lastKnownHunterPosition}."
            );
        }

        public void Update()
        {
            if (!wolf.IsAlertRemembered())
            {
                wolf.stateMachine.ChangeState(
                    wolf.patrolState
                );

                return;
            }

            if (!wolf.HasPendingPath())
            {
                wolf.TryPlanSearchPath();
            }

            wolf.FollowPath();
        }

        public void Exit()
        {
            wolf.ClearPath();
        }
    }
}
