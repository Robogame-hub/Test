using TankGame.Tank.Components;
using TankGame.Tank;
using TankGame.Tank.Animation;
using UnityEngine;
using UnityEngine.AI;

namespace TankGame.Tank.AI
{
    /// <summary>
    /// AI для танка: преследует цель по NavMesh и стреляет при возможности.
    /// Использует те же компоненты оружия/башни/анимаций, что и игрок.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(TankMovement))]
    [RequireComponent(typeof(TankTurret))]
    [RequireComponent(typeof(TankWeapon))]
    [RequireComponent(typeof(TankHealth))]
    public class NavMeshTankAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private TankController tankController;
        [SerializeField] private TankMovement movement;
        [SerializeField] private TankTurret turret;
        [SerializeField] private TankWeapon weapon;
        [SerializeField] private TankHealth health;
        [SerializeField] private TrackAnimationController trackAnimation;
        [SerializeField] private TankAnimationOrchestrator animationOrchestrator;
        [SerializeField] private NavMeshAgent agent;

        [Header("Targeting")]
        [SerializeField] private bool autoFindLocalPlayerTarget = true;
        [SerializeField] private float targetSearchInterval = 1f;
        [SerializeField] private float targetAimHeightOffset = 0.6f;

        [Header("Movement")]
        [SerializeField] private float chaseRange = 60f;
        [SerializeField] private float attackRange = 22f;
        [SerializeField] private float repathInterval = 0.2f;
        [SerializeField] private float turnAngleForFullInput = 60f;
        [SerializeField] private float moveAngleLimit = 100f;

        [Header("Combat")]
        [SerializeField] private float fireRange = 20f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask lineOfSightMask = ~0;

        private float nextTargetSearchTime;
        private float nextRepathTime;
        private float currentVerticalInput;
        private float currentHorizontalInput;

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
            if (animationOrchestrator == null)
                animationOrchestrator = GetComponent<TankAnimationOrchestrator>();
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            ConfigureAgent();
        }

        private void OnEnable()
        {
            if (tankController != null)
                tankController.SetIsLocalPlayer(false);
        }

        private void Update()
        {
            if (health != null && !health.IsAlive())
            {
                SetIdleState();
                return;
            }

            TryAutoFindTarget();
            if (target == null)
            {
                SetIdleState();
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            bool isInChaseRange = distance <= chaseRange;
            if (!isInChaseRange)
            {
                SetIdleState();
                return;
            }

            Vector3 aimPoint = GetTargetAimPoint(target);
            turret.SetExternalAimPoint(aimPoint);
            weapon.SetExternalAimPoint(aimPoint);

            if (!turret.IsAiming)
                turret.StartAiming();

            UpdateMovementInputs(distance);
            TryFireAtTarget(distance, aimPoint);
            TryReloadIfNeeded();
        }

        private void FixedUpdate()
        {
            if (health != null && !health.IsAlive())
                return;

            movement.ApplyMovement(currentVerticalInput, currentHorizontalInput, false);
            if (animationOrchestrator != null)
            {
                animationOrchestrator.ApplyInput(currentVerticalInput, currentHorizontalInput, false);
            }
            else
            {
                // Fallback для старых префабов без оркестратора.
                trackAnimation?.UpdateTrackAnimation(currentVerticalInput, currentHorizontalInput);
            }
            movement.AlignToGround();
        }

        private void OnDisable()
        {
            SetIdleState();
        }

        private void ConfigureAgent()
        {
            if (agent == null)
                return;

            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.autoBraking = true;
            agent.stoppingDistance = attackRange * 0.8f;
        }

        private void TryAutoFindTarget()
        {
            if (!autoFindLocalPlayerTarget || target != null || Time.time < nextTargetSearchTime)
                return;

            nextTargetSearchTime = Time.time + Mathf.Max(0.1f, targetSearchInterval);
            TankController[] tanks = FindObjectsByType<TankController>(FindObjectsSortMode.None);
            for (int i = 0; i < tanks.Length; i++)
            {
                TankController candidate = tanks[i];
                if (candidate == null || candidate == tankController || !candidate.IsLocalPlayer)
                    continue;

                TankHealth candidateHealth = candidate.GetComponent<TankHealth>();
                if (candidateHealth != null && !candidateHealth.IsAlive())
                    continue;

                target = candidate.transform;
                break;
            }
        }

        private void UpdateMovementInputs(float distanceToTarget)
        {
            if (agent == null || !agent.isOnNavMesh || target == null)
            {
                currentVerticalInput = 0f;
                currentHorizontalInput = 0f;
                return;
            }

            agent.nextPosition = transform.position;
            bool shouldMove = distanceToTarget > attackRange;

            if (shouldMove && Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);
                agent.SetDestination(target.position);
            }
            else if (!shouldMove && agent.hasPath)
            {
                agent.ResetPath();
            }

            if (!shouldMove)
            {
                currentVerticalInput = 0f;
                currentHorizontalInput = 0f;
                return;
            }

            Vector3 desiredWorld = agent.desiredVelocity;
            desiredWorld.y = 0f;
            if (desiredWorld.sqrMagnitude < 0.01f)
            {
                Vector3 fallback = target.position - transform.position;
                fallback.y = 0f;
                desiredWorld = fallback;
            }

            if (desiredWorld.sqrMagnitude < 0.01f)
            {
                currentVerticalInput = 0f;
                currentHorizontalInput = 0f;
                return;
            }

            desiredWorld.Normalize();
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float signedAngle = Vector3.SignedAngle(forward, desiredWorld, Vector3.up);
            currentHorizontalInput = Mathf.Clamp(signedAngle / Mathf.Max(1f, turnAngleForFullInput), -1f, 1f);

            // В этой модели TankMovement: vertical = -1 означает движение вперед.
            currentVerticalInput = Mathf.Abs(signedAngle) <= moveAngleLimit ? -1f : 0f;
        }

        private void TryFireAtTarget(float distanceToTarget, Vector3 aimPoint)
        {
            if (weapon == null || turret == null)
                return;

            if (distanceToTarget > fireRange)
                return;

            if (!weapon.CanFire || !turret.IsFirePointAligned)
                return;

            if (requireLineOfSight && !HasLineOfSight(aimPoint))
                return;

            float stability = turret.GetFireStability();
            weapon.Fire(stability);
            turret.ResetStability();
        }

        private void TryReloadIfNeeded()
        {
            if (weapon == null)
                return;

            if (!weapon.IsReloading && weapon.CurrentAmmoInMagazine <= 0 && weapon.ReserveAmmo > 0)
                weapon.TryReload();
        }

        private bool HasLineOfSight(Vector3 aimPoint)
        {
            Transform originTransform = weapon != null && weapon.FirePoint != null ? weapon.FirePoint : transform;
            Vector3 origin = originTransform.position;
            Vector3 direction = aimPoint - origin;
            float distance = direction.magnitude;
            if (distance <= 0.1f)
                return true;

            direction /= distance;
            Ray ray = new Ray(origin + direction * 0.25f, direction);
            if (!Physics.Raycast(ray, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        private Vector3 GetTargetAimPoint(Transform targetTransform)
        {
            Vector3 point = targetTransform.position;
            point.y += targetAimHeightOffset;
            return point;
        }

        private void SetIdleState()
        {
            currentVerticalInput = 0f;
            currentHorizontalInput = 0f;

            if (agent != null && agent.isOnNavMesh && agent.hasPath)
                agent.ResetPath();

            turret?.ClearExternalAimPoint();
            weapon?.ClearExternalAimPoint();

            if (turret != null && turret.IsAiming)
                turret.StopAiming();
        }
    }
}
