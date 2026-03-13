using UnityEngine;
using TankGame.Tank;
using TankGame.Session;

namespace TankGame.Game
{
    /// <summary>
    /// Инициализатор игры и локального спавна.
    /// Если будет включен сетевой режим, спавн должен делаться сетевым менеджером.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject tankPrefab;
        [SerializeField] private bool autoSpawnOnStart = true;

        [Header("Menu Tank Selection")]
        [Tooltip("Список танков, доступных из меню. Индекс берется из GameSessionSettings.SelectedTankIndex")]
        [SerializeField] private GameObject[] selectablePlayerTanks;
        [SerializeField] private bool useMenuSelectedTank = true;

        [Header("References")]
        [SerializeField] private TankController existingTank;

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            if (SpawnManager.Instance == null)
            {
                Debug.LogError("[GameInitializer] SpawnManager is missing in scene.");
                return;
            }

            if (TrySpawnTankFromMenuSelection())
                return;

            if (existingTank != null)
            {
                SpawnExistingTank(existingTank);
                return;
            }

            if (autoSpawnOnStart && tankPrefab != null)
            {
                SpawnTankPrefab(tankPrefab);
            }
            else if (autoSpawnOnStart && tankPrefab == null)
            {
                Debug.LogWarning("[GameInitializer] autoSpawnOnStart is true but tankPrefab is not assigned!");
            }
        }

        private bool TrySpawnTankFromMenuSelection()
        {
            if (!useMenuSelectedTank || selectablePlayerTanks == null || selectablePlayerTanks.Length == 0)
                return false;

            int selectedIndex = Mathf.Clamp(GameSessionSettings.SelectedTankIndex, 0, selectablePlayerTanks.Length - 1);
            GameObject selectedPrefab = selectablePlayerTanks[selectedIndex];
            if (selectedPrefab == null)
                return false;

            if (existingTank != null)
            {
                Destroy(existingTank.gameObject);
                existingTank = null;
            }

            SpawnTankPrefab(selectedPrefab);
            return true;
        }

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

        private void SpawnTankPrefab(GameObject prefab)
        {
            if (prefab == null || SpawnManager.Instance == null)
                return;

            GameObject tankObj = Instantiate(prefab);
            TankController tank = tankObj.GetComponent<TankController>();

            if (tank != null)
            {
                tank.SetIsLocalPlayer(true);
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
