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
            ApplyConfiguredSceneColors(scene);

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

        private static void ApplyConfiguredSceneColors(Scene scene)
        {
            if (scene.name != MainMenuSceneName && scene.name != LobbySceneName)
                return;

            MenuButtonFeedbackConfig config = MenuButtonFeedbackConfig.LoadDefault();
            if (config == null)
                return;

            ApplyButtonBaseColors(scene, config);
            ApplyStaticTextColors(scene, config);
            ApplySliderColors(scene, config);
            ApplyStatBarColors(scene, config);
        }

        private static void ApplyButtonBaseColors(Scene scene, MenuButtonFeedbackConfig config)
        {
            if (config == null)
                return;

            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || button.gameObject.scene != scene)
                    continue;

                Image targetImage = button.targetGraphic as Image;
                if (targetImage != null)
                    targetImage.color = config.normalButtonColor;

                ColorBlock colors = button.colors;
                colors.normalColor = config.normalButtonColor;
                colors.highlightedColor = config.hoverButtonColor;
                colors.selectedColor = config.hoverButtonColor;
                colors.pressedColor = config.pressedButtonColor;
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.06f;
                button.colors = colors;
            }
        }

        private static void ApplyStaticTextColors(Scene scene, MenuButtonFeedbackConfig config)
        {
            if (config == null)
                return;

            TMP_Text[] texts = Object.FindObjectsOfType<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.gameObject.scene != scene)
                    continue;
                if (text.GetComponentInParent<Button>() != null)
                    continue;

                SegmentedStatBar statBar = text.GetComponentInParent<SegmentedStatBar>();
                if (statBar != null)
                {
                    text.color = statBar.valueText == text
                        ? config.statValueTextColor
                        : config.statLabelTextColor;
                    continue;
                }

                text.color = config.staticTextColor;
            }
        }

        private static void ApplySliderColors(Scene scene, MenuButtonFeedbackConfig config)
        {
            if (config == null)
                return;

            Slider[] sliders = Object.FindObjectsOfType<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i];
                if (slider == null || slider.gameObject.scene != scene)
                    continue;

                if (slider.fillRect == null)
                {
                    Transform fill = slider.transform.Find("Fill Area/Fill");
                    if (fill == null)
                        fill = slider.transform.Find("FillArea/Fill");
                    if (fill != null)
                        slider.fillRect = fill as RectTransform;
                }

                if (slider.handleRect == null)
                {
                    Transform handle = slider.transform.Find("Handle");
                    if (handle == null)
                        handle = slider.transform.Find("Handle Slide Area/Handle");
                    if (handle != null)
                        slider.handleRect = handle as RectTransform;
                }

                Transform background = slider.transform.Find("Background");
                if (background != null && background.TryGetComponent(out Image bgImage))
                {
                    bgImage.color = config.sliderBackgroundColor;
                    bgImage.raycastTarget = false;
                }

                if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fillImage))
                    fillImage.color = config.sliderFillColor;

                if (slider.handleRect != null && slider.handleRect.TryGetComponent(out Image handleImage))
                    handleImage.color = config.sliderHandleColor;
            }
        }

        private static void ApplyStatBarColors(Scene scene, MenuButtonFeedbackConfig config)
        {
            if (config == null)
                return;

            SegmentedStatBar[] bars = Object.FindObjectsOfType<SegmentedStatBar>(true);
            for (int i = 0; i < bars.Length; i++)
            {
                SegmentedStatBar bar = bars[i];
                if (bar == null || bar.gameObject.scene != scene)
                    continue;

                Color backgroundColor = GetStatBackgroundColorForBar(config, bar);
                Color fillColor = GetStatFillColorForBar(config, bar);

                bar.backgroundColor = backgroundColor;
                bar.fillColor = fillColor;

                TintBarChildren(bar.backgroundContainer, backgroundColor);
                TintBarChildren(bar.fillContainer, fillColor);

                TMP_Text[] texts = bar.GetComponentsInChildren<TMP_Text>(true);
                for (int j = 0; j < texts.Length; j++)
                {
                    TMP_Text text = texts[j];
                    if (text == null)
                        continue;

                    text.color = bar.valueText == text
                        ? config.statValueTextColor
                        : config.statLabelTextColor;
                }
            }
        }

        private static Color GetStatBackgroundColorForBar(MenuButtonFeedbackConfig config, SegmentedStatBar bar)
        {
            switch (GetStatTypeKey(bar))
            {
                case "speed":
                    return config.speedStatBackgroundColor;
                case "armor":
                    return config.armorStatBackgroundColor;
                case "firepower":
                    return config.firepowerStatBackgroundColor;
                case "handling":
                    return config.handlingStatBackgroundColor;
                default:
                    return config.statBackgroundColor;
            }
        }

        private static Color GetStatFillColorForBar(MenuButtonFeedbackConfig config, SegmentedStatBar bar)
        {
            switch (GetStatTypeKey(bar))
            {
                case "speed":
                    return config.speedStatFillColor;
                case "armor":
                    return config.armorStatFillColor;
                case "firepower":
                    return config.firepowerStatFillColor;
                case "handling":
                    return config.handlingStatFillColor;
                default:
                    return config.statFillColor;
            }
        }

        private static string GetStatTypeKey(SegmentedStatBar bar)
        {
            if (bar == null)
                return string.Empty;

            string currentName = bar.gameObject.name.ToLowerInvariant();
            string parentName = bar.transform.parent != null ? bar.transform.parent.name.ToLowerInvariant() : string.Empty;
            string combined = currentName + " " + parentName;

            if (combined.Contains("speed"))
                return "speed";
            if (combined.Contains("armor"))
                return "armor";
            if (combined.Contains("firepower"))
                return "firepower";
            if (combined.Contains("handling"))
                return "handling";

            return string.Empty;
        }

        private static void TintBarChildren(RectTransform root, Color color)
        {
            if (root == null)
                return;

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null)
                    image.color = color;
            }
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
            feedback.targetButtonImage = button.targetGraphic as Image;
            feedback.Configure(
                config != null ? config.normalTextColor : new Color(0.96f, 0.86f, 0.67f, 1f),
                config != null ? config.hoverTextColor : new Color(1f, 0.74f, 0.37f, 1f),
                config != null ? config.pressedTextColor : new Color(1f, 0.96f, 0.87f, 1f),
                config != null ? config.normalButtonColor : new Color(0.27f, 0.17f, 0.1f, 0.88f),
                config != null ? config.hoverButtonColor : new Color(0.39f, 0.24f, 0.12f, 0.92f),
                config != null ? config.pressedButtonColor : new Color(0.54f, 0.31f, 0.13f, 0.96f),
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
