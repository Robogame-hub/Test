using UnityEngine;
using TankGame.Commands;
using TankGame.Tank.Components;

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
        [Tooltip("Компонент здоровья танка")]
        [SerializeField] private TankHealth health;
        [Tooltip("Компонент анимации гусениц")]
        [SerializeField] private TrackAnimationController trackAnimation;
        [Tooltip("Обработчик ввода")]
        [SerializeField] private TankInputHandler inputHandler;

        [Header("Player Settings")]
        [Tooltip("Является ли этот танк локальным игроком (управляется на этом клиенте)")]
        [SerializeField] private bool isLocalPlayer = true;
        
        private TankInputCommand cachedInput;

        // Публичные свойства для доступа к компонентам
        public TankMovement Movement => movement;
        public TankTurret Turret => turret;
        public TankWeapon Weapon => weapon;
        public TankHealth Health => health;
        public bool IsLocalPlayer => isLocalPlayer;

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
            if (health == null)
                health = GetComponent<TankHealth>();
            if (trackAnimation == null)
                trackAnimation = GetComponent<TrackAnimationController>() ?? gameObject.AddComponent<TrackAnimationController>();
            if (inputHandler == null)
                inputHandler = GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();
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
            if (command.IsFiring)
            {
                // Проверяем выравнивание FirePoint с направлением прицела
                if (!turret.IsFirePointAligned)
                    return;
                
                if (weapon.CanFire)
                {
                    float stability = turret.GetFireStability();
                    weapon.Fire(stability);
                    turret.ResetStability();
                }
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

