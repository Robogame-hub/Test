namespace TankGame.Core
{
    /// <summary>
    /// Интерфейс для объектов, которые могут использоваться в пуле
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
}

