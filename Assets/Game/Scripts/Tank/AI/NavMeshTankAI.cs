using System.Collections.Generic;
using TankGame.Commands;
using TankGame.Tank;
using TankGame.Tank.Components;
using UnityEngine;
using UnityEngine.AI;

namespace TankGame.Tank.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(TankMovement))]
    [RequireComponent(typeof(TankTurret))]
    [RequireComponent(typeof(TankWeapon))]
    [RequireComponent(typeof(TankHealth))]
    public class NavMeshTankAI : MonoBehaviour
    {
        private enum CheckpointRoutingMode
        {
            AssistIfBetter = 0,
            ForceWhenAvailable = 1
        }

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private TankController tankController;
        [SerializeField] private TankMovement movement;
        [SerializeField] private TankTurret turret;
        [SerializeField] private TankWeapon weapon;
        [SerializeField] private TankHealth health;
        [SerializeField] private TrackAnimationController trackAnimation;
        [SerializeField] private NavMeshAgent agent;

        [Header("Targeting")]
        [SerializeField] private bool autoFindLocalPlayerTarget = true;
        [SerializeField] private float targetSearchInterval = 1f;
        [SerializeField] private float targetAimHeightOffset = 0.6f;

        [Header("Movement")]
        [SerializeField] private float chaseRange = 60f;
        [SerializeField] private float attackRange = 22f;
        [SerializeField] private float minDistanceToOtherBots = 6f;
        [SerializeField] private float repathInterval = 0.2f;
        [SerializeField] private float turnAngleForFullInput = 60f;
        [SerializeField] private float moveAngleLimit = 100f;

        [Header("NavMesh Checkpoints")]
        [SerializeField] private bool useNavMeshCheckpoints = true;
        [SerializeField] private CheckpointRoutingMode checkpointRoutingMode = CheckpointRoutingMode.ForceWhenAvailable;
        [SerializeField] private Transform[] checkpointNodeTransforms;
        [SerializeField] private bool autoFindCheckpointNodesIfEmpty = true;
        [SerializeField] private float checkpointReachDistance = 2.75f;
        [SerializeField] private float checkpointSampleRadius = 4f;
        [SerializeField] private float checkpointReevaluateInterval = 0.4f;
        [SerializeField] private float checkpointMinBenefitDistance = 1.5f;
        [SerializeField] private bool keepCurrentCheckpointUntilReached = true;
        [SerializeField] private bool calculateCheckpointRouteOnceUntilCompleted = true;
        [SerializeField] private float checkpointStoppingDistance = 0.45f;
        [SerializeField] private float checkpointReachDistanceScale = 0.45f;

        [Header("Combat")]
        [SerializeField] private float fireRange = 20f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask lineOfSightMask = ~0;
        [SerializeField] private bool requireTurretAlignmentForFire = false;
        [SerializeField] private bool stopMovementWhenTargetNotVisible = true;
        [SerializeField] private float lineOfSightGraceTime = 0.35f;

        [Header("Anti-Loop Patrol")]
        [SerializeField] private bool enableAntiLoopPatrol = true;
        [SerializeField] private int patrolPointCount = 5;
        [SerializeField] private float loopDetectionRadius = 6f;
        [SerializeField] private float loopDetectionDuration = 3f;
        [SerializeField] private float loopMinProgressDistance = 1.25f;
        [SerializeField] private int maxAlternativeRouteAttemptsBeforePatrol = 3;
        [SerializeField] private float blockedNodeDuration = 8f;
        [SerializeField] private float patrolStoppingDistance = 0.4f;

        [Header("Weapon Switching")]
        [SerializeField] private bool enableWeaponSwitching = true;
        [SerializeField] private float machineGunPreferredRange = 12f;
        [SerializeField] private float weaponSwitchCooldown = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool enableAiDebugLogs = true;
        [SerializeField] private float destinationModeLogCooldown = 0.75f;

        private enum DestinationMode
        {
            None = 0,
            Direct = 1,
            Checkpoint = 2,
            Patrol = 3
        }

        private float nextTargetSearchTime;
        private float nextRepathTime;
        private float nextWeaponSwitchTime;
        private float lastLineOfSightTime = float.NegativeInfinity;
        private float nextTargetPathDistanceUpdateTime;
        private float cachedTargetPathDistance = float.PositiveInfinity;
        private float currentVerticalInput;
        private float currentHorizontalInput;

        private readonly List<NavMeshCheckpointNode> activeCheckpointRoute = new List<NavMeshCheckpointNode>(16);
        private int activeRouteIndex = -1;
        private float nextCheckpointReevaluateTime;
        private NavMeshCheckpointNode[] cachedCheckpointNodes;

        private NavMeshPath directPathBuffer;
        private NavMeshPath pathToCheckpointBuffer;
        private NavMeshPath checkpointToTargetPathBuffer;
        private readonly RaycastHit[] lineOfSightHits = new RaycastHit[16];
        private readonly List<NavMeshCheckpointNode> patrolRoute = new List<NavMeshCheckpointNode>(8);
        private readonly Dictionary<NavMeshCheckpointNode, float> blockedNodesUntilTime = new Dictionary<NavMeshCheckpointNode, float>(16);
        private int patrolRouteIndex = -1;
        private bool loopCheckActive;
        private float loopCheckStartTime;
        private int observedRouteIndex = -1;
        private float observedRouteStartDistance;
        private float observedRouteBestDistance;
        private int consecutiveAlternativeRouteFailures;
        private string lastIdleReason;
        private DestinationMode lastDestinationMode = DestinationMode.None;
        private float nextDestinationModeLogTime;

        private bool HasActiveRoute => activeRouteIndex >= 0 && activeRouteIndex < activeCheckpointRoute.Count;
        private bool IsPatrolling => patrolRouteIndex >= 0 && patrolRouteIndex < patrolRoute.Count;

        private void Awake()
        {
            if (tankController == null)
                tankController = GetComponent<TankController>();
            if (movement == null)
                movement = GetComponent<TankMovement>();
            if (turret == null)
                turret = GetComponent<TankTurret>();
            if (weapon == null)
                weapon = GetComponent<TankWeapon>();
            if (health == null)
                health = GetComponent<TankHealth>();
            if (trackAnimation == null)
                trackAnimation = GetComponent<TrackAnimationController>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            directPathBuffer = new NavMeshPath();
            pathToCheckpointBuffer = new NavMeshPath();
            checkpointToTargetPathBuffer = new NavMeshPath();

            ConfigureAgent();
            ClearCheckpointState();
        }

        private void OnEnable()
        {
            if (tankController != null)
            {
                tankController.SetIsLocalPlayer(false);
                tankController.SetAuthorityMode(TankController.AuthorityMode.NetworkProxy);
            }
        }

        private void Update()
        {
            if (health != null && !health.IsAlive())
            {
                SetIdleState("HealthDead");
                return;
            }

            TryAutoFindTarget();
            if (target == null)
            {
                SetIdleState("NoTarget");
                return;
            }

            float distance = GetPlanarDistance(transform.position, target.position);

            Vector3 aimPoint = GetTargetAimPoint(target);
            bool hasLineOfSight = HasLineOfSight(aimPoint);
            if (hasLineOfSight)
                lastLineOfSightTime = Time.time;

            bool lineOfSightAllowedForMovement = hasLineOfSight ||
                (Time.time - lastLineOfSightTime) <= Mathf.Max(0f, lineOfSightGraceTime);
            if (stopMovementWhenTargetNotVisible && !lineOfSightAllowedForMovement)
            {
                SetNoLineOfSightHoldState();
                return;
            }
            lastIdleReason = null;

            EvaluateLoopAndMaybeEnterPatrol(hasLineOfSight);

            turret.SetAimPoint(aimPoint);
            TankWeapon activeWeapon = GetActiveWeapon();
            activeWeapon?.SetAimPoint(aimPoint);

            if (!turret.IsAiming)
                turret.StartAiming();

            UpdateCombatWeapon(distance);
            UpdateMovementInputs(distance);
            TryFireAtTarget(distance, aimPoint, hasLineOfSight);
            TryReloadIfNeeded();
        }

        private void FixedUpdate()
        {
            if (health != null && !health.IsAlive())
                return;

            movement.ApplyMovement(currentVerticalInput, currentHorizontalInput, false);
            trackAnimation?.UpdateTrackAnimation(currentVerticalInput, currentHorizontalInput);
            movement.AlignToGround();
        }

        private void OnDisable()
        {
            SetIdleState("OnDisable");
        }

        private void ConfigureAgent()
        {
            if (agent == null)
                return;

            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.autoBraking = true;
            agent.stoppingDistance = attackRange * 0.8f;

            if (movement != null)
            {
                agent.speed = Mathf.Max(0.1f, movement.MoveSpeed);
                agent.angularSpeed = Mathf.Max(1f, movement.RotationSpeed);
                agent.acceleration = Mathf.Max(1f, movement.MoveSpeed * 3f);
            }
        }

        private void TryAutoFindTarget()
        {
            if (!autoFindLocalPlayerTarget || target != null || Time.time < nextTargetSearchTime)
                return;

            nextTargetSearchTime = Time.time + Mathf.Max(0.1f, targetSearchInterval);
            TankController localPlayer = TankRuntime.GetLocalPlayer();
            if (localPlayer == null || localPlayer == tankController)
                return;

            TankHealth candidateHealth = localPlayer.GetComponent<TankHealth>();
            if (candidateHealth != null && !candidateHealth.IsAlive())
                return;

            target = localPlayer.transform;
        }

        private void UpdateMovementInputs(float distanceToTarget)
        {
            if (agent == null || !agent.isOnNavMesh || target == null)
            {
                currentVerticalInput = 0f;
                currentHorizontalInput = 0f;
                LogAi("MOVE", "Agent unavailable or target missing, movement input reset.");
                return;
            }

            agent.nextPosition = transform.position;
            float effectiveDistanceToTarget = GetEffectiveDistanceToTarget(distanceToTarget);
            bool shouldMove = IsPatrolling || effectiveDistanceToTarget > fireRange;

            if (shouldMove && Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);
                UpdateAgentDestination();
            }
            else if (!shouldMove && agent.hasPath)
            {
                agent.ResetPath();
                ClearCheckpointState();
            }

            if (!shouldMove)
            {
                currentVerticalInput = 0f;
                currentHorizontalInput = 0f;
                return;
            }

            Vector3 desiredWorld = Vector3.zero;

            if (!agent.pathPending && agent.hasPath)
            {
                Vector3 toSteering = agent.steeringTarget - transform.position;
                toSteering.y = 0f;
                if (toSteering.sqrMagnitude > 0.01f)
                    desiredWorld = toSteering.normalized;
            }

            if (desiredWorld.sqrMagnitude < 0.01f)
            {
                desiredWorld = agent.desiredVelocity;
                desiredWorld.y = 0f;
            }

            if (desiredWorld.sqrMagnitude < 0.01f)
            {
                currentVerticalInput = 0f;
                currentHorizontalInput = 0f;

                if (!agent.pathPending)
                    UpdateAgentDestination();

                return;
            }

            Vector3 separation = GetSeparationFromOtherBots();
            if (separation.sqrMagnitude > 0.01f)
            {
                desiredWorld = (desiredWorld + separation).normalized;
                desiredWorld.y = 0f;
                if (desiredWorld.sqrMagnitude > 0.01f)
                    desiredWorld.Normalize();
            }

            if (desiredWorld.sqrMagnitude > 0.01f)
                desiredWorld.Normalize();

            Vector3 forward = -transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float signedAngle = Vector3.SignedAngle(forward, desiredWorld, Vector3.up);
            currentHorizontalInput = Mathf.Clamp(signedAngle / Mathf.Max(1f, turnAngleForFullInput), -1f, 1f);

            float vert = Mathf.Abs(signedAngle) <= moveAngleLimit ? 1f : 0f;
            float distToNearestBot = GetDistanceToNearestOtherBot();
            if (distToNearestBot >= 0f && distToNearestBot < minDistanceToOtherBots)
                vert = 0f;
            currentVerticalInput = vert;
        }

        private void UpdateAgentDestination()
        {
            if (agent == null || !agent.isOnNavMesh || target == null)
                return;

            Vector3 destination = target.position;
            bool usesCheckpointDestination = false;
            bool usesPatrolDestination = false;

            if (IsPatrolling && TryGetCurrentPatrolDestination(out Vector3 patrolDestination))
            {
                destination = patrolDestination;
                usesPatrolDestination = true;
                usesCheckpointDestination = true;
            }
            else if (IsPatrolling)
            {
                ClearPatrolState();
            }

            if (!usesPatrolDestination && TryGetCheckpointDestination(target.position, out Vector3 checkpointDestination))
            {
                destination = checkpointDestination;
                usesCheckpointDestination = true;
            }
            else if (!usesPatrolDestination)
            {
                ClearCheckpointState();
            }

            float desiredStoppingDistance;
            DestinationMode mode;
            if (usesPatrolDestination)
            {
                desiredStoppingDistance = Mathf.Max(0.05f, patrolStoppingDistance);
                mode = DestinationMode.Patrol;
            }
            else if (usesCheckpointDestination)
            {
                desiredStoppingDistance = Mathf.Max(0.05f, checkpointStoppingDistance);
                mode = DestinationMode.Checkpoint;
            }
            else
            {
                desiredStoppingDistance = attackRange * 0.8f;
                mode = DestinationMode.Direct;
            }

            agent.stoppingDistance = desiredStoppingDistance;

            agent.SetDestination(destination);
            LogDestinationMode(mode, destination, desiredStoppingDistance);
        }

        private void EvaluateLoopAndMaybeEnterPatrol(bool hasLineOfSight)
        {
            if (!enableAntiLoopPatrol || !hasLineOfSight || IsPatrolling || !HasActiveRoute)
            {
                ResetLoopCheck();
                return;
            }

            if (!TryGetCurrentRouteDestination(out Vector3 routeDestination))
            {
                ResetLoopCheck();
                return;
            }

            PurgeExpiredBlockedNodes();

            float distanceToCurrentRoutePoint = GetPlanarDistance(transform.position, routeDestination);
            if (distanceToCurrentRoutePoint > Mathf.Max(0.5f, loopDetectionRadius))
            {
                ResetLoopCheck();
                return;
            }

            if (!loopCheckActive || observedRouteIndex != activeRouteIndex)
            {
                loopCheckActive = true;
                loopCheckStartTime = Time.time;
                observedRouteIndex = activeRouteIndex;
                observedRouteStartDistance = distanceToCurrentRoutePoint;
                observedRouteBestDistance = distanceToCurrentRoutePoint;
                consecutiveAlternativeRouteFailures = 0;
                return;
            }

            if (distanceToCurrentRoutePoint < observedRouteBestDistance)
                observedRouteBestDistance = distanceToCurrentRoutePoint;

            if (Time.time - loopCheckStartTime < Mathf.Max(0.25f, loopDetectionDuration))
                return;

            float progress = observedRouteStartDistance - observedRouteBestDistance;
            if (progress >= Mathf.Max(0.1f, loopMinProgressDistance))
            {
                loopCheckStartTime = Time.time;
                observedRouteStartDistance = distanceToCurrentRoutePoint;
                observedRouteBestDistance = distanceToCurrentRoutePoint;
                return;
            }

            LogAi("LOOP",
                $"Detected low progress near route node idx={activeRouteIndex}. start={observedRouteStartDistance:F2}, best={observedRouteBestDistance:F2}, progress={progress:F2}");

            if (TrySwitchToAlternativeRoute())
            {
                ResetLoopCheck();
                return;
            }

            consecutiveAlternativeRouteFailures++;
            if (consecutiveAlternativeRouteFailures >= Mathf.Max(1, maxAlternativeRouteAttemptsBeforePatrol))
            {
                if (TryStartPatrolFromNearestNodes())
                {
                    consecutiveAlternativeRouteFailures = 0;
                    LogAi("PATROL", "Fallback to patrol route after repeated alternative route failures.");
                    return;
                }
            }

            loopCheckStartTime = Time.time;
            observedRouteStartDistance = distanceToCurrentRoutePoint;
            observedRouteBestDistance = distanceToCurrentRoutePoint;
        }

        private bool TryStartPatrolFromNearestNodes()
        {
            NavMeshCheckpointNode[] nodes = GetCheckpointNodes();
            if (nodes == null || nodes.Length == 0)
                return false;

            if (!TrySampleOnNavMesh(transform.position, out Vector3 selfOnMesh))
                return false;

            var candidates = new List<(NavMeshCheckpointNode node, float distance)>(nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                NavMeshCheckpointNode node = nodes[i];
                if (node == null)
                    continue;

                if (!TrySampleOnNavMesh(node.transform.position, out Vector3 nodeOnMesh))
                    continue;

                float distance = GetPathDistance(selfOnMesh, nodeOnMesh, pathToCheckpointBuffer);
                if (!IsFinitePathDistance(distance))
                    continue;

                candidates.Add((node, distance));
            }

            if (candidates.Count < 2)
            {
                LogAi("PATROL", "Cannot start patrol: less than 2 reachable checkpoint nodes.");
                return false;
            }

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            int desiredCount = Mathf.Clamp(patrolPointCount, 2, 16);

            patrolRoute.Clear();
            for (int i = 0; i < candidates.Count && patrolRoute.Count < desiredCount; i++)
            {
                NavMeshCheckpointNode node = candidates[i].node;
                if (patrolRoute.Contains(node))
                    continue;

                patrolRoute.Add(node);
            }

            if (patrolRoute.Count < 2)
            {
                patrolRoute.Clear();
                patrolRouteIndex = -1;
                LogAi("PATROL", "Cannot start patrol: selected nodes are insufficient.");
                return false;
            }

            ClearCheckpointState();
            patrolRouteIndex = 0;
            consecutiveAlternativeRouteFailures = 0;
            ResetLoopCheck();
            LogAi("PATROL", $"Patrol started with {patrolRoute.Count} nodes: {FormatNodeList(patrolRoute)}");
            return true;
        }

        private bool TrySwitchToAlternativeRoute()
        {
            if (target == null || !HasActiveRoute)
                return false;

            NavMeshCheckpointNode currentRouteNode = activeCheckpointRoute[activeRouteIndex];
            BlockNodeTemporarily(currentRouteNode);
            LogAi("ALT", $"Blocking node '{GetNodeName(currentRouteNode)}' for {blockedNodeDuration:F1}s and rebuilding route.");

            ClearCheckpointState();
            nextCheckpointReevaluateTime = 0f;

            if (TryBuildCheckpointRoute(target.position))
            {
                consecutiveAlternativeRouteFailures = 0;
                LogAi("ALT", $"Alternative route selected: {FormatNodeList(activeCheckpointRoute)}");
                return true;
            }

            LogAi("ALT", "Alternative route build failed.");
            return false;
        }

        private bool TryGetCurrentPatrolDestination(out Vector3 destination)
        {
            destination = Vector3.zero;
            if (!IsPatrolling)
                return false;

            while (IsPatrolling)
            {
                NavMeshCheckpointNode node = patrolRoute[patrolRouteIndex];
                if (node == null)
                {
                    AdvancePatrolIndex();
                    continue;
                }

                if (!TrySampleOnNavMesh(node.transform.position, out destination))
                {
                    AdvancePatrolIndex();
                    continue;
                }

                if (IsPositionReached(destination))
                {
                    AdvancePatrolIndex();
                    continue;
                }

                return true;
            }

            destination = Vector3.zero;
            return false;
        }

        private void AdvancePatrolIndex()
        {
            if (!IsPatrolling)
                return;

            patrolRouteIndex++;
            if (patrolRouteIndex >= patrolRoute.Count)
                patrolRouteIndex = 0;
        }

        private bool TryGetCheckpointDestination(Vector3 finalTargetPosition, out Vector3 checkpointDestination)
        {
            checkpointDestination = Vector3.zero;

            if (!useNavMeshCheckpoints)
            {
                ClearCheckpointState();
                return false;
            }

            AdvanceRouteIfReached();
            if ((keepCurrentCheckpointUntilReached || calculateCheckpointRouteOnceUntilCompleted) &&
                TryGetCurrentRouteDestination(out checkpointDestination))
                return true;

            if (Time.time < nextCheckpointReevaluateTime && TryGetCurrentRouteDestination(out checkpointDestination))
                return true;

            nextCheckpointReevaluateTime = Time.time + Mathf.Max(0.05f, checkpointReevaluateInterval);

            if (!TryBuildCheckpointRoute(finalTargetPosition))
            {
                ClearCheckpointState();
                return false;
            }

            AdvanceRouteIfReached();
            return TryGetCurrentRouteDestination(out checkpointDestination);
        }

        private bool TryBuildCheckpointRoute(Vector3 finalTargetPosition)
        {
            PurgeExpiredBlockedNodes();
            if (TryBuildCheckpointRouteInternal(finalTargetPosition, false))
                return true;

            if (blockedNodesUntilTime.Count <= 0)
                return false;

            LogAi("ROUTE", "Retry route build by temporarily ignoring blocked nodes.");
            return TryBuildCheckpointRouteInternal(finalTargetPosition, true);
        }

        private bool TryBuildCheckpointRouteInternal(Vector3 finalTargetPosition, bool ignoreBlockedNodes)
        {
            NavMeshCheckpointNode[] nodes = GetCheckpointNodes();
            if (nodes == null || nodes.Length == 0)
                return false;

            if (!TrySampleOnNavMesh(transform.position, out Vector3 selfOnMesh) ||
                !TrySampleOnNavMesh(finalTargetPosition, out Vector3 targetOnMesh))
                return false;

            float directPathDistance = GetPathDistance(selfOnMesh, targetOnMesh, directPathBuffer);

            var validNodes = new List<NavMeshCheckpointNode>(nodes.Length);
            var sampled = new List<Vector3>(nodes.Length);
            var indexByNode = new Dictionary<NavMeshCheckpointNode, int>(nodes.Length);

            for (int i = 0; i < nodes.Length; i++)
            {
                NavMeshCheckpointNode node = nodes[i];
                if (node == null)
                    continue;
                if (!ignoreBlockedNodes && IsNodeBlocked(node))
                    continue;

                if (!TrySampleOnNavMesh(node.transform.position, out Vector3 sampledPos))
                    continue;

                if (indexByNode.ContainsKey(node))
                    continue;

                int index = validNodes.Count;
                validNodes.Add(node);
                sampled.Add(sampledPos);
                indexByNode[node] = index;
            }

            int n = validNodes.Count;
            if (n == 0)
                return false;

            float[] goalCost = new float[n];
            float[] dist = new float[n];
            int[] prev = new int[n];
            bool[] visited = new bool[n];

            for (int i = 0; i < n; i++)
            {
                goalCost[i] = GetPathDistance(sampled[i], targetOnMesh, checkpointToTargetPathBuffer);
                dist[i] = float.PositiveInfinity;
                prev[i] = -1;
                visited[i] = false;

                float costFromSelf = GetPathDistance(selfOnMesh, sampled[i], pathToCheckpointBuffer);
                if (IsFinitePathDistance(costFromSelf))
                    dist[i] = costFromSelf;
            }

            bool hasReachableStart = false;
            for (int i = 0; i < n; i++)
            {
                if (IsFinitePathDistance(dist[i]))
                {
                    hasReachableStart = true;
                    break;
                }
            }

            if (!hasReachableStart)
                return false;

            for (int step = 0; step < n; step++)
            {
                int u = -1;
                float bestDist = float.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    if (visited[i])
                        continue;
                    if (dist[i] >= bestDist)
                        continue;
                    bestDist = dist[i];
                    u = i;
                }

                if (u < 0 || !IsFinitePathDistance(bestDist))
                    break;

                visited[u] = true;

                NavMeshCheckpointNode[] nextNodes = validNodes[u].NextNodes;
                if (nextNodes == null || nextNodes.Length == 0)
                    continue;

                for (int j = 0; j < nextNodes.Length; j++)
                {
                    NavMeshCheckpointNode next = nextNodes[j];
                    if (next == null)
                        continue;
                    if (!indexByNode.TryGetValue(next, out int v))
                        continue;

                    float edgeCost = GetPathDistance(sampled[u], sampled[v], pathToCheckpointBuffer);
                    if (!IsFinitePathDistance(edgeCost))
                        continue;

                    float alt = dist[u] + edgeCost;
                    if (alt < dist[v])
                    {
                        dist[v] = alt;
                        prev[v] = u;
                    }
                }
            }

            int bestGoal = -1;
            float bestTotal = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                if (!IsFinitePathDistance(dist[i]) || !IsFinitePathDistance(goalCost[i]))
                    continue;

                float total = dist[i] + goalCost[i];
                if (total < bestTotal)
                {
                    bestTotal = total;
                    bestGoal = i;
                }
            }

            if (bestGoal < 0)
                return false;

            if (checkpointRoutingMode == CheckpointRoutingMode.AssistIfBetter &&
                IsFinitePathDistance(directPathDistance) &&
                bestTotal >= directPathDistance - Mathf.Max(0f, checkpointMinBenefitDistance))
            {
                return false;
            }

            var routeIndices = new List<int>(n);
            int current = bestGoal;
            while (current >= 0)
            {
                routeIndices.Add(current);
                current = prev[current];
            }
            routeIndices.Reverse();

            if (routeIndices.Count == 0)
                return false;

            if (routeIndices.Count == 1 && IsPositionReached(sampled[routeIndices[0]]))
                return false;

            activeCheckpointRoute.Clear();
            for (int i = 0; i < routeIndices.Count; i++)
                activeCheckpointRoute.Add(validNodes[routeIndices[i]]);

            activeRouteIndex = 0;
            LogAi("ROUTE", $"Route built ({activeCheckpointRoute.Count} nodes): {FormatNodeList(activeCheckpointRoute)}");
            return true;
        }

        private NavMeshCheckpointNode[] GetCheckpointNodes()
        {
            if (checkpointNodeTransforms != null && checkpointNodeTransforms.Length > 0)
            {
                var resolved = new List<NavMeshCheckpointNode>(checkpointNodeTransforms.Length);
                for (int i = 0; i < checkpointNodeTransforms.Length; i++)
                {
                    Transform t = checkpointNodeTransforms[i];
                    if (t == null)
                        continue;

                    NavMeshCheckpointNode node = t.GetComponent<NavMeshCheckpointNode>();
                    if (node == null)
                        continue;

                    if (!resolved.Contains(node))
                        resolved.Add(node);
                }

                if (resolved.Count > 0)
                    return resolved.ToArray();
            }

            if (!autoFindCheckpointNodesIfEmpty)
                return null;

            if (cachedCheckpointNodes == null || cachedCheckpointNodes.Length == 0)
                cachedCheckpointNodes = FindObjectsOfType<NavMeshCheckpointNode>();

            return cachedCheckpointNodes;
        }

        private bool TryGetCurrentRouteDestination(out Vector3 destination)
        {
            destination = Vector3.zero;
            if (!HasActiveRoute)
                return false;

            NavMeshCheckpointNode node = activeCheckpointRoute[activeRouteIndex];
            if (node == null)
                return false;

            return TrySampleOnNavMesh(node.transform.position, out destination);
        }

        private void AdvanceRouteIfReached()
        {
            if (!HasActiveRoute)
                return;

            while (HasActiveRoute)
            {
                NavMeshCheckpointNode node = activeCheckpointRoute[activeRouteIndex];
                if (node == null)
                {
                    ClearCheckpointState();
                    return;
                }

                if (!TrySampleOnNavMesh(node.transform.position, out Vector3 checkpointPos))
                {
                    ClearCheckpointState();
                    return;
                }

                if (!IsPositionReached(checkpointPos))
                    return;

                activeRouteIndex++;
                if (activeRouteIndex >= activeCheckpointRoute.Count)
                {
                    ClearCheckpointState();
                    return;
                }
            }
        }

        private bool IsPositionReached(Vector3 position)
        {
            Vector3 delta = position - transform.position;
            float scaledReachDistance = checkpointReachDistance * Mathf.Clamp(checkpointReachDistanceScale, 0.1f, 1f);
            float reachDistance = Mathf.Max(0.1f, scaledReachDistance);
            return delta.sqrMagnitude <= reachDistance * reachDistance;
        }

        private bool TrySampleOnNavMesh(Vector3 sourcePosition, out Vector3 sampledPosition)
        {
            if (NavMesh.SamplePosition(sourcePosition, out NavMeshHit hit, Mathf.Max(0.5f, checkpointSampleRadius), NavMesh.AllAreas))
            {
                sampledPosition = hit.position;
                return true;
            }

            sampledPosition = Vector3.zero;
            return false;
        }

        private float GetPathDistance(Vector3 from, Vector3 to, NavMeshPath pathBuffer)
        {
            if (pathBuffer == null)
                return float.PositiveInfinity;

            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, pathBuffer) || pathBuffer.status != NavMeshPathStatus.PathComplete)
                return float.PositiveInfinity;

            if (pathBuffer.corners == null || pathBuffer.corners.Length < 2)
                return Vector3.Distance(from, to);

            float distance = 0f;
            for (int i = 1; i < pathBuffer.corners.Length; i++)
                distance += Vector3.Distance(pathBuffer.corners[i - 1], pathBuffer.corners[i]);

            return distance;
        }

        private float GetEffectiveDistanceToTarget(float fallbackPlanarDistance)
        {
            if (target == null || !useNavMeshCheckpoints)
                return fallbackPlanarDistance;

            if (Time.time < nextTargetPathDistanceUpdateTime && IsFinitePathDistance(cachedTargetPathDistance))
                return cachedTargetPathDistance;

            nextTargetPathDistanceUpdateTime = Time.time + Mathf.Max(0.05f, repathInterval);

            if (!TrySampleOnNavMesh(transform.position, out Vector3 selfOnMesh) ||
                !TrySampleOnNavMesh(target.position, out Vector3 targetOnMesh))
            {
                cachedTargetPathDistance = fallbackPlanarDistance;
                return cachedTargetPathDistance;
            }

            float pathDistance = GetPathDistance(selfOnMesh, targetOnMesh, directPathBuffer);
            cachedTargetPathDistance = IsFinitePathDistance(pathDistance) ? pathDistance : fallbackPlanarDistance;
            return cachedTargetPathDistance;
        }

        private static bool IsFinitePathDistance(float distance)
        {
            return !(float.IsInfinity(distance) || float.IsNaN(distance));
        }

        private void BlockNodeTemporarily(NavMeshCheckpointNode node)
        {
            if (node == null)
                return;

            float untilTime = Time.time + Mathf.Max(0.5f, blockedNodeDuration);
            blockedNodesUntilTime[node] = untilTime;
        }

        private bool IsNodeBlocked(NavMeshCheckpointNode node)
        {
            if (node == null)
                return false;

            if (!blockedNodesUntilTime.TryGetValue(node, out float untilTime))
                return false;

            return Time.time < untilTime;
        }

        private void PurgeExpiredBlockedNodes()
        {
            if (blockedNodesUntilTime.Count == 0)
                return;

            float now = Time.time;
            var toRemove = new List<NavMeshCheckpointNode>(blockedNodesUntilTime.Count);
            foreach (var pair in blockedNodesUntilTime)
            {
                if (pair.Key == null || now >= pair.Value)
                    toRemove.Add(pair.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                blockedNodesUntilTime.Remove(toRemove[i]);
        }

        private void ClearCheckpointState()
        {
            activeCheckpointRoute.Clear();
            activeRouteIndex = -1;
            nextCheckpointReevaluateTime = 0f;
            ResetLoopCheck();
        }

        private void ClearPatrolState()
        {
            patrolRoute.Clear();
            patrolRouteIndex = -1;
            consecutiveAlternativeRouteFailures = 0;
            ResetLoopCheck();
        }

        private void ResetLoopCheck()
        {
            loopCheckActive = false;
            loopCheckStartTime = 0f;
            observedRouteIndex = -1;
            observedRouteStartDistance = 0f;
            observedRouteBestDistance = 0f;
        }

        private Vector3 GetSeparationFromOtherBots()
        {
            if (minDistanceToOtherBots <= 0f) return Vector3.zero;
            var all = TankRuntime.GetAllTanks();
            Vector3 sum = Vector3.zero;
            int count = 0;
            Vector3 myPos = transform.position;
            myPos.y = 0f;
            for (int i = 0; i < all.Count; i++)
            {
                var t = all[i];
                if (t == null || t == tankController || t.IsLocalPlayer) continue;
                var h = t.GetComponent<TankHealth>();
                if (h != null && !h.IsAlive()) continue;
                Vector3 otherPos = t.transform.position;
                otherPos.y = 0f;
                float d = Vector3.Distance(myPos, otherPos);
                if (d < 0.01f || d > minDistanceToOtherBots) continue;
                Vector3 away = (myPos - otherPos).normalized;
                float strength = 1f - (d / minDistanceToOtherBots);
                sum += away * strength;
                count++;
            }
            if (count == 0) return Vector3.zero;
            return sum.normalized;
        }

        private float GetDistanceToNearestOtherBot()
        {
            if (minDistanceToOtherBots <= 0f) return -1f;
            var all = TankRuntime.GetAllTanks();
            float minSq = float.MaxValue;
            Vector3 myPos = transform.position;
            myPos.y = 0f;
            for (int i = 0; i < all.Count; i++)
            {
                var t = all[i];
                if (t == null || t == tankController || t.IsLocalPlayer) continue;
                var h = t.GetComponent<TankHealth>();
                if (h != null && !h.IsAlive()) continue;
                Vector3 otherPos = t.transform.position;
                otherPos.y = 0f;
                float sq = (otherPos - myPos).sqrMagnitude;
                if (sq < minSq) minSq = sq;
            }
            return minSq < float.MaxValue ? Mathf.Sqrt(minSq) : -1f;
        }

        private void TryFireAtTarget(float distanceToTarget, Vector3 aimPoint, bool hasLineOfSight)
        {
            TankWeapon activeWeapon = GetActiveWeapon();
            if (activeWeapon == null || turret == null)
                return;

            if (distanceToTarget > fireRange)
                return;

            if (requireTurretAlignmentForFire && !turret.IsFirePointAligned)
                return;

            if (requireLineOfSight && !hasLineOfSight)
                return;

            if (tankController != null)
            {
                TankInputCommand fireCommand = new TankInputCommand(
                    0f,
                    0f,
                    Vector2.zero,
                    true,
                    true,
                    false,
                    false,
                    0,
                    true,
                    true,
                    false,
                    0,
                    true,
                    aimPoint,
                    0f
                );

                tankController.ProcessCommand(fireCommand);
                return;
            }

            if (!activeWeapon.CanFire)
                return;

            float stability = turret.GetFireStability();
            activeWeapon.Fire(stability);
            turret.ResetStability();
        }

        private void TryReloadIfNeeded()
        {
            TankWeapon activeWeapon = GetActiveWeapon();
            if (activeWeapon == null)
                return;

            if (!activeWeapon.IsReloading && activeWeapon.CurrentAmmoInMagazine <= 0 && activeWeapon.ReserveAmmo > 0)
                activeWeapon.TryReload();
        }

        private void UpdateCombatWeapon(float distanceToTarget)
        {
            if (!enableWeaponSwitching || tankController == null || Time.time < nextWeaponSwitchTime)
                return;

            WeaponType desiredWeapon =
                distanceToTarget <= machineGunPreferredRange ? WeaponType.MachineGun : WeaponType.Cannon;

            if (tankController.ActiveWeaponType == desiredWeapon)
                return;

            if (desiredWeapon == WeaponType.MachineGun && tankController.MachineGunWeapon == null)
                return;

            if (desiredWeapon == WeaponType.Cannon && tankController.CannonWeapon == null)
                return;

            tankController.SwitchWeapon(desiredWeapon);
            weapon = tankController.Weapon;
            nextWeaponSwitchTime = Time.time + Mathf.Max(0.05f, weaponSwitchCooldown);
        }

        private TankWeapon GetActiveWeapon()
        {
            if (tankController != null && tankController.Weapon != null)
                return tankController.Weapon;

            return weapon;
        }

        private bool HasLineOfSight(Vector3 aimPoint)
        {
            if (target == null)
                return false;

            Vector3 lowPoint = target.position + Vector3.up * Mathf.Max(0.2f, targetAimHeightOffset * 0.5f);
            Vector3 midPoint = aimPoint;
            Vector3 highPoint = target.position + Vector3.up * (targetAimHeightOffset + 1.2f);

            return HasLineOfSightToPoint(lowPoint) ||
                   HasLineOfSightToPoint(midPoint) ||
                   HasLineOfSightToPoint(highPoint);
        }

        private bool HasLineOfSightToPoint(Vector3 point)
        {
            Transform originTransform = weapon != null && weapon.FirePoint != null ? weapon.FirePoint : transform;
            Vector3 origin = originTransform.position;
            Vector3 direction = point - origin;
            float distance = direction.magnitude;
            if (distance <= 0.1f)
                return true;

            direction /= distance;
            Ray ray = new Ray(origin + direction * 0.25f, direction);
            int hitCount = Physics.RaycastNonAlloc(ray, lineOfSightHits, distance, lineOfSightMask, QueryTriggerInteraction.Ignore);
            if (hitCount <= 0)
                return true;

            float nearestDistance = float.PositiveInfinity;
            Transform nearestHitTransform = null;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = lineOfSightHits[i];
                Transform hitTransform = hit.transform;
                if (hitTransform == null)
                    continue;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestHitTransform = hitTransform;
                }
            }

            if (nearestHitTransform == null)
                return true;

            return nearestHitTransform == target || nearestHitTransform.IsChildOf(target);
        }

        private Vector3 GetTargetAimPoint(Transform targetTransform)
        {
            Vector3 point = targetTransform.position;
            point.y += targetAimHeightOffset;
            return point;
        }

        private static float GetPlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void SetNoLineOfSightHoldState()
        {
            currentVerticalInput = 0f;
            currentHorizontalInput = 0f;

            if (agent != null && agent.isOnNavMesh && agent.hasPath)
                agent.ResetPath();

            lastDestinationMode = DestinationMode.None;
            nextDestinationModeLogTime = 0f;

            if (lastIdleReason != "NoLineOfSight")
            {
                lastIdleReason = "NoLineOfSight";
                LogAi("IDLE", "Hold due to NoLineOfSight (route preserved).");
            }

            turret?.ClearAimPoint();
            TankWeapon activeWeapon = GetActiveWeapon();
            activeWeapon?.ClearAimPoint();

            if (turret != null && turret.IsAiming)
                turret.StopAiming();
        }

        private void LogDestinationMode(DestinationMode mode, Vector3 destination, float stoppingDistance)
        {
            if (!enableAiDebugLogs)
                return;

            bool modeChanged = mode != lastDestinationMode;
            bool cooldownPassed = Time.time >= nextDestinationModeLogTime;
            if (!modeChanged && !cooldownPassed)
                return;

            nextDestinationModeLogTime = Time.time + Mathf.Max(0.1f, destinationModeLogCooldown);
            if (modeChanged)
                lastDestinationMode = mode;

            LogAi("DEST", $"Mode={mode}, stop={stoppingDistance:F2}, destination=({destination.x:F1},{destination.y:F1},{destination.z:F1})");
        }

        private void LogAi(string tag, string message)
        {
            if (!enableAiDebugLogs)
                return;

            Debug.Log($"[NavMeshTankAI][{name}][{tag}] {message}", this);
        }

        private static string FormatNodeList(List<NavMeshCheckpointNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return "<empty>";

            var parts = new List<string>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
                parts.Add(GetNodeName(nodes[i]));

            return string.Join(" -> ", parts);
        }

        private static string GetNodeName(NavMeshCheckpointNode node)
        {
            return node != null ? node.name : "<null>";
        }

        private void SetIdleState(string reason = null)
        {
            currentVerticalInput = 0f;
            currentHorizontalInput = 0f;

            if (agent != null && agent.isOnNavMesh && agent.hasPath)
                agent.ResetPath();

            ClearCheckpointState();
            ClearPatrolState();
            lastDestinationMode = DestinationMode.None;
            nextDestinationModeLogTime = 0f;

            if (!string.IsNullOrEmpty(reason) && reason != lastIdleReason)
            {
                lastIdleReason = reason;
                LogAi("IDLE", $"Enter idle: {reason}");
            }

            turret?.ClearAimPoint();
            TankWeapon activeWeapon = GetActiveWeapon();
            activeWeapon?.ClearAimPoint();

            if (turret != null && turret.IsAiming)
                turret.StopAiming();
        }
    }
}
