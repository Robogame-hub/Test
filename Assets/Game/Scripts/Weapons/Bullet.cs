using UnityEngine;
using TankGame.Core;
using TankGame.Tank.Components;
using TankGame.Audio;

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

        [Header("Impact Audio")]
        [Tooltip("Звук точного попадания (например, успешное пробитие цели)")]
        [SerializeField] private AudioClip preciseHitSound;
        [Tooltip("Звук рикошета (промах по цели или непробитие)")]
        [SerializeField] private AudioClip ricochetSound;
        [Tooltip("Громкость звука попадания/рикошета")]
        [SerializeField] [Range(0f, 1f)] private float impactSfxVolume = 1f;
        [Tooltip("Смещение эффекта от поверхности наружу, чтобы не клипался внутри коллайдера")]
        [SerializeField] private float impactSurfaceOffset = 0.02f;

        private Rigidbody rb;
        private Collider bulletCollider;
        private TankWeapon ownerWeapon;
        private GameObject impactEffect;
        private float impactVfxScale = 1f;
        private float lifetime;
        private float ricochetChance;
        private float ricochetChancePerSpread;
        private float lastSpread;
        private GameObject ricochetEffect;
        private AudioClip ricochetBounceSound;
        private float ricochetAngleMin;
        private float ricochetAngleMax;
        private bool hasRicocheted;
        private float runtimeDamage;
        private float spawnTime;
        private bool isActive;
        private bool hasImpacted;
        private BulletTracer tracer;
        private Vector3 previousPosition;

        public float Damage => runtimeDamage > 0f ? runtimeDamage : damage;
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

            // Bullet should not push rigidbodies on impact.
            bulletCollider.isTrigger = true;
            
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
        public void Initialize(TankWeapon weapon, GameObject impact, float life, float weaponDamage, float impactScale = 1f,
            float ricochetCh = 0f, float ricochetChPerSpread = 0f, float spread = 0f, GameObject ricochetVfx = null,
            AudioClip ricochetSnd = null, float ricochetAngMin = 15f, float ricochetAngMax = 80f)
        {
            ownerWeapon = weapon;
            impactEffect = impact;
            lifetime = life;
            runtimeDamage = Mathf.Max(0f, weaponDamage);
            impactVfxScale = Mathf.Max(0.01f, impactScale);
            ricochetChance = ricochetCh;
            ricochetChancePerSpread = ricochetChPerSpread;
            lastSpread = spread;
            ricochetEffect = ricochetVfx;
            ricochetBounceSound = ricochetSnd;
            ricochetAngleMin = ricochetAngMin;
            ricochetAngleMax = ricochetAngMax;
            hasRicocheted = false;
            spawnTime = Time.time;
            isActive = true;
            hasImpacted = false;
            previousPosition = transform.position;
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

        private void FixedUpdate()
        {
            if (!isActive)
                return;

            // Сохраняем позицию до физики — для точного определения точки входа при OnTriggerEnter.
            previousPosition = transform.position;
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

            if (hasImpacted)
                return;
            hasImpacted = true;

            // ИСПРАВЛЕНО: Проверяем что есть контакты перед доступом
            if (collision.contacts.Length == 0)
            {
                Debug.LogWarning($"[Bullet] No contact points in collision with {collision.gameObject.name}");
                HandleImpact(transform.position, -rb.linearVelocity.normalized, collision.gameObject);
                return;
            }

            // Получаем точку и нормаль столкновения
            ContactPoint contact = collision.contacts[0];
            Vector3 vel = rb != null ? rb.linearVelocity.normalized : transform.forward;
            Vector3 n = contact.normal;
            if (Vector3.Dot(n, vel) > 0f)
                n = -vel;
            HandleImpact(contact.point, n, collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || other == null)
                return;
            if (hasImpacted)
                return;

            // Ignore owner tank colliders.
            TankWeapon ownerCheck = other.GetComponentInParent<TankWeapon>();
            if (ownerCheck != null && ownerCheck == ownerWeapon)
                return;

            hasImpacted = true;

            ResolveTriggerImpact(other, out Vector3 hitPoint, out Vector3 hitNormal);

            HandleImpact(hitPoint, hitNormal, other.gameObject);
        }

        private void HandleImpact(Vector3 hitPoint, Vector3 hitNormal, GameObject hitObject)
        {
            if (hasRicocheted)
            {
                DoNormalImpact(hitPoint, hitNormal, hitObject);
                return;
            }

            float chance = ricochetChance + lastSpread * ricochetChancePerSpread;
            if (ricochetEffect != null && ricochetBounceSound != null && Random.value < Mathf.Clamp01(chance))
            {
                DoRicochet(hitPoint, hitNormal);
                return;
            }

            DoNormalImpact(hitPoint, hitNormal, hitObject);
        }

        private void DoRicochet(Vector3 hitPoint, Vector3 hitNormal)
        {
            PlayRicochetEffect(hitPoint, hitNormal);
            PlayRicochetSound(hitPoint);

            Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero;
            if (vel.sqrMagnitude < 0.0001f)
            {
                ReturnToPool();
                return;
            }

            Vector3 dir = vel.normalized;
            Vector3 reflected = Vector3.Reflect(dir, hitNormal.normalized);
            float angleY = Random.Range(ricochetAngleMin, ricochetAngleMax) * (Random.value > 0.5f ? 1f : -1f);
            Vector3 newDir = Quaternion.Euler(0f, angleY, 0f) * reflected;
            newDir.y = 0f;
            if (newDir.sqrMagnitude < 0.0001f)
                newDir = reflected;

            newDir.Normalize();
            float speed = vel.magnitude;

            transform.position = hitPoint + newDir * 0.15f;
            previousPosition = transform.position;
            if (rb != null)
                rb.linearVelocity = newDir * speed;

            hasRicocheted = true;
            hasImpacted = false;
        }

        private void DoNormalImpact(Vector3 hitPoint, Vector3 hitNormal, GameObject hitObject)
        {
            bool wasPreciseHit = false;

            IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                float finalDamage = CalculateDamageWithPenetration(out bool penetrated);
                damageable.TakeDamage(finalDamage, hitPoint, hitNormal);
                wasPreciseHit = penetrated;
            }

            PlayImpactEffect(hitPoint, hitNormal);
            PlayImpactSound(hitPoint, wasPreciseHit);

            ReturnToPool();
        }

        private void PlayRicochetEffect(Vector3 position, Vector3 normal)
        {
            if (ricochetEffect == null)
                return;

            Vector3 safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 spawnPosition = position + safeNormal * Mathf.Max(0f, impactSurfaceOffset);
            Quaternion rotation = Quaternion.LookRotation(safeNormal);
            GameObject vfx = Instantiate(ricochetEffect, spawnPosition, rotation);

            Vector3 prefabScale = ricochetEffect.transform.localScale;
            float minComponent = Mathf.Min(prefabScale.x, prefabScale.y, prefabScale.z);
            Vector3 baseScale = minComponent < 0.01f ? Vector3.one : prefabScale;
            vfx.transform.localScale = baseScale * impactVfxScale;

            ParticleSystem[] ps = vfx.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = 0f;
            foreach (ParticleSystem p in ps)
            {
                if (p != null) { p.Play(true); maxLifetime = Mathf.Max(maxLifetime, p.main.duration + (p.main.startLifetime.mode == ParticleSystemCurveMode.Constant ? p.main.startLifetime.constant : p.main.startLifetime.constantMax)); }
            }
            Destroy(vfx, maxLifetime > 0f ? maxLifetime : 2f);
        }

        private void PlayRicochetSound(Vector3 hitPoint)
        {
            if (ricochetBounceSound == null)
                return;
            AudioSource.PlayClipAtPoint(ricochetBounceSound, hitPoint, SfxVolumeUtility.GetScaled(impactSfxVolume));
        }
        
        /// <summary>
        /// Вычисляет финальный урон с учетом шанса пробития
        /// </summary>
        private float CalculateDamageWithPenetration(out bool penetrated)
        {
            // Проверяем шанс пробития (0-100%)
            float randomValue = Random.Range(0f, 100f);
            
            if (randomValue <= penetrationChance)
            {
                // Пробитие успешно - полный урон
                float fullDamage = Damage;
                penetrated = true;
                Debug.Log($"[Bullet] Penetration SUCCESS! Full damage: {fullDamage}");
                return fullDamage;
            }
            else
            {
                // Пробитие не удалось - урон уменьшается
                float reducedDamage = Damage * nonPenetrationDamageMultiplier;
                penetrated = false;
                Debug.Log($"[Bullet] Penetration FAILED! Reduced damage: {reducedDamage} (from {Damage}, multiplier: {nonPenetrationDamageMultiplier})");
                return reducedDamage;
            }
        }

        private void PlayImpactSound(Vector3 hitPoint, bool wasPreciseHit)
        {
            AudioClip clipToPlay = wasPreciseHit ? preciseHitSound : ricochetSound;
            if (clipToPlay == null)
                return;

            AudioSource.PlayClipAtPoint(clipToPlay, hitPoint, SfxVolumeUtility.GetScaled(impactSfxVolume));
        }

        private void PlayImpactEffect(Vector3 position, Vector3 normal)
        {
            if (impactEffect == null)
                return;

            Vector3 safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 spawnPosition = position + safeNormal * Mathf.Max(0f, impactSurfaceOffset);

            // Создаем эффект с правильной ориентацией
            Quaternion rotation = Quaternion.LookRotation(safeNormal);
            GameObject vfx = Instantiate(impactEffect, spawnPosition, rotation);

            // Масштаб эффекта (задаётся оружием: пушка — крупнее, пулемёт — меньше)
            vfx.transform.localScale *= impactVfxScale;

            // Запуск и автоуничтожение
            ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = 0f;
            if (particleSystems.Length > 0)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    ParticleSystem ps = particleSystems[i];
                    if (ps != null)
                    {
                        ps.Play(true);
                        float d = ps.main.duration;
                        if (ps.main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                            d += ps.main.startLifetime.constant;
                        else
                            d += ps.main.startLifetime.constantMax;
                        if (d > maxLifetime) maxLifetime = d;
                    }
                }
            }
            Destroy(vfx, maxLifetime > 0f ? maxLifetime : 2f);
        }

        private void ReturnToPool()
        {
            if (!isActive)
                return;

            isActive = false;
            runtimeDamage = 0f;

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
            hasImpacted = false;
            hasRicocheted = false;
            previousPosition = transform.position;

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

        private void ResolveTriggerImpact(Collider other, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            Vector3 currentPosition = transform.position;
            Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
            Vector3 velocityDir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward;

            // Старт луча — гарантированно перед поверхностью (расширяем назад по траектории).
            Vector3 rayOrigin = previousPosition - velocityDir * 5f;
            Vector3 rayEnd = currentPosition + velocityDir * 2f;
            Vector3 rayDir = (rayEnd - rayOrigin).normalized;
            float rayLen = Vector3.Distance(rayOrigin, rayEnd);

            if (rayLen > 0.001f)
            {
                // Physics.RaycastAll — все пересечения по пути, первый hit = точка входа (передняя сторона).
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, rayLen, -1, QueryTriggerInteraction.Collide);
                float bestDist = float.MaxValue;
                RaycastHit? bestHit = null;

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == bulletCollider || hits[i].collider.transform.IsChildOf(transform))
                        continue;
                    if (hits[i].collider != other)
                        continue;
                    if (hits[i].distance < bestDist)
                    {
                        bestDist = hits[i].distance;
                        bestHit = hits[i];
                    }
                }

                if (bestHit.HasValue)
                {
                    RaycastHit h = bestHit.Value;
                    hitPoint = h.point;
                    hitNormal = h.normal;
                    if (Vector3.Dot(hitNormal, velocityDir) > 0f)
                        hitNormal = -velocityDir;
                    return;
                }

                // Fallback: Collider.Raycast по тому же лучу.
                Ray ray = new Ray(rayOrigin, rayDir);
                if (other.Raycast(ray, out RaycastHit hit, rayLen))
                {
                    hitPoint = hit.point;
                    hitNormal = Vector3.Dot(hit.normal, velocityDir) > 0f ? -velocityDir : hit.normal;
                    return;
                }
            }

            // Если пуля оказалась внутри геометрии, пытаемся вычислить минимальный вектор выталкивания.
            if (bulletCollider != null &&
                Physics.ComputePenetration(
                    bulletCollider, currentPosition, transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 separationDirection, out float separationDistance))
            {
                Vector3 separationNormal = separationDirection.sqrMagnitude > 0.0001f
                    ? separationDirection.normalized
                    : -velocityDir;

                // Вектор выталкивания должен смотреть против движения пули (передняя сторона).
                if (Vector3.Dot(separationNormal, velocityDir) > 0f)
                    separationNormal = -separationNormal;

                hitNormal = separationNormal;
                hitPoint = currentPosition + separationNormal * (separationDistance + impactSurfaceOffset);
                return;
            }

            // Fallback: берем ближайшую точку и нормаль против скорости.
            Vector3 fallbackPoint = other.ClosestPoint(currentPosition);
            Vector3 fallbackNormal = velocity.sqrMagnitude > 0.0001f ? -velocity.normalized : -transform.forward;

            // Если ClosestPoint вернул позицию внутри/в центре, немного выносим точку назад по траектории.
            if ((fallbackPoint - currentPosition).sqrMagnitude < 0.0001f)
                fallbackPoint = currentPosition - fallbackNormal * 0.05f;

            hitPoint = fallbackPoint;
            hitNormal = fallbackNormal;
        }

        #endregion
    }
}
