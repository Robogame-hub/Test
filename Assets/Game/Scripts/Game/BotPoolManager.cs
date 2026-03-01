using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TankGame.Tank;

namespace TankGame.Game
{
    /// <summary>
    /// Пул ботов: настраиваемое количество, спавн-поинты.
    /// После смерти бот удаляется с карты и респавнится в случайном спавн-поинте.
    /// </summary>
    public class BotPoolManager : MonoBehaviour
    {
        [Header("Bot Settings")]
        [Tooltip("Префаб бота (танк с NavMeshTankAI)")]
        [SerializeField] private GameObject botPrefab;
        [Tooltip("Количество ботов на карте")]
        [SerializeField] private int botCount = 3;
        
        [Header("Spawn Points")]
        [Tooltip("Спавн-поинты для ботов. Пусто — используются все из SpawnManager")]
        [SerializeField] private SpawnPoint[] botSpawnPoints;
        [Tooltip("Использовать спавн-поинты SpawnManager если массив пуст")]
        [SerializeField] private bool useSpawnManagerPointsIfEmpty = true;
        
        [Header("Timing")]
        [Tooltip("Задержка перед первым спавном ботов (сек)")]
        [SerializeField] private float initialSpawnDelay = 0.5f;

        private readonly List<TankController> _spawnedBots = new List<TankController>(16);

        private void Start()
        {
            if (initialSpawnDelay > 0f)
            {
                Invoke(nameof(SpawnAllBots), initialSpawnDelay);
            }
            else
            {
                SpawnAllBots();
            }
        }

        /// <summary>
        /// Спавнит всех ботов в случайных спавн-поинтах.
        /// </summary>
        public void SpawnAllBots()
        {
            if (botPrefab == null)
            {
                Debug.LogWarning("[BotPoolManager] Bot prefab not assigned!");
                return;
            }

            SpawnPoint[] points = GetSpawnPoints();
            if (points == null || points.Length == 0)
            {
                Debug.LogError("[BotPoolManager] No spawn points! Assign botSpawnPoints or add SpawnPoints to scene.");
                return;
            }

            for (int i = 0; i < botCount; i++)
            {
                SpawnBot(points);
            }

            Debug.Log($"[BotPoolManager] Spawned {botCount} bots.");
        }

        /// <summary>
        /// Спавнит одного бота в случайном поинте.
        /// </summary>
        public TankController SpawnBot()
        {
            SpawnPoint[] points = GetSpawnPoints();
            return points != null && points.Length > 0 ? SpawnBot(points) : null;
        }

        private TankController SpawnBot(SpawnPoint[] points)
        {
            GameObject go = Instantiate(botPrefab);
            TankController tank = go.GetComponent<TankController>();
            if (tank == null)
            {
                tank = go.GetComponentInChildren<TankController>();
            }

            if (tank == null)
            {
                Debug.LogError("[BotPoolManager] Bot prefab has no TankController!");
                Destroy(go);
                return null;
            }

            tank.SetIsLocalPlayer(false);

            // Только свободные точки — бот не спавнится в занятую
            SpawnPoint[] freePoints = points.Where(sp => sp != null && !sp.IsOccupied).ToArray();
            if (freePoints.Length == 0)
            {
                Debug.LogWarning("[BotPoolManager] No free spawn points, cannot spawn bot.");
                Destroy(go);
                return null;
            }
            SpawnPoint point = freePoints[Random.Range(0, freePoints.Length)];
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.RegisterTankAtSpawnPoint(tank, point);
                point.SpawnTank(tank);
            }
            else
            {
                point.SpawnTank(tank);
            }

            _spawnedBots.Add(tank);
            return tank;
        }

        private SpawnPoint[] GetSpawnPoints()
        {
            if (botSpawnPoints != null && botSpawnPoints.Length > 0)
            {
                return botSpawnPoints;
            }
            if (useSpawnManagerPointsIfEmpty && SpawnManager.Instance != null)
            {
                return SpawnManager.Instance.GetAllSpawnPoints();
            }
            return null;
        }

        /// <summary>
        /// Количество заспавненных ботов.
        /// </summary>
        public int SpawnedBotCount => _spawnedBots.Count;
    }
}
