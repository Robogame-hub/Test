using UnityEngine;
using System;

namespace TankGame.Network
{
    /// <summary>
    /// Диагностический инструмент для проверки настройки Photon PUN 2
    /// Добавьте этот компонент на любой GameObject в сцене для проверки
    /// </summary>
    public class PhotonDiagnostics : MonoBehaviour
    {
        private bool photonInstalled = false;
        private Type photonNetworkType;

        private void Start()
        {
            // Проверяем наличие Photon через рефлексию (не зависит от директив компиляции)
            photonNetworkType = Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp") 
                             ?? Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp-firstpass")
                             ?? Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");
            
            photonInstalled = photonNetworkType != null;
            
            CheckPhotonSetup();
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;

            // Вывод диагностики в правой части экрана
            float screenWidth = Screen.width;
            float startX = screenWidth - 500; // Отступ от правого края
            float yPos = 10;
            
            GUI.Label(new Rect(startX, yPos, 480, 30), "PHOTON DIAGNOSTICS", style);
            yPos += 40;

            if (!photonInstalled)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(startX, yPos, 480, 30), "✗ PHOTON PUN 2 NOT INSTALLED!");
                yPos += 40;
                GUI.color = Color.yellow;
                style.fontSize = 16;
                GUI.Label(new Rect(startX, yPos, 480, 100), "Install from Asset Store:\n'PUN 2 - FREE'\n\nOr check Scripting Define Symbols:\nPlayer Settings → Scripting Define Symbols\nShould contain: PHOTON_UNITY_NETWORKING");
                GUI.color = Color.white;
                return;
            }

            // Photon установлен - используем рефлексию для доступа к PhotonNetwork
            try
            {
                bool isConnected = (bool)photonNetworkType.GetProperty("IsConnected").GetValue(null);
                string connectionStatus = isConnected ? "✓ CONNECTED" : "✗ DISCONNECTED";
                GUI.color = isConnected ? Color.green : Color.red;
                GUI.Label(new Rect(startX, yPos, 480, 30), $"Status: {connectionStatus}");
                yPos += 30;
                GUI.color = Color.white;

                // Проверка комнаты
                bool inRoom = (bool)photonNetworkType.GetProperty("InRoom").GetValue(null);
                if (inRoom)
                {
                    var currentRoom = photonNetworkType.GetProperty("CurrentRoom").GetValue(null);
                    var roomName = currentRoom.GetType().GetProperty("Name").GetValue(currentRoom);
                    var playerCount = currentRoom.GetType().GetProperty("PlayerCount").GetValue(currentRoom);
                    var maxPlayers = currentRoom.GetType().GetProperty("MaxPlayers").GetValue(currentRoom);

                    GUI.color = Color.green;
                    GUI.Label(new Rect(startX, yPos, 480, 30), $"Room: {roomName}");
                    yPos += 30;
                    GUI.Label(new Rect(startX, yPos, 480, 30), $"Players: {playerCount}/{maxPlayers}");
                    yPos += 30;
                    GUI.color = Color.white;

                    // Список игроков
                    var localPlayer = photonNetworkType.GetProperty("LocalPlayer").GetValue(null);
                    var actorNumber = localPlayer.GetType().GetProperty("ActorNumber").GetValue(localPlayer);
                    GUI.Label(new Rect(startX, yPos, 480, 30), $"Local Player ID: {actorNumber}");
                    yPos += 30;

                    var playerList = photonNetworkType.GetProperty("PlayerList").GetValue(null) as System.Array;
                    if (playerList != null)
                    {
                        foreach (var player in playerList)
                        {
                            var playerActorNumber = player.GetType().GetProperty("ActorNumber").GetValue(player);
                            var isLocal = (bool)player.GetType().GetProperty("IsLocal").GetValue(player);
                            var nickName = player.GetType().GetProperty("NickName").GetValue(player);
                            string isLocalText = isLocal ? " (YOU)" : "";
                            GUI.Label(new Rect(startX, yPos, 480, 30), $"Player {playerActorNumber}: {nickName ?? "Player"}{isLocalText}");
                            yPos += 25;
                        }
                    }
                }
                else
                {
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(startX, yPos, 480, 30), "Not in room");
                    yPos += 30;
                    GUI.color = Color.white;
                }

                yPos += 20;

                // Проверка объектов в сцене
                Type photonViewType = Type.GetType("Photon.Pun.PhotonView, Assembly-CSharp")
                                   ?? Type.GetType("Photon.Pun.PhotonView, Assembly-CSharp-firstpass")
                                   ?? Type.GetType("Photon.Pun.PhotonView, PhotonUnityNetworking");
                
                if (photonViewType != null)
                {
                    UnityEngine.Object[] photonViews = FindObjectsOfType(photonViewType);
                    GUI.Label(new Rect(startX, yPos, 480, 30), $"PhotonViews in scene: {photonViews.Length}");
                    yPos += 30;

                    int localViews = 0;
                    int remoteViews = 0;
                    foreach (var pv in photonViews)
                    {
                        bool isMine = (bool)pv.GetType().GetProperty("IsMine").GetValue(pv);
                        if (isMine) localViews++;
                        else remoteViews++;
                    }

                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(startX, yPos, 480, 30), $"  - Local (Mine): {localViews}");
                    yPos += 25;
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(startX, yPos, 480, 30), $"  - Remote (Others): {remoteViews}");
                    yPos += 25;
                    GUI.color = Color.white;
                }
            }
            catch (Exception e)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(startX, yPos, 480, 100), $"Error accessing Photon:\n{e.Message}");
                GUI.color = Color.white;
            }
        }

        [ContextMenu("Check Photon Setup")]
        public void CheckPhotonSetup()
        {
            Debug.Log("=== PHOTON DIAGNOSTICS ===");

            if (!photonInstalled)
            {
                Debug.LogError("[PhotonDiagnostics] Photon PUN 2: ✗ NOT INSTALLED");
                Debug.LogError("[PhotonDiagnostics] Please install 'PUN 2 - FREE' from Asset Store");
                Debug.LogError("[PhotonDiagnostics] Or check Scripting Define Symbols in Player Settings");
                Debug.Log("=== END DIAGNOSTICS ===");
                return;
            }

            Debug.Log($"[PhotonDiagnostics] Photon PUN 2: ✓ INSTALLED (found via reflection)");

            try
            {
                // Проверка App ID
                var photonServerSettings = photonNetworkType.GetProperty("PhotonServerSettings").GetValue(null);
                var appIdRealtime = photonServerSettings.GetType().GetProperty("AppIdRealtime").GetValue(photonServerSettings) as string;
                Debug.Log($"[PhotonDiagnostics] App ID: {(string.IsNullOrEmpty(appIdRealtime) ? "✗ NOT SET" : "✓ SET")}");

                // Проверка подключения
                bool isConnected = (bool)photonNetworkType.GetProperty("IsConnected").GetValue(null);
                Debug.Log($"[PhotonDiagnostics] Connected: {(isConnected ? "✓ YES" : "✗ NO")}");

                // Проверка комнаты
                bool inRoom = (bool)photonNetworkType.GetProperty("InRoom").GetValue(null);
                if (inRoom)
                {
                    var currentRoom = photonNetworkType.GetProperty("CurrentRoom").GetValue(null);
                    var roomName = currentRoom.GetType().GetProperty("Name").GetValue(currentRoom);
                    var playerCount = currentRoom.GetType().GetProperty("PlayerCount").GetValue(currentRoom);
                    var maxPlayers = currentRoom.GetType().GetProperty("MaxPlayers").GetValue(currentRoom);
                    Debug.Log($"[PhotonDiagnostics] In Room: ✓ YES - {roomName}");
                    Debug.Log($"[PhotonDiagnostics] Players: {playerCount}/{maxPlayers}");
                }
                else
                {
                    Debug.Log("[PhotonDiagnostics] In Room: ✗ NO");
                }

                // Проверка PhotonNetworkManager
                PhotonNetworkManager manager = FindObjectOfType<PhotonNetworkManager>();
                if (manager != null)
                {
                    Debug.Log("[PhotonDiagnostics] PhotonNetworkManager: ✓ FOUND");
                    
                    var prefabField = typeof(PhotonNetworkManager).GetField("tankPrefab",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prefabField != null)
                    {
                        GameObject prefab = prefabField.GetValue(manager) as GameObject;
                        if (prefab != null)
                        {
                            Debug.Log($"[PhotonDiagnostics] Tank Prefab: ✓ ASSIGNED - {prefab.name}");

                            Type prefabPhotonViewType = Type.GetType("Photon.Pun.PhotonView, Assembly-CSharp")
                                               ?? Type.GetType("Photon.Pun.PhotonView, PhotonUnityNetworking");
                            
                            if (prefabPhotonViewType != null)
                            {
                                Component pv = prefab.GetComponent(prefabPhotonViewType);
                                TankNetworkPhoton tnp = prefab.GetComponent<TankNetworkPhoton>();

                                Debug.Log($"[PhotonDiagnostics] PhotonView on prefab: {(pv != null ? "✓ YES" : "✗ NO")}");
                                Debug.Log($"[PhotonDiagnostics] TankNetworkPhoton on prefab: {(tnp != null ? "✓ YES" : "✗ NO")}");

#if UNITY_EDITOR
                                string prefabPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                                if (prefabPath.Contains("Resources"))
                                {
                                    Debug.Log($"[PhotonDiagnostics] Prefab in Resources: ✓ YES - {prefabPath}");
                                }
                                else
                                {
                                    Debug.LogWarning($"[PhotonDiagnostics] Prefab in Resources: ✗ NO - {prefabPath}");
                                    Debug.LogWarning("[PhotonDiagnostics] Photon requires prefabs to be in Resources folder!");
                                }
#endif
                            }
                        }
                        else
                        {
                            Debug.LogError("[PhotonDiagnostics] Tank Prefab: ✗ NOT ASSIGNED in PhotonNetworkManager!");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[PhotonDiagnostics] PhotonNetworkManager: ✗ NOT FOUND in scene!");
                }

                // Проверка объектов в сцене
                Type photonViewType = Type.GetType("Photon.Pun.PhotonView, Assembly-CSharp")
                                   ?? Type.GetType("Photon.Pun.PhotonView, PhotonUnityNetworking");
                
                if (photonViewType != null)
                {
                    UnityEngine.Object[] views = FindObjectsOfType(photonViewType);
                    Debug.Log($"[PhotonDiagnostics] PhotonViews in scene: {views.Length}");
                    foreach (var view in views)
                    {
                        bool isMine = (bool)view.GetType().GetProperty("IsMine").GetValue(view);
                        int viewID = (int)view.GetType().GetProperty("ViewID").GetValue(view);
                        Debug.Log($"  - {view.name}: IsMine={isMine}, ViewID={viewID}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotonDiagnostics] Error accessing Photon: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }

            Debug.Log("=== END DIAGNOSTICS ===");
        }
    }
}
