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
        [Tooltip("Индекс спавн-поинта для игрока (респавн всегда здесь). -1 = случайный")]
        [SerializeField] private int playerSpawnPointIndex = 0;
        
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
        /// Все спавн-поинты (для BotPoolManager и др.)
        /// </summary>
        public SpawnPoint[] GetAllSpawnPoints()
        {
            return spawnPoints;
        }
        
        /// <summary>
        /// Получить спавн-поинт игрока (для респавна)
        /// </summary>
        public SpawnPoint GetPlayerSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;
            if (playerSpawnPointIndex < 0)
                return GetRandomSpawnPoint();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null && spawnPoints[i].SpawnPointIndex == playerSpawnPointIndex)
                    return spawnPoints[i];
            }
            return spawnPoints[0];
        }
        
        /// <summary>
        /// Получить случайный спавн-поинт (без проверки занятости)
        /// </summary>
        public SpawnPoint GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;
            var availablePoints = spawnPoints.Where(sp => sp != null).ToArray();
            if (availablePoints.Length == 0)
                return null;
            return availablePoints[Random.Range(0, availablePoints.Length)];
        }
        
        /// <summary>
        /// Получить случайный свободный спавн-поинт (бот не может спавниться в занятую точку).
        /// </summary>
        public SpawnPoint GetRandomFreeSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;
            var freePoints = spawnPoints.Where(sp => sp != null && !sp.IsOccupied).ToArray();
            if (freePoints.Length == 0)
                return null;
            return freePoints[Random.Range(0, freePoints.Length)];
        }
        
        /// <summary>
        /// Получить свободный спавн-поинт (для обратной совместимости)
        /// </summary>
        public SpawnPoint GetFreeSpawnPoint()
        {
            return GetRandomFreeSpawnPoint() ?? GetRandomSpawnPoint();
        }
        
        /// <summary>
        /// Спавнит танк: игрок — в указанном поинте, бот — в случайном.
        /// </summary>
        public SpawnPoint SpawnTank(TankController tank)
        {
            if (tank == null)
            {
                Debug.LogError("[SpawnManager] Cannot spawn null tank!");
                return null;
            }
            
            SpawnPoint spawnPoint = tank.IsLocalPlayer ? GetPlayerSpawnPoint() : GetRandomFreeSpawnPoint();
            if (spawnPoint == null)
            {
                spawnPoint = GetRandomFreeSpawnPoint() ?? GetRandomSpawnPoint();
            }
            
            if (spawnPoint == null)
            {
                Debug.LogError("[SpawnManager] No available spawn points!");
                return null;
            }
            
            playerSpawnPoints[tank] = spawnPoint;
            spawnPoint.SpawnTank(tank);
            return spawnPoint;
        }
        
        /// <summary>
        /// Регистрирует танк в спавн-поинте без повторного спавна
        /// </summary>
        public void RegisterTankAtSpawnPoint(TankController tank, SpawnPoint spawnPoint)
        {
            if (tank == null || spawnPoint == null)
                return;
            
            // Сохраняем привязку танка к спавн-поинту
            playerSpawnPoints[tank] = spawnPoint;
            
            // Помечаем спавн-поинт как занятый
            spawnPoint.SetOccupied(true, tank);
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
        /// Респавнит танк: игрок — в указанном спавн-поинте, бот — в случайном.
        /// </summary>
        public void RespawnTank(TankController tank)
        {
            if (tank == null)
                return;
            
            SpawnPoint spawnPoint;
            if (tank.IsLocalPlayer)
            {
                spawnPoint = GetPlayerSpawnPoint();
            }
            else
            {
                spawnPoint = GetRandomFreeSpawnPoint();
            }
            
            if (spawnPoint == null)
            {
                spawnPoint = GetRandomFreeSpawnPoint() ?? GetRandomSpawnPoint();
            }
            
            if (spawnPoint != null)
            {
                playerSpawnPoints[tank] = spawnPoint;
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

