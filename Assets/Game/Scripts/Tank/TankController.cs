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
        [Tooltip("Обработчик ввода")]
        [SerializeField] private TankInputHandler inputHandler;

        [Header("Network Settings")]
        [Tooltip("Является ли этот танк локальным игроком (управляется на этом клиенте)")]
        [SerializeField] private bool isLocalPlayer = true;
        [Tooltip("Частота синхронизации по сети (Герц)")]
        [SerializeField] private float networkSyncRate = 20f;

        private float lastNetworkSyncTime;

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
            if (inputHandler == null)
                inputHandler = GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                // Для удаленных игроков применяем интерполяцию
                // (здесь будет логика интерполяции при интеграции с сетью)
                return;
            }

            // Локальный игрок - обрабатываем ввод (НЕ физический)
            ProcessLocalInput();

            // Сетевая синхронизация
            if (ShouldSyncNetwork())
            {
                SyncToNetwork();
            }
        }
        
        private void FixedUpdate()
        {
            if (!isLocalPlayer)
                return;
            
            // Физика - применяем движение (в FixedUpdate для стабильности!)
            TankInputCommand input = inputHandler.GetCurrentInput();
            ProcessPhysicalMovement(input);
            
            // Физика - выравнивание по земле
            movement.AlignToGround();
        }

        /// <summary>
        /// Обработка ввода локального игрока
        /// </summary>
        private void ProcessLocalInput()
        {
            TankInputCommand input = inputHandler.GetCurrentInput();
            
            // Отладка двойного выстрела
            if (input.IsFiring)
            {
                Debug.Log($"[TankController] Fire command received! Frame: {Time.frameCount}");
            }
            
            ProcessCommand(input);
        }

        /// <summary>
        /// Обработка команды (может быть локальной или сетевой)
        /// </summary>
        public void ProcessCommand(TankInputCommand command)
        {
            // Прицеливание (не физическое, можно в Update)
            if (command.IsAiming)
            {
                if (!turret.IsAiming)
                    turret.StartAiming();

                turret.RotateTurret(command.MouseDelta);
            }
            else
            {
                if (turret.IsAiming)
                    turret.StopAiming();
            }

            // Стрельба (не физическое, можно в Update)
            if (command.IsFiring)
            {
                Debug.Log($"[TankController] Fire command! CanFire={weapon.CanFire}, Stability={turret.CurrentStability}, Frame={Time.frameCount}");
                
                if (weapon.CanFire)
                {
                    weapon.Fire(turret.CurrentStability);
                    turret.ResetStability();
                }
                else
                {
                    Debug.LogWarning($"[TankController] Can't fire! Cooldown={Time.time - weapon.LastFireTime} < {weapon.FireCooldown}");
                }
            }
        }
        
        /// <summary>
        /// Обработка физического движения (вызывается в FixedUpdate)
        /// </summary>
        private void ProcessPhysicalMovement(TankInputCommand command)
        {
            // Движение танка (ФИЗИКА - только в FixedUpdate!)
            movement.ApplyMovement(command.VerticalInput, command.HorizontalInput);
        }

        /// <summary>
        /// Проверка нужно ли синхронизировать с сетью
        /// </summary>
        private bool ShouldSyncNetwork()
        {
            if (!isLocalPlayer)
                return false;

            float syncInterval = 1f / networkSyncRate;
            if (Time.time - lastNetworkSyncTime >= syncInterval)
            {
                lastNetworkSyncTime = Time.time;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Синхронизация состояния с сетью
        /// </summary>
        private void SyncToNetwork()
        {
            // Здесь будет код для отправки состояния танка по сети
            // Например, через Mirror, Netcode for GameObjects или Photon
            // 
            // NetworkWriter writer = new NetworkWriter();
            // movement.Serialize(writer);
            // turret.Serialize(writer);
            // SendToServer(writer);
        }

        /// <summary>
        /// Применение состояния с сети (для удаленных игроков)
        /// </summary>
        public void ApplyNetworkState(TankNetworkState state)
        {
            if (isLocalPlayer)
                return; // Локальный игрок не применяет сетевое состояние

            movement.SetPositionAndRotation(state.Position, state.Rotation);
            // Применяем другие параметры из state
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

    /// <summary>
    /// Структура для сетевой синхронизации состояния танка
    /// </summary>
    [System.Serializable]
    public struct TankNetworkState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Quaternion TurretRotation;
        public Quaternion CannonRotation;
        public float Health;
        public float Timestamp;
    }
}

