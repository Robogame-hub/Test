using TankGame.Tank.Components;
using TankGame.Tank;
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
        [Tooltip("Цель для преследования. Оставьте пустым при включённом авто-поиск игрока.")]
        [SerializeField] private Transform target;
        [Tooltip("Контроллер танка (подтягивается автоматически, если пусто).")]
        [SerializeField] private TankController tankController;
        [Tooltip("Компонент движения (подтягивается автоматически).")]
        [SerializeField] private TankMovement movement;
        [Tooltip("Башня танка (подтягивается автоматически).")]
        [SerializeField] private TankTurret turret;
        [Tooltip("Текущее оружие (подтягивается автоматически).")]
        [SerializeField] private TankWeapon weapon;
        [Tooltip("Здоровье бота (подтягивается автоматически).")]
        [SerializeField] private TankHealth health;
        [Tooltip("Анимация гусениц (подтягивается автоматически).")]
        [SerializeField] private TrackAnimationController trackAnimation;
        [Tooltip("NavMesh Agent для построения пути (подтягивается автоматически).")]
        [SerializeField] private NavMeshAgent agent;

        [Header("Targeting")]
        [Tooltip("Автоматически искать локального игрока как цель. Если выключено — укажите Target вручную.")]
        [SerializeField] private bool autoFindLocalPlayerTarget = true;
        [Tooltip("Как часто искать цель, сек.")]
        [SerializeField] private float targetSearchInterval = 1f;
        [Tooltip("Смещение точки прицеливания по высоте относительно центра цели (м).")]
        [SerializeField] private float targetAimHeightOffset = 0.6f;

        [Header("Movement")]
        [Tooltip("Дальность, на которой бот начинает преследовать цель (м).")]
        [SerializeField] private float chaseRange = 60f;
        [Tooltip("Дистанция атаки: ближе не подъезжает, стреляет с места (м).")]
        [SerializeField] private float attackRange = 22f;
        [Tooltip("Минимальная дистанция до других ботов (м). Ближе не подъезжает, чтобы не слипаться.")]
        [SerializeField] private float minDistanceToOtherBots = 6f;
        [Tooltip("Как часто пересчитывать путь к цели, сек.")]
        [SerializeField] private float repathInterval = 0.2f;
        [Tooltip("Угол между курсом и направлением к цели, при котором руль в упор (град).")]
        [SerializeField] private float turnAngleForFullInput = 60f;
        [Tooltip("Если угол до цели больше этого — бот не едет вперёд, только разворачивается (град).")]
        [SerializeField] private float moveAngleLimit = 100f;

        [Header("Combat")]
        [Tooltip("Максимальная дистанция стрельбы (м).")]
        [SerializeField] private float fireRange = 20f;
        [Tooltip("Стрелять только при отсутствии препятствий между ботом и целью.")]
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Слои, по которым проверяется линия видимости (луч).")]
        [SerializeField] private LayerMask lineOfSightMask = ~0;
        [Tooltip("Стрелять только когда ствол достаточно наведён на цель.")]
        [SerializeField] private bool requireTurretAlignmentForFire = false;

        [Header("Weapon Switching")]
        [Tooltip("Переключать пушку/пулемёт в зависимости от дистанции до цели.")]
        [SerializeField] private bool enableWeaponSwitching = true;
        [Tooltip("Ближе этой дистанции (м) бот предпочитает пулемёт.")]
        [SerializeField] private float machineGunPreferredRange = 12f;
        [Tooltip("Минимальная пауза между переключениями оружия, сек.")]
        [SerializeField] private float weaponSwitchCooldown = 0.5f;

        private float nextTargetSearchTime;
        private float nextRepathTime;
        private float nextWeaponSwitchTime;
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
            TankWeapon activeWeapon = GetActiveWeapon();
            activeWeapon?.SetExternalAimPoint(aimPoint);

            if (!turret.IsAiming)
                turret.StartAiming();

            UpdateCombatWeapon(distance);
            UpdateMovementInputs(distance);
            TryFireAtTarget(distance, aimPoint);
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
            TankController localPlayer = TankRegistry.GetLocalPlayer();
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

            // Не приближаться к другим ботам ближе minDistanceToOtherBots
            Vector3 separation = GetSeparationFromOtherBots();
            if (separation.sqrMagnitude > 0.01f)
            {
                desiredWorld = (desiredWorld + separation).normalized;
                desiredWorld.y = 0f;
                if (desiredWorld.sqrMagnitude > 0.01f)
                    desiredWorld.Normalize();
            }

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

        /// <summary>Вектор «отталкивания» от других ботов (нормализованный).</summary>
        private Vector3 GetSeparationFromOtherBots()
        {
            if (minDistanceToOtherBots <= 0f) return Vector3.zero;
            var all = TankRegistry.GetAllTanks();
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
            var all = TankRegistry.GetAllTanks();
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

        private void TryFireAtTarget(float distanceToTarget, Vector3 aimPoint)
        {
            TankWeapon activeWeapon = GetActiveWeapon();
            if (activeWeapon == null || turret == null)
                return;

            if (distanceToTarget > fireRange)
                return;

            if (!activeWeapon.CanFire)
                return;

            if (requireTurretAlignmentForFire && !turret.IsFirePointAligned)
                return;

            if (requireLineOfSight && !HasLineOfSight(aimPoint))
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
            TankWeapon activeWeapon = GetActiveWeapon();
            activeWeapon?.ClearExternalAimPoint();

            if (turret != null && turret.IsAiming)
                turret.StopAiming();
        }
    }
}
