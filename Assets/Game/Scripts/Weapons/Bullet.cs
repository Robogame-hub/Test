using UnityEngine;
using TankGame.Core;
using TankGame.Tank.Components;

namespace TankGame.Weapons
{
    /// <summary>
    /// Улучшенная и оптимизированная пуля
    /// Использует IPoolable для работы с пулом объектов
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour, IPoolable
    {
        [Header("Damage Settings")]
        [Tooltip("Урон наносимый пулей при попадании")]
        [SerializeField] private float damage = 10f;
        
        [Header("Penetration Settings")]
        [Tooltip("Шанс пробития брони (0-100%). При успешном пробитии наносится полный урон")]
        [Range(0f, 100f)]
        [SerializeField] private float penetrationChance = 70f;
        
        [Tooltip("Множитель урона при непробитии (0.5 = 50% урона, 0.3 = 30% урона)")]
        [Range(0f, 1f)]
        [SerializeField] private float nonPenetrationDamageMultiplier = 0.5f;
        
        [Header("Tracer Settings")]
        [Tooltip("Включить трассер (визуальный след)")]
        [SerializeField] private bool enableTracer = true;

        private Rigidbody rb;
        private Collider bulletCollider;
        private TankWeapon ownerWeapon;
        private GameObject impactEffect;
        private float lifetime;
        private float spawnTime;
        private bool isActive;
        private BulletTracer tracer;

        public float Damage => damage;
        public float TimeAlive => Time.time - spawnTime;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Rigidbody
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            ConfigureRigidbody();

            // Collider
            bulletCollider = GetComponent<Collider>();
            if (bulletCollider == null)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.05f;
                bulletCollider = sphere;
            }

            bulletCollider.isTrigger = false;
            
            // Tracer
            if (enableTracer)
            {
                tracer = GetComponent<BulletTracer>();
                if (tracer == null)
                {
                    tracer = gameObject.AddComponent<BulletTracer>();
                }
            }
        }

        private void ConfigureRigidbody()
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>
        /// Инициализация пули (вызывается при получении из пула)
        /// </summary>
        public void Initialize(TankWeapon weapon, GameObject impact, float life)
        {
            ownerWeapon = weapon;
            impactEffect = impact;
            lifetime = life;
            spawnTime = Time.time;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive)
                return;

            // Проверка времени жизни
            if (TimeAlive >= lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isActive)
                return;

            // Игнорируем столкновение с танком владельцем
            // Проверяем наличие компонента TankWeapon (значит это танк)
            TankWeapon ownerCheck = collision.gameObject.GetComponentInParent<TankWeapon>();
            if (ownerCheck != null && ownerCheck == ownerWeapon)
            {
                Debug.Log($"[Bullet] Ignoring collision with owner tank: {collision.gameObject.name}");
                return;
            }

            // ИСПРАВЛЕНО: Проверяем что есть контакты перед доступом
            if (collision.contacts.Length == 0)
            {
                Debug.LogWarning($"[Bullet] No contact points in collision with {collision.gameObject.name}");
                HandleImpact(transform.position, -rb.linearVelocity.normalized, collision.gameObject);
                return;
            }

            // Получаем точку и нормаль столкновения
            ContactPoint contact = collision.contacts[0];
            HandleImpact(contact.point, contact.normal, collision.gameObject);
        }

        private void HandleImpact(Vector3 hitPoint, Vector3 hitNormal, GameObject hitObject)
        {
            // Проверяем есть ли у объекта компонент получения урона
            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                // Вычисляем финальный урон с учетом пробития
                float finalDamage = CalculateDamageWithPenetration();
                damageable.TakeDamage(finalDamage, hitPoint, hitNormal);
            }

            // Эффект попадания
            PlayImpactEffect(hitPoint, hitNormal);

            // Возвращаем в пул
            ReturnToPool();
        }
        
        /// <summary>
        /// Вычисляет финальный урон с учетом шанса пробития
        /// </summary>
        private float CalculateDamageWithPenetration()
        {
            // Проверяем шанс пробития (0-100%)
            float randomValue = Random.Range(0f, 100f);
            
            if (randomValue <= penetrationChance)
            {
                // Пробитие успешно - полный урон
                Debug.Log($"[Bullet] Penetration SUCCESS! Full damage: {damage}");
                return damage;
            }
            else
            {
                // Пробитие не удалось - урон уменьшается
                float reducedDamage = damage * nonPenetrationDamageMultiplier;
                Debug.Log($"[Bullet] Penetration FAILED! Reduced damage: {reducedDamage} (from {damage}, multiplier: {nonPenetrationDamageMultiplier})");
                return reducedDamage;
            }
        }

        private void PlayImpactEffect(Vector3 position, Vector3 normal)
        {
            if (impactEffect == null)
                return;

            // Создаем эффект с правильной ориентацией
            Quaternion rotation = Quaternion.LookRotation(normal);
            GameObject vfx = Instantiate(impactEffect, position, rotation);

            // Автоматическое уничтожение
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                if (!ps.main.loop)
                {
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(vfx, duration);
                }
                else
                {
                    ps.Stop();
                    Destroy(vfx, 2f);
                }
            }
            else
            {
                Destroy(vfx, 2f);
            }
        }

        private void ReturnToPool()
        {
            if (!isActive)
                return;

            isActive = false;

            if (ownerWeapon != null)
            {
                ownerWeapon.ReturnBullet(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #region IPoolable Implementation

        public void OnSpawnFromPool()
        {
            spawnTime = Time.time;
            isActive = true;

            // Сброс физики
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }
            
            // Включаем коллайдер
            if (bulletCollider != null)
            {
                bulletCollider.enabled = true;
            }
            
            // Включаем трассер
            if (enableTracer && tracer != null)
            {
                tracer.EnableTracer();
            }
            
            Debug.Log($"[Bullet] Spawned from pool: {gameObject.name} at {transform.position}");
        }

        public void OnReturnToPool()
        {
            isActive = false;

            // Сброс физики
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // Делаем kinematic чтобы не падали в пуле
            }
            
            // Отключаем коллайдер
            if (bulletCollider != null)
            {
                bulletCollider.enabled = false;
            }
            
            // Очищаем трассер
            if (enableTracer && tracer != null)
            {
                tracer.OnReturnToPool();
            }
            
            Debug.Log($"[Bullet] Returned to pool: {gameObject.name}");
        }

        #endregion
    }
}

