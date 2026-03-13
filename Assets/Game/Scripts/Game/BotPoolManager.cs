using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TankGame.Tank;
using TankGame.Session;

namespace TankGame.Game
{
    /// <summary>
    /// Пул ботов: настраиваемое количество, спавн-поинты.
    /// Каждый бот закрепляется за своим спавн-поинтом; после смерти респавнится в том же поинте.
    /// </summary>
    public class BotPoolManager : MonoBehaviour
    {
        [Header("Bot Settings")]
        [SerializeField] private GameObject botPrefab;
        [SerializeField] private int botCount = 3;

        [Header("Session Overrides")]
        [Tooltip("Если запуск из лобби в режиме одиночной игры, использовать количество ботов из сессии")]
        [SerializeField] private bool useSessionSoloBotCount = true;
        [Tooltip("Не спавнить ботов, если матч запущен как лобби-комната")]
        [SerializeField] private bool disableBotsForLobbyMode = true;

        [Header("Spawn Points")]
        [SerializeField] private SpawnPoint[] botSpawnPoints;
        [SerializeField] private bool useSpawnManagerPointsIfEmpty = true;
        [SerializeField] private float minSpawnSeparation = 4f;

        [Header("Timing")]
        [SerializeField] private float initialSpawnDelay = 0.5f;

        private readonly List<TankController> _spawnedBots = new List<TankController>(16);

        private void Start()
        {
            if (ShouldSkipBotSpawn())
                return;

            ResolveBotCountFromSession();

            if (initialSpawnDelay > 0f)
            {
                Invoke(nameof(SpawnAllBots), initialSpawnDelay);
            }
            else
            {
                SpawnAllBots();
            }
        }

        private bool ShouldSkipBotSpawn()
        {
            if (!disableBotsForLobbyMode)
                return false;

            return GameSessionSettings.StartMode == MatchStartMode.Lobby;
        }

        private void ResolveBotCountFromSession()
        {
            if (useSessionSoloBotCount)
            {
                if (GameSessionSettings.StartMode == MatchStartMode.SoloWithBots)
                {
                    botCount = GameSessionSettings.SoloBotCount;
                }
                else if (GameSessionSettings.StartMode == MatchStartMode.Sandbox)
                {
                    botCount = GameSessionSettings.SandboxBotCount;
                }
            }

            // 1 local player + bots must never exceed max player slots.
            int maxBotsAllowed = Mathf.Max(0, GameSessionSettings.MaxPlayers - 1);
            botCount = Mathf.Clamp(botCount, 0, maxBotsAllowed);
        }

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

            var uniquePoints = points.Where(sp => sp != null).Distinct().ToArray();
            var freePoints = uniquePoints.Where(sp => !sp.IsOccupied).ToArray();

            if (freePoints.Length == 0)
            {
                Debug.LogWarning("[BotPoolManager] No free spawn points!");
                return;
            }

            for (int i = freePoints.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (freePoints[i], freePoints[j]) = (freePoints[j], freePoints[i]);
            }

            int spawned = 0;
            for (int i = 0; i < botCount && i < freePoints.Length; i++)
            {
                if (SpawnBotAtPoint(freePoints[i]) != null)
                    spawned++;
            }

            Debug.Log($"[BotPoolManager] Spawned {spawned} bots at different spawn points.");
        }

        public TankController SpawnBot()
        {
            SpawnPoint[] points = GetSpawnPoints();
            if (points == null || points.Length == 0)
                return null;

            var available = points.Where(sp => sp != null && !sp.IsOccupied).ToArray();
            if (available.Length == 0)
            {
                Debug.LogWarning("[BotPoolManager] No free spawn points, cannot spawn bot.");
                return null;
            }

            SpawnPoint point = available[Random.Range(0, available.Length)];
            return SpawnBotAtPoint(point);
        }

        private TankController SpawnBotAtPoint(SpawnPoint point)
        {
            if (point == null || point.IsOccupied)
            {
                Debug.LogWarning("[BotPoolManager] Spawn point is null or occupied.");
                return null;
            }

            point.SetOccupied(true, null);

            GameObject go = Instantiate(botPrefab);
            TankController tank = go.GetComponent<TankController>();
            if (tank == null)
                tank = go.GetComponentInChildren<TankController>();

            if (tank == null)
            {
                Debug.LogError("[BotPoolManager] Bot prefab has no TankController!");
                point.SetOccupied(false);
                Destroy(go);
                return null;
            }

            tank.SetIsLocalPlayer(false);

            if (SpawnManager.Instance != null)
                SpawnManager.Instance.RegisterTankAtSpawnPoint(tank, point);
            point.SpawnTank(tank);

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

        public int SpawnedBotCount => _spawnedBots.Count;
    }
}
