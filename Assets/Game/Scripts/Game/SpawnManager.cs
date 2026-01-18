using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TankGame.Tank;

namespace TankGame.Game
{
    /// <summary>
    /// Менеджер спавн-поинтов для управления спавном и респавном танков
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("Массив всех спавн-поинтов в сцене")]
        [SerializeField] private SpawnPoint[] spawnPoints = new SpawnPoint[6];
        
        [Header("Settings")]
        [Tooltip("Префаб танка для спавна (если нужно спавнить автоматически)")]
        [SerializeField] private GameObject tankPrefab;
        
        [Tooltip("Автоматически найти все SpawnPoint в сцене")]
        [SerializeField] private bool autoFindSpawnPoints = true;
        
        private Dictionary<TankController, SpawnPoint> playerSpawnPoints = new Dictionary<TankController, SpawnPoint>();
        private static SpawnManager instance;
        
        public static SpawnManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SpawnManager>();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            if (autoFindSpawnPoints)
            {
                FindSpawnPoints();
            }
            
            ValidateSpawnPoints();
        }
        
        /// <summary>
        /// Находит все SpawnPoint в сцене
        /// </summary>
        private void FindSpawnPoints()
        {
            SpawnPoint[] found = FindObjectsOfType<SpawnPoint>();
            
            if (found.Length > 0)
            {
                // Сортируем по индексу
                spawnPoints = found.OrderBy(sp => sp.SpawnPointIndex).ToArray();
                Debug.Log($"[SpawnManager] Found {spawnPoints.Length} spawn points");
            }
            else
            {
                Debug.LogWarning("[SpawnManager] No spawn points found in scene!");
            }
        }
        
        /// <summary>
        /// Проверяет корректность спавн-поинтов
        /// </summary>
        private void ValidateSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[SpawnManager] No spawn points assigned! Assign manually or enable autoFindSpawnPoints.");
                return;
            }
            
            // Проверяем что все поинты назначены
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                {
                    Debug.LogWarning($"[SpawnManager] Spawn point at index {i} is null!");
                }
            }
        }
        
        /// <summary>
        /// Получить свободный спавн-поинт
        /// </summary>
        public SpawnPoint GetFreeSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;
            
            // Ищем первый свободный поинт
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null && !spawnPoint.IsOccupied)
                {
                    return spawnPoint;
                }
            }
            
            // Если все заняты, выбираем случайный
            var randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Debug.LogWarning($"[SpawnManager] All spawn points occupied! Using random point: {randomPoint.SpawnPointIndex}");
            return randomPoint;
        }
        
        /// <summary>
        /// Спавнит танк в свободном спавн-поинте
        /// </summary>
        public SpawnPoint SpawnTank(TankController tank)
        {
            if (tank == null)
            {
                Debug.LogError("[SpawnManager] Cannot spawn null tank!");
                return null;
            }
            
            SpawnPoint spawnPoint = GetFreeSpawnPoint();
            
            if (spawnPoint == null)
            {
                Debug.LogError("[SpawnManager] No available spawn points!");
                return null;
            }
            
            // Сохраняем привязку танка к спавн-поинту
            playerSpawnPoints[tank] = spawnPoint;
            
            // Спавним танк
            spawnPoint.SpawnTank(tank);
            
            return spawnPoint;
        }
        
        /// <summary>
        /// Получить спавн-поинт для конкретного танка
        /// </summary>
        public SpawnPoint GetTankSpawnPoint(TankController tank)
        {
            if (tank == null)
                return null;
            
            if (playerSpawnPoints.TryGetValue(tank, out SpawnPoint spawnPoint))
            {
                return spawnPoint;
            }
            
            return null;
        }
        
        /// <summary>
        /// Респавнит танк в его оригинальном спавн-поинте
        /// </summary>
        public void RespawnTank(TankController tank)
        {
            if (tank == null)
                return;
            
            SpawnPoint spawnPoint = GetTankSpawnPoint(tank);
            
            if (spawnPoint == null)
            {
                // Если нет привязки, используем свободный поинт
                spawnPoint = GetFreeSpawnPoint();
                if (spawnPoint != null)
                {
                    playerSpawnPoints[tank] = spawnPoint;
                }
            }
            
            if (spawnPoint != null)
            {
                spawnPoint.SpawnTank(tank);
                Debug.Log($"[SpawnManager] Tank {tank.name} respawned at point {spawnPoint.SpawnPointIndex}");
            }
        }
        
        /// <summary>
        /// Освобождает спавн-поинт
        /// </summary>
        public void FreeSpawnPoint(TankController tank)
        {
            if (tank == null)
                return;
            
            if (playerSpawnPoints.TryGetValue(tank, out SpawnPoint spawnPoint))
            {
                spawnPoint.SetOccupied(false);
                playerSpawnPoints.Remove(tank);
            }
        }
    }
}

