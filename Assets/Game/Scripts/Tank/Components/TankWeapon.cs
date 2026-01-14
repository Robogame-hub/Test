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
        [SerializeField] private Transform firePoint;
        [SerializeField] private BulletComponent bulletPrefab;
        [SerializeField] private float bulletSpeed = 20f;
        [SerializeField] private float fireCooldown = 0.5f;
        [SerializeField] private float bulletLifetime = 5f;
        [SerializeField] private int bulletPoolSize = 20;

        [Header("Spread Settings")]
        [SerializeField] private float minSpreadAngle = 0.5f;
        [SerializeField] private float maxSpreadAngle = 5f;

        [Header("VFX")]
        [SerializeField] private GameObject muzzleVFX;
        [SerializeField] private GameObject impactVFX;

        private ObjectPool<BulletComponent> bulletPool;
        private Transform bulletPoolParent;
        private float lastFireTime;

        public Transform FirePoint => firePoint;
        public bool CanFire => Time.time - lastFireTime >= fireCooldown;

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
            if (!CanFire || firePoint == null || bulletPool == null)
                return;

            // Расчет разброса на основе стабильности
            float spread = Mathf.Lerp(maxSpreadAngle, minSpreadAngle, stability);
            float angle = Random.Range(-spread, spread);

            // Направление выстрела
            Vector3 direction = firePoint.up;
            direction = Quaternion.AngleAxis(angle, firePoint.forward) * direction;

            // Получаем пулю из пула
            BulletComponent bullet = bulletPool.Get();
            if (bullet == null)
                return;

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

