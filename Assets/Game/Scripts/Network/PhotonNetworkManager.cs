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
#if PHOTON_UNITY_NETWORKING
    public class PhotonNetworkManager : MonoBehaviourPunCallbacks
#else
    public class PhotonNetworkManager : MonoBehaviour
#endif
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
        
        [Tooltip("Спавнить локальный танк, если Photon не подключился через это время (секунды). 0 = отключено")]
        [SerializeField] private float fallbackSpawnDelay = 5f;

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
        private bool hasSpawnedTank = false;
        private float connectionStartTime = 0f;
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
            Debug.Log($"[PhotonNetworkManager] Start() called! GameObject: {gameObject.name}, Active: {gameObject.activeSelf}, autoConnectOnStart: {autoConnectOnStart}");
            connectionStartTime = Time.time;
            
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
            else
            {
                Debug.LogWarning("[PhotonNetworkManager] Start(): autoConnectOnStart is FALSE! Photon will NOT connect automatically. Enable Auto Connect On Start in Inspector!");
            }
            
            // Если включен fallback - планируем спавн локального танка через N секунд
            // ВАЖНО: Fallback используется ТОЛЬКО для офлайн-режима, если Photon не подключился
            // При нормальном подключении танк спавнится через OnJoinedRoom() -> SpawnPlayerTank()
            if (fallbackSpawnDelay > 0f)
            {
                Invoke(nameof(CheckAndSpawnFallbackTank), fallbackSpawnDelay);
                Debug.Log($"[PhotonNetworkManager] Fallback spawn scheduled in {fallbackSpawnDelay} seconds (will only spawn if Photon not connected)");
            }
#endif
        }

        private void Update()
        {
#if PHOTON_UNITY_NETWORKING
            // Периодическая проверка состояния подключения и комнаты
            // Если подключены, но не в комнате - принудительно присоединяемся
            if (Time.frameCount % 120 == 0) // Каждые 2 секунды при 60 FPS
            {
                // Диагностика состояния подключения (только если не подключены)
                if (!PhotonNetwork.IsConnected && isConnecting && Time.frameCount % 360 == 0) // Каждые 6 секунд
                {
                    var state = PhotonNetwork.NetworkClientState;
                    Debug.LogWarning($"[PhotonNetworkManager] Still connecting... State: {state}, OfflineMode: {PhotonNetwork.OfflineMode}, Time since Connect(): {Time.time - connectionStartTime:F1}s");
                }
                
                if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && !isConnecting)
                {
                    Debug.LogWarning("[PhotonNetworkManager] Update(): Connected to Photon but NOT in room! Attempting to join room '" + roomName + "'...");
                    JoinOrCreateRoom();
                }
                // Если в комнате - выводим диагностику раз в 10 секунд
                else if (PhotonNetwork.InRoom && Time.frameCount % 600 == 0)
                {
                    Debug.Log($"[PhotonNetworkManager] Status check - Room: {PhotonNetwork.CurrentRoom?.Name}, Players: {PhotonNetwork.CurrentRoom?.PlayerCount}/{PhotonNetwork.CurrentRoom?.MaxPlayers}");
                }
            }
#endif
        }
        
        /// <summary>
        /// Проверяет прогресс подключения через 1 секунду после попытки подключения
        /// Используется для ранней диагностики проблем
        /// </summary>
        private void CheckConnectionProgress()
        {
#if PHOTON_UNITY_NETWORKING
            if (!PhotonNetwork.IsConnected && isConnecting)
            {
                var state = PhotonNetwork.NetworkClientState;
                var elapsedTime = Time.time - connectionStartTime;
                
                Debug.Log($"[PhotonNetworkManager] Connection progress check (after {elapsedTime:F1}s): State={state}, OfflineMode={PhotonNetwork.OfflineMode}");
                
                // Если состояние осталось PeerCreated - значит подключение не начинается
                if (state == ClientState.PeerCreated)
                {
                    Debug.LogWarning("[PhotonNetworkManager] ⚠️ State is still PeerCreated after 1 second! Connection may not be starting.");
                    Debug.LogWarning("[PhotonNetworkManager] This usually means:");
                    Debug.LogWarning("  1. Photon cannot establish network connection");
                    Debug.LogWarning("  2. Check internet connection");
                    Debug.LogWarning("  3. Check firewall/antivirus settings");
                }
            }
#endif
        }
        
        /// <summary>
        /// Проверяет статус подключения через 10 секунд после попытки подключения
        /// Используется для диагностики, если OnConnectedToMaster() не вызывается
        /// </summary>
        private void CheckConnectionStatus()
        {
#if PHOTON_UNITY_NETWORKING
            if (!PhotonNetwork.IsConnected && isConnecting)
            {
                var state = PhotonNetwork.NetworkClientState;
                var serverSettings = PhotonNetwork.PhotonServerSettings;
                
                Debug.LogError($"[PhotonNetworkManager] ⚠️ CONNECTION TIMEOUT! OnConnectedToMaster() was NOT called after 10 seconds!");
                Debug.LogError($"[PhotonNetworkManager] Current state: {state}");
                Debug.LogError($"[PhotonNetworkManager] OfflineMode: {PhotonNetwork.OfflineMode}");
                Debug.LogError($"[PhotonNetworkManager] AppId: {serverSettings?.AppSettings.AppIdRealtime ?? "NULL"}");
                Debug.LogError($"[PhotonNetworkManager] StartInOfflineMode: {serverSettings?.StartInOfflineMode ?? false}");
                Debug.LogError($"[PhotonNetworkManager] Possible causes:");
                Debug.LogError("  1. No internet connection");
                Debug.LogError("  2. Firewall/Antivirus blocking Photon");
                Debug.LogError("  3. Start In Offline Mode is enabled");
                Debug.LogError("  4. Invalid App ID");
                Debug.LogError($"[PhotonNetworkManager] Check Photon logs for more details (Photon → Pun → Wizard → Show Settings → Pun Logging: Full)");
            }
#endif
        }
        
        /// <summary>
        /// Проверяет подключение и спавнит локальный танк, если Photon не подключился
        /// ВАЖНО: Используется ТОЛЬКО если Photon действительно не подключился (для офлайн-режима)
        /// </summary>
        private void CheckAndSpawnFallbackTank()
        {
#if PHOTON_UNITY_NETWORKING
            if (hasSpawnedTank)
            {
                Debug.Log("[PhotonNetworkManager] Tank already spawned, skipping fallback check");
                return; // Танк уже заспавнен (через Photon или локально)
            }
            
            // ВАЖНО: Проверяем еще раз состояние Photon перед fallback спавном
            // Fallback используется ТОЛЬКО если Photon действительно не подключился
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                Debug.Log("[PhotonNetworkManager] Photon connected and in room - fallback not needed. Tank should spawn via Photon.");
                // Если подключены, танк должен спавниться через OnJoinedRoom() -> SpawnPlayerTank()
                // Если танк еще не заспавнен, возможно OnJoinedRoom еще не вызвался
                // Не спавним локально - ждем Photon спавна
                return;
            }
            
            // Fallback спавн ТОЛЬКО если Photon действительно не подключен
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogWarning($"[PhotonNetworkManager] Photon not connected/in room after {fallbackSpawnDelay}s. Connection: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}");
                Debug.LogWarning("[PhotonNetworkManager] Spawning LOCAL tank as fallback (OFFLINE MODE). Other players will NOT see this tank!");
                SpawnLocalTank();
            }
#endif
        }
        
        /// <summary>
        /// Спавнит локальный танк без Photon (для офлайн режима или fallback)
        /// </summary>
        private void SpawnLocalTank()
        {
#if PHOTON_UNITY_NETWORKING
            if (hasSpawnedTank)
            {
                Debug.LogWarning("[PhotonNetworkManager] Tank already spawned, skipping local spawn");
                return;
            }
            
            if (tankPrefab == null)
            {
                Debug.LogError("[PhotonNetworkManager] Tank prefab not assigned! Cannot spawn local tank.");
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
            
            // Спавним танк обычным способом (без Photon)
            GameObject tankInstance = Instantiate(tankPrefab, spawnPosition, spawnRotation);
            
            if (tankInstance != null)
            {
                // ВАЖНО: Активируем танк после спавна (иначе он будет выключенным!)
                tankInstance.SetActive(true);
                
                TankController tankController = tankInstance.GetComponent<TankController>();
                TankNetworkPhoton networkPhoton = tankInstance.GetComponent<TankNetworkPhoton>();
                
                if (tankController != null)
                {
                    // Устанавливаем isLocalPlayer
                    if (networkPhoton != null)
                    {
                        networkPhoton.SetIsLocalPlayer(true);
                    }
                    else
                    {
                        tankController.SetIsLocalPlayer(true);
                    }
                    
                    // Если используем SpawnManager, привязываем танк к спавн-поинту
                    // ВАЖНО: Используем SpawnPoint.SpawnTank() вместо SetOccupied(), 
                    // чтобы правильно активировать танк и все его компоненты
                    if (useSpawnManager && SpawnManager.Instance != null)
                    {
                        SpawnPoint spawnPoint = SpawnManager.Instance.GetFreeSpawnPoint();
                        if (spawnPoint != null)
                        {
                            // SpawnPoint.SpawnTank() активирует танк и все компоненты
                            // НО танк уже в правильной позиции, так что просто вызываем ActivateTank
                            // или используем SetOccupied (но танк уже активирован выше)
                            spawnPoint.SetOccupied(true, tankController);
                        }
                    }
                    
                    hasSpawnedTank = true;
                    Debug.Log($"[PhotonNetworkManager] Local tank spawned and activated: {tankInstance.name} at {spawnPosition}");
                }
                else
                {
                    Debug.LogError("[PhotonNetworkManager] Tank prefab doesn't have TankController component!");
                    Destroy(tankInstance);
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

            // ВАЖНО: Проверяем OfflineMode - если включен, Photon не будет подключаться к серверам!
            if (PhotonNetwork.OfflineMode)
            {
                Debug.LogWarning("[PhotonNetworkManager] ⚠️ PHOTON OFFLINE MODE IS ENABLED! Photon will NOT connect to servers!");
                Debug.LogWarning("[PhotonNetworkManager] To fix: Photon → Pun → Wizard → Show Settings → Start In Offline Mode = OFF");
                Debug.LogWarning("[PhotonNetworkManager] Or set PhotonNetwork.OfflineMode = false in code");
                
                // Отключаем OfflineMode принудительно
                PhotonNetwork.OfflineMode = false;
                Debug.Log("[PhotonNetworkManager] OfflineMode disabled. Will retry connection...");
            }

            isConnecting = true;
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
            
            // Проверяем настройки Photon перед подключением
            var serverSettings = PhotonNetwork.PhotonServerSettings;
            if (serverSettings == null || string.IsNullOrEmpty(serverSettings.AppSettings.AppIdRealtime))
            {
                Debug.LogError("[PhotonNetworkManager] Photon App ID not configured! Please open Photon → Pun → Wizard and set App ID Realtime.");
                isConnecting = false;
                return;
            }
            
            // Проверяем StartInOfflineMode в настройках
            if (serverSettings.StartInOfflineMode)
            {
                Debug.LogError("[PhotonNetworkManager] ⚠️ Start In Offline Mode is ENABLED in PhotonServerSettings!");
                Debug.LogError("[PhotonNetworkManager] Photon will NOT connect to servers with this setting enabled!");
                Debug.LogError("[PhotonNetworkManager] Fix: Photon → Pun → Wizard → Show Settings → Start In Offline Mode = OFF");
                isConnecting = false;
                return;
            }
            
            Debug.Log($"[PhotonNetworkManager] Calling PhotonNetwork.ConnectUsingSettings()... GameVersion={gameVersion}, AppId={serverSettings.AppSettings.AppIdRealtime}");
            Debug.Log($"[PhotonNetworkManager] Current state: NetworkClientState={PhotonNetwork.NetworkClientState}, OfflineMode={PhotonNetwork.OfflineMode}");
            
            // Проверяем готовность к подключению
            if (PhotonNetwork.NetworkClientState != ClientState.PeerCreated && PhotonNetwork.NetworkClientState != ClientState.Disconnected)
            {
                Debug.LogWarning($"[PhotonNetworkManager] Cannot connect! Current state: {PhotonNetwork.NetworkClientState}. Expected: PeerCreated or Disconnected");
                isConnecting = false;
                return;
            }
            
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[PhotonNetworkManager] ConnectUsingSettings() called. Waiting for OnConnectedToMaster() callback...");
            
            // Диагностика состояния через 1 секунду (чтобы увидеть изменения)
            Invoke(nameof(CheckConnectionProgress), 1f);
            
            // Запускаем таймер для диагностики, если подключение не установится
            Invoke(nameof(CheckConnectionStatus), 10f);
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

            // Проверяем готовность к операциям (важно для веб-версии)
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning($"[PhotonNetworkManager] Photon connected but not ready for operations! Server: {PhotonNetwork.Server}, State: {PhotonNetwork.NetworkingClient.State}");
                // Планируем повторную попытку через 0.5 секунды
                Invoke(nameof(JoinOrCreateRoom), 0.5f);
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

            Debug.Log($"[PhotonNetworkManager] Attempting to join/create room: '{roomName}' (fixed name for all players)");
            Debug.Log($"[PhotonNetworkManager] Room Options - MaxPlayers: {roomOptions.MaxPlayers}, IsVisible: {roomOptions.IsVisible}, IsOpen: {roomOptions.IsOpen}");
            Debug.Log($"[PhotonNetworkManager] Calling PhotonNetwork.JoinOrCreateRoom(\"{roomName}\")... IsConnectedAndReady={PhotonNetwork.IsConnectedAndReady}, Server: {PhotonNetwork.Server}, State: {PhotonNetwork.NetworkingClient.State}");
            
            bool joinResult = PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
            if (!joinResult)
            {
                Debug.LogError($"[PhotonNetworkManager] PhotonNetwork.JoinOrCreateRoom() returned FALSE! Server: {PhotonNetwork.Server}, State: {PhotonNetwork.NetworkingClient.State}");
                Debug.LogError("[PhotonNetworkManager] Will retry joining room in 1 second...");
                // Повторная попытка через 1 секунду
                Invoke(nameof(JoinOrCreateRoom), 1f);
            }
            else
            {
                Debug.Log($"[PhotonNetworkManager] ✓ JoinOrCreateRoom() called successfully! Room name: '{roomName}'. Waiting for OnJoinedRoom() callback...");
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
        /// Спавнит танк для локального игрока через Photon
        /// </summary>
        private void SpawnPlayerTank()
        {
            if (hasSpawnedTank)
            {
                Debug.LogWarning("[PhotonNetworkManager] Tank already spawned, skipping Photon spawn");
                return;
            }
            
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

            // Проверяем, что мы подключены к Photon и в комнате перед спавном
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[PhotonNetworkManager] Cannot spawn tank - not connected to Photon!");
                return;
            }
            
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("[PhotonNetworkManager] Cannot spawn tank - not in room!");
                return;
            }
            
            string prefabPath = GetPrefabResourcePath(tankPrefab);
            Debug.Log($"[PhotonNetworkManager] Attempting to spawn tank via Photon.Instantiate with path: '{prefabPath}' at position: {spawnPosition}");
            
            // Спавним танк через Photon (позиция уже установлена из SpawnPoint)
            GameObject tankInstance = PhotonNetwork.Instantiate(
                prefabPath,
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
                    
                    // Убеждаемся, что танк находится в правильной позиции после спавна
                    // Photon.Instantiate спавнит с позицией, но нужно сбросить velocity
                    Rigidbody rb = tankInstance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Сбрасываем velocity после спавна
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    
                    // Если используем SpawnManager, привязываем танк к спавн-поинту БЕЗ повторного спавна
                    // Позиция уже установлена через PhotonNetwork.Instantiate
                    if (useSpawnManager && SpawnManager.Instance != null)
                    {
                        // Находим спавн-поинт который использовался для спавна (по позиции)
                        SpawnPoint usedSpawnPoint = null;
                        float minDistance = float.MaxValue;
                        
                        foreach (var sp in FindObjectsOfType<SpawnPoint>())
                        {
                            float distance = Vector3.Distance(sp.Position, spawnPosition);
                            if (distance < 0.1f && distance < minDistance) // Точное совпадение позиции
                            {
                                usedSpawnPoint = sp;
                                minDistance = distance;
                            }
                        }
                        
                        // Если не нашли по позиции, используем первый свободный
                        if (usedSpawnPoint == null)
                        {
                            usedSpawnPoint = SpawnManager.Instance.GetFreeSpawnPoint();
                        }
                        
                        if (usedSpawnPoint != null)
                        {
                            // Регистрируем танк в SpawnManager БЕЗ повторного спавна
                            SpawnManager.Instance.RegisterTankAtSpawnPoint(tankController, usedSpawnPoint);
                        }
                    }
                    
                    hasSpawnedTank = true;
                    Debug.Log($"[PhotonNetworkManager] Player tank spawned via Photon: {tankInstance.name} at {spawnPosition}");
                }
                else
                {
                    Debug.LogError("[PhotonNetworkManager] Tank instance doesn't have required components!");
                }
            }
            else
            {
                Debug.LogError($"[PhotonNetworkManager] PhotonNetwork.Instantiate returned NULL! Prefab path: '{prefabPath}'. Check that:");
                Debug.LogError("  1. Prefab is in Resources folder (Assets/Resources/TANK.prefab)");
                Debug.LogError("  2. Prefab name matches resource path ('TANK')");
                Debug.LogError("  3. Prefab has PhotonView component");
                Debug.LogError("  4. PhotonView is properly configured with Observed Components");
            }
        }

        /// <summary>
        /// Получает путь к префабу в Resources для Photon.Instantiate
        /// </summary>
        private string GetPrefabResourcePath(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[PhotonNetworkManager] Prefab is null!");
                return "";
            }

            // Photon требует путь относительно папки Resources без расширения
            // Например: "TANK" для Assets/Resources/TANK.prefab
            string path = prefab.name;
            
            // Проверяем, что префаб действительно находится в Resources
            // Получаем путь через AssetDatabase (только в редакторе)
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(assetPath))
            {
                // Если путь содержит Resources, извлекаем правильный путь
                if (assetPath.Contains("Resources"))
                {
                    int resourcesIndex = assetPath.IndexOf("Resources/") + "Resources/".Length;
                    string resourcesPath = assetPath.Substring(resourcesIndex);
                    // Убираем расширение .prefab
                    if (resourcesPath.EndsWith(".prefab"))
                    {
                        path = resourcesPath.Substring(0, resourcesPath.Length - ".prefab".Length);
                    }
                    else
                    {
                        path = resourcesPath;
                    }
                    Debug.Log($"[PhotonNetworkManager] Using resource path from AssetDatabase: '{path}' (from '{assetPath}')");
                }
                else
                {
                    Debug.LogWarning($"[PhotonNetworkManager] Prefab is not in Resources folder! Path: {assetPath}. Using prefab name: '{path}'. This may cause Photon.Instantiate to fail!");
                }
            }
#endif
            
            // В рантайме (или если AssetDatabase недоступен) используем имя префаба
            // Photon ищет префабы в Resources по имени без расширения
            // ВАЖНО: Префаб должен быть в папке Resources с именем, совпадающим с prefab.name
            if (string.IsNullOrEmpty(path))
            {
                path = prefab.name;
            }
            
            // Проверяем, что путь не пустой
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[PhotonNetworkManager] Prefab name is empty! Cannot determine resource path.");
                return "";
            }
            
            Debug.Log($"[PhotonNetworkManager] GetPrefabResourcePath returning: '{path}' (prefab name: '{prefab.name}')");
            return path;
        }

        #region Photon Callbacks

        public void OnConnectedToMaster()
        {
            Debug.Log("[PhotonNetworkManager] ✓ OnConnectedToMaster() callback called! Connected to Photon Master Server");
            isConnecting = false;
            
            // В веб-версии может быть небольшая задержка перед готовностью к операциям
            // Используем корутину для надежного подключения к комнате
            StartCoroutine(JoinRoomWhenReady());
        }

#if PHOTON_UNITY_NETWORKING
        private System.Collections.IEnumerator JoinRoomWhenReady()
        {
            // Ждем, пока клиент полностью готов к операциям (особенно важно для веб-версии)
            int attempts = 0;
            const int maxAttempts = 20; // 2 секунды (20 * 0.1s)
            
            while (attempts < maxAttempts)
            {
                if (PhotonNetwork.IsConnectedAndReady)
                {
                    Debug.Log("[PhotonNetworkManager] Photon is ready for operations! Calling JoinOrCreateRoom()...");
                    JoinOrCreateRoom();
                    yield break;
                }
                
                attempts++;
                Debug.Log($"[PhotonNetworkManager] Waiting for Photon to be ready... Attempt {attempts}/{maxAttempts}");
                yield return new WaitForSeconds(0.1f);
            }
            
            // Если не готов после всех попыток, все равно пытаемся присоединиться
            Debug.LogWarning("[PhotonNetworkManager] Photon not ready after max attempts, attempting JoinOrCreateRoom anyway...");
            JoinOrCreateRoom();
        }
#endif

        public void OnJoinedRoom()
        {
            Debug.Log($"[PhotonNetworkManager] ✓ OnJoinedRoom() callback called! Joined room: {PhotonNetwork.CurrentRoom.Name}");
            playerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Проверяем, сколько уже игроков в комнате (для диагностики)
            Debug.Log($"[PhotonNetworkManager] Room info - Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
            
            // Выводим список всех игроков в комнате
            Debug.Log($"[PhotonNetworkManager] Players in room:");
            foreach (var player in PhotonNetwork.PlayerList)
            {
                Debug.Log($"[PhotonNetworkManager]   - Player {player.ActorNumber}: {player.NickName} (IsLocal: {player.IsLocal}, IsMasterClient: {player.IsMasterClient})");
            }
            
            // Проверяем, сколько уже танков в сцене (должны быть танки других игроков, если они уже присоединились)
            var existingTanks = FindObjectsOfType<TankController>();
            Debug.Log($"[PhotonNetworkManager] Existing tanks in scene before spawn: {existingTanks.Length}");
            foreach (var tank in existingTanks)
            {
#if PHOTON_UNITY_NETWORKING
                PhotonView pv = tank.GetComponent<PhotonView>();
                if (pv != null)
                {
                    Debug.Log($"[PhotonNetworkManager] Existing tank: {tank.name}, PhotonView.IsMine: {pv.IsMine}, Owner: {pv.Owner?.NickName ?? "None"}, ViewID: {pv.ViewID}");
                }
                else
                {
                    Debug.LogWarning($"[PhotonNetworkManager] WARNING: Tank {tank.name} has NO PhotonView - other players will NOT see it!");
                }
#endif
            }
            
            // ВАЖНО: Спавним танк через Photon (не локально!)
            // SpawnPlayerTank() использует PhotonNetwork.Instantiate(), что гарантирует, что все игроки увидят танк
            Debug.Log("[PhotonNetworkManager] Spawning player tank via Photon (all players will see it)...");
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
            Debug.Log($"[PhotonNetworkManager] Player {newPlayer.NickName} (ActorNumber: {newPlayer.ActorNumber}) entered room. Current players: {PhotonNetwork.CurrentRoom.PlayerCount}");
            
            // Проверяем, есть ли уже танки других игроков в сцене
            var allTanks = FindObjectsOfType<TankController>();
            Debug.Log($"[PhotonNetworkManager] Total tanks in scene: {allTanks.Length}");
            foreach (var tank in allTanks)
            {
#if PHOTON_UNITY_NETWORKING
                PhotonView pv = tank.GetComponent<PhotonView>();
                if (pv != null)
                {
                    Debug.Log($"[PhotonNetworkManager] Tank: {tank.name}, PhotonView.IsMine: {pv.IsMine}, Owner: {pv.Owner?.NickName ?? "None"}, ViewID: {pv.ViewID}");
                }
                else
                {
                    Debug.Log($"[PhotonNetworkManager] Tank: {tank.name}, NO PhotonView (local tank)");
                }
#endif
            }
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[PhotonNetworkManager] Player {otherPlayer.NickName} left room");
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogError($"[PhotonNetworkManager] ✗ OnDisconnected() called! Cause: {cause}");
            isConnecting = false;
            
            // Выводим детальную информацию о причине отключения
            string causeMessage = GetDisconnectCauseMessage(cause);
            Debug.LogError($"[PhotonNetworkManager] Disconnect reason: {causeMessage}");
            
            // Если это не намеренное отключение, пытаемся переподключиться через 3 секунды
            if (cause != DisconnectCause.DisconnectByClientLogic && autoConnectOnStart)
            {
                Debug.LogWarning("[PhotonNetworkManager] Will attempt to reconnect in 3 seconds...");
                Invoke(nameof(Connect), 3f);
            }
        }

#if PHOTON_UNITY_NETWORKING
        /// <summary>
        /// Получает понятное сообщение о причине отключения
        /// </summary>
        private string GetDisconnectCauseMessage(DisconnectCause cause)
        {
            switch (cause)
            {
                case DisconnectCause.None:
                    return "No error - normal disconnect";
                case DisconnectCause.ExceptionOnConnect:
                    return "Exception while connecting - check internet connection and firewall";
                case DisconnectCause.DnsExceptionOnConnect:
                    return "DNS exception - check internet connection";
                case DisconnectCause.ServerAddressInvalid:
                    return "Server address invalid - check App ID in Photon Settings";
                case DisconnectCause.ApplicationQuit:
                    return "Application quit - normal";
                case DisconnectCause.DisconnectByClientLogic:
                    return "Disconnected by client - normal";
                case DisconnectCause.DisconnectByServerLogic:
                    return "Disconnected by server - check Photon dashboard";
                case DisconnectCause.DisconnectByServerReasonUnknown:
                    return "Disconnected by server (reason unknown) - check Photon dashboard";
                case DisconnectCause.ClientTimeout:
                    return "Client timeout - check internet connection";
                case DisconnectCause.ServerTimeout:
                    return "Server timeout - Photon servers may be busy";
                case DisconnectCause.MaxCcuReached:
                    return "Max CCU reached - upgrade Photon subscription or wait";
                default:
                    return $"Unknown cause: {cause}";
            }
        }
#endif

        // Неиспользуемые методы интерфейсов (для совместимости с Photon callbacks)
        public void OnConnected() { }
        public void OnLeftRoom() { }
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
        public void OnMasterClientSwitched(Player newMasterClient) { }
        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
        public void OnCustomAuthenticationResponse(System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Debug.LogError($"[PhotonNetworkManager] ✗ OnCustomAuthenticationFailed() called! Message: {debugMessage}");
        }

        #endregion
#endif
    }
}

