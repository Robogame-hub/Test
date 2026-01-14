using UnityEngine;
using TankGame.Tank;
using TankGame.Tank.Components;

namespace TankGame.Examples
{
    /// <summary>
    /// Пример создания кастомного танка с уникальными способностями
    /// </summary>
    public class CustomTankExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Ссылка на контроллер танка")]
        [SerializeField] private TankController tankController;

        [Header("Custom Abilities")]
        [Tooltip("Множитель скорости при активации буста")]
        [SerializeField] private float boostSpeed = 10f;
        [Tooltip("Длительность буста (секунды)")]
        [SerializeField] private float boostDuration = 2f;
        [Tooltip("Клавиша активации буста")]
        [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;

        private bool isBoosting;
        private float boostEndTime;
        private float originalSpeed;

        private void Start()
        {
            if (tankController == null)
                tankController = GetComponent<TankController>();

            if (tankController != null)
            {
                originalSpeed = tankController.Movement.MoveSpeed;
            }

            // Подписываемся на события здоровья
            if (tankController.Health != null)
            {
                tankController.Health.OnDamageTaken.AddListener(OnDamageTaken);
                tankController.Health.OnDeath.AddListener(OnDeath);
            }
        }

        private void Update()
        {
            HandleBoost();
        }

        private void HandleBoost()
        {
            // Активация буста
            if (Input.GetKeyDown(boostKey) && !isBoosting)
            {
                ActivateBoost();
            }

            // Деактивация буста
            if (isBoosting && Time.time >= boostEndTime)
            {
                DeactivateBoost();
            }
        }

        private void ActivateBoost()
        {
            isBoosting = true;
            boostEndTime = Time.time + boostDuration;

            // Увеличиваем скорость
            // Примечание: Нужно добавить setter в TankMovement или использовать рефлексию
            Debug.Log("Boost activated! Speed increased!");
            
            // Можно добавить VFX
            // Instantiate(boostVFX, transform.position, Quaternion.identity);
        }

        private void DeactivateBoost()
        {
            isBoosting = false;
            Debug.Log("Boost deactivated! Speed returned to normal.");
        }

        private void OnDamageTaken(Vector3 hitPoint, Vector3 hitNormal)
        {
            Debug.Log($"Tank took damage at {hitPoint}");
            
            // Можно добавить реакцию на урон
            // - Экранный эффект
            // - Звук
            // - Вибрация контроллера
        }

        private void OnDeath()
        {
            Debug.Log("Tank destroyed! Game Over!");
            
            // Можно добавить:
            // - Экран смерти
            // - Статистику
            // - Таймер респавна
        }

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (tankController?.Health != null)
            {
                tankController.Health.OnDamageTaken.RemoveListener(OnDamageTaken);
                tankController.Health.OnDeath.RemoveListener(OnDeath);
            }
        }
    }
}

