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
        [Tooltip("Точка где появляется эффект выстрела (если не указана, используется firePoint)")]
        [SerializeField] private Transform muzzleVFXPoint;
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
        [Tooltip("Дополнительный разброс при движении танка (градусы)")]
        [SerializeField] private float movementSpreadMultiplier = 3f;

        [Header("VFX")]
        [Tooltip("Эффект дульной вспышки при выстреле")]
        [SerializeField] private GameObject muzzleVFX;
        [Tooltip("Эффект попадания пули")]
        [SerializeField] private GameObject impactVFX;
        
        [Header("Debug")]
        [Tooltip("Показывать debug ray направления выстрела")]
        [SerializeField] private bool showDebugRay = true;
        [Tooltip("Длина debug ray")]
        [SerializeField] private float debugRayLength = 20f;
        [Tooltip("Время отображения debug ray")]
        [SerializeField] private float debugRayDuration = 2f;

        private ObjectPool<BulletComponent> bulletPool;
        private Transform bulletPoolParent;
        private float lastFireTime;
        private bool isFiring; // Защита от двойного выстрела в одном кадре
        private TankMovement tankMovement; // Для получения фактора движения

        public Transform FirePoint => firePoint;
        public bool CanFire => Time.time - lastFireTime >= fireCooldown && !isFiring;
        public float LastFireTime => lastFireTime;
        public float FireCooldown => fireCooldown;

        private void Awake()
        {
            InitializeFirePoint();
            InitializeBulletPool();
            tankMovement = GetComponentInParent<TankMovement>();
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

            // ИСПРАВЛЕНО: Создаем pool parent как корневой объект сцены, а не дочерний танка
            // Это предотвращает движение неактивных пуль вместе с танком
            bulletPoolParent = new GameObject($"BulletPool_{gameObject.name}").transform;
            bulletPoolParent.position = Vector3.zero;

            bulletPool = new ObjectPool<BulletComponent>(
                bulletPrefab,
                bulletPoolSize,
                bulletPoolParent,
                expandable: true
            );
            
            Debug.Log($"[TankWeapon] Bullet pool created: {bulletPoolParent.name} with {bulletPoolSize} bullets");
        }

        /// <summary>
        /// Выстрел с учетом разброса
        /// </summary>
        public void Fire(float stability)
        {
            Debug.Log($"[TankWeapon.Fire] Called! isFiring={isFiring}, CanFire={CanFire}, Frame={Time.frameCount}, Time={Time.time}");
            
            // Защита от двойного выстрела
            if (isFiring)
            {
                Debug.LogWarning($"[TankWeapon] Попытка двойного выстрела в одном кадре! Frame={Time.frameCount}");
                return;
            }
            
            if (!CanFire)
            {
                Debug.LogWarning($"[TankWeapon] CanFire=false! Cooldown remaining: {fireCooldown - (Time.time - lastFireTime)}");
                return;
            }
            
            if (firePoint == null)
            {
                Debug.LogError("[TankWeapon] FirePoint is null!");
                return;
            }
            
            if (bulletPool == null)
            {
                Debug.LogError("[TankWeapon] BulletPool is null!");
                return;
            }

            isFiring = true;
            Debug.Log($"[TankWeapon] FIRING! Setting isFiring=true, Frame={Time.frameCount}");

            // ═══════════════════════════════════════════════════════════════
            // СНАЙПЕРСКИЙ ВЫСТРЕЛ: FirePoint → Прицел → Цель
            // ═══════════════════════════════════════════════════════════════
            
            // ШАГ 1: Расчет разброса
            // ─────────────────────────────────────────────────────────────
            // Базовый разброс от стабильности пушки (движение мыши)
            float spread = Mathf.Lerp(maxSpreadAngle, minSpreadAngle, stability);
            
            // Добавляем разброс от движения танка
            if (tankMovement != null)
            {
                float movementFactor = tankMovement.GetMovementFactor();
                spread += movementFactor * movementSpreadMultiplier;
            }
            
            // Защита от отрицательного разброса
            spread = Mathf.Max(0f, spread);
            
            // ШАГ 2: Найти точку куда смотрит прицел
            // ─────────────────────────────────────────────────────────────
            // GetAimPoint() делает raycast от камеры через центр экрана
            // и возвращает точку в мире куда направлен UI прицел
            Vector3 targetPoint = GetAimPoint();
            
            // ШАГ 3: Направление ОТ FirePoint К точке прицеливания
            // ─────────────────────────────────────────────────────────────
            // Это и есть "raycast от FirePoint в направлении прицела"!
            Vector3 direction = (targetPoint - firePoint.position).normalized;
            
            // Проверка на нулевое направление
            if (direction.magnitude < 0.001f)
            {
                Debug.LogWarning("[TankWeapon] Invalid direction! Using firePoint.forward");
                direction = firePoint.forward;
            }
            
            Debug.Log($"[TankWeapon] Sniper Shot: FirePoint={firePoint.position} → Target={targetPoint}, Spread={spread:F2}°");
            
            // ШАГ 4: Применяем разброс (в стабильном состоянии разброс = 0.5°)
            // ─────────────────────────────────────────────────────────────
            if (spread > 0.001f)
            {
                float randomAngleX = Random.Range(-spread, spread);
                float randomAngleY = Random.Range(-spread, spread);
                
                // Применяем разброс вокруг направления к прицелу
                Quaternion spreadRotation = Quaternion.Euler(randomAngleX, randomAngleY, 0f);
                Quaternion aimRotation = Quaternion.LookRotation(direction);
                direction = aimRotation * spreadRotation * Vector3.forward;
            }
            
            direction = direction.normalized;
            
            // РЕЗУЛЬТАТ: Пуля летит ОТ FirePoint В направлении прицела (± разброс)
            // В стабильном состоянии = снайперский выстрел точно в цель! 🎯

            // Получаем пулю из пула
            BulletComponent bullet = bulletPool.Get();
            if (bullet == null)
            {
                Debug.LogError("[TankWeapon] Failed to get bullet from pool!");
                isFiring = false;
                return;
            }

            // ИСПРАВЛЕНО: Убираем parent (пуля должна быть независимой в мире)
            bullet.transform.SetParent(null);
            
            // Настраиваем позицию и ротацию
            bullet.transform.SetPositionAndRotation(
                firePoint.position,
                Quaternion.LookRotation(direction)
            );

            // Инициализируем пулю
            bullet.Initialize(this, impactVFX, bulletLifetime);

            // Применяем физику
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * bulletSpeed;
                bulletRb.angularVelocity = Vector3.zero; // Сброс вращения
            }
            
            Debug.Log($"[TankWeapon] Bullet fired: {bullet.name} at {firePoint.position} direction {direction}");

            // VFX
            PlayMuzzleVFX();
            
            // Debug Ray
            DrawDebugRay(firePoint.position, direction);

            lastFireTime = Time.time;
            
            // Сбрасываем флаг в конце кадра
            StartCoroutine(ResetFiringFlag());
        }
        
        private System.Collections.IEnumerator ResetFiringFlag()
        {
            Debug.Log($"[TankWeapon] Waiting to reset isFiring flag... Frame={Time.frameCount}");
            yield return new WaitForEndOfFrame();
            isFiring = false;
            Debug.Log($"[TankWeapon] isFiring flag RESET to false. Frame={Time.frameCount}");
        }
        
        /// <summary>
        /// Получить точку прицеливания для снайперского выстрела
        /// 
        /// АЛГОРИТМ:
        /// 1. Raycast от камеры через центр экрана (где UI прицел)
        /// 2. Находим точку в мире куда "смотрит" прицел
        /// 3. Направление = ОТ FirePoint К этой точке
        /// 4. Пуля летит по этому направлению (+ разброс)
        /// 
        /// РЕЗУЛЬТАТ: В стабильном состоянии = снайперский выстрел точно в центр прицела!
        /// </summary>
        private Vector3 GetAimPoint()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Fallback: используем направление вперед от FirePoint
                Debug.LogWarning("[TankWeapon] Camera.main not found! Using firePoint.forward");
                return firePoint.position + firePoint.forward * 100f;
            }
            
            // ШАГ 1: Raycast от центра экрана (где UI прицел смотрит)
            Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            // Максимальная дистанция выстрела
            float maxDistance = 500f;
            
            // ШАГ 2: Найти точку в мире куда направлен прицел
            if (Physics.Raycast(cameraRay, out RaycastHit hit, maxDistance))
            {
                // Нашли цель - точка попадания
                Debug.Log($"[TankWeapon] Aim Point: {hit.point}, Distance: {Vector3.Distance(firePoint.position, hit.point):F1}m, Target: {hit.collider.name}");
                return hit.point;
            }
            else
            {
                // Не нашли - точка вдали по направлению прицела
                Vector3 farPoint = cameraRay.origin + cameraRay.direction * maxDistance;
                Debug.Log($"[TankWeapon] Aim Point: Far distance {maxDistance}m (no target)");
                return farPoint;
            }
        }
        
        
        /// <summary>
        /// Рисует debug ray для визуализации СНАЙПЕРСКОГО выстрела
        /// 
        /// ВИЗУАЛИЗАЦИЯ:
        /// 🔴 Красный крест = FirePoint (точка выстрела)
        /// 🔵 Синяя линия = Raycast от камеры (где прицел смотрит)
        /// 🔷 Голубая линия = Направление ОТ FirePoint К прицелу (БЕЗ разброса)
        /// 🟡 Желтая линия = Направление пули (С разбросом)
        /// 🟢 Зеленый крест = Точка попадания
        /// 
        /// В стабильном состоянии: голубая и желтая линии совпадают!
        /// </summary>
        private void DrawDebugRay(Vector3 origin, Vector3 direction)
        {
            if (!showDebugRay)
                return;
            
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;
            
            // Точка куда смотрит прицел (в мире)
            Vector3 targetPoint = GetAimPoint();
            
            // ═══════════════════════════════════════════════════════════════
            // ВИЗУАЛИЗАЦИЯ СНАЙПЕРСКОГО ВЫСТРЕЛА
            // ═══════════════════════════════════════════════════════════════
            
            // 1. 🔴 FirePoint (точка выстрела)
            Debug.DrawLine(origin + Vector3.up * 0.1f, origin - Vector3.up * 0.1f, Color.red, debugRayDuration);
            Debug.DrawLine(origin + Vector3.right * 0.1f, origin - Vector3.right * 0.1f, Color.red, debugRayDuration);
            
            // 2. 🔵 Raycast от камеры к прицелу (что видит игрок)
            Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Debug.DrawLine(cameraRay.origin, targetPoint, Color.cyan, debugRayDuration);
            
            // 3. 🔷 Направление ОТ FirePoint К прицелу (БЕЗ разброса - снайперская линия)
            Debug.DrawLine(origin, targetPoint, Color.blue, debugRayDuration * 1.5f);
            
            // 4. 🟡 Направление пули (С разбросом - реальный выстрел)
            Vector3 bulletEndPoint = origin + direction * Vector3.Distance(origin, targetPoint);
            Debug.DrawLine(origin, bulletEndPoint, Color.yellow, debugRayDuration);
            
            // 5. 🟢 Точка попадания
            Debug.DrawLine(targetPoint + Vector3.up * 0.2f, targetPoint - Vector3.up * 0.2f, Color.green, debugRayDuration);
            Debug.DrawLine(targetPoint + Vector3.right * 0.2f, targetPoint - Vector3.right * 0.2f, Color.green, debugRayDuration);
            
            // Дополнительно: маркер разброса (насколько отклонилась желтая от голубой)
            float spreadDeviation = Vector3.Angle(targetPoint - origin, direction);
            Debug.DrawLine(bulletEndPoint, targetPoint, Color.magenta, debugRayDuration);
            
            // Лог для отладки
            float movementFactor = tankMovement != null ? tankMovement.GetMovementFactor() : 0f;
            float distance = Vector3.Distance(origin, targetPoint);
            Debug.Log($"[TankWeapon] 🎯 Sniper Shot: Distance={distance:F1}m, Spread Deviation={spreadDeviation:F2}°, MovementFactor={movementFactor:F2}");
        }

        private void PlayMuzzleVFX()
        {
            if (muzzleVFX == null)
                return;

            // Используем muzzleVFXPoint если он назначен, иначе firePoint
            Transform effectPoint = muzzleVFXPoint != null ? muzzleVFXPoint : firePoint;
            
            if (effectPoint == null)
            {
                Debug.LogWarning("[TankWeapon] No point for muzzle VFX! Assign muzzleVFXPoint or firePoint.");
                return;
            }

            GameObject vfx = Instantiate(muzzleVFX, effectPoint.position, effectPoint.rotation, effectPoint);
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
            
            Debug.Log($"[TankWeapon] Muzzle VFX played at: {effectPoint.name} ({effectPoint.position})");
        }

        /// <summary>
        /// Возвращает пулю в пул
        /// </summary>
        public void ReturnBullet(BulletComponent bullet)
        {
            if (bulletPool != null && bullet != null)
            {
                // Возвращаем пулю под parent pool
                bullet.transform.SetParent(bulletPoolParent);
                bullet.transform.localPosition = Vector3.zero;
                bullet.transform.localRotation = Quaternion.identity;
                
                bulletPool.Return(bullet);
                
                Debug.Log($"[TankWeapon] Bullet returned to pool: {bullet.name}");
            }
        }

        private void OnDestroy()
        {
            // Очищаем пул
            bulletPool?.Clear();
            
            // Уничтожаем pool parent объект
            if (bulletPoolParent != null)
            {
                Destroy(bulletPoolParent.gameObject);
            }
            
            Debug.Log("[TankWeapon] Destroyed and cleaned up bullet pool");
        }
        
        /// <summary>
        /// Визуализация FirePoint и MuzzleVFXPoint в редакторе
        /// </summary>
        private void OnDrawGizmos()
        {
            // FirePoint - откуда летит пуля
            if (firePoint != null)
            {
                // Красная сфера - FirePoint
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(firePoint.position, 0.1f);
                
                // Желтая линия - направление выстрела
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(firePoint.position, firePoint.forward * 2f);
                
                // Зеленая линия - up (для понимания ориентации)
                Gizmos.color = Color.green;
                Gizmos.DrawRay(firePoint.position, firePoint.up * 0.5f);
            }
            
            // MuzzleVFXPoint - где появляется эффект выстрела
            if (muzzleVFXPoint != null)
            {
                // Оранжевая сфера - MuzzleVFXPoint
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                Gizmos.DrawWireSphere(muzzleVFXPoint.position, 0.15f);
                
                // Оранжевая линия к FirePoint (показывает связь)
                if (firePoint != null)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                    Gizmos.DrawLine(firePoint.position, muzzleVFXPoint.position);
                }
            }
        }
    }
}

