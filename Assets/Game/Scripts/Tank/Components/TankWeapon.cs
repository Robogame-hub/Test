using UnityEngine;
using TankGame.Utils;
using BulletComponent = TankGame.Weapons.Bullet;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент оружия танка
    /// Отвечает за стрельбу и управление пулями
    /// </summary>
    public class TankWeapon : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [Tooltip("Точка откуда вылетает снаряд")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Префаб пули")]
        [SerializeField] private BulletComponent bulletPrefab;
        [Tooltip("Скорость полета пули")]
        [SerializeField] private float bulletSpeed = 20f;
        [Tooltip("Время между выстрелами (секунды)")]
        [SerializeField] private float fireCooldown = 0.5f;
        [Tooltip("Время жизни пули до автоматического уничтожения (секунды)")]
        [SerializeField] private float bulletLifetime = 5f;
        [Tooltip("Начальный размер пула пуль (для оптимизации)")]
        [SerializeField] private int bulletPoolSize = 20;

        [Header("Spread Settings")]
        [Tooltip("Минимальный разброс при максимальной стабильности (градусы)")]
        [SerializeField] private float minSpreadAngle = 0.5f;
        [Tooltip("Максимальный разброс при нулевой стабильности (градусы)")]
        [SerializeField] private float maxSpreadAngle = 5f;

        [Header("VFX")]
        [Tooltip("Эффект дульной вспышки при выстреле")]
        [SerializeField] private GameObject muzzleVFX;
        [Tooltip("Эффект попадания пули")]
        [SerializeField] private GameObject impactVFX;

        private ObjectPool<BulletComponent> bulletPool;
        private Transform bulletPoolParent;
        private float lastFireTime;
        private bool isFiring; // Защита от двойного выстрела в одном кадре

        public Transform FirePoint => firePoint;
        public bool CanFire => Time.time - lastFireTime >= fireCooldown && !isFiring;

        private void Awake()
        {
            InitializeFirePoint();
            InitializeBulletPool();
        }

        private void InitializeFirePoint()
        {
            if (firePoint == null)
            {
                Transform turret = transform.Find("Turret");
                if (turret != null)
                {
                    Transform cannon = turret.Find("Cannon") ?? turret;
                    firePoint = cannon.Find("FirePoint") ?? cannon;
                }
            }
        }

        private void InitializeBulletPool()
        {
            if (bulletPrefab == null)
            {
                Debug.LogError("TankWeapon: Bullet prefab is not assigned!");
                return;
            }

            bulletPoolParent = new GameObject("BulletPool").transform;
            bulletPoolParent.SetParent(transform);

            bulletPool = new ObjectPool<BulletComponent>(
                bulletPrefab,
                bulletPoolSize,
                bulletPoolParent,
                expandable: true
            );
        }

        /// <summary>
        /// Выстрел с учетом разброса
        /// </summary>
        public void Fire(float stability)
        {
            // Защита от двойного выстрела
            if (isFiring)
            {
                Debug.LogWarning("TankWeapon: Попытка двойного выстрела в одном кадре!");
                return;
            }
            
            if (!CanFire || firePoint == null || bulletPool == null)
                return;

            isFiring = true;

            // Расчет разброса на основе стабильности
            float spread = Mathf.Lerp(maxSpreadAngle, minSpreadAngle, stability);
            float angle = Random.Range(-spread, spread);

            // Направление выстрела
            Vector3 direction = firePoint.up;
            direction = Quaternion.AngleAxis(angle, firePoint.forward) * direction;

            // Получаем пулю из пула
            BulletComponent bullet = bulletPool.Get();
            if (bullet == null)
            {
                isFiring = false;
                return;
            }

            // Настраиваем пулю
            bullet.transform.SetPositionAndRotation(
                firePoint.position,
                Quaternion.LookRotation(direction, firePoint.forward)
            );

            bullet.Initialize(this, impactVFX, bulletLifetime);

            // Применяем физику
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * bulletSpeed;
            }

            // VFX
            PlayMuzzleVFX();

            lastFireTime = Time.time;
            
            // Сбрасываем флаг в конце кадра
            StartCoroutine(ResetFiringFlag());
        }
        
        private System.Collections.IEnumerator ResetFiringFlag()
        {
            yield return new WaitForEndOfFrame();
            isFiring = false;
        }

        private void PlayMuzzleVFX()
        {
            if (muzzleVFX == null || firePoint == null)
                return;

            GameObject vfx = Instantiate(muzzleVFX, firePoint.position, firePoint.rotation, firePoint);
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();

            if (ps != null && !ps.main.loop)
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(vfx, duration);
            }
            else
            {
                Destroy(vfx, 2f); // Fallback
            }
        }

        /// <summary>
        /// Возвращает пулю в пул
        /// </summary>
        public void ReturnBullet(BulletComponent bullet)
        {
            if (bulletPool != null && bullet != null)
            {
                bulletPool.Return(bullet);
            }
        }

        private void OnDestroy()
        {
            bulletPool?.Clear();
        }
    }
}

