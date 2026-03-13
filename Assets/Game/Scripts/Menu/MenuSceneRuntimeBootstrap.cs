using System.IO;
using TankGame.Session;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace TankGame.Menu
{
    /// <summary>
    /// Runtime safety net for menu scenes:
    /// - ensures menu music exists in MainMenu/Lobby
    /// - ensures AudioListener exists in menu scenes
    /// - wires critical Lobby buttons if LobbyController is missing in scene
    /// </summary>
    public static class MenuSceneRuntimeBootstrap
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string LobbySceneName = "Lobby";
        private const string CoreSceneName = "Core";

        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string CoreScenePath = "Assets/Scenes/Core.unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitAfterFirstSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyForScene(scene);
        }

        private static void ApplyForScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            bool isMenuScene = scene.name == MainMenuSceneName || scene.name == LobbySceneName;
            if (!isMenuScene)
                return;

            EnsureAudioListener();
            MenuMusicPlayer.EnsureInstance();

            if (scene.name != LobbySceneName)
                return;

            // Do not duplicate normal flow if LobbyController is present.
            LobbyController lobbyController = Object.FindObjectOfType<LobbyController>();
            if (lobbyController != null)
                return;

            WireLobbyBackButton();
            WireLobbyPlaySoloButton();
        }

        private static void EnsureAudioListener()
        {
            AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
            if (listeners != null && listeners.Length > 0)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                cam = Object.FindObjectOfType<Camera>();

            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                return;
            }

            if (cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }

        private static void WireLobbyBackButton()
        {
            GameObject go = GameObject.Find("BackButton");
            if (go == null)
                return;

            Button backButton = go.GetComponent<Button>();
            if (backButton == null)
                return;

            backButton.onClick.RemoveListener(LoadMainMenu);
            backButton.onClick.AddListener(LoadMainMenu);
        }

        private static void WireLobbyPlaySoloButton()
        {
            GameObject go = GameObject.Find("PlaySoloButton");
            if (go == null)
                return;

            Button playSoloButton = go.GetComponent<Button>();
            if (playSoloButton == null)
                return;

            playSoloButton.onClick.RemoveListener(PlaySoloFallback);
            playSoloButton.onClick.AddListener(PlaySoloFallback);
        }

        private static void PlaySoloFallback()
        {
            GameSessionSettings.PrepareSolo(GameSessionSettings.MaxPlayers);
            LoadSceneWithFallback(CoreSceneName, CoreScenePath, "MenuSceneRuntimeBootstrap");
        }

        private static void LoadMainMenu()
        {
            LoadSceneWithFallback(MainMenuSceneName, MainMenuScenePath, "MenuSceneRuntimeBootstrap");
        }

        private static void LoadSceneWithFallback(string sceneName, string scenePath, string logPrefix)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

#if UNITY_EDITOR
            if (File.Exists(scenePath))
            {
                EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif

            Debug.LogError($"[{logPrefix}] Cannot load '{sceneName}'. Also missing fallback path '{scenePath}'.");
        }
    }
}
