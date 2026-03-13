using UnityEngine;

namespace TankGame.Session
{
    public enum MatchStartMode
    {
        None = 0,
        Lobby = 1,
        SoloWithBots = 2,
        Sandbox = 3
    }

    public static class GameSessionSettings
    {
        public const int MaxPlayers = 6;
        public const int DefaultSoloBotCount = 3;
        public const int DefaultSandboxBotCount = 3;

        private const string KeySelectedTankIndex = "Session.SelectedTankIndex";
        private const string KeyStartMode = "Session.StartMode";
        private const string KeySoloBotCount = "Session.SoloBotCount";
        private const string KeySandboxBotCount = "Session.SandboxBotCount";
        private const string KeyPlayerNickname = "Session.PlayerNickname";

        public static int SelectedTankIndex
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeySelectedTankIndex, 0));
            set
            {
                PlayerPrefs.SetInt(KeySelectedTankIndex, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static MatchStartMode StartMode
        {
            get => (MatchStartMode)PlayerPrefs.GetInt(KeyStartMode, (int)MatchStartMode.None);
            set
            {
                PlayerPrefs.SetInt(KeyStartMode, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static int SoloBotCount
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeySoloBotCount, DefaultSoloBotCount), 0, MaxPlayers);
            set
            {
                PlayerPrefs.SetInt(KeySoloBotCount, Mathf.Clamp(value, 0, MaxPlayers));
                PlayerPrefs.Save();
            }
        }

        public static int SandboxBotCount
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeySandboxBotCount, DefaultSandboxBotCount), 0, MaxPlayers);
            set
            {
                PlayerPrefs.SetInt(KeySandboxBotCount, Mathf.Clamp(value, 0, MaxPlayers));
                PlayerPrefs.Save();
            }
        }

        public static string PlayerNickname
        {
            get => PlayerPrefs.GetString(KeyPlayerNickname, "Player");
            set
            {
                string safe = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
                if (safe.Length > 24)
                    safe = safe.Substring(0, 24);

                PlayerPrefs.SetString(KeyPlayerNickname, safe);
                PlayerPrefs.Save();
            }
        }

        public static void PrepareLobby()
        {
            StartMode = MatchStartMode.Lobby;
        }

        public static void PrepareSolo(int botCount)
        {
            SoloBotCount = botCount;
            StartMode = MatchStartMode.SoloWithBots;
        }

        public static void PrepareSandbox()
        {
            SandboxBotCount = DefaultSandboxBotCount;
            StartMode = MatchStartMode.Sandbox;
        }

        public static void PrepareSandbox(int botCount)
        {
            SandboxBotCount = botCount;
            StartMode = MatchStartMode.Sandbox;
        }
    }
}
