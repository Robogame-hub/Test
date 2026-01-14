using UnityEngine;

namespace TankGame.Core
{
    /// <summary>
    /// Интерфейс для объектов, которые могут получать урон
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
        float GetHealth();
        bool IsAlive();
    }
}

