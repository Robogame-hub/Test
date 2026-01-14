using UnityEngine;
using UnityEngine.Events;
using TankGame.Core;

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

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged; // current, max
        public UnityEvent<Vector3, Vector3> OnDamageTaken; // hitPoint, hitNormal
        public UnityEvent OnDeath;

        private float currentHealth;
        private float lastDamageTime;
        private bool isAlive = true;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
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
            isAlive = false;
            OnDeath?.Invoke();
        }

        public void Respawn()
        {
            currentHealth = maxHealth;
            isAlive = true;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public float GetHealth() => currentHealth;
        public bool IsAlive() => isAlive;
    }
}

