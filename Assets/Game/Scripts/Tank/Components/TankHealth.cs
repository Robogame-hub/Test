using UnityEngine;
using UnityEngine.Events;
using TankGame.Core;
using TankGame.Game;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент здоровья танка
    /// </summary>
    public class TankHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [Tooltip("Максимальное количество здоровья")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Может ли танк восстанавливать здоровье со временем")]
        [SerializeField] private bool canRegenerate = false;
        [Tooltip("Скорость регенерации здоровья в секунду")]
        [SerializeField] private float regenerationRate = 5f;
        [Tooltip("Задержка перед началом регенерации после получения урона (секунды)")]
        [SerializeField] private float regenerationDelay = 3f;

        [Header("Death Settings")]
        [Tooltip("Эффект взрыва при уничтожении танка")]
        [SerializeField] private GameObject explosionEffect;
        [Tooltip("Время до респавна после смерти (секунды)")]
        [SerializeField] private float respawnDelay = 10f;
        [Tooltip("Скрывать танк после смерти (вместо удаления)")]
        [SerializeField] private bool hideOnDeath = true;

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged; // current, max
        public UnityEvent<Vector3, Vector3> OnDamageTaken; // hitPoint, hitNormal
        public UnityEvent OnDeath;
        public UnityEvent OnRespawn; // Событие респавна

        private float currentHealth;
        private float lastDamageTime;
        private bool isAlive = true;
        private TankController tankController;
        private bool isRespawning = false;
        private float deathTime;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            tankController = GetComponent<TankController>();
        }

        private void Update()
        {
            if (canRegenerate && isAlive && currentHealth < maxHealth)
            {
                if (Time.time - lastDamageTime >= regenerationDelay)
                {
                    Heal(regenerationRate * Time.deltaTime);
                }
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!isAlive || damage <= 0)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            lastDamageTime = Time.time;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke(hitPoint, hitNormal);

            if (currentHealth <= 0 && isAlive)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive || amount <= 0)
                return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (!isAlive)
                return;
                
            isAlive = false;
            deathTime = Time.time;
            
            // Эффект взрыва
            PlayExplosionEffect();
            
            // Отключаем танк
            DisableTank();
            
            // Запускаем респавн через respawnDelay секунд
            Invoke(nameof(Respawn), respawnDelay);
            
            OnDeath?.Invoke();
            
            Debug.Log($"[TankHealth] Tank {gameObject.name} destroyed! Respawn in {respawnDelay} seconds.");
        }
        
        /// <summary>
        /// Воспроизводит эффект взрыва
        /// </summary>
        private void PlayExplosionEffect()
        {
            if (explosionEffect == null)
                return;
            
            // Создаем эффект взрыва в позиции танка
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            
            // Автоматическое уничтожение эффекта
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null && !ps.main.loop)
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(explosion, duration);
            }
            else
            {
                Destroy(explosion, 5f); // Fallback
            }
        }
        
        /// <summary>
        /// Отключает танк при смерти
        /// </summary>
        private void DisableTank()
        {
            if (hideOnDeath)
            {
                // Скрываем визуально
                SetTankVisible(false);
            }
            else
            {
                // Удаляем танк
                Destroy(gameObject);
                return;
            }
            
            // Отключаем коллайдеры
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Останавливаем физику
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Отключаем компоненты управления
            if (tankController != null)
            {
                tankController.enabled = false;
            }
        }
        
        /// <summary>
        /// Включает/выключает видимость танка
        /// </summary>
        private void SetTankVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }
        
        /// <summary>
        /// Включает танк после респавна
        /// </summary>
        private void EnableTank()
        {
            // Включаем коллайдеры
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }
            
            // Включаем физику
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            
            // Включаем компоненты управления
            if (tankController != null)
            {
                tankController.enabled = true;
            }
            
            // Показываем танк
            SetTankVisible(true);
        }

        /// <summary>
        /// Респавнит танк в его спавн-поинте
        /// </summary>
        public void Respawn()
        {
            if (isRespawning)
                return;
                
            isRespawning = true;
            
            // Используем SpawnManager для респавна
            if (SpawnManager.Instance != null && tankController != null)
            {
                SpawnManager.Instance.RespawnTank(tankController);
            }
            
            // Восстанавливаем здоровье
            currentHealth = maxHealth;
            isAlive = true;
            
            // Включаем танк
            EnableTank();
            
            isRespawning = false;
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnRespawn?.Invoke();
            
            Debug.Log($"[TankHealth] Tank {gameObject.name} respawned!");
        }

        public float GetHealth() => currentHealth;
        public bool IsAlive() => isAlive;
    }
}

