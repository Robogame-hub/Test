using UnityEngine;
using TankGame.Utils;
using BulletComponent = TankGame.Weapons.Bullet;
using UnityEngine.Events;

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
        [Tooltip("Размер магазина")]
        [SerializeField] private int magazineSize = 10;
        [Tooltip("Общий запас патронов (без магазина)")]
        [SerializeField] private int reserveAmmo = 50;
        [Tooltip("Длительность перезарядки (секунды)")]
        [SerializeField] private float reloadDuration = 1.5f;

        [Header("Damage Settings")]
        [Tooltip("Базовый урон, который наносит это оружие")]
        [SerializeField] private float bulletDamage = 10f;

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
        [Tooltip("Множитель масштаба эффекта попадания (для пушки можно 2–4)")]
        [SerializeField] private float impactVfxScaleMultiplier = 1f;
        [Tooltip("Множитель масштаба дульной вспышки")]
        [SerializeField] private float muzzleVfxScaleMultiplier = 1f;
        [Tooltip("Учитывать масштаб MuzzleVFXPoint при спавне эффекта")]
        [SerializeField] private bool inheritMuzzlePointScale = false;

        [Header("Animation")]
        [Tooltip("Animator пушки/ствола для анимации выстрела")]
        [SerializeField] private Animator weaponAnimator;
        [Tooltip("Название Trigger-параметра в Animator для выстрела")]
        [SerializeField] private string fireAnimationTrigger = "Fire";
        [Tooltip("Animator ленты патронов для анимации при выстреле")]
        [SerializeField] private Animator ammoBeltAnimator;
        [Tooltip("Название Trigger-параметра в Animator ленты патронов")]
        [SerializeField] private string ammoBeltFireAnimationTrigger = "Fire";
        [Tooltip("Запускать анимацию ленты при выстреле этого оружия")]
        [SerializeField] private bool playAmmoBeltAnimationOnFire = true;

        [Header("Audio")]
        [Tooltip("Источник звука для этого оружия (если не назначен, будет найден автоматически)")]
        [SerializeField] private AudioSource weaponAudioSource;
        [Tooltip("Звук выстрела этого оружия (для пушки и пулемета назначаются разные клипы)")]
        [SerializeField] private AudioClip fireSound;
        [Tooltip("Звук перезарядки этого оружия")]
        [SerializeField] private AudioClip reloadSound;
        [Tooltip("Звук попытки выстрела при пустом магазине")]
        [SerializeField] private AudioClip emptyShotSound;
        [Tooltip("Громкость звуков оружия")]
        [SerializeField] [Range(0f, 1f)] private float weaponSfxVolume = 1f;
        [Tooltip("Минимальная пауза между dry-fire звуками (секунды)")]
        [SerializeField] private float emptyShotSoundCooldown = 0.2f;
        
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
        private bool isReloading;
        private int currentAmmoInMagazine;
        private TankMovement tankMovement; // Для получения фактора движения
        private TankTurret tankTurret; // Для проверки выравнивания башни
        private Coroutine reloadCoroutine;
        private int fireAnimationTriggerHash;
        private int ammoBeltFireAnimationTriggerHash;
        private float lastEmptyShotSoundTime = -999f;
        private bool hasExternalAimPoint;
        private Vector3 externalAimPoint;

        [System.Serializable]
        public class AmmoChangedEvent : UnityEvent<int, int, int> { }

        [Header("Events")]
        [Tooltip("Событие изменения боезапаса: currentMagazine, magazineSize, reserveAmmo")]
        [SerializeField] private AmmoChangedEvent onAmmoChanged = new AmmoChangedEvent();

        public Transform FirePoint => firePoint;
        public bool CanFire => Time.time - lastFireTime >= fireCooldown && !isFiring && !isReloading && currentAmmoInMagazine > 0;
        public float LastFireTime => lastFireTime;
        public float FireCooldown => fireCooldown;
        public int CurrentAmmoInMagazine => currentAmmoInMagazine;
        public int MagazineSize => magazineSize;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;
        public AmmoChangedEvent OnAmmoChanged => onAmmoChanged;
        public float BulletDamage => bulletDamage;

        private void Awake()
        {
            InitializeFirePoint();
            InitializeWeaponAnimator();
            InitializeBulletPool();
            tankMovement = GetComponentInParent<TankMovement>();
            tankTurret = GetComponentInParent<TankTurret>();
            currentAmmoInMagazine = Mathf.Max(0, magazineSize);
            NotifyAmmoChanged();
        }
        
        // Визуализация линии выстрела теперь в TankTurret через LineRenderer

        private void InitializeFirePoint()
        {
            if (firePoint == null)
            {
                Transform turret = FindChildRecursive(transform, "ZUBR_TURRET")
                    ?? FindChildRecursive(transform, "Turret");

                if (turret != null)
                    firePoint = FindChildRecursive(turret, "FirePoint");

                if (firePoint == null)
                    firePoint = FindChildRecursive(transform, "FirePoint");

                if (firePoint == null && turret != null)
                    firePoint = turret;
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            Transform direct = parent.Find(name);
            if (direct != null)
                return direct;

            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
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
        }

        private void InitializeWeaponAnimator()
        {
            if (weaponAnimator == null)
            {
                weaponAnimator = GetComponent<Animator>()
                    ?? GetComponentInParent<Animator>()
                    ?? GetComponentInChildren<Animator>();
            }

            if (weaponAudioSource == null)
            {
                weaponAudioSource = GetComponent<AudioSource>()
                    ?? GetComponentInParent<AudioSource>()
                    ?? GetComponentInChildren<AudioSource>();
            }

            fireAnimationTriggerHash = string.IsNullOrWhiteSpace(fireAnimationTrigger)
                ? 0
                : Animator.StringToHash(fireAnimationTrigger);

            ammoBeltFireAnimationTriggerHash = string.IsNullOrWhiteSpace(ammoBeltFireAnimationTrigger)
                ? 0
                : Animator.StringToHash(ammoBeltFireAnimationTrigger);
        }

        /// <summary>
        /// Выстрел с учетом разброса
        /// </summary>
        public void Fire(float stability)
        {
            if (isFiring)
                return;
            
            if (!CanFire)
                return;
            
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

            float spread = Mathf.Lerp(maxSpreadAngle, minSpreadAngle, stability);
            
            if (tankMovement != null)
            {
                float movementFactor = tankMovement.GetMovementFactor();
                spread += movementFactor * movementSpreadMultiplier;
            }
            
            spread = Mathf.Max(0f, spread);
            Vector3 targetPoint = GetAimPointFromMouse();
            
            // Вычисляем направление от FirePoint к точке прицела
            Vector3 directionToTarget = targetPoint - firePoint.position;
            
            // ВАЖНО: Проецируем направление на горизонтальную плоскость (XZ)
            // Убираем вертикальную компоненту (Y), чтобы пуля летела горизонтально
            directionToTarget.y = 0f;
            
            if (directionToTarget.magnitude < 0.001f)
            {
                directionToTarget = firePoint.forward;
                directionToTarget.y = 0f;
            }
            
            Vector3 direction = directionToTarget.normalized;

            if (spread > 0.001f)
            {
                float randomAngleY = Random.Range(-spread, spread);
                Quaternion spreadRotation = Quaternion.Euler(0f, randomAngleY, 0f);
                Quaternion aimRotation = Quaternion.LookRotation(direction);
                direction = aimRotation * spreadRotation * Vector3.forward;
            }
            
            direction = direction.normalized;
            
            BulletComponent bullet = bulletPool.Get();
            if (bullet == null)
            {
                Debug.LogError("[TankWeapon] Failed to get bullet from pool!");
                isFiring = false;
                return;
            }

            bullet.transform.SetParent(null);
            
            // Настраиваем позицию и ротацию
            bullet.transform.SetPositionAndRotation(
                firePoint.position,
                Quaternion.LookRotation(direction)
            );

            // Инициализируем пулю
            bullet.Initialize(this, impactVFX, bulletLifetime, bulletDamage, impactVfxScaleMultiplier);

            // Применяем физику
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = direction * bulletSpeed;
                bulletRb.angularVelocity = Vector3.zero; // Сброс вращения
            }
            
            PlayMuzzleVFX();
            PlayFireAnimation();
            PlayAmmoBeltAnimation();
            PlayFireSound();
            DrawDebugRay(firePoint.position, direction);

            lastFireTime = Time.time;
            currentAmmoInMagazine = Mathf.Max(0, currentAmmoInMagazine - 1);
            NotifyAmmoChanged();
            
            StartCoroutine(ResetFiringFlag());
        }
        
        private System.Collections.IEnumerator ResetFiringFlag()
        {
            yield return new WaitForEndOfFrame();
            isFiring = false;
        }
        
        /// <summary>
        /// Получить точку прицеливания от курсора мыши (для топдаун шутера)
        /// Проецирует курсор на плоскость земли
        /// </summary>
        private Vector3 GetAimPointFromMouse()
        {
            if (hasExternalAimPoint)
                return externalAimPoint;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return firePoint.position + firePoint.forward * 100f;
            }
            
            // В топдаун шутере проецируем курсор мыши на плоскость земли
            // Получаем высоту танка для плоскости
            Transform tankTransform = transform.root;
            float groundHeight = tankTransform != null ? tankTransform.position.y : 0f;
            
            // Создаем плоскость на уровне земли
            Plane groundPlane = new Plane(Vector3.up, groundHeight);
            
            Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (groundPlane.Raycast(mouseRay, out float distance))
            {
                return mouseRay.GetPoint(distance);
            }

            float maxDistance = 500f;
            Vector3 farPoint = mouseRay.origin + mouseRay.direction * maxDistance;
            farPoint.y = groundHeight;
            return farPoint;
        }
        
        
        /// <summary>
        /// Рисует debug ray для визуализации выстрела в топдаун шутере
        /// </summary>
        private void DrawDebugRay(Vector3 origin, Vector3 direction)
        {
            if (!showDebugRay)
                return;
            
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;
            
            // Точка куда смотрит курсор мыши (в мире)
            Vector3 targetPoint = GetAimPointFromMouse();
            
            // ═══════════════════════════════════════════════════════════════
            // ВИЗУАЛИЗАЦИЯ ВЫСТРЕЛА
            // ═══════════════════════════════════════════════════════════════
            
            // 1. 🔴 FirePoint (точка выстрела)
            Debug.DrawLine(origin + Vector3.up * 0.1f, origin - Vector3.up * 0.1f, Color.red, debugRayDuration);
            Debug.DrawLine(origin + Vector3.right * 0.1f, origin - Vector3.right * 0.1f, Color.red, debugRayDuration);
            
            // 2. 🔵 Raycast от камеры к курсору мыши
            Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
            Debug.DrawLine(mouseRay.origin, targetPoint, Color.cyan, debugRayDuration);
            
            // 3. 🔷 Направление ОТ FirePoint К курсору (БЕЗ разброса)
            Debug.DrawLine(origin, targetPoint, Color.blue, debugRayDuration * 1.5f);
            
            // 4. 🟡 Направление пули (С разбросом - реальный выстрел)
            Vector3 bulletEndPoint = origin + direction * Vector3.Distance(origin, targetPoint);
            Debug.DrawLine(origin, bulletEndPoint, Color.yellow, debugRayDuration);
            
            // 5. 🟢 Точка попадания
            Debug.DrawLine(targetPoint + Vector3.up * 0.2f, targetPoint - Vector3.up * 0.2f, Color.green, debugRayDuration);
            Debug.DrawLine(targetPoint + Vector3.right * 0.2f, targetPoint - Vector3.right * 0.2f, Color.green, debugRayDuration);
            
            // Дополнительно: маркер разброса
            float spreadDeviation = Vector3.Angle(targetPoint - origin, direction);
            Debug.DrawLine(bulletEndPoint, targetPoint, Color.magenta, debugRayDuration);
            
        }

        public bool TryReload()
        {
            if (isReloading)
                return false;
            if (currentAmmoInMagazine >= magazineSize)
                return false;
            if (reserveAmmo <= 0)
                return false;

            PlayReloadSound();
            reloadCoroutine = StartCoroutine(ReloadRoutine());
            return true;
        }

        private System.Collections.IEnumerator ReloadRoutine()
        {
            isReloading = true;
            NotifyAmmoChanged(); // Сразу обновляем UI состоянием "перезарядка"
            yield return new WaitForSeconds(reloadDuration);

            int neededAmmo = magazineSize - currentAmmoInMagazine;
            int ammoToLoad = Mathf.Min(neededAmmo, reserveAmmo);

            currentAmmoInMagazine += ammoToLoad;
            reserveAmmo -= ammoToLoad;

            isReloading = false;
            reloadCoroutine = null;
            NotifyAmmoChanged();
        }

        private void PlayMuzzleVFX()
        {
            if (muzzleVFX == null)
                return;

            // Используем muzzleVFXPoint если он назначен, иначе firePoint
            Transform effectPoint = muzzleVFXPoint != null ? muzzleVFXPoint : firePoint;
            
            if (effectPoint == null)
                return;

            // Не делаем дочерним объектом muzzle point: так эффект не зависит от скейла/иерархии ствола.
            GameObject vfx = Instantiate(muzzleVFX, effectPoint.position, effectPoint.rotation);

            // Явный контроль масштаба эффекта, чтобы исключить неожиданные изменения размера из иерархии.
            Vector3 baseScale = muzzleVFX.transform.localScale;
            Vector3 scaled = baseScale * Mathf.Max(0.01f, muzzleVfxScaleMultiplier);
            if (inheritMuzzlePointScale)
                scaled = Vector3.Scale(scaled, effectPoint.lossyScale);
            vfx.transform.localScale = scaled;

            ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);

            float maxLifetime = 0f;
            if (particleSystems.Length > 0)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    ParticleSystem ps = particleSystems[i];
                    if (ps == null)
                        continue;

                    ps.Play(true);

                    float duration = ps.main.duration;
                    if (ps.main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                        duration += ps.main.startLifetime.constant;
                    else
                        duration += ps.main.startLifetime.constantMax;

                    if (duration > maxLifetime)
                        maxLifetime = duration;
                }
            }

            Destroy(vfx, maxLifetime > 0f ? maxLifetime : 2f); // Fallback
            
        }

        private void PlayFireAnimation()
        {
            if (weaponAnimator == null || fireAnimationTriggerHash == 0)
                return;

            // Reset перед SetTrigger помогает корректно перезапускать анимацию при частой стрельбе.
            weaponAnimator.ResetTrigger(fireAnimationTriggerHash);
            weaponAnimator.SetTrigger(fireAnimationTriggerHash);
        }

        private void PlayAmmoBeltAnimation()
        {
            if (!playAmmoBeltAnimationOnFire || ammoBeltAnimator == null || ammoBeltFireAnimationTriggerHash == 0)
                return;

            // Та же логика перезапуска, что и у анимации ствола.
            ammoBeltAnimator.ResetTrigger(ammoBeltFireAnimationTriggerHash);
            ammoBeltAnimator.SetTrigger(ammoBeltFireAnimationTriggerHash);
        }

        public void SetAmmoBeltAnimationOnFire(bool enabled)
        {
            playAmmoBeltAnimationOnFire = enabled;
        }

        private void PlayFireSound()
        {
            if (weaponAudioSource == null || fireSound == null)
                return;

            weaponAudioSource.PlayOneShot(fireSound, weaponSfxVolume);
        }

        private void PlayReloadSound()
        {
            if (weaponAudioSource == null || reloadSound == null)
                return;

            weaponAudioSource.PlayOneShot(reloadSound, weaponSfxVolume);
        }

        public void TryPlayEmptyShotSound()
        {
            if (weaponAudioSource == null || emptyShotSound == null)
                return;

            if (Time.time - lastEmptyShotSoundTime < emptyShotSoundCooldown)
                return;

            lastEmptyShotSoundTime = Time.time;
            weaponAudioSource.PlayOneShot(emptyShotSound, weaponSfxVolume);
        }

        public void SetExternalAimPoint(Vector3 worldPoint)
        {
            hasExternalAimPoint = true;
            externalAimPoint = worldPoint;
        }

        public void ClearExternalAimPoint()
        {
            hasExternalAimPoint = false;
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

            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);
        }

        private void NotifyAmmoChanged()
        {
            onAmmoChanged?.Invoke(currentAmmoInMagazine, magazineSize, reserveAmmo);
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

