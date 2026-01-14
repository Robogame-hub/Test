using UnityEngine;
using TankGame.Tank;
using TankGame.Commands;

namespace TankGame.Examples
{
    /// <summary>
    /// Пример простого AI для танка
    /// </summary>
    public class AITankExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Ссылка на контроллер танка")]
        [SerializeField] private TankController tankController;
        [Tooltip("Цель для преследования")]
        [SerializeField] private Transform target;

        [Header("AI Settings")]
        [Tooltip("Дальность обнаружения целей")]
        [SerializeField] private float detectionRange = 30f;
        [Tooltip("Дистанция атаки")]
        [SerializeField] private float attackRange = 20f;
        [Tooltip("Время прицеливания перед выстрелом (секунды)")]
        [SerializeField] private float aimTime = 1f;

        private float currentAimTime;
        private bool isAiming;

        private void Start()
        {
            if (tankController == null)
                tankController = GetComponent<TankController>();

            // AI танк не локальный игрок
            // tankController.IsLocalPlayer = false; // Если есть такой сеттер
        }

        private void Update()
        {
            if (tankController == null || target == null)
                return;

            // Создаем команду на основе AI логики
            TankInputCommand aiCommand = CalculateAIInput();
            
            // Выполняем команду
            tankController.ProcessCommand(aiCommand);
        }

        private TankInputCommand CalculateAIInput()
        {
            Vector3 directionToTarget = target.position - transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            float vertical = 0f;
            float horizontal = 0f;
            Vector2 mouseDelta = Vector2.zero;
            bool aiming = false;
            bool firing = false;

            // Если цель в радиусе обнаружения
            if (distanceToTarget <= detectionRange)
            {
                // Поворачиваем танк к цели
                Vector3 directionFlat = new Vector3(directionToTarget.x, 0, directionToTarget.z);
                float angleToTarget = Vector3.SignedAngle(transform.forward, directionFlat, Vector3.up);

                // Поворот
                if (Mathf.Abs(angleToTarget) > 5f)
                {
                    horizontal = Mathf.Sign(angleToTarget);
                }

                // Движение к цели или от неё
                if (distanceToTarget > attackRange)
                {
                    vertical = -1f; // Двигаемся вперед
                }
                else if (distanceToTarget < attackRange * 0.5f)
                {
                    vertical = 1f; // Отступаем
                }

                // Прицеливание и стрельба
                if (distanceToTarget <= attackRange && Mathf.Abs(angleToTarget) < 10f)
                {
                    aiming = true;
                    
                    if (!isAiming)
                    {
                        isAiming = true;
                        currentAimTime = 0f;
                    }

                    currentAimTime += Time.deltaTime;

                    // Имитируем движение мыши для прицеливания
                    // (в реальности нужно вычислять угол до цели)
                    mouseDelta = CalculateAimDelta();

                    // Стреляем когда достаточно прицелились
                    if (currentAimTime >= aimTime)
                    {
                        firing = true;
                        currentAimTime = 0f;
                    }
                }
                else
                {
                    isAiming = false;
                }
            }

            return new TankInputCommand(vertical, horizontal, mouseDelta, aiming, firing);
        }

        private Vector2 CalculateAimDelta()
        {
            // Упрощенный расчет - в реальности нужно учитывать:
            // - Текущий угол башни
            // - Желаемый угол башни (к цели)
            // - Скорость поворота
            
            Vector3 directionToTarget = target.position - tankController.Turret.Turret.position;
            
            // Горизонтальный угол (башня)
            Vector3 directionFlat = new Vector3(directionToTarget.x, 0, directionToTarget.z);
            float horizontalAngle = Vector3.SignedAngle(tankController.Turret.Turret.up, directionFlat, tankController.Turret.Turret.forward);

            // Вертикальный угол (пушка)
            float verticalAngle = Vector3.Angle(Vector3.up, directionToTarget) - 90f;

            // Преобразуем в mouseDelta
            Vector2 delta = new Vector2(horizontalAngle * 0.1f, verticalAngle * 0.1f);
            
            return delta;
        }

        private void OnDrawGizmos()
        {
            if (target == null)
                return;

            // Визуализация радиусов
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Линия к цели
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}

