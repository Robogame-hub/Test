using UnityEngine;
using TankGame.Tank;
using TankGame.Game;

#if PHOTON_PUN_2
using Photon.Pun;
using Photon.Realtime;
#endif

namespace TankGame.Network
{
    /// <summary>
    /// Менеджер сети для Photon PUN 2
    /// Управляет подключением, комнатами и спавном танков
    /// </summary>
    public class PhotonNetworkManager : MonoBehaviour
#if PHOTON_PUN_2
        , IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
#endif
    {
        [Header("Network Settings")]
        [Tooltip("Версия игры (для разделения игроков разных версий)")]
        [SerializeField] private string gameVersion = "1.0";
        
        [Tooltip("Максимальное количество игроков в комнате")]
        [SerializeField] private byte maxPlayersPerRoom = 6;
        
        [Tooltip("Имя комнаты (если пусто, создается случайная)")]
        [SerializeField] private string roomName = "";

        [Header("Tank Spawn")]
        [Tooltip("Префаб танка для спавна (должен иметь TankNetworkPhoton и PhotonView)")]
        [SerializeField] private GameObject tankPrefab;
        
        [Tooltip("Использовать SpawnManager для определения спавн-поинтов")]
        [SerializeField] private bool useSpawnManager = true;

        private static PhotonNetworkManager instance;
        public static PhotonNetworkManager Instance => instance;

#if PHOTON_PUN_2
        private bool isConnecting = false;
        private int playerNumber = 0;
#endif

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

#if PHOTON_PUN_2
            PhotonNetwork.AddCallbackTarget(this);
#else
            Debug.LogWarning("[PhotonNetworkManager] Photon PUN 2 not installed! This component requires Photon PUN 2 to work.");
#endif
        }

        private void Start()
        {
#if PHOTON_PUN_2
            // Подключаемся автоматически при старте
            if (!PhotonNetwork.IsConnected)
            {
                Connect();
            }
#endif
        }

        private void OnDestroy()
        {
#if PHOTON_PUN_2
            PhotonNetwork.RemoveCallbackTarget(this);
#endif
        }

        /// <summary>
        /// Подключиться к Photon
        /// </summary>
        public void Connect()
        {
#if PHOTON_PUN_2
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonNetworkManager] Already connected to Photon!");
                return;
            }

            isConnecting = true;
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[PhotonNetworkManager] Connecting to Photon...");
#else
            Debug.LogError("[PhotonNetworkManager] Photon PUN 2 not installed! Cannot connect.");
#endif
        }

        /// <summary>
        /// Подключиться к комнате (или создать, если не существует)
        /// </summary>
        public void JoinOrCreateRoom()
        {
#if PHOTON_PUN_2
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("[PhotonNetworkManager] Not connected to Photon! Connecting first...");
                Connect();
                return;
            }

            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "Room_" + Random.Range(1000, 9999);
            }

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
            Debug.Log($"[PhotonNetworkManager] Joining or creating room: {roomName}");
#else
            Debug.LogError("[PhotonNetworkManager] Photon PUN 2 not installed!");
#endif
        }

        /// <summary>
        /// Отключиться от Photon
        /// </summary>
        public void Disconnect()
        {
#if PHOTON_PUN_2
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                Debug.Log("[PhotonNetworkManager] Disconnected from Photon");
            }
#else
            Debug.LogError("[PhotonNetworkManager] Photon PUN 2 not installed!");
#endif
        }

#if PHOTON_PUN_2
        /// <summary>
        /// Спавнит танк для локального игрока
        /// </summary>
        private void SpawnPlayerTank()
        {
            if (tankPrefab == null)
            {
                Debug.LogError("[PhotonNetworkManager] Tank prefab not assigned! Cannot spawn tank.");
                return;
            }

            // Определяем позицию спавна
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (useSpawnManager && SpawnManager.Instance != null)
            {
                SpawnPoint spawnPoint = SpawnManager.Instance.GetFreeSpawnPoint();
                if (spawnPoint != null)
                {
                    spawnPosition = spawnPoint.Position;
                    spawnRotation = spawnPoint.Rotation;
                }
            }

            // Спавним танк через Photon
            GameObject tankInstance = PhotonNetwork.Instantiate(
                GetPrefabResourcePath(tankPrefab),
                spawnPosition,
                spawnRotation
            );

            if (tankInstance != null)
            {
                TankController tankController = tankInstance.GetComponent<TankController>();
                TankNetworkPhoton networkPhoton = tankInstance.GetComponent<TankNetworkPhoton>();
                
                if (tankController != null && networkPhoton != null)
                {
                    // Устанавливаем isLocalPlayer через сетевой компонент
                    networkPhoton.SetIsLocalPlayer(true);
                    
                    // Если используем SpawnManager, привязываем танк к спавн-поинту
                    if (useSpawnManager && SpawnManager.Instance != null)
                    {
                        SpawnManager.Instance.SpawnTank(tankController);
                    }
                    
                    Debug.Log($"[PhotonNetworkManager] Player tank spawned: {tankInstance.name}");
                }
            }
        }

        /// <summary>
        /// Получает путь к префабу в Resources для Photon.Instantiate
        /// </summary>
        private string GetPrefabResourcePath(GameObject prefab)
        {
            // Photon требует путь относительно папки Resources
            // Если префаб не в Resources, нужно переместить или использовать имя без расширения
            string path = prefab.name;
            
            // Если префаб в Resources, используем его путь
            // Иначе используем только имя (префаб должен быть в Resources)
            if (!path.Contains("Resources"))
            {
                Debug.LogWarning($"[PhotonNetworkManager] Tank prefab should be in Resources folder for Photon.Using name: {path}");
            }
            
            return path;
        }

        #region Photon Callbacks

        public void OnConnectedToMaster()
        {
            Debug.Log("[PhotonNetworkManager] Connected to Photon Master Server");
            isConnecting = false;
            
            // Автоматически подключаемся к комнате
            JoinOrCreateRoom();
        }

        public void OnJoinedRoom()
        {
            Debug.Log($"[PhotonNetworkManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");
            playerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Спавним танк для игрока
            SpawnPlayerTank();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[PhotonNetworkManager] Failed to join room: {message}");
        }

        public void OnCreatedRoom()
        {
            Debug.Log($"[PhotonNetworkManager] Created room: {PhotonNetwork.CurrentRoom.Name}");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[PhotonNetworkManager] Failed to create room: {message}");
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[PhotonNetworkManager] Player {newPlayer.NickName} entered room");
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[PhotonNetworkManager] Player {otherPlayer.NickName} left room");
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"[PhotonNetworkManager] Disconnected from Photon. Cause: {cause}");
            isConnecting = false;
        }

        // Неиспользуемые методы интерфейсов
        public void OnConnected() { }
        public void OnLeftRoom() { }
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
        public void OnMasterClientSwitched(Player newMasterClient) { }
        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }

        #endregion
#endif
    }
}

