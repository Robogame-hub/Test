using System.IO;
using TankGame.Session;
using TMPro;
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

            ApplyConfiguredUIFontToAllUi();
            MenuDesertTheme.ApplyScene(scene);

            if (scene.name == CoreSceneName)
            {
                EnsureCorePauseMenu();
                return;
            }

            bool isMenuScene = scene.name == MainMenuSceneName || scene.name == LobbySceneName;
            if (!isMenuScene)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureAudioListener();
            MenuMusicPlayer.EnsureInstance();

            if (scene.name != LobbySceneName)
                return;

            // Do not duplicate normal flow if LobbyController is present.
            LobbyController lobbyController = Object.FindObjectOfType<LobbyController>();
            if (lobbyController != null)
                return;

            HideLobbyPlaySoloButton();
            WireLobbyBackButton();
            WireLobbyNicknameInput();
            WireLobbyButtonFeedbacks();
        }

        private static void EnsureCorePauseMenu()
        {
            BattlePauseMenuController controller = Object.FindObjectOfType<BattlePauseMenuController>(true);
            if (controller != null)
                return;

            GameObject go = new GameObject("BattlePauseMenuController");
            go.AddComponent<BattlePauseMenuController>();
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

        private static void ApplyConfiguredUIFontToAllUi()
        {
            MenuButtonFeedbackConfig config = MenuButtonFeedbackConfig.LoadDefault();
            if (config == null || config.uiFont == null)
                return;

            TMP_Text[] allTexts = Object.FindObjectsOfType<TMP_Text>(true);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text text = allTexts[i];
                if (text != null && text.font != config.uiFont)
                    text.font = config.uiFont;
            }

            TMP_InputField[] allInputs = Object.FindObjectsOfType<TMP_InputField>(true);
            for (int i = 0; i < allInputs.Length; i++)
            {
                TMP_InputField input = allInputs[i];
                if (input == null)
                    continue;

                if (input.textComponent != null && input.textComponent.font != config.uiFont)
                    input.textComponent.font = config.uiFont;

                if (input.placeholder is TMP_Text placeholder && placeholder.font != config.uiFont)
                    placeholder.font = config.uiFont;
            }
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

        private static void WireLobbyNicknameInput()
        {
            GameObject go = GameObject.Find("NicknameInput");
            if (go == null)
                return;

            TMP_InputField nicknameInput = go.GetComponent<TMP_InputField>();
            if (nicknameInput == null)
                return;

            nicknameInput.SetTextWithoutNotify(GameSessionSettings.PlayerNickname);
            nicknameInput.onValueChanged.RemoveListener(OnLobbyNicknameChanged);
            nicknameInput.onValueChanged.AddListener(OnLobbyNicknameChanged);
            nicknameInput.onEndEdit.RemoveListener(OnLobbyNicknameChanged);
            nicknameInput.onEndEdit.AddListener(OnLobbyNicknameChanged);
        }

        private static void OnLobbyNicknameChanged(string value)
        {
            GameSessionSettings.PlayerNickname = value;
        }

        private static void HideLobbyPlaySoloButton()
        {
            GameObject go = GameObject.Find("PlaySoloButton");
            if (go == null)
                return;

            go.SetActive(false);
        }

        private static void WireLobbyButtonFeedbacks()
        {
            MenuButtonFeedbackConfig config = MenuButtonFeedbackConfig.LoadDefault();
            AudioSource audioSource = EnsureLobbyButtonFeedbackAudioSource();

            ConfigureLobbyButtonFeedback("RefreshButton", config, audioSource);
            ConfigureLobbyButtonFeedback("CreateButton", config, audioSource);
            ConfigureLobbyButtonFeedback("BackButton", config, audioSource);
        }

        private static AudioSource EnsureLobbyButtonFeedbackAudioSource()
        {
            GameObject existing = GameObject.Find("LobbyButtonFeedbackAudio");
            if (existing != null)
            {
                AudioSource existingSource = existing.GetComponent<AudioSource>();
                if (existingSource != null)
                    return existingSource;
            }

            GameObject audioGo = new GameObject("LobbyButtonFeedbackAudio");
            AudioSource source = audioGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private static void ConfigureLobbyButtonFeedback(string objectName, MenuButtonFeedbackConfig config, AudioSource audioSource)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
                return;

            Button button = go.GetComponent<Button>();
            if (button == null)
                return;

            MenuButtonFeedback feedback = go.GetComponent<MenuButtonFeedback>();
            if (feedback == null)
                feedback = go.AddComponent<MenuButtonFeedback>();

            feedback.button = button;
            feedback.targetText = go.GetComponentInChildren<TMP_Text>(true);
            feedback.Configure(
                config != null ? config.normalTextColor : new Color(0.96f, 0.86f, 0.67f, 1f),
                config != null ? config.hoverTextColor : new Color(1f, 0.74f, 0.37f, 1f),
                config != null ? config.pressedTextColor : new Color(1f, 0.96f, 0.87f, 1f),
                config != null ? Mathf.Max(1f, config.hoverTextScale) : 1.08f,
                config != null ? Mathf.Max(1f, config.scaleLerpSpeed) : 16f,
                audioSource,
                config != null ? config.hoverSound : null,
                config != null ? config.clickSound : null);
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
