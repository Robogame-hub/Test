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
        [SerializeField] private float damage = 10f;

        private Rigidbody rb;
        private Collider bulletCollider;
        private TankWeapon ownerWeapon;
        private GameObject impactEffect;
        private float lifetime;
        private float spawnTime;
        private bool isActive;

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
            if (collision.gameObject.GetComponentInParent<TankWeapon>() != null)
                return;

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
                damageable.TakeDamage(damage, hitPoint, hitNormal);
            }

            // Эффект попадания
            PlayImpactEffect(hitPoint, hitNormal);

            // Возвращаем в пул
            ReturnToPool();
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
            }
        }

        public void OnReturnToPool()
        {
            isActive = false;

            // Сброс физики
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        #endregion
    }
}

