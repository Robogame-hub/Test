using UnityEngine;
using System.Collections.Generic;
using TankGame.Tank;

namespace TankGame.Examples
{
    /// <summary>
    /// Пример Game Manager для управления несколькими танками
    /// </summary>
    public class GameManagerExample : MonoBehaviour
    {
        [Header("Tanks")]
        [SerializeField] private List<TankController> allTanks = new List<TankController>();
        [SerializeField] private TankController playerTank;

        [Header("Game Settings")]
        [SerializeField] private int scoreToWin = 10;
        [SerializeField] private float respawnDelay = 3f;

        private Dictionary<TankController, int> scores = new Dictionary<TankController, int>();
        private bool gameEnded;

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Регистрируем все танки
            foreach (var tank in allTanks)
            {
                if (tank != null)
                {
                    RegisterTank(tank);
                }
            }

            Debug.Log($"Game started with {allTanks.Count} tanks!");
        }

        private void RegisterTank(TankController tank)
        {
            // Инициализируем счет
            scores[tank] = 0;

            // Подписываемся на события
            if (tank.Health != null)
            {
                tank.Health.OnDeath.AddListener(() => OnTankDestroyed(tank));
            }
        }

        private void OnTankDestroyed(TankController destroyedTank)
        {
            if (gameEnded)
                return;

            Debug.Log($"Tank {destroyedTank.name} destroyed!");

            // Находим танк, который убил (упрощенно - последний нанесший урон)
            // В реальности нужно отслеживать кто нанес урон через событие OnDamageTaken

            // Планируем респавн
            Invoke(nameof(RespawnTank), respawnDelay);
        }

        private void RespawnTank(/* TankController tank */)
        {
            // Восстановить танк
            // tank.Health.Respawn();
            // Телепортировать на точку спавна
            // tank.transform.position = spawnPoint.position;
            
            Debug.Log("Tank respawned!");
        }

        private void AddScore(TankController tank, int points)
        {
            if (gameEnded || !scores.ContainsKey(tank))
                return;

            scores[tank] += points;
            Debug.Log($"{tank.name} score: {scores[tank]}");

            // Проверка победы
            if (scores[tank] >= scoreToWin)
            {
                EndGame(tank);
            }
        }

        private void EndGame(TankController winner)
        {
            gameEnded = true;
            Debug.Log($"Game Over! Winner: {winner.name} with {scores[winner]} points!");

            // Остановить всех танков
            foreach (var tank in allTanks)
            {
                if (tank != null)
                    tank.enabled = false;
            }

            // Показать экран победы
            // ShowVictoryScreen(winner);
        }

        public void RestartGame()
        {
            gameEnded = false;

            // Сброс счета
            foreach (var tank in allTanks)
            {
                if (tank != null)
                {
                    scores[tank] = 0;
                    tank.Health.Respawn();
                    tank.enabled = true;
                }
            }

            Debug.Log("Game restarted!");
        }

        // UI методы
        public int GetPlayerScore()
        {
            return playerTank != null && scores.ContainsKey(playerTank) 
                ? scores[playerTank] 
                : 0;
        }

        public Dictionary<TankController, int> GetAllScores()
        {
            return new Dictionary<TankController, int>(scores);
        }

        private void OnDestroy()
        {
            // Отписываемся от всех событий
            foreach (var tank in allTanks)
            {
                if (tank?.Health != null)
                {
                    // tank.Health.OnDeath.RemoveListener(...);
                }
            }
        }
    }
}

