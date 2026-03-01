using UnityEngine;
using UnityEngine.Events;
using TankGame.Core;
using TankGame.Game;
using TankGame.Tank.AI;

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
            OnHealthChanged ??= new UnityEvent<float, float>();
            OnDamageTaken ??= new UnityEvent<Vector3, Vector3>();
            OnDeath ??= new UnityEvent();
            OnRespawn ??= new UnityEvent();

            currentHealth = maxHealth;
            tankController = GetComponentInParent<TankController>();
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
            
            PlayExplosionEffect();
            DisableTank();
            
            // Бот: освобождаем спавн-поинт (респавн будет в случайном)
            if (tankController != null && !tankController.IsLocalPlayer && SpawnManager.Instance != null)
            {
                SpawnManager.Instance.FreeSpawnPoint(tankController);
            }
            
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
        /// Отключает танк при смерти (полностью скрывает и отключает Gizmos).
        /// Игрока сразу переносит в спавн-поинт, чтобы не рисовалось на месте смерти.
        /// </summary>
        private void DisableTank()
        {
            if (hideOnDeath)
            {
                SetTankVisible(false);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            Transform rootForRb = transform.root;
            Collider[] colliders = rootForRb.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
                collider.enabled = false;
            
            Rigidbody rb = rootForRb.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Отключаем компоненты на корне танка (Gizmos и управление)
            Transform root = transform.root;
            if (tankController != null) tankController.enabled = false;
            var movement = root.GetComponent<TankMovement>();
            if (movement != null) movement.enabled = false;
            var turret = root.GetComponent<TankTurret>();
            if (turret != null) turret.enabled = false;
            var weapon = root.GetComponent<TankWeapon>();
            if (weapon != null) weapon.enabled = false;
            var ai = root.GetComponent<NavMeshTankAI>();
            if (ai != null) ai.enabled = false;
            
            // Игрока сразу переносим в спавн-поинт (труп не остаётся на месте смерти)
            if (tankController != null && tankController.IsLocalPlayer && SpawnManager.Instance != null)
            {
                var spawnPoint = SpawnManager.Instance.GetPlayerSpawnPoint();
                if (spawnPoint != null)
                {
                    rootForRb.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);
                    if (rb != null)
                    {
                        rb.position = spawnPoint.Position;
                        rb.rotation = spawnPoint.Rotation;
                    }
                }
            }
        }
        
        /// <summary>
        /// Включает/выключает видимость танка
        /// </summary>
        private void SetTankVisible(bool visible)
        {
            Renderer[] renderers = transform.root.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = visible;
        }
        
        /// <summary>
        /// Включает танк после респавна (включает обратно компоненты и видимость).
        /// </summary>
        private void EnableTank()
        {
            Transform root = transform.root;
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
                collider.enabled = true;
            
            Rigidbody rb = root.GetComponentInChildren<Rigidbody>();
            if (rb != null)
                rb.isKinematic = false;
            if (tankController != null) tankController.enabled = true;
            var movement = root.GetComponent<TankMovement>();
            if (movement != null) movement.enabled = true;
            var turret = root.GetComponent<TankTurret>();
            if (turret != null)
            {
                turret.enabled = true;
                if (tankController != null && tankController.IsLocalPlayer)
                    turret.SnapCameraToTank();
            }
            var weapon = root.GetComponent<TankWeapon>();
            if (weapon != null) weapon.enabled = true;
            // Включаем AI только у ботов: у игрока не трогаем, иначе OnEnable снова вызовет SetIsLocalPlayer(false)
            var ai = root.GetComponent<NavMeshTankAI>();
            if (ai != null && (tankController == null || !tankController.IsLocalPlayer))
                ai.enabled = true;
            
            SetTankVisible(true);
        }

        /// <summary>
        /// Респавнит танк в спавн-поинте и включает камеру.
        /// </summary>
        public void Respawn()
        {
            if (isRespawning)
                return;
            isRespawning = true;
            
            currentHealth = maxHealth;
            isAlive = true;
            
            if (SpawnManager.Instance != null && tankController != null)
                SpawnManager.Instance.RespawnTank(tankController);
            
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

