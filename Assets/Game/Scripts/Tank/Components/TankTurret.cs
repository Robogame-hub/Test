using UnityEngine;
using TankGame.Core;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент башни танка
    /// Отвечает за вращение башни и пушки
    /// </summary>
    public class TankTurret : MonoBehaviour, INetworkSyncable
    {
        [Header("Turret Settings")]
        [Tooltip("Transform башни танка")]
        [SerializeField] private Transform turret;
        [Tooltip("Скорость вращения башни (градусов в секунду)")]
        [SerializeField] private float turretRotationSpeed = 60f;
        [Tooltip("Сглаживание вращения башни (0 = без сглаживания, 10+ = плавное)")]
        [SerializeField] private float turretSmoothness = 8f;

        [Header("Cannon Settings")]
        [Tooltip("Transform пушки танка")]
        [SerializeField] private Transform cannon;
        [Tooltip("Скорость вращения пушки вверх-вниз (градусов в секунду)")]
        [SerializeField] private float cannonRotationSpeed = 60f;
        [Tooltip("Сглаживание вращения пушки (0 = без сглаживания, 10+ = плавное)")]
        [SerializeField] private float cannonSmoothness = 8f;
        [Tooltip("Минимальный угол наклона пушки вниз (градусы)")]
        [SerializeField] private float minCannonAngle = -30f;
        [Tooltip("Максимальный угол наклона пушки вверх (градусы)")]
        [SerializeField] private float maxCannonAngle = 10f;

        [Header("Aiming Settings")]
        [Tooltip("Объект прицела (UI элемент)")]
        [SerializeField] private GameObject crosshair;
        [Tooltip("Максимальная стабильность прицеливания (1.0 = полная точность)")]
        [SerializeField] private float maxAimStability = 1f;
        [Tooltip("Скорость увеличения стабильности когда башня неподвижна")]
        [SerializeField] private float stabilityIncreaseRate = 0.5f;
        [Tooltip("Скорость уменьшения стабильности при движении башни")]
        [SerializeField] private float stabilityDecreaseRate = 2f;

        private float currentStability;
        private float turretRotationVelocity;
        private bool isAiming;
        private float currentTurretRotation;
        private float currentCannonRotation;

        public Transform Turret => turret;
        public Transform Cannon => cannon;
        public float CurrentStability => currentStability;
        public bool IsAiming => isAiming;

        private void Awake()
        {
            InitializeTransforms();
        }

        private void Start()
        {
            if (crosshair)
                crosshair.SetActive(false);
        }

        private void InitializeTransforms()
        {
            if (turret == null)
                turret = transform.Find("Turret");

            if (cannon == null && turret != null)
                cannon = turret.Find("Cannon") ?? turret;
        }

        /// <summary>
        /// Начинает прицеливание
        /// </summary>
        public void StartAiming()
        {
            isAiming = true;
            if (crosshair)
                crosshair.SetActive(true);
        }

        /// <summary>
        /// Прекращает прицеливание
        /// </summary>
        public void StopAiming()
        {
            isAiming = false;
            if (crosshair)
                crosshair.SetActive(false);
            currentStability = 0f;
            
            // Сбрасываем накопленное вращение для плавного перехода
            currentTurretRotation = 0f;
            currentCannonRotation = 0f;
        }

        /// <summary>
        /// Вращает башню и пушку на основе ввода мыши (с плавным сглаживанием)
        /// </summary>
        public void RotateTurret(Vector2 mouseDelta)
        {
            if (!isAiming)
                return;

            float totalMovement = 0f;

            // Вращение башни с сглаживанием
            if (turret != null)
            {
                // Целевое вращение
                float targetRotation = mouseDelta.x * turretRotationSpeed * Time.deltaTime;
                
                // Плавное сглаживание
                if (turretSmoothness > 0)
                {
                    currentTurretRotation = Mathf.Lerp(currentTurretRotation, targetRotation, Time.deltaTime * turretSmoothness);
                }
                else
                {
                    currentTurretRotation = targetRotation;
                }
                
                turret.Rotate(0f, 0f, currentTurretRotation);
                totalMovement += Mathf.Abs(currentTurretRotation);
            }

            // Вращение пушки с сглаживанием
            if (cannon != null)
            {
                // Целевое вращение
                float targetRotation = mouseDelta.y * cannonRotationSpeed * Time.deltaTime;
                
                // Плавное сглаживание
                if (cannonSmoothness > 0)
                {
                    currentCannonRotation = Mathf.Lerp(currentCannonRotation, targetRotation, Time.deltaTime * cannonSmoothness);
                }
                else
                {
                    currentCannonRotation = targetRotation;
                }
                
                // Применяем с ограничением углов
                float currentAngle = cannon.localEulerAngles.x;
                if (currentAngle > 180f)
                    currentAngle -= 360f;

                float newAngle = Mathf.Clamp(currentAngle + currentCannonRotation, minCannonAngle, maxCannonAngle);
                cannon.localRotation = Quaternion.Euler(newAngle, 0f, 0f);
                totalMovement += Mathf.Abs(currentCannonRotation);
            }

            // Обновление стабильности прицеливания
            UpdateStability(totalMovement);
        }

        private void UpdateStability(float movement)
        {
            if (movement > 0.01f)
            {
                turretRotationVelocity = movement;
                currentStability = Mathf.Max(0f, currentStability - stabilityDecreaseRate * Time.deltaTime);
            }
            else
            {
                turretRotationVelocity = Mathf.Lerp(turretRotationVelocity, 0f, Time.deltaTime * 5f);
                if (turretRotationVelocity < 0.1f)
                {
                    currentStability = Mathf.Min(maxAimStability, currentStability + stabilityIncreaseRate * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Сбрасывает стабильность после выстрела
        /// </summary>
        public void ResetStability()
        {
            currentStability = 0f;
        }

        public void Serialize(NetworkWriter writer)
        {
            if (turret != null)
                writer.WriteQuaternion(turret.localRotation);
            if (cannon != null)
                writer.WriteQuaternion(cannon.localRotation);
            writer.WriteFloat(currentStability);
            writer.WriteBool(isAiming);
        }

        public void Deserialize(NetworkReader reader)
        {
            if (turret != null)
                turret.localRotation = reader.ReadQuaternion();
            if (cannon != null)
                cannon.localRotation = reader.ReadQuaternion();
            currentStability = reader.ReadFloat();
            isAiming = reader.ReadBool();
        }
    }
}

