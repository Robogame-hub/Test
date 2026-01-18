using UnityEngine;
using TankGame.Tank;
using TankGame.Network;

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
        
        [Tooltip("Автоматически спавнить танки при старте (ТОЛЬКО для офлайн-режима!)")]
        [SerializeField] private bool autoSpawnOnStart = false;
        
        [Tooltip("Количество танков для спавна (если autoSpawnOnStart = true)")]
        [SerializeField] private int spawnCount = 1;
        
        [Header("References")]
        [Tooltip("Ссылка на существующий танк в сцене (если не нужно спавнить)")]
        [SerializeField] private TankController existingTank;

        private void Start()
        {
            InitializeGame();
        }
        
        /// <summary>
        /// Инициализирует игру и спавнит танки
        /// ВАЖНО: Не спавнит танки, если Photon подключен (PhotonNetworkManager должен это делать)
        /// </summary>
        private void InitializeGame()
        {
#if PHOTON_UNITY_NETWORKING
            // ПРОВЕРКА: Если Photon доступен, проверяем подключение
            bool photonAvailable = false;
            try
            {
                var photonNetworkType = System.Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp")
                                         ?? System.Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");
                if (photonNetworkType != null)
                {
                    bool isConnected = (bool)photonNetworkType.GetProperty("IsConnected").GetValue(null);
                    bool isInRoom = (bool)photonNetworkType.GetProperty("InRoom").GetValue(null);
                    bool isConnecting = PhotonNetworkManager.Instance != null && PhotonNetworkManager.Instance.enabled;
                    
                    if (isConnected || isInRoom || isConnecting)
                    {
                        photonAvailable = true;
                        Debug.Log("[GameInitializer] Photon is enabled! Skipping local spawn - PhotonNetworkManager will spawn tanks via Photon.");
                        // НЕ спавним танки локально, если Photon используется
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameInitializer] Could not check Photon status: {e.Message}");
            }
            
            // Если Photon не подключен - спавним локально (офлайн-режим)
            Debug.Log("[GameInitializer] Photon not connected/available - using local spawn (offline mode)");
#endif

            // Если есть существующий танк в сцене
            if (existingTank != null)
            {
                SpawnExistingTank(existingTank);
                return;
            }
            
            // Автоматический спавн префабов (только для офлайн-режима!)
            if (autoSpawnOnStart && tankPrefab != null)
            {
                Debug.LogWarning("[GameInitializer] autoSpawnOnStart is enabled - this is for OFFLINE mode only! For online multiplayer, disable this and use PhotonNetworkManager instead.");
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnTankPrefab();
                }
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

