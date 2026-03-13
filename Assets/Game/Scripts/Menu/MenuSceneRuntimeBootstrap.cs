using System.IO;
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
    /// - wires Lobby BackButton even if LobbyController is missing in scene
    /// </summary>
    public static class MenuSceneRuntimeBootstrap
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string LobbySceneName = "Lobby";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

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

            if (scene.name == LobbySceneName)
                WireLobbyBackButton();
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

        private static void LoadMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                SceneManager.LoadScene(MainMenuSceneName);
                return;
            }

#if UNITY_EDITOR
            if (File.Exists(MainMenuScenePath))
            {
                EditorSceneManager.LoadSceneInPlayMode(MainMenuScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif

            Debug.LogError($"[MenuSceneRuntimeBootstrap] Cannot load '{MainMenuSceneName}'. Also missing fallback path '{MainMenuScenePath}'.");
        }
    }
}
