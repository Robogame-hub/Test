using UnityEngine;
using TankGame.Commands;
using TankGame.Tank.Components;
using UnityEngine.Events;
using System.Collections;

namespace TankGame.Tank
{
    /// <summary>
    /// Главный контроллер танка (модульная архитектура)
    /// Координирует работу всех компонентов танка
    /// Следует принципу единственной ответственности (SRP)
    /// </summary>
    [RequireComponent(typeof(TankMovement))]
    [RequireComponent(typeof(TankTurret))]
    [RequireComponent(typeof(TankWeapon))]
    [RequireComponent(typeof(TankHealth))]
    public class TankController : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("Компонент движения танка")]
        [SerializeField] private TankMovement movement;
        [Tooltip("Компонент башни танка")]
        [SerializeField] private TankTurret turret;
        [Tooltip("Компонент оружия танка")]
        [SerializeField] private TankWeapon weapon;
        [Tooltip("Оружие пушки (слот 1)")]
        [SerializeField] private TankWeapon cannonWeapon;
        [Tooltip("Оружие пулемета (слот 2)")]
        [SerializeField] private TankWeapon machineGunWeapon;
        [Tooltip("Компонент здоровья танка")]
        [SerializeField] private TankHealth health;
        [Tooltip("Компонент анимации гусениц")]
        [SerializeField] private TrackAnimationController trackAnimation;
        [Tooltip("Обработчик ввода")]
        [SerializeField] private TankInputHandler inputHandler;

        [Header("Player Settings")]
        [Tooltip("Является ли этот танк локальным игроком (управляется на этом клиенте)")]
        [SerializeField] private bool isLocalPlayer = true;

        [Header("Weapon Switching")]
        [SerializeField] private WeaponType startWeapon = WeaponType.Cannon;
        [Tooltip("Количество выстрелов в очереди пулемета")]
        [SerializeField] private int machineGunBurstCount = 5;
        [Tooltip("Время удержания ЛКМ для перехода пулемета в режим очереди")]
        [SerializeField] private float machineGunHoldForBurst = 0.2f;

        [System.Serializable]
        public class WeaponChangedEvent : UnityEvent<WeaponType, TankWeapon> { }
        [Header("Events")]
        [SerializeField] private WeaponChangedEvent onWeaponChanged = new WeaponChangedEvent();
        
        private TankInputCommand cachedInput;
        private Coroutine machineGunBurstCoroutine;
        private bool isMachineGunBursting;
        private bool machineGunPendingSingleShot;
        private float machineGunPressTime;
        private WeaponType activeWeaponType = WeaponType.Cannon;

        // Публичные свойства для доступа к компонентам
        public TankMovement Movement => movement;
        public TankTurret Turret => turret;
        public TankWeapon Weapon => weapon;
        public TankWeapon CannonWeapon => cannonWeapon;
        public TankWeapon MachineGunWeapon => machineGunWeapon;
        public WeaponType ActiveWeaponType => activeWeaponType;
        public TankHealth Health => health;
        public bool IsLocalPlayer => isLocalPlayer;
        public WeaponChangedEvent OnWeaponChanged => onWeaponChanged;

        /// <summary>
        /// Устанавливает, является ли танк локальным игроком (для сетевой игры)
        /// </summary>
        public void SetIsLocalPlayer(bool isLocal)
        {
            isLocalPlayer = isLocal;
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Получаем компоненты если не назначены
            if (movement == null)
                movement = GetComponent<TankMovement>();
            if (turret == null)
                turret = GetComponent<TankTurret>();
            if (weapon == null)
                weapon = GetComponent<TankWeapon>();
            if (cannonWeapon == null)
                cannonWeapon = weapon;
            if (machineGunWeapon == null)
            {
                TankWeapon[] weapons = GetComponents<TankWeapon>();
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i] != null && weapons[i] != cannonWeapon)
                    {
                        machineGunWeapon = weapons[i];
                        break;
                    }
                }
            }
            if (health == null)
                health = GetComponent<TankHealth>();
            if (trackAnimation == null)
                trackAnimation = GetComponent<TrackAnimationController>() ?? gameObject.AddComponent<TrackAnimationController>();
            if (inputHandler == null)
                inputHandler = GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();

            SwitchWeapon(startWeapon, true);
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            if (inputHandler == null)
                return;

            ProcessLocalInput();
        }
        
        private void FixedUpdate()
        {
            if (!isLocalPlayer)
                return;
            
            // Физика - используем ввод, собранный в Update (без повторного чтения Input)
            ProcessPhysicalMovement(cachedInput);
            trackAnimation?.UpdateTrackAnimation(cachedInput.VerticalInput, cachedInput.HorizontalInput);
            
            // Физика - выравнивание по земле
            movement.AlignToGround();
        }

        /// <summary>
        /// Обработка ввода локального игрока
        /// </summary>
        private void ProcessLocalInput()
        {
            if (inputHandler == null)
                return;
            
            cachedInput = inputHandler.GetCurrentInput();
            ProcessCommand(cachedInput);
        }

        /// <summary>
        /// Обработка команды (может быть локальной или сетевой)
        /// </summary>
        public void ProcessCommand(TankInputCommand command)
        {
            HandleWeaponSwitchCommand(command);

            // Прицеливание
            if (command.IsAiming)
            {
                if (!turret.IsAiming)
                    turret.StartAiming();
            }
            else
            {
                if (turret.IsAiming)
                    turret.StopAiming();
            }

            // Перезарядка
            if (command.IsReloadRequested)
            {
                weapon.TryReload();
            }

            // Стрельба
            if (activeWeaponType == WeaponType.MachineGun)
            {
                ProcessMachineGunFire(command);
            }
            else if ((command.IsFiringPressed || command.IsFiring) && turret.IsFirePointAligned && weapon.CanFire)
            {
                float stability = turret.GetFireStability();
                weapon.Fire(stability);
                turret.ResetStability();
            }
        }
        
        /// <summary>
        /// Обработка физического движения (вызывается в FixedUpdate)
        /// </summary>
        private void ProcessPhysicalMovement(TankInputCommand command)
        {
            // Движение танка (ФИЗИКА - только в FixedUpdate!)
            movement.ApplyMovement(command.VerticalInput, command.HorizontalInput, command.IsBoosting);
        }

        private void HandleWeaponSwitchCommand(TankInputCommand command)
        {
            if (command.WeaponSlot == 1)
                SwitchWeapon(WeaponType.Cannon);
            else if (command.WeaponSlot == 2)
                SwitchWeapon(WeaponType.MachineGun);
        }

        private void TryStartMachineGunBurst()
        {
            if (weapon == null || isMachineGunBursting)
                return;

            if (!weapon.CanFire)
                return;

            if (machineGunBurstCoroutine != null)
                StopCoroutine(machineGunBurstCoroutine);

            machineGunBurstCoroutine = StartCoroutine(FireMachineGunBurstRoutine());
        }

        private void ProcessMachineGunFire(TankInputCommand command)
        {
            bool hasLegacyFireOnly = command.IsFiring && !command.IsFiringPressed && !command.IsFiringHeld && !command.IsFiringReleased;
            if (hasLegacyFireOnly)
            {
                TryStartMachineGunBurst();
                return;
            }

            if (command.IsFiringPressed)
            {
                machineGunPendingSingleShot = true;
                machineGunPressTime = Time.time;
            }

            if (machineGunPendingSingleShot &&
                command.IsFiringHeld &&
                Time.time - machineGunPressTime >= machineGunHoldForBurst)
            {
                machineGunPendingSingleShot = false;
                TryStartMachineGunBurst();
            }

            if (command.IsFiringReleased && machineGunPendingSingleShot)
            {
                machineGunPendingSingleShot = false;
                TryFireSingleMachineGunShot();
            }
        }

        private void TryFireSingleMachineGunShot()
        {
            if (weapon == null || !weapon.CanFire)
                return;
            if (!turret.IsFirePointAligned)
                return;

            float stability = turret.GetFireStability();
            weapon.Fire(stability);
            turret.ResetStability();
        }

        private IEnumerator FireMachineGunBurstRoutine()
        {
            isMachineGunBursting = true;
            int shotsToFire = Mathf.Max(1, machineGunBurstCount);

            for (int i = 0; i < shotsToFire; i++)
            {
                if (weapon == null || !turret.IsAiming)
                    break;

                if (!turret.IsFirePointAligned)
                {
                    yield return null;
                    i--;
                    continue;
                }

                if (!weapon.CanFire)
                    break;

                float stability = turret.GetFireStability();
                weapon.Fire(stability);
                turret.ResetStability();

                yield return new WaitForSeconds(Mathf.Max(0.01f, weapon.FireCooldown));
            }

            weapon?.TryReload();
            isMachineGunBursting = false;
            machineGunBurstCoroutine = null;
        }

        public void SwitchWeapon(WeaponType targetWeapon, bool force = false)
        {
            if (!force && activeWeaponType == targetWeapon)
                return;

            TankWeapon nextWeapon = targetWeapon == WeaponType.MachineGun ? machineGunWeapon : cannonWeapon;
            if (nextWeapon == null)
                return;

            if (machineGunBurstCoroutine != null)
            {
                StopCoroutine(machineGunBurstCoroutine);
                machineGunBurstCoroutine = null;
                isMachineGunBursting = false;
            }

            machineGunPendingSingleShot = false;

            weapon = nextWeapon;
            activeWeaponType = targetWeapon;
            turret?.SetWeapon(weapon);
            turret?.SetWeaponMode(activeWeaponType);
            onWeaponChanged?.Invoke(activeWeaponType, weapon);
        }

        private void OnDisable()
        {
            if (machineGunBurstCoroutine != null)
            {
                StopCoroutine(machineGunBurstCoroutine);
                machineGunBurstCoroutine = null;
            }

            isMachineGunBursting = false;
            machineGunPendingSingleShot = false;
        }


        #region Debug

        private void OnDrawGizmos()
        {
            if (movement != null && Application.isPlaying)
            {
                // Отображаем направление танка
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);

                // Отображаем направление башни
                if (turret != null && turret.Turret != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(turret.Turret.position, turret.Turret.up * 3f);
                }
            }
        }

        #endregion
    }
}

