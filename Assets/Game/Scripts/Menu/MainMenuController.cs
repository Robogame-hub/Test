using System.IO;
using TankGame.Session;
using TankGame.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace TankGame.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [Tooltip("РРјСЏ СЃС†РµРЅС‹ Р»РѕР±Р±Рё РґР»СЏ РєРЅРѕРїРєРё 'РРіСЂР°С‚СЊ'.")]
        public string lobbySceneName = "Lobby";
        [Tooltip("РРјСЏ СЃС†РµРЅС‹ РїРµСЃРѕС‡РЅРёС†С‹/РјР°С‚С‡Р° РґР»СЏ РєРЅРѕРїРєРё 'РџРµСЃРѕС‡РЅРёС†Р°'.")]
        public string sandboxSceneName = "Core";

        [Header("Panels")]
        [Tooltip("Р›РµРІР°СЏ РїР°РЅРµР»СЊ РјРµРЅСЋ, РіРґРµ РЅР°С…РѕРґСЏС‚СЃСЏ РѕСЃРЅРѕРІРЅС‹Рµ РєРЅРѕРїРєРё.")]
        public GameObject mainPanel;
        [Tooltip("РџР°РЅРµР»СЊ РЅР°СЃС‚СЂРѕРµРє (СЃРµРЅСЃР°/Р·РІСѓРє/СЏР·С‹Рє).")]
        public GameObject settingsPanel;

        [Header("Menu Buttons")]
        [Tooltip("РљРЅРѕРїРєР° РїРµСЂРµС…РѕРґР° РІ Р»РѕР±Р±Рё.")]
        public Button playButton;
        [Tooltip("РљРЅРѕРїРєР° Р±С‹СЃС‚СЂРѕРіРѕ СЃС‚Р°СЂС‚Р° РІ РїРµСЃРѕС‡РЅРёС†Сѓ.")]
        public Button sandboxButton;
        [Tooltip("РљРЅРѕРїРєР° РѕС‚РєСЂС‹С‚РёСЏ РїР°РЅРµР»Рё РЅР°СЃС‚СЂРѕРµРє.")]
        public Button settingsButton;
        [Tooltip("РљРЅРѕРїРєР° РІС‹С…РѕРґР° РёР· РёРіСЂС‹.")]
        public Button exitButton;
        [Tooltip("РљРЅРѕРїРєР° РІРѕР·РІСЂР°С‚Р° РёР· РЅР°СЃС‚СЂРѕРµРє РІ РіР»Р°РІРЅРѕРµ РјРµРЅСЋ.")]
        public Button backFromSettingsButton;

        [Header("Menu Text Color")]
        [Tooltip("Р¦РІРµС‚ С‚РµРєСЃС‚Р° РєРЅРѕРїРѕРє РіР»Р°РІРЅРѕРіРѕ РјРµРЅСЋ.")]
        public Color menuButtonTextColor = new Color32(0x0F, 0xF3, 0x00, 0xFF);
        [Header("Button Feedback")]
        [Tooltip("Источник звука для фидбека кнопок (hover/click).")]
        public AudioSource buttonFeedbackAudioSource;
        [Tooltip("Звук при наведении на кнопку.")]
        public AudioClip buttonHoverSound;
        [Tooltip("Звук при нажатии на кнопку.")]
        public AudioClip buttonClickSound;
        [Tooltip("Цвет текста кнопки при наведении.")]
        public Color buttonHoverTextColor = Color.red;
        [Tooltip("Цвет текста кнопки при нажатии.")]
        public Color buttonPressedTextColor = Color.white;
        [Tooltip("Множитель масштаба текста при наведении.")]
        [Min(1f)]
        public float buttonHoverTextScale = 1.08f;
        [Tooltip("Скорость анимации масштаба текста кнопки.")]
        [Min(1f)]
        public float buttonScaleLerpSpeed = 16f;

        [Header("Sensitivity")]
        [Tooltip("РЎР»Р°Р№РґРµСЂ РѕР±С‰РµР№ С‡СѓРІСЃС‚РІРёС‚РµР»СЊРЅРѕСЃС‚Рё СѓРїСЂР°РІР»РµРЅРёСЏ.")]
        public Slider masterSensitivitySlider;
        [Tooltip("РЎР»Р°Р№РґРµСЂ РіРѕСЂРёР·РѕРЅС‚Р°Р»СЊРЅРѕР№ С‡СѓРІСЃС‚РІРёС‚РµР»СЊРЅРѕСЃС‚Рё.")]
        public Slider horizontalSensitivitySlider;
        [Tooltip("РЎР»Р°Р№РґРµСЂ РІРµСЂС‚РёРєР°Р»СЊРЅРѕР№ С‡СѓРІСЃС‚РІРёС‚РµР»СЊРЅРѕСЃС‚Рё.")]
        public Slider verticalSensitivitySlider;

        [Header("Sound")]
        [Tooltip("РЎР»Р°Р№РґРµСЂ РѕР±С‰РµР№ РіСЂРѕРјРєРѕСЃС‚Рё.")]
        public Slider masterVolumeSlider;
        [Tooltip("РЎР»Р°Р№РґРµСЂ РіСЂРѕРјРєРѕСЃС‚Рё РјСѓР·С‹РєРё.")]
        public Slider musicVolumeSlider;
        [Tooltip("РЎР»Р°Р№РґРµСЂ РіСЂРѕРјРєРѕСЃС‚Рё СЌС„С„РµРєС‚РѕРІ (SFX).")]
        public Slider sfxVolumeSlider;

        [Header("Language")]
        [Tooltip("РљРЅРѕРїРєР° РїРµСЂРµРєР»СЋС‡РµРЅРёСЏ СЏР·С‹РєР° РІР»РµРІРѕ.")]
        public Button languagePrevButton;
        [Tooltip("РљРЅРѕРїРєР° РїРµСЂРµРєР»СЋС‡РµРЅРёСЏ СЏР·С‹РєР° РІРїСЂР°РІРѕ.")]
        public Button languageNextButton;
        [Tooltip("РўРµРєСЃС‚ СЃ С‚РµРєСѓС‰РёРј РІС‹Р±СЂР°РЅРЅС‹Рј СЏР·С‹РєРѕРј.")]
        public TMP_Text languageValueText;

        [Header("Tank")]
        [Tooltip("РљРѕРЅС‚СЂРѕР»Р»РµСЂ РІС‹Р±РѕСЂР° С‚Р°РЅРєР° РЅР° РїСЂР°РІРѕР№ РїР°РЅРµР»Рё.")]
        public TankSelectionController tankSelection;

        private InputSettings inputSettings;
        private AudioSettings audioSettings;
        private bool isInitializing;

        private void Start()
        {
            MenuMusicPlayer.EnsureInstance();
            EnsureSettingsPanelAsSibling();
            inputSettings = InputSettings.Instance;
            audioSettings = AudioSettings.Instance;

            ApplyMenuTextColor();
            SetupButtonFeedbacks();
            HookButtons();
            InitSettingsPanel();
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            UnhookButtons();
            UnhookSettingsEvents();
        }

        private void HookButtons()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (sandboxButton != null) sandboxButton.onClick.AddListener(OnSandboxClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
            if (backFromSettingsButton != null) backFromSettingsButton.onClick.AddListener(ShowMainPanel);

            if (languagePrevButton != null) languagePrevButton.onClick.AddListener(OnPrevLanguage);
            if (languageNextButton != null) languageNextButton.onClick.AddListener(OnNextLanguage);
        }

        private void UnhookButtons()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (sandboxButton != null) sandboxButton.onClick.RemoveListener(OnSandboxClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(ShowSettingsPanel);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
            if (backFromSettingsButton != null) backFromSettingsButton.onClick.RemoveListener(ShowMainPanel);

            if (languagePrevButton != null) languagePrevButton.onClick.RemoveListener(OnPrevLanguage);
            if (languageNextButton != null) languageNextButton.onClick.RemoveListener(OnNextLanguage);
        }

        private void InitSettingsPanel()
        {
            isInitializing = true;

            if (inputSettings != null)
            {
                ConfigureSensitivitySlider(masterSensitivitySlider, inputSettings.MinSensitivity, inputSettings.MaxSensitivity, inputSettings.MasterSensitivity);
                ConfigureSensitivitySlider(horizontalSensitivitySlider, inputSettings.MinSensitivity, inputSettings.MaxSensitivity, inputSettings.HorizontalSensitivity);
                ConfigureSensitivitySlider(verticalSensitivitySlider, inputSettings.MinSensitivity, inputSettings.MaxSensitivity, inputSettings.VerticalSensitivity);
            }

            if (audioSettings != null)
            {
                ConfigureVolumeSlider(masterVolumeSlider, audioSettings.MasterVolume);
                ConfigureVolumeSlider(musicVolumeSlider, audioSettings.MusicVolume);
                ConfigureVolumeSlider(sfxVolumeSlider, audioSettings.SfxVolume);
            }

            RefreshLanguageValue();
            HookSettingsEvents();

            isInitializing = false;
        }

        private void ConfigureSensitivitySlider(Slider slider, float min, float max, float value)
        {
            if (slider == null)
                return;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
        }

        private void ConfigureVolumeSlider(Slider slider, float value)
        {
            if (slider == null)
                return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
        }

        private void HookSettingsEvents()
        {
            if (masterSensitivitySlider != null) masterSensitivitySlider.onValueChanged.AddListener(OnMasterSensitivityChanged);
            if (horizontalSensitivitySlider != null) horizontalSensitivitySlider.onValueChanged.AddListener(OnHorizontalSensitivityChanged);
            if (verticalSensitivitySlider != null) verticalSensitivitySlider.onValueChanged.AddListener(OnVerticalSensitivityChanged);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private void UnhookSettingsEvents()
        {
            if (masterSensitivitySlider != null) masterSensitivitySlider.onValueChanged.RemoveListener(OnMasterSensitivityChanged);
            if (horizontalSensitivitySlider != null) horizontalSensitivitySlider.onValueChanged.RemoveListener(OnHorizontalSensitivityChanged);
            if (verticalSensitivitySlider != null) verticalSensitivitySlider.onValueChanged.RemoveListener(OnVerticalSensitivityChanged);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        private void ApplyMenuTextColor()
        {
            ApplyButtonTextColor(playButton);
            ApplyButtonTextColor(sandboxButton);
            ApplyButtonTextColor(settingsButton);
            ApplyButtonTextColor(exitButton);
            ApplyButtonTextColor(backFromSettingsButton);
            ApplyButtonTextColor(languagePrevButton);
            ApplyButtonTextColor(languageNextButton);
        }

        private void ApplyButtonTextColor(Button button)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.color = menuButtonTextColor;
        }
        private void SetupButtonFeedbacks()
        {
            EnsureButtonFeedbackAudioSource();
            ConfigureButtonFeedback(playButton);
            ConfigureButtonFeedback(sandboxButton);
            ConfigureButtonFeedback(settingsButton);
            ConfigureButtonFeedback(exitButton);
            ConfigureButtonFeedback(backFromSettingsButton);
            ConfigureButtonFeedback(languagePrevButton);
            ConfigureButtonFeedback(languageNextButton);

            if (tankSelection != null)
            {
                tankSelection.ApplyButtonFeedbackSettings(
                    buttonFeedbackAudioSource,
                    buttonHoverSound,
                    buttonClickSound,
                    menuButtonTextColor,
                    buttonHoverTextColor,
                    buttonPressedTextColor,
                    buttonHoverTextScale,
                    buttonScaleLerpSpeed);
            }
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

            MenuButtonFeedback feedback = button.GetComponent<MenuButtonFeedback>();
            if (feedback == null)
                feedback = button.gameObject.AddComponent<MenuButtonFeedback>();

            feedback.button = button;
            feedback.targetText = button.GetComponentInChildren<TMP_Text>(true);
            feedback.Configure(
                menuButtonTextColor,
                buttonHoverTextColor,
                buttonPressedTextColor,
                buttonHoverTextScale,
                buttonScaleLerpSpeed,
                buttonFeedbackAudioSource,
                buttonHoverSound,
                buttonClickSound);
        }
        private void EnsureSettingsPanelAsSibling()
        {
            if (mainPanel == null || settingsPanel == null)
                return;

            Transform mainParent = mainPanel.transform.parent;
            if (mainParent == null)
                return;

            if (settingsPanel.transform.parent != mainPanel.transform)
                return;

            settingsPanel.transform.SetParent(mainParent, false);

            RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
            RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
            if (mainRect == null || settingsRect == null)
                return;

            settingsRect.anchorMin = mainRect.anchorMin;
            settingsRect.anchorMax = mainRect.anchorMax;
            settingsRect.pivot = mainRect.pivot;
            settingsRect.anchoredPosition = Vector2.zero;
            settingsRect.sizeDelta = Vector2.zero;
            settingsRect.offsetMin = Vector2.zero;
            settingsRect.offsetMax = Vector2.zero;
        }

        public void ShowMainPanel()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (mainPanel != null)
                mainPanel.SetActive(true);
        }

        public void ShowSettingsPanel()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
            if (mainPanel != null)
                mainPanel.SetActive(false);
        }

        private void OnPlayClicked()
        {
            GameSessionSettings.PrepareLobby();
            LoadConfiguredScene(lobbySceneName, "Assets/Scenes/Lobby.unity");
        }

        private void OnSandboxClicked()
        {
            GameSessionSettings.PrepareSandbox();
            LoadConfiguredScene(sandboxSceneName, "Assets/Scenes/Core.unity");
        }

        private void LoadConfiguredScene(string sceneName, string editorFallbackPath)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[MainMenuController] Scene name is empty.");
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(editorFallbackPath) && File.Exists(editorFallbackPath))
            {
                EditorSceneManager.LoadSceneInPlayMode(editorFallbackPath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif

            Debug.LogError($"[MainMenuController] Scene '{sceneName}' is not in active Build Profile/shared scene list and fallback path '{editorFallbackPath}' was not found.");
        }

        private void OnExitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnPrevLanguage()
        {
            int next = (int)LocalizationService.CurrentLanguage - 1;
            if (next < 0)
                next = 4;
            LocalizationService.CurrentLanguage = (GameLanguage)next;
            RefreshLanguageValue();
        }

        private void OnNextLanguage()
        {
            int next = ((int)LocalizationService.CurrentLanguage + 1) % 5;
            LocalizationService.CurrentLanguage = (GameLanguage)next;
            RefreshLanguageValue();
        }

        private void RefreshLanguageValue()
        {
            if (languageValueText != null)
                languageValueText.text = LocalizationService.GetLanguageNativeName(LocalizationService.CurrentLanguage);
        }

        private void OnMasterSensitivityChanged(float value)
        {
            if (isInitializing || inputSettings == null) return;
            inputSettings.MasterSensitivity = value;
        }

        private void OnHorizontalSensitivityChanged(float value)
        {
            if (isInitializing || inputSettings == null) return;
            inputSettings.HorizontalSensitivity = value;
        }

        private void OnVerticalSensitivityChanged(float value)
        {
            if (isInitializing || inputSettings == null) return;
            inputSettings.VerticalSensitivity = value;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (isInitializing || audioSettings == null) return;
            audioSettings.MasterVolume = value;
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (isInitializing || audioSettings == null) return;
            audioSettings.MusicVolume = value;
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (isInitializing || audioSettings == null) return;
            audioSettings.SfxVolume = value;
        }
    }
}















