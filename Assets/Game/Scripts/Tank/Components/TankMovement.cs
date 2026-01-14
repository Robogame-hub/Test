using UnityEngine;
using TankGame.Core;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент движения танка с реалистичной физикой
    /// Отвечает за перемещение, выравнивание по поверхности и наклоны
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TankMovement : MonoBehaviour, INetworkSyncable
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 15f;

        [Header("Ground Alignment - Suspension Points")]
        [SerializeField] private float groundCheckDistance = 3f;
        [SerializeField] private float groundAlignSpeed = 8f;
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private float suspensionOffset = 1f; // Расстояние между точками подвески
        
        [Header("Physics Tilt Settings")]
        [SerializeField] private bool enablePhysicsTilt = true;
        [SerializeField] private float accelerationTiltAmount = 5f; // Наклон при ускорении
        [SerializeField] private float turnTiltAmount = 10f; // Наклон при повороте
        [SerializeField] private float tiltSmoothSpeed = 3f;

        private Rigidbody rb;
        private float currentYaw;
        private Vector3 currentVelocity;
        private Vector3 targetVelocity;
        private float lastVerticalInput;
        private float lastHorizontalInput;

        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public float CurrentYaw => currentYaw;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ConfigureRigidbody();
        }

        private void Start()
        {
            currentYaw = transform.eulerAngles.y;
        }

        private void ConfigureRigidbody()
        {
            rb.freezeRotation = true;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        /// <summary>
        /// Применяет ввод для движения танка с плавным ускорением
        /// </summary>
        public void ApplyMovement(float vertical, float horizontal)
        {
            // Движение вперед/назад с плавным ускорением
            Vector3 moveDirection = transform.forward * -vertical;
            targetVelocity = moveDirection * moveSpeed;
            
            // Плавное изменение скорости (acceleration/deceleration)
            float accelRate = vertical != 0 ? acceleration : deceleration;
            currentVelocity = Vector3.Lerp(
                currentVelocity, 
                targetVelocity, 
                accelRate * Time.deltaTime
            );
            
            rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

            // Вращение танка
            currentYaw += horizontal * rotationSpeed * Time.deltaTime;
            
            // Сохраняем ввод для расчета наклонов
            lastVerticalInput = vertical;
            lastHorizontalInput = horizontal;
        }

        /// <summary>
        /// Выравнивание танка по поверхности земли с системой подвески
        /// </summary>
        public void AlignToGround()
        {
            // Используем 4 точки подвески (как у настоящего танка)
            Vector3 frontLeft = transform.position + transform.forward * suspensionOffset - transform.right * suspensionOffset;
            Vector3 frontRight = transform.position + transform.forward * suspensionOffset + transform.right * suspensionOffset;
            Vector3 rearLeft = transform.position - transform.forward * suspensionOffset - transform.right * suspensionOffset;
            Vector3 rearRight = transform.position - transform.forward * suspensionOffset + transform.right * suspensionOffset;

            // Проверяем каждую точку подвески
            bool hitFL = Physics.Raycast(frontLeft + Vector3.up, Vector3.down, out RaycastHit hitFrontLeft, groundCheckDistance, groundMask);
            bool hitFR = Physics.Raycast(frontRight + Vector3.up, Vector3.down, out RaycastHit hitFrontRight, groundCheckDistance, groundMask);
            bool hitRL = Physics.Raycast(rearLeft + Vector3.up, Vector3.down, out RaycastHit hitRearLeft, groundCheckDistance, groundMask);
            bool hitRR = Physics.Raycast(rearRight + Vector3.up, Vector3.down, out RaycastHit hitRearRight, groundCheckDistance, groundMask);

            // Если хотя бы 3 точки касаются земли, выравниваем танк
            int hitCount = (hitFL ? 1 : 0) + (hitFR ? 1 : 0) + (hitRL ? 1 : 0) + (hitRR ? 1 : 0);
            if (hitCount < 3)
                return;

            // Вычисляем среднюю нормаль поверхности
            Vector3 averageNormal = Vector3.zero;
            if (hitFL) averageNormal += hitFrontLeft.normal;
            if (hitFR) averageNormal += hitFrontRight.normal;
            if (hitRL) averageNormal += hitRearLeft.normal;
            if (hitRR) averageNormal += hitRearRight.normal;
            averageNormal = (averageNormal / hitCount).normalized;

            // Создаем вращение на основе yaw
            Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 forward = yawRotation * Vector3.forward;

            // Проецируем направление на плоскость земли
            Vector3 alignedForward = Vector3.ProjectOnPlane(forward, averageNormal).normalized;
            if (alignedForward.sqrMagnitude < 0.001f)
                return;

            Quaternion groundRotation = Quaternion.LookRotation(alignedForward, averageNormal);

            // Добавляем физические наклоны (тангаж и крен)
            if (enablePhysicsTilt)
            {
                groundRotation = ApplyPhysicsTilt(groundRotation);
            }

            // Плавно применяем вращение
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                groundRotation,
                groundAlignSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Применяет физические наклоны при ускорении и поворотах
        /// </summary>
        private Quaternion ApplyPhysicsTilt(Quaternion baseRotation)
        {
            // Наклон назад при ускорении вперед (как у настоящего танка)
            float pitchTilt = -lastVerticalInput * accelerationTiltAmount;
            
            // Наклон в сторону при повороте
            float rollTilt = lastHorizontalInput * turnTiltAmount;

            // Плавно применяем наклоны
            float currentPitch = Mathf.Lerp(0f, pitchTilt, tiltSmoothSpeed * Time.deltaTime);
            float currentRoll = Mathf.Lerp(0f, rollTilt, tiltSmoothSpeed * Time.deltaTime);

            // Добавляем наклоны к базовому вращению
            Quaternion tiltRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
            return baseRotation * tiltRotation;
        }

        /// <summary>
        /// Устанавливает позицию и поворот (для сетевой синхронизации)
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            currentYaw = rotation.eulerAngles.y;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteVector3(transform.position);
            writer.WriteQuaternion(transform.rotation);
            writer.WriteVector3(rb.linearVelocity);
            writer.WriteFloat(currentYaw);
        }

        public void Deserialize(NetworkReader reader)
        {
            Vector3 position = reader.ReadVector3();
            Quaternion rotation = reader.ReadQuaternion();
            Vector3 velocity = reader.ReadVector3();
            currentYaw = reader.ReadFloat();

            transform.SetPositionAndRotation(position, rotation);
            rb.linearVelocity = velocity;
        }
    }
}

