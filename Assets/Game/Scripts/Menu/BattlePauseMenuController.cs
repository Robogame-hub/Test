using System;
using System.Collections.Generic;
using System.IO;
using TankGame.Session;
using TankGame.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TankGame.Menu
{
    public class BattlePauseMenuController : MonoBehaviour
    {
        private const string CoreSceneName = "Core";
        private const string MainMenuSceneName = "MainMenu";
        private const string CoreScenePath = "Assets/Scenes/Core.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        private static BattlePauseMenuController instance;
        private static int lastHandledEscapeFrame = -1;

        [Header("Button Feedback")]
        [SerializeField] private AudioSource buttonFeedbackAudioSource;
        [Header("Shared Feedback Config")]
        [SerializeField] private MenuButtonFeedbackConfig sharedButtonFeedbackConfig;

        private GameObject battleUiRoot;
        private GameObject menuPanel;
        private GameObject settingsPanel;
        private Button restartButton;
        private Button settingsButton;
        private Button mainMenuButton;
        private Button desktopButton;
        private Button languagePrevButton;
        private Button languageNextButton;
        private Button backFromSettingsButton;
        private TMP_Text languageValueText;

        private Slider masterSensitivitySlider;
        private Slider horizontalSensitivitySlider;
        private Slider verticalSensitivitySlider;
        private Slider masterVolumeSlider;
        private Slider musicVolumeSlider;
        private Slider sfxVolumeSlider;

        private TMP_Text masterSensitivityValueText;
        private TMP_Text horizontalSensitivityValueText;
        private TMP_Text verticalSensitivityValueText;
        private TMP_Text masterVolumeValueText;
        private TMP_Text musicVolumeValueText;
        private TMP_Text sfxVolumeValueText;

        private InputSettings inputSettings;
        private AudioSettings audioSettings;
        private bool isMenuVisible;
        private bool isInitializing;
        private bool battleUiWasActiveBeforeMenu;
        private readonly Dictionary<Transform, bool> battleUiChildStates = new Dictionary<Transform, bool>(32);

        public static bool TryHandleEscapePressed()
        {
            if (instance == null || !instance.isActiveAndEnabled)
                return false;

            if (lastHandledEscapeFrame == Time.frameCount)
                return true;

            lastHandledEscapeFrame = Time.frameCount;
            instance.ToggleMenu();
            return true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            battleUiRoot = GameObject.Find("UIManager");
            if (!BindSceneUi())
            {
                Debug.LogWarning("[BattlePauseMenuController] Pause menu UI not found in scene. Create it via TankGame/Scenes/Create Core Pause Menu Panel.");
                enabled = false;
                return;
            }

            inputSettings = InputSettings.Instance;
            audioSettings = AudioSettings.Instance;

            ApplySharedButtonFeedbackConfig();
            SetupButtonFeedbacks();
            InitSettingsPanel();
            RefreshRestartButtonVisibility();
            SetMenuVisible(false);
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += RefreshLanguageValue;
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= RefreshLanguageValue;
        }

        private bool BindSceneUi()
        {
            menuPanel = FindSceneObjectByName("PauseMenuPanel");
            settingsPanel = FindSceneObjectByName("PauseSettingsPanel");
            if (menuPanel == null || settingsPanel == null)
                return false;

            restartButton = FindInChildrenByName<Button>(menuPanel.transform, "RestartButton");
            settingsButton = FindInChildrenByName<Button>(menuPanel.transform, "SettingsButton");
            mainMenuButton = FindInChildrenByName<Button>(menuPanel.transform, "MainMenuButton");
            desktopButton = FindInChildrenByName<Button>(menuPanel.transform, "DesktopButton");

            Rebind(settingsButton, OpenSettingsPanel);
            Rebind(mainMenuButton, OnBackToMainMenuClicked);
            Rebind(desktopButton, OnExitDesktopClicked);
            Rebind(restartButton, OnRestartClicked);

            masterSensitivitySlider = FindFirstInChildrenByNames<Slider>(settingsPanel.transform, "MasterSensitivitySlider", "MasterSensSlider");
            horizontalSensitivitySlider = FindFirstInChildrenByNames<Slider>(settingsPanel.transform, "HorizontalSensitivitySlider", "HorizontalSensSlider");
            verticalSensitivitySlider = FindFirstInChildrenByNames<Slider>(settingsPanel.transform, "VerticalSensitivitySlider", "VerticalSensSlider");
            masterVolumeSlider = FindFirstInChildrenByNames<Slider>(settingsPanel.transform, "MasterVolumeSlider");
            musicVolumeSlider = FindFirstInChildrenByNames<Slider>(settingsPanel.transform, "MusicVolumeSlider");
            sfxVolumeSlider = FindFirstInChildrenByNames<Slider>(settingsPanel.transform, "SfxVolumeSlider");

            masterSensitivityValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "MasterSensitivityValue");
            horizontalSensitivityValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "HorizontalSensitivityValue");
            verticalSensitivityValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "VerticalSensitivityValue");
            masterVolumeValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "MasterVolumeValue");
            musicVolumeValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "MusicVolumeValue");
            sfxVolumeValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "SfxVolumeValue");
            languageValueText = FindFirstInChildrenByNames<TMP_Text>(settingsPanel.transform, "LanguageValue");

            languagePrevButton = FindInChildrenByName<Button>(settingsPanel.transform, "LanguagePrevButton");
            languageNextButton = FindInChildrenByName<Button>(settingsPanel.transform, "LanguageNextButton");
            backFromSettingsButton = FindInChildrenByName<Button>(settingsPanel.transform, "BackFromSettingsButton");

            Rebind(languagePrevButton, OnPrevLanguage);
            Rebind(languageNextButton, OnNextLanguage);
            Rebind(backFromSettingsButton, CloseSettingsPanel);

            HideUnsupportedSensitivityControls();

            return settingsButton != null
                && mainMenuButton != null
                && desktopButton != null;
        }

        private static void Rebind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void InitSettingsPanel()
        {
            if (inputSettings == null || audioSettings == null)
                return;

            isInitializing = true;

            NormalizeSliderVisual(horizontalSensitivitySlider);
            NormalizeSliderVisual(masterVolumeSlider);
            NormalizeSliderVisual(musicVolumeSlider);
            NormalizeSliderVisual(sfxVolumeSlider);

            SetupSensitivitySlider(horizontalSensitivitySlider, inputSettings.HorizontalSensitivity, horizontalSensitivityValueText);

            SetupVolumeSlider(masterVolumeSlider, audioSettings.MasterVolume, masterVolumeValueText);
            SetupVolumeSlider(musicVolumeSlider, audioSettings.MusicVolume, musicVolumeValueText);
            SetupVolumeSlider(sfxVolumeSlider, audioSettings.SfxVolume, sfxVolumeValueText);

            if (horizontalSensitivitySlider != null)
                horizontalSensitivitySlider.onValueChanged.AddListener(v => OnSensitivityChanged(v, t => inputSettings.HorizontalSensitivity = t, horizontalSensitivityValueText));

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(v => OnVolumeChanged(v, t => audioSettings.MasterVolume = t, masterVolumeValueText));
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(v => OnVolumeChanged(v, t => audioSettings.MusicVolume = t, musicVolumeValueText));
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(v => OnVolumeChanged(v, t => audioSettings.SfxVolume = t, sfxVolumeValueText));

            RefreshLanguageValue();
            isInitializing = false;
        }

        private static void NormalizeSliderVisual(Slider slider)
        {
            if (slider == null)
                return;

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

            if (slider.targetGraphic == null && slider.handleRect != null)
            {
                Graphic g = slider.handleRect.GetComponent<Graphic>();
                if (g != null)
                    slider.targetGraphic = g;
            }

            // Enforce sane runtime geometry/colors for sliders in case scene layout was broken.
            slider.interactable = true;

            if (slider.fillRect != null)
            {
                slider.fillRect.anchorMin = new Vector2(0f, 0f);
                slider.fillRect.anchorMax = new Vector2(1f, 1f);
                slider.fillRect.offsetMin = Vector2.zero;
                slider.fillRect.offsetMax = Vector2.zero;
            }

            Transform fillArea = slider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                RectTransform fillAreaRt = fillArea as RectTransform;
                if (fillAreaRt != null)
                {
                    fillAreaRt.anchorMin = new Vector2(0f, 0f);
                    fillAreaRt.anchorMax = new Vector2(1f, 1f);
                    fillAreaRt.offsetMin = new Vector2(10f, 6f);
                    fillAreaRt.offsetMax = new Vector2(-10f, -6f);
                }
            }

            if (slider.handleRect != null)
            {
                slider.handleRect.anchorMin = new Vector2(0f, 0.5f);
                slider.handleRect.anchorMax = new Vector2(0f, 0.5f);
                slider.handleRect.pivot = new Vector2(0.5f, 0.5f);
                if (slider.handleRect.sizeDelta.y < 18f)
                    slider.handleRect.sizeDelta = new Vector2(16f, 22f);
            }

            Transform background = slider.transform.Find("Background");
            if (background != null)
            {
                Image bgImage = background.GetComponent<Image>();
                if (bgImage != null)
                {
                    Color c = bgImage.color;
                    bgImage.color = new Color(c.r, c.g, c.b, Mathf.Max(c.a, 0.28f));
                    bgImage.raycastTarget = false;
                }
            }
        }

        private void SetupSensitivitySlider(Slider slider, float value, TMP_Text label)
        {
            if (slider == null || inputSettings == null)
                return;

            slider.minValue = inputSettings.MinSensitivity;
            slider.maxValue = inputSettings.MaxSensitivity;
            slider.value = value;
            if (label != null) label.text = value.ToString("F2");
        }

        private static void SetupVolumeSlider(Slider slider, float value, TMP_Text label)
        {
            if (slider == null)
                return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            if (label != null) label.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private void OnSensitivityChanged(float value, Action<float> setter, TMP_Text label)
        {
            if (isInitializing) return;
            setter?.Invoke(value);
            if (label != null) label.text = value.ToString("F2");
        }

        private void OnVolumeChanged(float value, Action<float> setter, TMP_Text label)
        {
            if (isInitializing) return;
            setter?.Invoke(value);
            if (label != null) label.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private void SetupButtonFeedbacks()
        {
            EnsureButtonFeedbackAudioSource();
            ConfigureButtonFeedback(restartButton);
            ConfigureButtonFeedback(settingsButton);
            ConfigureButtonFeedback(mainMenuButton);
            ConfigureButtonFeedback(desktopButton);
            ConfigureButtonFeedback(languagePrevButton);
            ConfigureButtonFeedback(languageNextButton);
            ConfigureButtonFeedback(backFromSettingsButton);
        }

        private void EnsureButtonFeedbackAudioSource()
        {
            if (buttonFeedbackAudioSource != null)
                return;

            buttonFeedbackAudioSource = GetComponent<AudioSource>();
            if (buttonFeedbackAudioSource == null)
                buttonFeedbackAudioSource = gameObject.AddComponent<AudioSource>();

            buttonFeedbackAudioSource.playOnAwake = false;
            buttonFeedbackAudioSource.loop = false;
            buttonFeedbackAudioSource.spatialBlend = 0f;
        }

        private void ConfigureButtonFeedback(Button button)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.color = GetButtonNormalColor();

            MenuButtonFeedback feedback = button.GetComponent<MenuButtonFeedback>();
            if (feedback == null)
                feedback = button.gameObject.AddComponent<MenuButtonFeedback>();

            feedback.button = button;
            feedback.targetText = text;
            feedback.Configure(
                GetButtonNormalColor(),
                GetButtonHoverColor(),
                GetButtonPressedColor(),
                GetButtonHoverScale(),
                GetButtonScaleLerpSpeed(),
                buttonFeedbackAudioSource,
                GetButtonHoverSound(),
                GetButtonClickSound());
        }

        private void ApplySharedButtonFeedbackConfig()
        {
            if (sharedButtonFeedbackConfig == null)
                sharedButtonFeedbackConfig = MenuButtonFeedbackConfig.LoadDefault();
        }

        private Color GetButtonNormalColor()
        {
            MenuButtonFeedbackConfig cfg = sharedButtonFeedbackConfig;
            return cfg != null ? cfg.normalTextColor : new Color32(0x0F, 0xF3, 0x00, 0xFF);
        }

        private Color GetButtonHoverColor()
        {
            MenuButtonFeedbackConfig cfg = sharedButtonFeedbackConfig;
            return cfg != null ? cfg.hoverTextColor : Color.red;
        }

        private Color GetButtonPressedColor()
        {
            MenuButtonFeedbackConfig cfg = sharedButtonFeedbackConfig;
            return cfg != null ? cfg.pressedTextColor : Color.white;
        }

        private float GetButtonHoverScale()
        {
            MenuButtonFeedbackConfig cfg = sharedButtonFeedbackConfig;
            return cfg != null ? Mathf.Max(1f, cfg.hoverTextScale) : 1.08f;
        }

        private float GetButtonScaleLerpSpeed()
        {
            MenuButtonFeedbackConfig cfg = sharedButtonFeedbackConfig;
            return cfg != null ? Mathf.Max(1f, cfg.scaleLerpSpeed) : 16f;
        }

        private AudioClip GetButtonHoverSound()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.hoverSound : null;
        }

        private AudioClip GetButtonClickSound()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.clickSound : null;
        }

        private void HideUnsupportedSensitivityControls()
        {
            HideControlByName(settingsPanel, "MasterSensitivityRow");
            HideControlByName(settingsPanel, "MasterSensitivitySlider");
            HideControlByName(settingsPanel, "MasterSensitivityValue");
            HideControlByName(settingsPanel, "MasterSensitivityLabel");
            HideControlByName(settingsPanel, "VerticalSensitivityRow");
            HideControlByName(settingsPanel, "VerticalSensitivitySlider");
            HideControlByName(settingsPanel, "VerticalSensitivityValue");
            HideControlByName(settingsPanel, "VerticalSensitivityLabel");

            if (masterSensitivitySlider != null)
                masterSensitivitySlider.gameObject.SetActive(false);
            if (verticalSensitivitySlider != null)
                verticalSensitivitySlider.gameObject.SetActive(false);
            if (masterSensitivityValueText != null)
                masterSensitivityValueText.gameObject.SetActive(false);
            if (verticalSensitivityValueText != null)
                verticalSensitivityValueText.gameObject.SetActive(false);
        }

        private static void HideControlByName(GameObject root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
                return;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].name, name, StringComparison.OrdinalIgnoreCase))
                {
                    all[i].gameObject.SetActive(false);
                    return;
                }
            }
        }

        private void ToggleMenu()
        {
            SetMenuVisible(!isMenuVisible);
        }

        private void SetMenuVisible(bool visible)
        {
            bool wasVisible = isMenuVisible;
            isMenuVisible = visible;
            menuPanel.SetActive(visible);
            settingsPanel.SetActive(false);

            if (visible)
            {
                HideBattleUiForPause();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if (wasVisible)
                    RestoreBattleUiAfterPause();

                Tank.TankInputHandler[] handlers = FindObjectsOfType<Tank.TankInputHandler>(true);
                for (int i = 0; i < handlers.Length; i++)
                {
                    if (handlers[i] != null)
                        handlers[i].ForceLockCursor();
                }
            }
        }

        private void HideBattleUiForPause()
        {
            if (battleUiRoot == null)
                return;

            battleUiWasActiveBeforeMenu = battleUiRoot.activeSelf;
            if (!battleUiWasActiveBeforeMenu)
                return;

            bool pauseInsideBattleUi = menuPanel != null
                && menuPanel.transform.IsChildOf(battleUiRoot.transform);

            if (!pauseInsideBattleUi)
            {
                battleUiRoot.SetActive(false);
                return;
            }

            battleUiChildStates.Clear();
            Transform root = battleUiRoot.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                bool containsPauseUi = menuPanel.transform.IsChildOf(child)
                    || settingsPanel.transform.IsChildOf(child);
                if (containsPauseUi)
                    continue;

                battleUiChildStates[child] = child.gameObject.activeSelf;
                child.gameObject.SetActive(false);
            }
        }

        private void RestoreBattleUiAfterPause()
        {
            if (battleUiRoot == null)
                return;

            if (battleUiRoot.activeSelf != battleUiWasActiveBeforeMenu)
                battleUiRoot.SetActive(battleUiWasActiveBeforeMenu);

            if (!battleUiWasActiveBeforeMenu)
                return;

            if (battleUiChildStates.Count == 0)
                return;

            foreach (KeyValuePair<Transform, bool> state in battleUiChildStates)
            {
                if (state.Key != null)
                    state.Key.gameObject.SetActive(state.Value);
            }

            battleUiChildStates.Clear();
        }

        private void OpenSettingsPanel()
        {
            menuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void CloseSettingsPanel()
        {
            settingsPanel.SetActive(false);
            menuPanel.SetActive(true);
        }

        private void RefreshRestartButtonVisibility()
        {
            if (restartButton == null)
                return;

            MatchStartMode mode = GameSessionSettings.StartMode;
            restartButton.gameObject.SetActive(mode == MatchStartMode.Sandbox || mode == MatchStartMode.SoloWithBots);
        }

        private void RefreshLanguageValue()
        {
            if (languageValueText != null)
                languageValueText.text = LocalizationService.GetLanguageNativeName(LocalizationService.CurrentLanguage);
            RefreshRestartButtonVisibility();
        }

        private void OnPrevLanguage()
        {
            int next = (int)LocalizationService.CurrentLanguage - 1;
            if (next < 0) next = 4;
            LocalizationService.CurrentLanguage = (GameLanguage)next;
            RefreshLanguageValue();
        }

        private void OnNextLanguage()
        {
            int next = ((int)LocalizationService.CurrentLanguage + 1) % 5;
            LocalizationService.CurrentLanguage = (GameLanguage)next;
            RefreshLanguageValue();
        }

        private void OnRestartClicked()
        {
            SetMenuVisible(false);
            LoadSceneWithFallback(CoreSceneName, CoreScenePath, "BattlePauseMenuController");
        }

        private void OnBackToMainMenuClicked()
        {
            SetMenuVisible(false);
            LoadSceneWithFallback(MainMenuSceneName, MainMenuScenePath, "BattlePauseMenuController");
        }

        private void OnExitDesktopClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
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

        private static T FindInChildrenByName<T>(Transform root, string name) where T : Component
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
                return null;
            T[] all = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            }
            return null;
        }

        private static T FindFirstInChildrenByNames<T>(Transform root, params string[] names) where T : Component
        {
            if (names == null || names.Length == 0)
                return null;

            for (int i = 0; i < names.Length; i++)
            {
                T found = FindInChildrenByName<T>(root, names[i]);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static GameObject FindSceneObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            GameObject active = GameObject.Find(objectName);
            if (active != null)
                return active;

            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform tr = all[i];
                if (tr == null)
                    continue;

                GameObject go = tr.gameObject;
                if (go == null || !string.Equals(go.name, objectName, StringComparison.Ordinal))
                    continue;

                if (!go.scene.IsValid())
                    continue;

                return go;
            }

            return null;
        }
    }
}
