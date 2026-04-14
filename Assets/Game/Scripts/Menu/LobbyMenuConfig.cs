using System;
using UnityEngine;

namespace TankGame.Menu
{
    [Serializable]
    public sealed class LobbyMenuRoomConfigEntry
    {
        public string roomNameKey;
        public string fallbackRoomName;
        public int currentPlayers;
        public int maxPlayers = 6;
        public bool isPasswordProtected;
        public string password;
    }

    [Serializable]
    public sealed class LobbyMenuConfigData
    {
        public string battleSceneName = "Core";
        public string defaultNickname = "Player";
        public LobbyMenuRoomConfigEntry[] initialDebugRooms;
    }

    public static class LobbyMenuConfig
    {
        private const string ConfigPath = "Menu/LobbyMenuConfig";

        public static LobbyMenuConfigData LoadOrDefault()
        {
            TextAsset configAsset = Resources.Load<TextAsset>(ConfigPath);
            if (configAsset == null)
                return CreateDefault();

            LobbyMenuConfigData config = JsonUtility.FromJson<LobbyMenuConfigData>(configAsset.text);
            if (config == null)
                return CreateDefault();

            if (config.initialDebugRooms == null || config.initialDebugRooms.Length == 0)
                config.initialDebugRooms = CreateDefault().initialDebugRooms;

            if (string.IsNullOrWhiteSpace(config.battleSceneName))
                config.battleSceneName = "Core";

            if (string.IsNullOrWhiteSpace(config.defaultNickname))
                config.defaultNickname = "Player";

            return config;
        }

        private static LobbyMenuConfigData CreateDefault()
        {
            return new LobbyMenuConfigData
            {
                battleSceneName = "Core",
                defaultNickname = "Player",
                initialDebugRooms = new[]
                {
                    new LobbyMenuRoomConfigEntry
                    {
                        roomNameKey = "lobby.debug_room_open",
                        fallbackRoomName = "Debug Room",
                        currentPlayers = 0,
                        maxPlayers = 6,
                        isPasswordProtected = false,
                        password = string.Empty
                    },
                    new LobbyMenuRoomConfigEntry
                    {
                        roomNameKey = "lobby.debug_room_locked",
                        fallbackRoomName = "Locked Debug Room",
                        currentPlayers = 0,
                        maxPlayers = 6,
                        isPasswordProtected = true,
                        password = "1234"
                    }
                }
            };
        }
    }
}
