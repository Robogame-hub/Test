using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private GameObject impactEffect;
    
    private Rigidbody rb;
    private float spawnTime;
    private TankController tankController;
    
    public void SetImpactEffect(GameObject effect)
    {
        impactEffect = effect;
    }
    
    private void OnEnable()
    {
        spawnTime = Time.time;
        
        // Получаем или добавляем Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Настройка физики пули
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Убеждаемся, что есть коллайдер и он не является триггером
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = false;
        }
        else
        {
            // Убеждаемся, что коллайдер не является триггером для физических столкновений
            bulletCollider.isTrigger = false;
        }
        
        // Ищем TankController для возврата в пул
        if (tankController == null)
        {
            tankController = FindFirstObjectByType<TankController>();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Игнорируем столкновение с самим танком (родительским объектом)
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Tank"))
        {
            return;
        }
        
        // Получаем точку контакта и нормаль поверхности
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;
        
        HandleHit(hitPoint, hitNormal);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Обработка триггеров (если коллайдер настроен как триггер)
        if (other.CompareTag("Player") || other.CompareTag("Tank"))
        {
            return;
        }
        
        // Для триггеров нормаль не доступна, используем направление движения пули
        HandleHit(transform.position, null);
    }
    
    private void HandleHit(Vector3 hitPoint, Vector3? hitNormal = null)
    {
        float lifeTime = Time.time - spawnTime;
        
        // Создание VFX эффекта попадания
        PlayImpactVFX(hitPoint, hitNormal);
        
        // Здесь можно добавить логику нанесения урона
        // Например:
        // IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        // if (damageable != null)
        // {
        //     damageable.TakeDamage(damage);
        // }
        
        Debug.Log($"Пуля уничтожена при столкновении. Время жизни: {lifeTime:F2}с, Позиция: {hitPoint}");
        
        // Возвращаем пулю в пул вместо уничтожения
        ReturnToPool();
    }
    
    private void PlayImpactVFX(Vector3 hitPoint, Vector3? hitNormal)
    {
        if (impactEffect == null) return;
        
        // Определяем ориентацию VFX
        Quaternion rotation = Quaternion.identity;
        if (hitNormal.HasValue)
        {
            // Ориентируем VFX по нормали поверхности
            rotation = Quaternion.LookRotation(hitNormal.Value);
        }
        else
        {
            // Если нормаль не указана, используем обратное направление движения пули
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                rotation = Quaternion.LookRotation(-rb.linearVelocity.normalized);
            }
        }
        
        // Создаем VFX эффект
        GameObject vfxInstance = Instantiate(impactEffect, hitPoint, rotation);
        
        // Если это ParticleSystem, проигрываем его
        ParticleSystem particles = vfxInstance.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
            
            // Автоматически уничтожаем объект после завершения эффекта
            if (!particles.main.loop)
            {
                float duration = particles.main.duration + particles.main.startLifetime.constantMax;
                Destroy(vfxInstance, duration);
            }
        }
        else
        {
            // Если это не ParticleSystem, проверяем наличие других компонентов VFX
            // Например, Visual Effect (VFX Graph)
            var visualEffect = vfxInstance.GetComponent<UnityEngine.VFX.VisualEffect>();
            if (visualEffect != null)
            {
                visualEffect.Play();
            }
        }
        
        Debug.Log($"VFX эффект создан в позиции: {hitPoint}");
    }
    
    private void ReturnToPool()
    {
        if (tankController != null)
        {
            tankController.ReturnBulletToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    public float GetLifetime()
    {
        return Time.time - spawnTime;
    }
}

