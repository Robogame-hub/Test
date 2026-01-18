using UnityEngine;
using TankGame.Tank;
using TankGame.Commands;
using TankGame.Tank.Components;

#if PHOTON_PUN_2
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
#if PHOTON_PUN_2
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
        }

        private void Start()
        {
#if PHOTON_PUN_2
            // Устанавливаем isLocalPlayer в зависимости от владения Photon
            if (tankController != null && photonView != null)
            {
                SetIsLocalPlayer(photonView.IsMine);
            }
#else
            Debug.LogWarning("[TankNetworkPhoton] Photon PUN 2 not installed! Install from Asset Store or via Package Manager. This component will not work without Photon.");
#endif
        }

        private void Update()
        {
#if PHOTON_PUN_2
            if (photonView == null || tankController == null)
                return;

            // Локальный игрок - отправляем ввод на сервер
            if (photonView.IsMine)
            {
                SendInputToServer();
            }
            // Удаленный игрок - интерполируем позицию
            else if (enableInterpolation)
            {
                InterpolatePosition();
            }
#endif
        }

#if PHOTON_PUN_2
        /// <summary>
        /// Отправляет ввод на сервер
        /// </summary>
        private void SendInputToServer()
        {
            float sendInterval = 1f / inputSendRate;
            if (Time.time - lastInputSendTime >= sendInterval)
            {
                if (inputHandler != null)
                {
                    TankInputCommand input = inputHandler.GetCurrentInput();
                    // Отправляем ввод через RPC
                    photonView.RPC("RPC_ProcessInput", RpcTarget.All, 
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
        /// </summary>
        private void InterpolatePosition()
        {
            if (tankMovement == null)
                return;

            // Интерполируем позицию
            transform.position = Vector3.SmoothDamp(
                transform.position,
                networkPosition,
                ref positionVelocity,
                1f / interpolationSpeed
            );

            // Интерполируем вращение
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                networkRotation,
                Time.deltaTime * interpolationSpeed
            );

            // Интерполируем вращение башни
            if (tankTurret != null && tankTurret.Turret != null)
            {
                tankTurret.Turret.rotation = Quaternion.Slerp(
                    tankTurret.Turret.rotation,
                    networkTurretRotation,
                    Time.deltaTime * interpolationSpeed
                );
            }
        }

        /// <summary>
        /// RPC для обработки ввода (вызывается на всех клиентах)
        /// </summary>
        [PunRPC]
        private void RPC_ProcessInput(float vertical, float horizontal, float mouseX, float mouseY, bool aiming, bool firing)
        {
            if (tankController == null)
                return;

            // Создаем команду из сетевых данных
            TankInputCommand command = new TankInputCommand(
                vertical,
                horizontal,
                new Vector2(mouseX, mouseY),
                aiming,
                firing
            );

            // Обрабатываем команду (только если это не локальный игрок, чтобы избежать двойной обработки)
            // Но для простоты обрабатываем на всех, так как локальный игрок не обрабатывает RPC ввод
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
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Отправляем данные на другие клиенты
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
                // Получаем данные от других клиентов
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                networkTurretRotation = (Quaternion)stream.ReceiveNext();
                
                float health = (float)stream.ReceiveNext();
                float maxHealth = (float)stream.ReceiveNext();
                
                if (tankHealth != null && !photonView.IsMine)
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
#if PHOTON_PUN_2
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
                // Используем публичный метод TankController
                tankController.SetIsLocalPlayer(isLocal);
            }
        }

        #region Debug
        private void OnDrawGizmos()
        {
#if PHOTON_PUN_2
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

