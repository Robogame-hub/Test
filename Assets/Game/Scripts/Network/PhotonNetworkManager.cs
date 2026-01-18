using UnityEngine;
using TankGame.Tank;
using TankGame.Game;

#if PHOTON_UNITY_NETWORKING
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
    {
        [Header("Network Settings")]
        [Tooltip("Версия игры (для разделения игроков разных версий)")]
        [SerializeField] private string gameVersion = "1.0";
        
        [Tooltip("Максимальное количество игроков в комнате")]
        [SerializeField] private byte maxPlayersPerRoom = 16;
        
        [Tooltip("Имя комнаты (если пусто, используется дефолтное имя)")]
        [SerializeField] private string roomName = "MainRoom";
        
        [Tooltip("Автоматически подключаться и создавать/присоединяться к комнате при старте")]
        [SerializeField] private bool autoConnectOnStart = true;

        [Header("Tank Spawn")]
        [Tooltip("Префаб танка для спавна (должен иметь TankNetworkPhoton и PhotonView)")]
        [SerializeField] private GameObject tankPrefab;
        
        [Tooltip("Использовать SpawnManager для определения спавн-поинтов")]
        [SerializeField] private bool useSpawnManager = true;

        private static PhotonNetworkManager instance;
        public static PhotonNetworkManager Instance => instance;

#if PHOTON_UNITY_NETWORKING
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

#if PHOTON_UNITY_NETWORKING
            PhotonNetwork.AddCallbackTarget(this);
#else
            Debug.LogWarning("[PhotonNetworkManager] Photon PUN 2 not installed! This component requires Photon PUN 2 to work.");
#endif
        }

        private void Start()
        {
#if PHOTON_UNITY_NETWORKING
            if (autoConnectOnStart)
            {
                // Подключаемся и создаем/присоединяемся к комнате автоматически при старте
                if (!PhotonNetwork.IsConnected)
                {
                    Debug.Log("[PhotonNetworkManager] Start(): Not connected, calling Connect()...");
                    Connect();
                }
                else
                {
                    Debug.Log("[PhotonNetworkManager] Start(): Already connected!");
                    if (!PhotonNetwork.InRoom)
                    {
                        // Уже подключены, но не в комнате - присоединяемся
                        Debug.Log("[PhotonNetworkManager] Start(): Not in room, calling JoinOrCreateRoom()...");
                        JoinOrCreateRoom();
                    }
                    else
                    {
                        Debug.Log($"[PhotonNetworkManager] Start(): Already in room: {PhotonNetwork.CurrentRoom.Name}");
                    }
                }
            }
#endif
        }

        private void Update()
        {
#if PHOTON_UNITY_NETWORKING
            // Проверка состояния подключения (для диагностики)
            // Можно удалить после отладки
            if (Time.frameCount % 120 == 0) // Каждые 2 секунды при 60 FPS
            {
                if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && !isConnecting)
                {
                    Debug.LogWarning("[PhotonNetworkManager] Connected but not in room! Attempting to join...");
                    JoinOrCreateRoom();
                }
            }
#endif
        }

        private void OnDestroy()
        {
#if PHOTON_UNITY_NETWORKING
            PhotonNetwork.RemoveCallbackTarget(this);
#endif
        }

        /// <summary>
        /// Подключиться к Photon
        /// </summary>
        public void Connect()
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[PhotonNetworkManager] Already connected to Photon!");
                // Если уже подключены, но не в комнате - присоединяемся
                if (!PhotonNetwork.InRoom)
                {
                    JoinOrCreateRoom();
                }
                return;
            }

            isConnecting = true;
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
            
            Debug.Log($"[PhotonNetworkManager] Calling PhotonNetwork.ConnectUsingSettings()... GameVersion={gameVersion}");
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[PhotonNetworkManager] ConnectUsingSettings() called. Waiting for OnConnectedToMaster() callback...");
#else
            Debug.LogError("[PhotonNetworkManager] Photon PUN 2 not installed! Cannot connect.");
#endif
        }

        /// <summary>
        /// Подключиться к комнате (или создать, если не существует)
        /// Все игроки попадают в одну и ту же комнату
        /// </summary>
        public void JoinOrCreateRoom()
        {
#if PHOTON_UNITY_NETWORKING
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("[PhotonNetworkManager] Not connected to Photon! Connecting first...");
                Connect();
                return;
            }

            // Используем фиксированное имя комнаты для всех игроков
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "MainRoom";
            }

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true, // Комната открыта для подключения в любой момент
                EmptyRoomTtl = 0, // Комната не удаляется когда пустая (0 = никогда не удаляется)
                PlayerTtl = 0 // Игроки не удаляются при отключении (0 = никогда не удаляются)
            };

            Debug.Log($"[PhotonNetworkManager] Calling PhotonNetwork.JoinOrCreateRoom(\"{roomName}\")...");
            bool joinResult = PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
            if (!joinResult)
            {
                Debug.LogError($"[PhotonNetworkManager] PhotonNetwork.JoinOrCreateRoom() returned FALSE! This means the call failed immediately.");
            }
            else
            {
                Debug.Log($"[PhotonNetworkManager] JoinOrCreateRoom() called successfully. Waiting for callback...");
            }
#else
            Debug.LogError("[PhotonNetworkManager] Photon PUN 2 not installed!");
#endif
        }

        /// <summary>
        /// Отключиться от Photon
        /// </summary>
        public void Disconnect()
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                Debug.Log("[PhotonNetworkManager] Disconnected from Photon");
            }
#else
            Debug.LogError("[PhotonNetworkManager] Photon PUN 2 not installed!");
#endif
        }

#if PHOTON_UNITY_NETWORKING
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
            Debug.Log("[PhotonNetworkManager] ✓ OnConnectedToMaster() callback called! Connected to Photon Master Server");
            isConnecting = false;
            
            // Автоматически создаем/присоединяемся к комнате
            // Комната всегда одна и та же для всех игроков
            Debug.Log("[PhotonNetworkManager] Calling JoinOrCreateRoom()...");
            JoinOrCreateRoom();
        }

        public void OnJoinedRoom()
        {
            Debug.Log($"[PhotonNetworkManager] ✓ OnJoinedRoom() callback called! Joined room: {PhotonNetwork.CurrentRoom.Name}");
            playerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Спавним танк для игрока
            SpawnPlayerTank();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[PhotonNetworkManager] ✗ OnJoinRoomFailed() callback called! ReturnCode: {returnCode}, Message: {message}");
            // Повторная попытка подключения через 2 секунды
            Invoke(nameof(JoinOrCreateRoom), 2f);
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

        // Неиспользуемые методы интерфейсов (для совместимости с Photon callbacks)
        public void OnConnected() { }
        public void OnLeftRoom() { }
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
        public void OnMasterClientSwitched(Player newMasterClient) { }
        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
        public void OnCustomAuthenticationResponse(System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }

        #endregion
#endif
    }
}

