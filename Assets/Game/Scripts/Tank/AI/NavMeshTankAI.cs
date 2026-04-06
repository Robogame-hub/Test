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
    public sealed class NavMeshTankAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private TankController tankController;
        [SerializeField] private TankMovement movement;
        [SerializeField] private TankTurret turret;
        [SerializeField] private TankWeapon weapon;
        [SerializeField] private TankHealth health;
        [SerializeField] private NavMeshAgent agent;

        [Header("Target")]
        [SerializeField] private bool autoFindLocalPlayerTarget = true;
        [SerializeField] private float targetSearchInterval = 1f;
        [SerializeField] private float targetAimHeightOffset = 0.6f;
        [SerializeField] private float targetDetectionRange = 70f;
        [SerializeField] [Range(10f, 180f)] private float visionConeAngle = 130f;

        [Header("Movement")]
        [SerializeField] private float chaseRange = 80f;
        [SerializeField] private float attackRange = 22f;
        [SerializeField] private float repathInterval = 0.2f;
        [SerializeField] private float turnAngleForFullInput = 45f;
        [SerializeField] private float moveAngleLimit = 75f;
        [SerializeField] private float rotateInPlaceAngle = 45f;
        [SerializeField] private float rotateInPlaceExitAngle = 18f;
        [SerializeField] private float turnDeadZoneAngle = 3f;
        [SerializeField] private float turnFlipSuppressionAngle = 20f;
        [SerializeField] private float slowMoveTurnAngle = 25f;
        [SerializeField] [Range(0f, 1f)] private float slowMoveFactorAtHighTurn = 0.25f;
        [SerializeField] private float pathCornerLookAheadDistance = 2.5f;
        [SerializeField] private float desiredDirectionSmoothing = 10f;
        [SerializeField] private float destinationSampleRadius = 3f;
        [SerializeField] private bool invertBodyForward = true;

        [Header("Patrol")]
        [SerializeField] private bool useNavMeshCheckpoints = true;
        [SerializeField] private Transform[] checkpointNodeTransforms;
        [SerializeField] private bool autoFindCheckpointNodesIfEmpty = true;
        [SerializeField] private bool uniquePatrolRoutePerBot = true;
        [SerializeField] private float checkpointReachDistance = 0.12f;
        [SerializeField] private float checkpointSampleRadius = 1.5f;
        [SerializeField] private float checkpointMaxSnapDistance = 1.25f;
        [SerializeField] private float patrolStoppingDistance = 0.03f;
        [SerializeField] private float patrolStuckTimeout = 3f;
        [SerializeField] private float patrolPathFailureTimeout = 1.5f;
        [SerializeField] private float patrolRouteRebuildInterval = 4f;
        [SerializeField] private int fallbackPatrolPointCount = 5;
        [SerializeField] private float fallbackPatrolRadius = 50f;
        [SerializeField] private float fallbackPatrolPointSpacing = 8f;

        [Header("Stuck Recovery")]
        [SerializeField] private float stuckCheckInterval = 0.35f;
        [SerializeField] private float stuckMinMoveDistance = 0.2f;
        [SerializeField] private float stuckTimeToRecover = 1.3f;
        [SerializeField] private float stuckMinDistanceToGoal = 1.5f;
        [SerializeField] [Range(-1f, -0.1f)] private float stuckReverseInput = -0.8f;
        [SerializeField] private float stuckReverseDuration = 0.55f;
        [SerializeField] [Range(0.1f, 1f)] private float stuckTurnInput = 0.9f;
        [SerializeField] private float stuckTurnDuration = 0.6f;
        [SerializeField] private float stuckRecoveryCooldown = 1.2f;
        [SerializeField] private int maxRecoveriesBeforeSkipPoint = 2;

        [Header("Combat")]
        [SerializeField] private float fireRange = 25f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask lineOfSightMask = ~0;
        [SerializeField] private bool requireTurretAlignmentForFire = false;
        [SerializeField] private float chaseMemoryDuration = 2f;
        [SerializeField] private float searchDuration = 4f;
        [SerializeField] private float searchReachDistance = 3f;

        [Header("Squad Tactics")]
        [SerializeField] private bool enableSquadCoordination = true;
        [SerializeField] private float allyAttackClaimRange = 18f;
        [SerializeField] private float flankRadius = 24f;
        [SerializeField] private float flankRadiusJitter = 3f;

        [Header("Weapon Switching")]
        [SerializeField] private bool enableWeaponSwitching = true;
        [SerializeField] private float machineGunPreferredRange = 12f;
        [SerializeField] private float weaponSwitchCooldown = 0.5f;

        [Header("Authority")]
        [SerializeField] private bool forceBotAuthority = true;
        [SerializeField] private TankController.AuthorityMode botAuthorityMode = TankController.AuthorityMode.LocalOnly;

        [Header("Debug")]
        [SerializeField] private bool enableAiDebugLogs = false;

        private readonly RaycastHit[] lineOfSightHits = new RaycastHit[16];
        private readonly List<NavMeshCheckpointNode> checkpointBuffer = new List<NavMeshCheckpointNode>(32);
        private readonly List<NavMeshTankAI> squadBuffer = new List<NavMeshTankAI>(16);

        private NavMeshCheckpointNode[] cachedCheckpointNodes;
        private TankAIBrain brain;
        private TankAIPatrolPlanner patrolPlanner;

        private float nextTargetSearchTime;
        private float nextRepathTime;
        private float nextWeaponSwitchTime;
        private float nextPatrolRouteRebuildTime;
        private float invalidPathStartTime = float.NegativeInfinity;

        private Vector3 lastAgentDestination;
        private bool hasLastAgentDestination;
        private TankAIState lastLoggedState = TankAIState.Idle;
        private TankAIState previousDecisionState = TankAIState.Idle;
        private int patrolVariantSeed;
        private int patrolRouteRebuildCount;
        private int lastTurnSign;
        private bool rotateInPlaceActive;
        private Vector3 smoothedDesiredWorld;
        private bool hasSmoothedDesiredWorld;
        private StuckRecoveryPhase stuckRecoveryPhase = StuckRecoveryPhase.None;
        private float stuckRecoveryPhaseEndTime;
        private int stuckRecoveryTurnSign = 1;
        private float nextStuckSampleTime;
        private Vector3 lastStuckSamplePosition;
        private bool hasStuckSamplePosition;
        private float stuckAccumulatedTime;
        private int consecutiveStuckRecoveries;
        private float stuckRecoveryCooldownUntil;

        private enum StuckRecoveryPhase
        {
            None = 0,
            Reverse = 1,
            Pivot = 2
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureAgent();
            brain = new TankAIBrain(chaseMemoryDuration, searchDuration, searchReachDistance);
            patrolPlanner = new TankAIPatrolPlanner();
            patrolVariantSeed = Mathf.Abs(GetInstanceID());
        }

        private void OnEnable()
        {
            ApplyBotAuthority();
            ResetRuntimeState();
            BuildPatrolRoute(true);
        }

        private void OnDisable()
        {
            SendIdleCommand();
            if (agent != null && agent.isOnNavMesh && agent.hasPath)
                agent.ResetPath();
        }

        private void Update()
        {
            if (!IsOperational())
            {
                SendIdleCommand();
                return;
            }

            if (tankController != null && tankController.IsLocalPlayer)
            {
                SendIdleCommand();
                return;
            }

            TryAutoFindTarget();
            Transform targetTransform = ResolveTarget();
            bool hasTarget = targetTransform != null;

            Vector3 targetPosition = hasTarget ? targetTransform.position : Vector3.zero;
            float distanceToTarget = hasTarget
                ? GetPlanarDistance(transform.position, targetPosition)
                : float.PositiveInfinity;

            float effectiveDetectionRange = Mathf.Max(Mathf.Max(0.5f, targetDetectionRange), chaseRange);
            bool targetInDetectionRange = hasTarget && distanceToTarget <= effectiveDetectionRange;
            Vector3 visibleAimPoint = hasTarget ? GetTargetAimPoint(targetTransform) : Vector3.zero;
            bool targetVisible = hasTarget && IsTargetVisible(targetTransform, visibleAimPoint, distanceToTarget);

            TankAIBrainInput brainInput = new TankAIBrainInput
            {
                Time = Time.time,
                SelfPosition = transform.position,
                HasTarget = hasTarget,
                TargetVisible = targetVisible,
                TargetInDetectionRange = targetInDetectionRange,
                TargetPosition = targetPosition
            };

            TankAIBrainDecision decision = brain.Evaluate(brainInput);
            LogStateChange(decision.State);

            if (decision.State == TankAIState.Patrol && previousDecisionState != TankAIState.Patrol)
                BuildPatrolRoute(true);
            previousDecisionState = decision.State;

            bool hasDestination = TryResolveDestination(
                decision,
                targetTransform,
                hasTarget,
                targetPosition,
                distanceToTarget,
                out Vector3 destination,
                out float stoppingDistance);
            UpdateAgentDestination(hasDestination, destination, stoppingDistance);
            HandlePatrolPathFailures(decision.State, hasDestination);
            UpdateStuckRecoveryState(decision.State, hasDestination, destination, stoppingDistance);

            Vector2 movementInput = ComputeMovementInput(hasDestination);
            UpdateCombatWeapon(hasTarget ? distanceToTarget : float.PositiveInfinity);

            bool shouldAim = decision.ShouldAim && (hasTarget || decision.State == TankAIState.Search);
            Vector3 aimPoint = shouldAim
                ? (hasTarget ? visibleAimPoint : decision.AimPoint + Vector3.up * targetAimHeightOffset)
                : Vector3.zero;

            bool shouldFire = hasTarget && targetVisible && distanceToTarget <= Mathf.Max(0.5f, fireRange);
            if (requireTurretAlignmentForFire && turret != null && !turret.IsFirePointAligned)
                shouldFire = false;

            TankWeapon activeWeapon = GetActiveWeapon();
            bool shouldReload = ShouldReload(activeWeapon);

            SendCommand(movementInput.y, movementInput.x, shouldAim, aimPoint, shouldFire, shouldReload);
        }

        private void ResolveReferences()
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
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
        }

        private void ConfigureAgent()
        {
            if (agent == null)
                return;

            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.autoBraking = true;
            agent.autoRepath = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = 50 + (Mathf.Abs(GetInstanceID()) % 49);

            if (movement != null)
            {
                agent.speed = Mathf.Max(0.1f, movement.MoveSpeed);
                agent.angularSpeed = Mathf.Max(120f, movement.RotationSpeed * 2f);
                agent.acceleration = Mathf.Max(6f, movement.MoveSpeed * 5f);
            }
        }

        private void ApplyBotAuthority()
        {
            if (!forceBotAuthority || tankController == null)
                return;

            tankController.SetIsLocalPlayer(false);
            tankController.SetAuthorityMode(botAuthorityMode);
        }

        private void ResetRuntimeState()
        {
            brain?.Reset();
            nextTargetSearchTime = 0f;
            nextRepathTime = 0f;
            nextWeaponSwitchTime = 0f;
            nextPatrolRouteRebuildTime = 0f;
            invalidPathStartTime = float.NegativeInfinity;
            hasLastAgentDestination = false;
            lastLoggedState = TankAIState.Idle;
            previousDecisionState = TankAIState.Idle;
            patrolRouteRebuildCount = 0;
            lastTurnSign = 0;
            rotateInPlaceActive = false;
            smoothedDesiredWorld = Vector3.zero;
            hasSmoothedDesiredWorld = false;
            stuckRecoveryPhase = StuckRecoveryPhase.None;
            stuckRecoveryPhaseEndTime = 0f;
            stuckRecoveryTurnSign = 1;
            nextStuckSampleTime = 0f;
            lastStuckSamplePosition = Vector3.zero;
            hasStuckSamplePosition = false;
            stuckAccumulatedTime = 0f;
            consecutiveStuckRecoveries = 0;
            stuckRecoveryCooldownUntil = 0f;
        }

        private bool IsOperational()
        {
            if (movement == null || turret == null || health == null || agent == null)
                return false;
            if (!health.IsAlive())
                return false;
            if (!agent.isOnNavMesh)
                return false;
            return true;
        }

        private void TryAutoFindTarget()
        {
            if (!autoFindLocalPlayerTarget || target != null || Time.time < nextTargetSearchTime)
                return;

            nextTargetSearchTime = Time.time + Mathf.Max(0.1f, targetSearchInterval);
            TankController localPlayer = TankRuntime.GetLocalPlayer();
            if (localPlayer == null || localPlayer == tankController)
                return;

            TankHealth localHealth = localPlayer.GetComponent<TankHealth>();
            if (localHealth != null && !localHealth.IsAlive())
                return;

            target = localPlayer.transform;
        }

        private Transform ResolveTarget()
        {
            if (target == null)
                return null;

            if (target == transform || target.IsChildOf(transform))
            {
                target = null;
                return null;
            }

            TankHealth targetHealth = target.GetComponentInParent<TankHealth>();
            if (targetHealth != null && !targetHealth.IsAlive())
            {
                target = null;
                return null;
            }

            return target;
        }

        private bool TryResolveDestination(
            TankAIBrainDecision decision,
            Transform targetTransform,
            bool hasTarget,
            Vector3 targetPosition,
            float distanceToTarget,
            out Vector3 destination,
            out float stoppingDistance)
        {
            destination = Vector3.zero;
            stoppingDistance = Mathf.Max(0.5f, attackRange * 0.85f);
            float patrolReachDistance = Mathf.Clamp(checkpointReachDistance, 0.03f, 0.2f);
            float patrolStopDistance = Mathf.Clamp(patrolStoppingDistance, 0.01f, 0.12f);

            switch (decision.State)
            {
                case TankAIState.Chase:
                {
                    if (hasTarget)
                    {
                        Vector3 chaseDestination = targetPosition;
                        if (enableSquadCoordination &&
                            TryGetSquadChaseDestination(targetTransform, targetPosition, distanceToTarget, out Vector3 squadDestination))
                        {
                            chaseDestination = squadDestination;
                        }

                        if (GetPlanarDistance(transform.position, targetPosition) > chaseRange)
                        {
                            destination = chaseDestination;
                            stoppingDistance = Mathf.Max(0.5f, attackRange * 0.75f);
                            return true;
                        }

                        destination = chaseDestination;
                        stoppingDistance = Mathf.Max(0.5f, attackRange * 0.75f);
                        return true;
                    }

                    if (decision.HasDestination)
                    {
                        destination = decision.Destination;
                        stoppingDistance = Mathf.Max(0.5f, attackRange * 0.75f);
                        return true;
                    }

                    return false;
                }

                case TankAIState.Search:
                {
                    if (decision.HasDestination)
                    {
                        destination = decision.Destination;
                        stoppingDistance = patrolStopDistance;
                        return true;
                    }

                    return false;
                }

                case TankAIState.Patrol:
                {
                    if (Time.time >= nextPatrolRouteRebuildTime && (patrolPlanner == null || !patrolPlanner.HasRoute))
                        BuildPatrolRoute(true);

                    if (patrolPlanner != null &&
                        patrolPlanner.TryGetCurrentDestination(
                            transform.position,
                            Time.time,
                            patrolReachDistance,
                            patrolStuckTimeout,
                            out destination))
                    {
                        stoppingDistance = patrolStopDistance;
                        return true;
                    }

                    BuildPatrolRoute(false);

                    if (patrolPlanner != null &&
                        patrolPlanner.TryGetCurrentDestination(
                            transform.position,
                            Time.time,
                            patrolReachDistance,
                            patrolStuckTimeout,
                            out destination))
                    {
                        stoppingDistance = patrolStopDistance;
                        return true;
                    }

                    return false;
                }

                case TankAIState.Idle:
                default:
                    return false;
            }
        }

        private void UpdateAgentDestination(bool hasDestination, Vector3 destination, float stoppingDistance)
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            if (stuckRecoveryPhase != StuckRecoveryPhase.None)
            {
                if (agent.hasPath)
                    agent.ResetPath();
                hasLastAgentDestination = false;
                return;
            }

            agent.nextPosition = transform.position;

            if (!hasDestination)
            {
                if (agent.hasPath)
                    agent.ResetPath();
                hasLastAgentDestination = false;
                return;
            }

            if (!TryProjectDestinationOnNavMesh(destination, out Vector3 projectedDestination))
            {
                if (agent.hasPath)
                    agent.ResetPath();
                hasLastAgentDestination = false;
                return;
            }

            bool destinationChanged = !hasLastAgentDestination ||
                                      (lastAgentDestination - projectedDestination).sqrMagnitude > 0.1225f;

            if (!destinationChanged && Time.time < nextRepathTime)
                return;

            nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);
            agent.stoppingDistance = Mathf.Max(0.1f, stoppingDistance);
            if (!agent.SetDestination(projectedDestination))
                return;

            lastAgentDestination = projectedDestination;
            hasLastAgentDestination = true;
        }

        private void HandlePatrolPathFailures(TankAIState state, bool hasDestination)
        {
            if (state != TankAIState.Patrol ||
                !hasDestination ||
                patrolPlanner == null ||
                agent == null ||
                !agent.isOnNavMesh ||
                stuckRecoveryPhase != StuckRecoveryPhase.None)
            {
                invalidPathStartTime = float.NegativeInfinity;
                return;
            }

            if (agent.pathPending)
                return;

            bool invalidPath = agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                               agent.pathStatus == NavMeshPathStatus.PathPartial;

            if (!invalidPath)
            {
                invalidPathStartTime = float.NegativeInfinity;
                return;
            }

            if (invalidPathStartTime < 0f)
            {
                invalidPathStartTime = Time.time;
                return;
            }

            if (Time.time - invalidPathStartTime < Mathf.Max(0.5f, patrolPathFailureTimeout))
                return;

            patrolPlanner.SkipCurrentPoint();
            invalidPathStartTime = float.NegativeInfinity;
            stuckAccumulatedTime = 0f;
            hasStuckSamplePosition = false;
            LogAi("PATROL", "Skipped patrol point due to invalid/partial path.");
        }

        private bool TryGetSquadChaseDestination(
            Transform targetTransform,
            Vector3 targetPosition,
            float selfDistanceToTarget,
            out Vector3 destination)
        {
            destination = targetPosition;

            if (!enableSquadCoordination || targetTransform == null)
                return false;
            if (selfDistanceToTarget <= Mathf.Max(2f, attackRange * 0.85f))
                return false;

            PopulateSquadBufferForTarget(targetTransform.root, squadBuffer);
            if (squadBuffer.Count <= 1)
                return false;

            bool anotherBotAlreadyEngaging = false;
            float claimDistance = Mathf.Max(attackRange * 0.95f, allyAttackClaimRange);
            for (int i = 0; i < squadBuffer.Count; i++)
            {
                NavMeshTankAI ai = squadBuffer[i];
                if (ai == null || ai == this || !ai.isActiveAndEnabled)
                    continue;

                if (ai.health != null && !ai.health.IsAlive())
                    continue;

                float allyDistanceToTarget = GetPlanarDistance(ai.transform.position, targetPosition);
                if (allyDistanceToTarget <= claimDistance)
                {
                    anotherBotAlreadyEngaging = true;
                    break;
                }
            }

            if (!anotherBotAlreadyEngaging)
                return false;

            squadBuffer.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            int selfSlot = squadBuffer.IndexOf(this);
            if (selfSlot < 0)
                return false;

            int slotCount = Mathf.Max(2, squadBuffer.Count);
            float angleStep = 360f / slotCount;
            float baseAngle = patrolVariantSeed % 360;
            float baseRadius = Mathf.Max(flankRadius, attackRange * 1.15f);

            for (int attempt = 0; attempt < slotCount; attempt++)
            {
                int slot = (selfSlot + attempt) % slotCount;
                float radius = baseRadius + (slot % 2) * Mathf.Max(0f, flankRadiusJitter);
                float angle = baseAngle + angleStep * slot;
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 candidate = targetPosition + radial * radius;

                if (!TryProjectDestinationOnNavMesh(candidate, out Vector3 projected))
                    continue;

                if (!TryHasCompletePathTo(projected))
                    continue;

                destination = projected;
                return true;
            }

            return false;
        }

        private void PopulateSquadBufferForTarget(Transform targetRoot, List<NavMeshTankAI> output)
        {
            output.Clear();
            if (targetRoot == null)
                return;

            IReadOnlyList<TankController> allTanks = TankRuntime.GetAllTanks();
            for (int i = 0; i < allTanks.Count; i++)
            {
                TankController controller = allTanks[i];
                if (controller == null || controller.IsLocalPlayer)
                    continue;

                NavMeshTankAI ai = controller.GetComponent<NavMeshTankAI>();
                if (ai == null || !ai.isActiveAndEnabled)
                    continue;

                if (ai.health != null && !ai.health.IsAlive())
                    continue;

                if (ai.target == null || ai.target.root != targetRoot)
                    continue;

                output.Add(ai);
            }
        }

        private bool TryGetCurrentSteeringDirection(out Vector3 direction)
        {
            direction = Vector3.zero;
            if (agent == null || !agent.isOnNavMesh)
                return false;

            if (!agent.pathPending && agent.hasPath && agent.path.corners != null && agent.path.corners.Length >= 2)
            {
                float lookAheadDistance = Mathf.Max(0.5f, pathCornerLookAheadDistance);
                Vector3[] corners = agent.path.corners;
                for (int i = 1; i < corners.Length; i++)
                {
                    Vector3 toCorner = corners[i] - transform.position;
                    toCorner.y = 0f;
                    if (toCorner.sqrMagnitude <= 0.01f)
                        continue;

                    if (toCorner.magnitude >= lookAheadDistance || i == corners.Length - 1)
                    {
                        direction = toCorner.normalized;
                        return true;
                    }
                }
            }

            Vector3 toSteeringTarget = agent.steeringTarget - transform.position;
            toSteeringTarget.y = 0f;
            if (toSteeringTarget.sqrMagnitude > 0.01f)
            {
                direction = toSteeringTarget.normalized;
                return true;
            }

            Vector3 desiredVelocity = agent.desiredVelocity;
            desiredVelocity.y = 0f;
            if (desiredVelocity.sqrMagnitude > 0.01f)
            {
                direction = desiredVelocity.normalized;
                return true;
            }

            return false;
        }

        private void UpdateStuckRecoveryState(
            TankAIState state,
            bool hasDestination,
            Vector3 destination,
            float stoppingDistance)
        {
            if (stuckRecoveryPhase != StuckRecoveryPhase.None)
            {
                TryAdvanceStuckRecoveryPhase();
                return;
            }

            bool movingState = state == TankAIState.Patrol || state == TankAIState.Chase || state == TankAIState.Search;
            if (!movingState || !hasDestination || agent == null || !agent.isOnNavMesh)
            {
                nextStuckSampleTime = 0f;
                hasStuckSamplePosition = false;
                stuckAccumulatedTime = 0f;
                consecutiveStuckRecoveries = 0;
                return;
            }

            if (Time.time < stuckRecoveryCooldownUntil)
                return;

            if (agent.pathPending)
                return;

            float goalThreshold = Mathf.Max(stuckMinDistanceToGoal, stoppingDistance + Mathf.Max(0.2f, checkpointReachDistance));
            float distanceToGoal = GetPlanarDistance(transform.position, destination);
            if (distanceToGoal <= goalThreshold)
            {
                stuckAccumulatedTime = 0f;
                hasStuckSamplePosition = false;
                consecutiveStuckRecoveries = 0;
                return;
            }

            if (Time.time < nextStuckSampleTime)
                return;

            float sampleInterval = Mathf.Max(0.1f, stuckCheckInterval);
            nextStuckSampleTime = Time.time + sampleInterval;

            Vector3 samplePosition = transform.position;
            samplePosition.y = 0f;

            if (!hasStuckSamplePosition)
            {
                hasStuckSamplePosition = true;
                lastStuckSamplePosition = samplePosition;
                return;
            }

            float movedDistance = Vector3.Distance(lastStuckSamplePosition, samplePosition);
            lastStuckSamplePosition = samplePosition;

            bool wantsToMove = agent.desiredVelocity.sqrMagnitude > 0.09f ||
                               (agent.hasPath && agent.remainingDistance > goalThreshold);
            if (!wantsToMove)
            {
                stuckAccumulatedTime = 0f;
                return;
            }

            if (TryGetCurrentSteeringDirection(out Vector3 steeringDirection))
            {
                Vector3 forward = invertBodyForward ? -transform.forward : transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.01f)
                {
                    float headingError = Mathf.Abs(Vector3.SignedAngle(forward.normalized, steeringDirection, Vector3.up));
                    float turningThreshold = Mathf.Max(rotateInPlaceExitAngle, slowMoveTurnAngle + 4f);
                    if (headingError >= turningThreshold)
                    {
                        stuckAccumulatedTime = 0f;
                        return;
                    }
                }
            }

            if (movedDistance < Mathf.Max(0.02f, stuckMinMoveDistance))
            {
                stuckAccumulatedTime += sampleInterval;
            }
            else
            {
                stuckAccumulatedTime = 0f;
                if (consecutiveStuckRecoveries > 0)
                    consecutiveStuckRecoveries--;
            }

            if (stuckAccumulatedTime < Mathf.Max(0.5f, stuckTimeToRecover))
                return;

            BeginStuckRecovery(state, destination);
        }

        private void BeginStuckRecovery(TankAIState state, Vector3 destination)
        {
            Vector3 toGoal = destination - transform.position;
            toGoal.y = 0f;

            Vector3 forward = invertBodyForward ? -transform.forward : transform.forward;
            forward.y = 0f;

            int turnSign = 1;
            if (toGoal.sqrMagnitude > 0.01f && forward.sqrMagnitude > 0.01f)
            {
                float signedAngle = Vector3.SignedAngle(forward.normalized, toGoal.normalized, Vector3.up);
                if (Mathf.Abs(signedAngle) > 1f)
                    turnSign = signedAngle >= 0f ? 1 : -1;
                else
                    turnSign = ((patrolVariantSeed + consecutiveStuckRecoveries) & 1) == 0 ? 1 : -1;
            }
            else
            {
                turnSign = ((patrolVariantSeed + consecutiveStuckRecoveries) & 1) == 0 ? 1 : -1;
            }

            stuckRecoveryTurnSign = turnSign == 0 ? 1 : turnSign;
            stuckRecoveryPhase = StuckRecoveryPhase.Reverse;
            stuckRecoveryPhaseEndTime = Time.time + Mathf.Max(0.1f, stuckReverseDuration);
            stuckAccumulatedTime = 0f;
            hasStuckSamplePosition = false;
            consecutiveStuckRecoveries++;

            if (state == TankAIState.Patrol &&
                maxRecoveriesBeforeSkipPoint > 0 &&
                consecutiveStuckRecoveries >= maxRecoveriesBeforeSkipPoint &&
                patrolPlanner != null)
            {
                patrolPlanner.SkipCurrentPoint();
                consecutiveStuckRecoveries = 0;
                LogAi("STUCK", "Repeated recovery on patrol point, skipped to next linked checkpoint.");
            }

            if (agent != null && agent.isOnNavMesh && agent.hasPath)
                agent.ResetPath();

            hasLastAgentDestination = false;
            nextRepathTime = 0f;
            rotateInPlaceActive = false;
            lastTurnSign = 0;
            LogAi("STUCK", $"Recovery started. phase=Reverse turn={stuckRecoveryTurnSign}");
        }

        private void TryAdvanceStuckRecoveryPhase()
        {
            if (stuckRecoveryPhase == StuckRecoveryPhase.None || Time.time < stuckRecoveryPhaseEndTime)
                return;

            if (stuckRecoveryPhase == StuckRecoveryPhase.Reverse)
            {
                stuckRecoveryPhase = StuckRecoveryPhase.Pivot;
                stuckRecoveryPhaseEndTime = Time.time + Mathf.Max(0.1f, stuckTurnDuration);
                LogAi("STUCK", "Recovery phase switched to Pivot.");
                return;
            }

            stuckRecoveryPhase = StuckRecoveryPhase.None;
            stuckRecoveryPhaseEndTime = 0f;
            hasLastAgentDestination = false;
            nextRepathTime = 0f;
            hasStuckSamplePosition = false;
            stuckAccumulatedTime = 0f;
            stuckRecoveryCooldownUntil = Time.time + Mathf.Max(0.2f, stuckRecoveryCooldown);
            LogAi("STUCK", "Recovery completed.");
        }

        private bool TryGetStuckRecoveryInput(out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            if (stuckRecoveryPhase == StuckRecoveryPhase.None)
                return false;

            if (stuckRecoveryPhase == StuckRecoveryPhase.Reverse)
            {
                float reverseVertical = Mathf.Clamp(stuckReverseInput, -1f, -0.05f);
                float reverseHorizontal = Mathf.Clamp01(Mathf.Abs(stuckTurnInput)) * 0.35f * stuckRecoveryTurnSign;
                movementInput = new Vector2(reverseHorizontal, reverseVertical);
                return true;
            }

            float turnHorizontal = Mathf.Clamp01(Mathf.Abs(stuckTurnInput)) * stuckRecoveryTurnSign;
            movementInput = new Vector2(turnHorizontal, 0f);
            return true;
        }

        private Vector2 ComputeMovementInput(bool hasDestination)
        {
            if (TryGetStuckRecoveryInput(out Vector2 stuckRecoveryInput))
                return stuckRecoveryInput;

            if (!hasDestination || agent == null || !agent.isOnNavMesh)
            {
                hasSmoothedDesiredWorld = false;
                return Vector2.zero;
            }

            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                hasSmoothedDesiredWorld = false;
                return Vector2.zero;
            }

            Vector3 desiredWorld = Vector3.zero;

            if (!agent.pathPending && agent.hasPath && agent.path.corners != null && agent.path.corners.Length >= 2)
            {
                float lookAheadDistance = Mathf.Max(0.5f, pathCornerLookAheadDistance);
                Vector3[] corners = agent.path.corners;
                for (int i = 1; i < corners.Length; i++)
                {
                    Vector3 toCorner = corners[i] - transform.position;
                    toCorner.y = 0f;
                    if (toCorner.sqrMagnitude <= 0.01f)
                        continue;

                    if (toCorner.magnitude >= lookAheadDistance || i == corners.Length - 1)
                    {
                        desiredWorld = toCorner.normalized;
                        break;
                    }
                }
            }

            if (desiredWorld.sqrMagnitude < 0.01f)
            {
                Vector3 toSteeringTarget = agent.steeringTarget - transform.position;
                toSteeringTarget.y = 0f;
                if (toSteeringTarget.sqrMagnitude > 0.01f)
                    desiredWorld = toSteeringTarget.normalized;
            }

            if (desiredWorld.sqrMagnitude < 0.01f)
            {
                desiredWorld = agent.desiredVelocity;
                desiredWorld.y = 0f;
            }

            if (desiredWorld.sqrMagnitude < 0.01f)
                return Vector2.zero;

            desiredWorld.Normalize();
            desiredWorld = GetSmoothedDesiredDirection(desiredWorld);

            Vector3 forward = invertBodyForward ? -transform.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return Vector2.zero;
            forward.Normalize();

            float signedAngle = Vector3.SignedAngle(forward, desiredWorld, Vector3.up);
            float absAngle = Mathf.Abs(signedAngle);

            float rotateEnter = Mathf.Max(1f, rotateInPlaceAngle);
            float rotateExitUpper = Mathf.Max(1f, rotateEnter - 1f);
            float rotateExit = Mathf.Clamp(rotateInPlaceExitAngle, 1f, rotateExitUpper);
            if (rotateInPlaceActive)
            {
                if (absAngle <= rotateExit)
                    rotateInPlaceActive = false;
            }
            else if (absAngle >= rotateEnter)
            {
                rotateInPlaceActive = true;
            }

            if (rotateInPlaceActive)
            {
                int turnSign = GetStableTurnSign(signedAngle, absAngle);
                if (turnSign == 0)
                    turnSign = lastTurnSign != 0 ? lastTurnSign : 1;

                lastTurnSign = turnSign;
                return new Vector2(turnSign, 0f);
            }

            if (absAngle <= Mathf.Max(0f, turnDeadZoneAngle))
            {
                lastTurnSign = 0;
                return new Vector2(0f, 1f);
            }

            int stableTurnSign = GetStableTurnSign(signedAngle, absAngle);
            float stableSignedAngle = stableTurnSign == 0 ? signedAngle : stableTurnSign * absAngle;
            lastTurnSign = stableTurnSign != 0 ? stableTurnSign : lastTurnSign;

            float horizontal = Mathf.Clamp(stableSignedAngle / Mathf.Max(1f, turnAngleForFullInput), -1f, 1f);
            if (Mathf.Abs(horizontal) <= 0.05f)
                horizontal = 0f;
            float vertical = absAngle <= moveAngleLimit ? 1f : 0f;

            if (vertical > 0f && absAngle > slowMoveTurnAngle)
            {
                float turnRangeMax = Mathf.Max(slowMoveTurnAngle + 0.1f, rotateInPlaceAngle);
                float t = Mathf.InverseLerp(slowMoveTurnAngle, turnRangeMax, absAngle);
                vertical = Mathf.Lerp(1f, Mathf.Clamp01(slowMoveFactorAtHighTurn), t);
            }

            return new Vector2(horizontal, vertical);
        }

        private int GetStableTurnSign(float signedAngle, float absAngle)
        {
            if (absAngle <= Mathf.Max(0f, turnDeadZoneAngle))
                return 0;

            int rawTurnSign = signedAngle >= 0f ? 1 : -1;
            if (lastTurnSign != 0 &&
                rawTurnSign != lastTurnSign &&
                absAngle <= Mathf.Max(turnDeadZoneAngle + 0.1f, turnFlipSuppressionAngle))
            {
                return lastTurnSign;
            }

            return rawTurnSign;
        }

        private Vector3 GetSmoothedDesiredDirection(Vector3 desiredWorld)
        {
            desiredWorld.y = 0f;
            if (desiredWorld.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            desiredWorld.Normalize();

            if (!hasSmoothedDesiredWorld)
            {
                smoothedDesiredWorld = desiredWorld;
                hasSmoothedDesiredWorld = true;
                return smoothedDesiredWorld;
            }

            float smoothing = Mathf.Max(0f, desiredDirectionSmoothing);
            if (smoothing <= 0f)
            {
                smoothedDesiredWorld = desiredWorld;
                return smoothedDesiredWorld;
            }

            float t = 1f - Mathf.Exp(-smoothing * Mathf.Max(0.0001f, Time.deltaTime));
            smoothedDesiredWorld = Vector3.Slerp(smoothedDesiredWorld, desiredWorld, t);
            smoothedDesiredWorld.y = 0f;

            if (smoothedDesiredWorld.sqrMagnitude <= 0.0001f)
                smoothedDesiredWorld = desiredWorld;
            else
                smoothedDesiredWorld.Normalize();

            return smoothedDesiredWorld;
        }

        private void BuildPatrolRoute(bool force)
        {
            if (patrolPlanner == null)
                return;

            if (!force && Time.time < nextPatrolRouteRebuildTime)
                return;

            nextPatrolRouteRebuildTime = Time.time + Mathf.Max(1f, patrolRouteRebuildInterval);
            patrolPlanner.Reset();
            int variantSeed = uniquePatrolRoutePerBot
                ? (patrolVariantSeed + patrolRouteRebuildCount * 7919)
                : 0;
            patrolPlanner.SetRouteVariantSeed(variantSeed);
            patrolRouteRebuildCount++;

            if (useNavMeshCheckpoints)
            {
                ResolveCheckpointNodes(checkpointBuffer);
                patrolPlanner.RebuildFromCheckpoints(
                    checkpointBuffer,
                    transform.position,
                    checkpointSampleRadius,
                    checkpointMaxSnapDistance);
            }

            if (!patrolPlanner.HasRoute)
            {
                patrolPlanner.RebuildFallbackRoute(
                    transform.position,
                    fallbackPatrolPointCount,
                    fallbackPatrolRadius,
                    checkpointSampleRadius,
                    fallbackPatrolPointSpacing);
            }

            LogAi("PATROL", $"Route rebuild complete. points={patrolPlanner.PatrolPointCount}");
        }

        private void ResolveCheckpointNodes(List<NavMeshCheckpointNode> output)
        {
            output.Clear();

            if (checkpointNodeTransforms != null && checkpointNodeTransforms.Length > 0)
            {
                for (int i = 0; i < checkpointNodeTransforms.Length; i++)
                {
                    Transform current = checkpointNodeTransforms[i];
                    if (current == null)
                        continue;

                    NavMeshCheckpointNode node = current.GetComponent<NavMeshCheckpointNode>();
                    if (node == null || output.Contains(node))
                        continue;

                    output.Add(node);
                }

                if (output.Count > 0)
                    return;
            }

            if (!autoFindCheckpointNodesIfEmpty)
                return;

            cachedCheckpointNodes = FindObjectsOfType<NavMeshCheckpointNode>();
            if (cachedCheckpointNodes == null || cachedCheckpointNodes.Length == 0)
                return;

            for (int i = 0; i < cachedCheckpointNodes.Length; i++)
            {
                NavMeshCheckpointNode node = cachedCheckpointNodes[i];
                if (node != null && !output.Contains(node))
                    output.Add(node);
            }
        }

        private bool IsTargetVisible(Transform targetTransform, Vector3 aimPoint, float distanceToTarget)
        {
            if (targetTransform == null)
                return false;

            if (distanceToTarget > Mathf.Max(0.1f, targetDetectionRange))
                return false;

            if (visionConeAngle < 179f)
            {
                Vector3 forward = invertBodyForward ? -transform.forward : transform.forward;
                forward.y = 0f;

                Vector3 toTarget = targetTransform.position - transform.position;
                toTarget.y = 0f;

                if (forward.sqrMagnitude > 0.01f && toTarget.sqrMagnitude > 0.01f)
                {
                    float angle = Vector3.Angle(forward.normalized, toTarget.normalized);
                    if (angle > visionConeAngle * 0.5f)
                        return false;
                }
            }

            if (!requireLineOfSight)
                return true;

            return HasLineOfSight(targetTransform, aimPoint);
        }

        private bool HasLineOfSight(Transform targetTransform, Vector3 aimPoint)
        {
            Vector3 lowPoint = targetTransform.position + Vector3.up * Mathf.Max(0.2f, targetAimHeightOffset * 0.5f);
            Vector3 midPoint = aimPoint;
            Vector3 highPoint = targetTransform.position + Vector3.up * (targetAimHeightOffset + 1.2f);

            return HasLineOfSightToPoint(targetTransform, lowPoint) ||
                   HasLineOfSightToPoint(targetTransform, midPoint) ||
                   HasLineOfSightToPoint(targetTransform, highPoint);
        }

        private bool HasLineOfSightToPoint(Transform targetTransform, Vector3 point)
        {
            Transform fireOrigin = weapon != null && weapon.FirePoint != null ? weapon.FirePoint : transform;
            Vector3 origin = fireOrigin.position;
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
            Transform nearestHit = null;

            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = lineOfSightHits[i].transform;
                if (hitTransform == null)
                    continue;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                float hitDistance = lineOfSightHits[i].distance;
                if (hitDistance < nearestDistance)
                {
                    nearestDistance = hitDistance;
                    nearestHit = hitTransform;
                }
            }

            if (nearestHit == null)
                return true;

            return nearestHit.root == targetTransform.root;
        }

        private bool TryProjectDestinationOnNavMesh(Vector3 destination, out Vector3 projectedDestination)
        {
            float radius = Mathf.Max(0.5f, destinationSampleRadius);
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                projectedDestination = hit.position;
                return true;
            }

            projectedDestination = Vector3.zero;
            return false;
        }

        private bool TryHasCompletePathTo(Vector3 destination)
        {
            if (!agent.isOnNavMesh)
                return false;

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
                return false;

            return path.status == NavMeshPathStatus.PathComplete;
        }

        private void UpdateCombatWeapon(float distanceToTarget)
        {
            if (!enableWeaponSwitching || tankController == null || Time.time < nextWeaponSwitchTime)
                return;

            WeaponType desiredType = distanceToTarget <= machineGunPreferredRange
                ? WeaponType.MachineGun
                : WeaponType.Cannon;

            if (tankController.ActiveWeaponType == desiredType)
                return;

            if (desiredType == WeaponType.MachineGun && tankController.MachineGunWeapon == null)
                return;

            if (desiredType == WeaponType.Cannon && tankController.CannonWeapon == null)
                return;

            tankController.SwitchWeapon(desiredType);
            weapon = tankController.Weapon;
            nextWeaponSwitchTime = Time.time + Mathf.Max(0.05f, weaponSwitchCooldown);
        }

        private TankWeapon GetActiveWeapon()
        {
            if (tankController != null && tankController.Weapon != null)
                return tankController.Weapon;

            return weapon;
        }

        private bool ShouldReload(TankWeapon activeWeapon)
        {
            if (activeWeapon == null)
                return false;

            return !activeWeapon.IsReloading &&
                   activeWeapon.CurrentAmmoInMagazine <= 0 &&
                   activeWeapon.ReserveAmmo > 0;
        }

        private void SendCommand(
            float verticalInput,
            float horizontalInput,
            bool isAiming,
            Vector3 aimPoint,
            bool isFiring,
            bool requestReload)
        {
            bool hasAimPoint = isAiming;
            TankInputCommand command = new TankInputCommand(
                verticalInput,
                horizontalInput,
                Vector2.zero,
                isAiming,
                isFiring,
                requestReload,
                false,
                0,
                isFiring,
                isFiring,
                false,
                0,
                hasAimPoint,
                aimPoint,
                0f
            );

            if (tankController != null)
            {
                tankController.ApplyExternalCommand(command);
                return;
            }

            movement?.ApplyMovement(verticalInput, horizontalInput, false);
        }

        private void SendIdleCommand()
        {
            SendCommand(0f, 0f, false, Vector3.zero, false, false);
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

        private void LogStateChange(TankAIState state)
        {
            if (!enableAiDebugLogs || state == lastLoggedState)
                return;

            lastLoggedState = state;
            LogAi("STATE", $"Switched to {state}");
        }

        private void LogAi(string tag, string message)
        {
            if (!enableAiDebugLogs)
                return;

            Debug.Log($"[NavMeshTankAI][{name}][{tag}] {message}", this);
        }
    }
}
