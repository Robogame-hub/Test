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
        [SerializeField] private float turnAngleForFullInput = 60f;
        [SerializeField] private float moveAngleLimit = 100f;
        [SerializeField] private bool invertBodyForward = true;

        [Header("Patrol")]
        [SerializeField] private bool useNavMeshCheckpoints = true;
        [SerializeField] private Transform[] checkpointNodeTransforms;
        [SerializeField] private bool autoFindCheckpointNodesIfEmpty = true;
        [SerializeField] private float checkpointReachDistance = 2.5f;
        [SerializeField] private float checkpointSampleRadius = 4f;
        [SerializeField] private float patrolStoppingDistance = 1f;
        [SerializeField] private float patrolStuckTimeout = 3f;
        [SerializeField] private float patrolPathFailureTimeout = 1.5f;
        [SerializeField] private float patrolRouteRebuildInterval = 4f;
        [SerializeField] private int fallbackPatrolPointCount = 5;
        [SerializeField] private float fallbackPatrolRadius = 50f;
        [SerializeField] private float fallbackPatrolPointSpacing = 8f;

        [Header("Combat")]
        [SerializeField] private float fireRange = 25f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask lineOfSightMask = ~0;
        [SerializeField] private bool requireTurretAlignmentForFire = false;
        [SerializeField] private float chaseMemoryDuration = 2f;
        [SerializeField] private float searchDuration = 4f;
        [SerializeField] private float searchReachDistance = 3f;

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

        private void Awake()
        {
            ResolveReferences();
            ConfigureAgent();
            brain = new TankAIBrain(chaseMemoryDuration, searchDuration, searchReachDistance);
            patrolPlanner = new TankAIPatrolPlanner();
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

            bool hasDestination = TryResolveDestination(decision, hasTarget, targetPosition, out Vector3 destination, out float stoppingDistance);
            UpdateAgentDestination(hasDestination, destination, stoppingDistance);
            HandlePatrolPathFailures(decision.State, hasDestination);

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

            if (movement != null)
            {
                agent.speed = Mathf.Max(0.1f, movement.MoveSpeed);
                agent.angularSpeed = Mathf.Max(1f, movement.RotationSpeed);
                agent.acceleration = Mathf.Max(2f, movement.MoveSpeed * 3f);
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
            bool hasTarget,
            Vector3 targetPosition,
            out Vector3 destination,
            out float stoppingDistance)
        {
            destination = Vector3.zero;
            stoppingDistance = Mathf.Max(0.5f, attackRange * 0.85f);

            switch (decision.State)
            {
                case TankAIState.Chase:
                {
                    if (hasTarget)
                    {
                        if (GetPlanarDistance(transform.position, targetPosition) > chaseRange)
                        {
                            destination = targetPosition;
                            stoppingDistance = Mathf.Max(0.5f, attackRange * 0.75f);
                            return true;
                        }

                        destination = targetPosition;
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
                        stoppingDistance = Mathf.Max(0.5f, patrolStoppingDistance);
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
                            checkpointReachDistance,
                            patrolStuckTimeout,
                            out destination))
                    {
                        stoppingDistance = Mathf.Max(0.4f, patrolStoppingDistance);
                        return true;
                    }

                    BuildPatrolRoute(false);

                    if (patrolPlanner != null &&
                        patrolPlanner.TryGetCurrentDestination(
                            transform.position,
                            Time.time,
                            checkpointReachDistance,
                            patrolStuckTimeout,
                            out destination))
                    {
                        stoppingDistance = Mathf.Max(0.4f, patrolStoppingDistance);
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

            agent.nextPosition = transform.position;

            if (!hasDestination)
            {
                if (agent.hasPath)
                    agent.ResetPath();
                hasLastAgentDestination = false;
                return;
            }

            bool destinationChanged = !hasLastAgentDestination ||
                                      (lastAgentDestination - destination).sqrMagnitude > 1f;

            if (!destinationChanged && Time.time < nextRepathTime)
                return;

            nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);
            agent.stoppingDistance = Mathf.Max(0.1f, stoppingDistance);
            agent.SetDestination(destination);

            lastAgentDestination = destination;
            hasLastAgentDestination = true;
        }

        private void HandlePatrolPathFailures(TankAIState state, bool hasDestination)
        {
            if (state != TankAIState.Patrol || !hasDestination || patrolPlanner == null || agent == null || !agent.isOnNavMesh)
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
            LogAi("PATROL", "Skipped patrol point due to invalid/partial path.");
        }

        private Vector2 ComputeMovementInput(bool hasDestination)
        {
            if (!hasDestination || agent == null || !agent.isOnNavMesh)
                return Vector2.zero;

            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.15f)
                return Vector2.zero;

            Vector3 desiredWorld = Vector3.zero;

            if (!agent.pathPending && agent.hasPath)
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

            Vector3 forward = invertBodyForward ? -transform.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return Vector2.zero;
            forward.Normalize();

            float signedAngle = Vector3.SignedAngle(forward, desiredWorld, Vector3.up);
            float horizontal = Mathf.Clamp(signedAngle / Mathf.Max(1f, turnAngleForFullInput), -1f, 1f);
            float vertical = Mathf.Abs(signedAngle) <= moveAngleLimit ? 1f : 0f;

            return new Vector2(horizontal, vertical);
        }

        private void BuildPatrolRoute(bool force)
        {
            if (patrolPlanner == null)
                return;

            if (!force && Time.time < nextPatrolRouteRebuildTime)
                return;

            nextPatrolRouteRebuildTime = Time.time + Mathf.Max(1f, patrolRouteRebuildInterval);
            patrolPlanner.Reset();

            if (useNavMeshCheckpoints)
            {
                ResolveCheckpointNodes(checkpointBuffer);
                patrolPlanner.RebuildFromCheckpoints(checkpointBuffer, transform.position, checkpointSampleRadius);
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
