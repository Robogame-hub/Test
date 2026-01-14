using UnityEngine;
using TankGame.Core;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент движения танка
    /// Отвечает только за перемещение и выравнивание по поверхности
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TankMovement : MonoBehaviour, INetworkSyncable
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Ground Alignment")]
        [SerializeField] private float groundCheckDistance = 3f;
        [SerializeField] private float groundAlignSpeed = 8f;
        [SerializeField] private LayerMask groundMask = -1;

        private Rigidbody rb;
        private float currentYaw;
        private Vector3 targetVelocity;

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
        /// Применяет ввод для движения танка
        /// </summary>
        public void ApplyMovement(float vertical, float horizontal)
        {
            // Движение вперед/назад
            Vector3 moveDirection = transform.forward * -vertical;
            targetVelocity = moveDirection * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

            // Вращение танка
            currentYaw += horizontal * rotationSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Выравнивание танка по поверхности земли
        /// </summary>
        public void AlignToGround()
        {
            Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);

            if (!Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundMask))
                return;

            Vector3 groundNormal = hit.normal;

            // Создаем вращение только на основе yaw
            Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 forward = yawRotation * Vector3.forward;

            // Проецируем направление на плоскость земли
            Vector3 alignedForward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            if (alignedForward.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(alignedForward, groundNormal);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                groundAlignSpeed * Time.deltaTime
            );
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

