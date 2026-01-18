using UnityEngine;
using TankGame.Tank;
using TankGame.Commands;
using TankGame.Tank.Components;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

namespace TankGame.Network
{
    /// <summary>
    /// Сетевой компонент танка для Photon PUN 2
    /// Синхронизирует ввод, позицию, вращение, здоровье и стрельбу
    /// </summary>
    [RequireComponent(typeof(TankController))]
#if PHOTON_UNITY_NETWORKING
    public class TankNetworkPhoton : MonoBehaviourPunCallbacks, IPunObservable
#else
    public class TankNetworkPhoton : MonoBehaviour
#endif
    {
        [Header("References")]
        [Tooltip("Ссылка на TankController (автоматически найдется)")]
        [SerializeField] private TankController tankController;
        
        [Tooltip("Ссылка на TankInputHandler (автоматически найдется)")]
        [SerializeField] private TankInputHandler inputHandler;
        
        [Tooltip("Ссылка на TankMovement (для синхронизации позиции)")]
        [SerializeField] private TankMovement tankMovement;
        
        [Tooltip("Ссылка на TankTurret (для синхронизации вращения)")]
        [SerializeField] private TankTurret tankTurret;
        
        [Tooltip("Ссылка на TankHealth (для синхронизации здоровья)")]
        [SerializeField] private TankHealth tankHealth;

        [Header("Network Settings")]
        [Tooltip("Частота отправки ввода на сервер (Гц)")]
        [SerializeField] private float inputSendRate = 30f;
        
        [Tooltip("Включить интерполяцию позиции для удаленных игроков")]
        [SerializeField] private bool enableInterpolation = true;
        
        [Tooltip("Скорость интерполяции")]
        [SerializeField] private float interpolationSpeed = 15f;

        // Сетевое состояние
        private Vector3 networkPosition;
        private Quaternion networkRotation;
        private Quaternion networkTurretRotation;
        private float networkHealth;
        private float lastInputSendTime;
        
        // Буферы для интерполяции
        private Vector3 positionVelocity;
        private Quaternion rotationVelocity;

        private void Awake()
        {
            // Находим компоненты если не назначены
            if (tankController == null)
                tankController = GetComponent<TankController>();
            if (inputHandler == null)
                inputHandler = GetComponent<TankInputHandler>();
            if (tankMovement == null && tankController != null)
                tankMovement = tankController.Movement;
            if (tankTurret == null && tankController != null)
                tankTurret = tankController.Turret;
            if (tankHealth == null && tankController != null)
                tankHealth = tankController.Health;
            
#if PHOTON_UNITY_NETWORKING
            // Диагностика: проверяем PhotonView при создании танка
            if (photonView != null)
            {
                Debug.Log($"[TankNetworkPhoton] Awake() - Tank {gameObject.name} created with PhotonView! IsMine={photonView.IsMine}, ViewID={photonView.ViewID}, Owner={photonView.Owner?.NickName ?? "None"}");
            }
            else
            {
                Debug.Log($"[TankNetworkPhoton] Awake() - Tank {gameObject.name} created WITHOUT PhotonView (local tank)");
            }
            
            // Устанавливаем isLocalPlayer РАНЬШЕ (в Awake), чтобы TankController.Update() мог использовать его
            // PhotonView может быть еще не готов в Awake, поэтому проверяем в Start() и обновляем если нужно
            // Но для локального спавна (без Photon) устанавливаем true сразу
            if (tankController != null)
            {
                // По умолчанию true для локального спавна (без Photon)
                // В Start() обновим если есть photonView
                if (photonView == null)
                {
                    SetIsLocalPlayer(true);
                }
            }
#endif
        }

        private void Start()
        {
#if PHOTON_UNITY_NETWORKING
            // Устанавливаем isLocalPlayer в зависимости от владения Photon
            // Вызывается после Awake, чтобы photonView был точно инициализирован
            if (tankController != null)
            {
                bool currentIsLocal = tankController.IsLocalPlayer;
                bool shouldBeLocal = false;
                
                if (photonView != null)
                {
                    shouldBeLocal = photonView.IsMine;
                    Debug.Log($"[TankNetworkPhoton] Start() - photonView.IsMine={photonView.IsMine}, currentIsLocal={currentIsLocal}");
                }
                else
                {
                    // Если нет PhotonView, это локальный танк (офлайн режим)
                    shouldBeLocal = true;
                    Debug.Log($"[TankNetworkPhoton] Start() - No PhotonView, shouldBeLocal=true");
                }
                
                // Если isLocalPlayer уже установлен в true (например, через PhotonNetworkManager),
                // НЕ перезаписываем его на false даже если photonView.IsMine == false
                // Это может произойти если PhotonView еще не синхронизировался
                if (currentIsLocal && !shouldBeLocal)
                {
                    Debug.LogWarning($"[TankNetworkPhoton] Start() - tank is already local (true), but photonView.IsMine={shouldBeLocal}. Keeping isLocalPlayer=true.");
                    // НЕ меняем - оставляем как есть
                }
                else
                {
                    // Устанавливаем isLocalPlayer
                    SetIsLocalPlayer(shouldBeLocal);
                    Debug.Log($"[TankNetworkPhoton] Start() - SetIsLocalPlayer({shouldBeLocal}) for tank {tankController.name}");
                }
            }
            
            // Инициализируем сетевые позиции текущими значениями при спавне
            // Это предотвращает резкое перемещение танка при первом получении данных
            // ВАЖНО: ТОЛЬКО для удаленных игроков (не локального!)
            if (photonView != null && !photonView.IsMine)
            {
                networkPosition = transform.position;
                networkRotation = transform.rotation;
                
                if (tankTurret != null && tankTurret.Turret != null)
                {
                    networkTurretRotation = tankTurret.Turret.rotation;
                }
                else
                {
                    networkTurretRotation = Quaternion.identity;
                }
                
                Debug.Log($"[TankNetworkPhoton] Initialized network position for REMOTE player: {networkPosition}");
            }
            else if (photonView != null && photonView.IsMine)
            {
                // Для локального игрока НЕ инициализируем networkPosition
                // Это предотвращает случайное использование старых значений
                Debug.Log($"[TankNetworkPhoton] LOCAL player - network position NOT initialized (to prevent conflicts)");
            }
#else
            // Без Photon - всегда локальный игрок
            if (tankController != null)
            {
                SetIsLocalPlayer(true);
            }
            Debug.LogWarning("[TankNetworkPhoton] Photon PUN 2 not installed! Install from Asset Store or via Package Manager. This component will not work without Photon.");
#endif
        }

        private void Update()
        {
#if PHOTON_UNITY_NETWORKING
            if (photonView == null || tankController == null)
                return;

            // Локальный игрок - отправляем ввод на сервер
            if (photonView.IsMine)
            {
                SendInputToServer();
            }
            // Удаленный игрок - интерполируем позицию НЕ здесь, а в FixedUpdate для корректной работы с Rigidbody
#endif
        }

        private void FixedUpdate()
        {
#if PHOTON_UNITY_NETWORKING
            // Дополнительная проверка - не интерполируем локального игрока
            if (photonView == null || photonView.IsMine || tankController == null)
                return;

            // ВАЖНО: Проверяем еще раз, что это НЕ локальный игрок
            // (дополнительная защита от случайного вызова для локального танка)
            if (tankController.IsLocalPlayer)
            {
                // Это локальный игрок - НЕ интерполируем!
                // Если мы здесь, значит что-то не так с проверками выше
                if (Time.frameCount % 120 == 0) // Логируем раз в 2 секунды при 60 FPS
                {
                    Debug.LogWarning($"[TankNetworkPhoton] FixedUpdate - INTERPOLATION BLOCKED for LOCAL player: {tankController.name}");
                }
                return;
            }

            // Удаленный игрок - интерполируем позицию в FixedUpdate для корректной работы с Rigidbody
            // ВАЖНО: InterpolatePosition() не вызывается для локального игрока благодаря проверкам выше
            if (enableInterpolation)
            {
                InterpolatePosition();
            }
#endif
        }

#if PHOTON_UNITY_NETWORKING
        /// <summary>
        /// Отправляет ввод на сервер
        /// </summary>
        private void SendInputToServer()
        {
            float sendInterval = 1f / inputSendRate;
            if (Time.time - lastInputSendTime >= sendInterval)
            {
                if (inputHandler != null && photonView != null && photonView.IsMine)
                {
                    TankInputCommand input = inputHandler.GetCurrentInput();
                    // Отправляем ввод через RPC только на другие клиенты (не себе)
                    photonView.RPC("RPC_ProcessInput", RpcTarget.Others, 
                        input.VerticalInput, 
                        input.HorizontalInput,
                        input.MouseDelta.x,
                        input.MouseDelta.y,
                        input.IsAiming,
                        input.IsFiring);
                }
                lastInputSendTime = Time.time;
            }
        }

        /// <summary>
        /// Интерполирует позицию удаленного игрока
        /// Использует Rigidbody.MovePosition для корректной работы с физикой
        /// ВАЖНО: Не вызывается для локального игрока (IsMine = true)
        /// </summary>
        private void InterpolatePosition()
        {
            // Дополнительная проверка - не интерполируем локального игрока
            // Также проверяем, что tankController.IsLocalPlayer = false для дополнительной защиты
            if (photonView == null || photonView.IsMine || tankController == null || tankController.IsLocalPlayer || tankMovement == null)
                return;

            // Для танков с Rigidbody используем MovePosition/MoveRotation вместо прямого изменения transform
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                // Проверяем, что networkPosition валидна (не нулевая или очень далекая)
                float distance = Vector3.Distance(rb.position, networkPosition);
                if (distance > 100f)
                {
                    // Если позиция слишком далеко, телепортируем сразу (возможно, это первый пакет)
                    rb.position = networkPosition;
                    rb.rotation = networkRotation;
                    return;
                }
                
                // Интерполируем позицию через Rigidbody (для корректной работы с физикой)
                // Используем FixedUpdate-совместимую интерполяцию
                Vector3 targetPosition = Vector3.SmoothDamp(
                    rb.position,
                    networkPosition,
                    ref positionVelocity,
                    1f / interpolationSpeed,
                    Mathf.Infinity,
                    Time.fixedDeltaTime
                );
                rb.MovePosition(targetPosition);

                // Интерполируем вращение через Rigidbody
                Quaternion targetRotation = Quaternion.Slerp(
                    rb.rotation,
                    networkRotation,
                    Time.fixedDeltaTime * interpolationSpeed
                );
                rb.MoveRotation(targetRotation);
            }
            else
            {
                // Если нет Rigidbody или он kinematic, используем transform
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    networkPosition,
                    ref positionVelocity,
                    1f / interpolationSpeed
                );

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    networkRotation,
                    Time.deltaTime * interpolationSpeed
                );
            }

            // Интерполируем вращение башни (всегда через transform, т.к. башня не имеет Rigidbody)
            // ВАЖНО: Только для удаленных игроков (не локального)
            if (tankTurret != null && tankTurret.Turret != null && photonView != null && !photonView.IsMine)
            {
                tankTurret.Turret.rotation = Quaternion.Slerp(
                    tankTurret.Turret.rotation,
                    networkTurretRotation,
                    Time.deltaTime * interpolationSpeed
                );
            }
        }

        /// <summary>
        /// RPC для обработки ввода (вызывается только на удаленных клиентах)
        /// Локальный игрок обрабатывает ввод напрямую через TankInputHandler
        /// </summary>
        [PunRPC]
        private void RPC_ProcessInput(float vertical, float horizontal, float mouseX, float mouseY, bool aiming, bool firing)
        {
            // Не обрабатываем ввод для локального игрока - он обрабатывает его сам
            if (photonView == null || photonView.IsMine || tankController == null)
                return;

            // Создаем команду из сетевых данных
            TankInputCommand command = new TankInputCommand(
                vertical,
                horizontal,
                new Vector2(mouseX, mouseY),
                aiming,
                firing
            );

            // Обрабатываем команду только для удаленных игроков
            tankController.ProcessCommand(command);
        }

        /// <summary>
        /// RPC для синхронизации стрельбы
        /// </summary>
        [PunRPC]
        private void RPC_Fire(float stability)
        {
            if (tankController == null || !tankController.Weapon.CanFire)
                return;

            tankController.Weapon.Fire(stability);
        }

        /// <summary>
        /// RPC для синхронизации здоровья
        /// </summary>
        [PunRPC]
        private void RPC_SetHealth(float health, float maxHealth)
        {
            if (tankHealth == null)
                return;

            // Обновляем здоровье напрямую (без вызова TakeDamage, чтобы избежать повторных событий)
            // Это должно быть реализовано через публичный метод в TankHealth для сетевой синхронизации
        }

        /// <summary>
        /// Photon Observable - синхронизация позиции и вращения
        /// ВАЖНО: Для локального игрока (IsMine) только отправляем данные, не получаем
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Отправляем данные на другие клиенты (только для локального игрока)
                // Проверяем, что это действительно локальный игрок
                if (photonView == null || !photonView.IsMine)
                    return;
                
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                
                if (tankTurret != null && tankTurret.Turret != null)
                {
                    stream.SendNext(tankTurret.Turret.rotation);
                }
                else
                {
                    stream.SendNext(Quaternion.identity);
                }
                
                if (tankHealth != null)
                {
                    stream.SendNext(tankHealth.CurrentHealth);
                    stream.SendNext(tankHealth.MaxHealth);
                }
                else
                {
                    stream.SendNext(100f);
                    stream.SendNext(100f);
                }
            }
            else
            {
                // Получаем данные от других клиентов (только для удаленных игроков)
                // ВАЖНО: Не получаем данные для локального игрока!
                if (photonView == null || photonView.IsMine)
                    return;
                
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                networkTurretRotation = (Quaternion)stream.ReceiveNext();
                
                float health = (float)stream.ReceiveNext();
                float maxHealth = (float)stream.ReceiveNext();
                
                if (tankHealth != null)
                {
                    networkHealth = health;
                    // Можно обновить здоровье визуально, но не вызывать TakeDamage
                }
            }
        }

        /// <summary>
        /// Отправляет команду стрельбы на все клиенты
        /// </summary>
        public void NetworkFire(float stability)
        {
#if PHOTON_UNITY_NETWORKING
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("RPC_Fire", RpcTarget.All, stability);
            }
#endif
        }
#endif

        /// <summary>
        /// Устанавливает, является ли танк локальным игроком
        /// Вынесен из блока #if для доступности из PhotonNetworkManager
        /// </summary>
        public void SetIsLocalPlayer(bool isLocal)
        {
            if (tankController != null)
            {
                bool wasLocal = tankController.IsLocalPlayer;
                // Используем публичный метод TankController
                tankController.SetIsLocalPlayer(isLocal);
                
                if (wasLocal != isLocal)
                {
                    Debug.Log($"[TankNetworkPhoton] SetIsLocalPlayer called: {wasLocal} -> {isLocal} for tank {tankController.name}");
                }
            }
        }

        #region Debug
        private void OnDrawGizmos()
        {
#if PHOTON_UNITY_NETWORKING
            if (photonView != null && Application.isPlaying)
            {
                Gizmos.color = photonView.IsMine ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.5f);
            }
#endif
        }
        #endregion
    }
}

