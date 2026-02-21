using UnityEngine;
using TankGame.Commands;
using TankGame.Tank.Components;
using UnityEngine.Events;

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
        [Tooltip("Автоматически перезаряжать пулемет при пустом магазине")]
        [SerializeField] private bool autoReloadMachineGun = true;

        [System.Serializable]
        public class WeaponChangedEvent : UnityEvent<WeaponType, TankWeapon> { }
        [Header("Events")]
        [SerializeField] private WeaponChangedEvent onWeaponChanged = new WeaponChangedEvent();
        
        private TankInputCommand cachedInput;
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

        private void Start()
        {
            // Повторно применяем стартовый слот после инициализации всех компонентов.
            // Это защищает от редких случаев, когда роли оружия/ссылки переопределяются позднее.
            SwitchWeapon(startWeapon, true);
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
            ResolveWeaponSlots();
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

        private void ProcessMachineGunFire(TankInputCommand command)
        {
            if (weapon == null)
                return;

            bool wantsFire = command.IsFiringPressed || command.IsFiringHeld || command.IsFiring;
            if (!wantsFire)
                return;

            if (!turret.IsFirePointAligned)
                return;

            if (weapon.CanFire)
            {
                float stability = turret.GetFireStability();
                weapon.Fire(stability);
                turret.ResetStability();
                return;
            }

            if (autoReloadMachineGun &&
                !weapon.IsReloading &&
                weapon.CurrentAmmoInMagazine <= 0 &&
                weapon.ReserveAmmo > 0)
            {
                weapon.TryReload();
            }
        }

        public void SwitchWeapon(WeaponType targetWeapon, bool force = false)
        {
            if (!force && activeWeaponType == targetWeapon)
                return;

            ResolveWeaponSlots();

            TankWeapon nextWeapon = targetWeapon == WeaponType.MachineGun ? machineGunWeapon : cannonWeapon;
            if (nextWeapon == null)
            {
                if (targetWeapon == WeaponType.MachineGun)
                    Debug.LogWarning("[TankController] MachineGun weapon is not assigned/found. Assign a second TankWeapon in inspector.");
                return;
            }

            weapon = nextWeapon;
            activeWeaponType = targetWeapon;
            turret?.SetWeapon(weapon);
            turret?.SetWeaponMode(activeWeaponType);
            onWeaponChanged?.Invoke(activeWeaponType, weapon);
        }

        private TankWeapon FindAlternativeWeapon(TankWeapon primaryWeapon)
        {
            TankWeapon[] localWeapons = GetComponents<TankWeapon>();
            for (int i = 0; i < localWeapons.Length; i++)
            {
                if (localWeapons[i] != null && localWeapons[i] != primaryWeapon)
                    return localWeapons[i];
            }

            TankWeapon[] childWeapons = GetComponentsInChildren<TankWeapon>(true);
            for (int i = 0; i < childWeapons.Length; i++)
            {
                if (childWeapons[i] != null && childWeapons[i] != primaryWeapon)
                    return childWeapons[i];
            }

            return null;
        }

        private void ResolveWeaponSlots()
        {
            TankWeapon[] localWeapons = GetComponents<TankWeapon>();
            TankWeapon[] childWeapons = GetComponentsInChildren<TankWeapon>(true);

            TankWeapon[] allWeapons = new TankWeapon[localWeapons.Length + childWeapons.Length];
            int count = 0;

            for (int i = 0; i < localWeapons.Length; i++)
            {
                TankWeapon w = localWeapons[i];
                if (w == null)
                    continue;
                allWeapons[count++] = w;
            }

            for (int i = 0; i < childWeapons.Length; i++)
            {
                TankWeapon w = childWeapons[i];
                if (w == null)
                    continue;

                bool exists = false;
                for (int j = 0; j < count; j++)
                {
                    if (allWeapons[j] == w)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    allWeapons[count++] = w;
            }

            if (count == 0)
                return;

            if (count == 1)
            {
                if (cannonWeapon == null)
                    cannonWeapon = allWeapons[0];
                if (weapon == null)
                    weapon = cannonWeapon;
                return;
            }

            TankWeapon fastest = allWeapons[0];
            TankWeapon slowest = allWeapons[0];
            for (int i = 1; i < count; i++)
            {
                TankWeapon w = allWeapons[i];
                if (w == null)
                    continue;

                if (w.FireCooldown < fastest.FireCooldown)
                    fastest = w;
                if (w.FireCooldown > slowest.FireCooldown)
                    slowest = w;
            }

            if (machineGunWeapon == null)
                machineGunWeapon = fastest;
            if (cannonWeapon == null)
                cannonWeapon = slowest;

            if (machineGunWeapon == cannonWeapon)
            {
                for (int i = 0; i < count; i++)
                {
                    TankWeapon candidate = allWeapons[i];
                    if (candidate != null && candidate != machineGunWeapon)
                    {
                        cannonWeapon = candidate;
                        break;
                    }
                }
            }

            if (weapon == null)
                weapon = cannonWeapon != null ? cannonWeapon : machineGunWeapon;
        }

        private void OnDisable()
        {
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

