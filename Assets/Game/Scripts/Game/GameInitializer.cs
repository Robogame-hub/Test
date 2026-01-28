using UnityEngine;
using TankGame.Tank;

namespace TankGame.Game
{
    /// <summary>
    /// Инициализатор игры - спавнит танки при старте
    /// ВАЖНО: Если Photon включен, танки должны спавниться через PhotonNetworkManager, а не здесь!
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Префаб танка для спавна (если нужно спавнить автоматически)")]
        [SerializeField] private GameObject tankPrefab;
        
        [Tooltip("Автоматически спавнить танк при старте в случайном спавн-поинте")]
        [SerializeField] private bool autoSpawnOnStart = true;
        
        [Header("References")]
        [Tooltip("Ссылка на существующий танк в сцене (если не нужно спавнить)")]
        [SerializeField] private TankController existingTank;

        private void Start()
        {
            InitializeGame();
        }
        
        /// <summary>
        /// Инициализирует игру и спавнит танки локально
        /// </summary>
        private void InitializeGame()
        {
            // Если есть существующий танк в сцене
            if (existingTank != null)
            {
                SpawnExistingTank(existingTank);
                return;
            }
            
            // Автоматический спавн префаба в случайном спавн-поинте
            if (autoSpawnOnStart && tankPrefab != null)
            {
                SpawnTankPrefab();
            }
            else if (autoSpawnOnStart && tankPrefab == null)
            {
                Debug.LogWarning("[GameInitializer] autoSpawnOnStart is true but tankPrefab is not assigned!");
            }
        }
        
        /// <summary>
        /// Спавнит существующий танк в сцене
        /// </summary>
        private void SpawnExistingTank(TankController tank)
        {
            if (tank == null || SpawnManager.Instance == null)
                return;
            
            SpawnPoint spawnPoint = SpawnManager.Instance.SpawnTank(tank);
            
            if (spawnPoint != null)
            {
                Debug.Log($"[GameInitializer] Existing tank {tank.name} spawned at point {spawnPoint.SpawnPointIndex}");
            }
        }
        
        /// <summary>
        /// Спавнит танк из префаба
        /// </summary>
        private void SpawnTankPrefab()
        {
            if (tankPrefab == null || SpawnManager.Instance == null)
                return;
            
            GameObject tankObj = Instantiate(tankPrefab);
            TankController tank = tankObj.GetComponent<TankController>();
            
            if (tank != null)
            {
                SpawnPoint spawnPoint = SpawnManager.Instance.SpawnTank(tank);
                
                if (spawnPoint != null)
                {
                    Debug.Log($"[GameInitializer] Tank {tank.name} spawned from prefab at point {spawnPoint.SpawnPointIndex}");
                }
            }
            else
            {
                Debug.LogError("[GameInitializer] Tank prefab doesn't have TankController component!");
                Destroy(tankObj);
            }
        }
    }
}

