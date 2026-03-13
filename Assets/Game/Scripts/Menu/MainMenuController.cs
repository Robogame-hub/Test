using TankGame.Session;
using TankGame.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankGame.Menu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [Tooltip("Имя сцены лобби для кнопки 'Играть'.")]
        public string lobbySceneName = "Lobby";
        [Tooltip("Имя сцены песочницы/матча для кнопки 'Песочница'.")]
        public string sandboxSceneName = "Core";

        [Header("Panels")]
        [Tooltip("Левая панель меню, где находятся основные кнопки.")]
        public GameObject mainPanel;
        [Tooltip("Панель настроек (сенса/звук/язык).")]
        public GameObject settingsPanel;

        [Header("Menu Buttons")]
        [Tooltip("Кнопка перехода в лобби.")]
        public Button playButton;
        [Tooltip("Кнопка быстрого старта в песочницу.")]
        public Button sandboxButton;
        [Tooltip("Кнопка открытия панели настроек.")]
        public Button settingsButton;
        [Tooltip("Кнопка выхода из игры.")]
        public Button exitButton;
        [Tooltip("Кнопка возврата из настроек в главное меню.")]
        public Button backFromSettingsButton;

        [Header("Menu Text Color")]
        [Tooltip("Цвет текста кнопок главного меню.")]
        public Color menuButtonTextColor = new Color32(0x0F, 0xF3, 0x00, 0xFF);

        [Header("Sensitivity")]
        [Tooltip("Слайдер общей чувствительности управления.")]
        public Slider masterSensitivitySlider;
        [Tooltip("Слайдер горизонтальной чувствительности.")]
        public Slider horizontalSensitivitySlider;
        [Tooltip("Слайдер вертикальной чувствительности.")]
        public Slider verticalSensitivitySlider;

        [Header("Sound")]
        [Tooltip("Слайдер общей громкости.")]
        public Slider masterVolumeSlider;
        [Tooltip("Слайдер громкости музыки.")]
        public Slider musicVolumeSlider;
        [Tooltip("Слайдер громкости эффектов (SFX).")]
        public Slider sfxVolumeSlider;

        [Header("Language")]
        [Tooltip("Кнопка переключения языка влево.")]
        public Button languagePrevButton;
        [Tooltip("Кнопка переключения языка вправо.")]
        public Button languageNextButton;
        [Tooltip("Текст с текущим выбранным языком.")]
        public TMP_Text languageValueText;

        [Header("Tank")]
        [Tooltip("Контроллер выбора танка на правой панели.")]
        public TankSelectionController tankSelection;

        private InputSettings inputSettings;
        private AudioSettings audioSettings;
        private bool isInitializing;

        private void Start()
        {
            inputSettings = InputSettings.Instance;
            audioSettings = AudioSettings.Instance;

            ApplyMenuTextColor();
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

        public void ShowMainPanel()
        {
            SetMainMenuButtonsVisible(true);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (mainPanel != null && !mainPanel.activeSelf)
                mainPanel.SetActive(true);
        }

        public void ShowSettingsPanel()
        {
            SetMainMenuButtonsVisible(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
            if (mainPanel != null && !mainPanel.activeSelf)
                mainPanel.SetActive(true);
        }

        private void SetMainMenuButtonsVisible(bool visible)
        {
            SetButtonVisible(playButton, visible);
            SetButtonVisible(sandboxButton, visible);
            SetButtonVisible(settingsButton, visible);
            SetButtonVisible(exitButton, visible);
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }

        private void OnPlayClicked()
        {
            GameSessionSettings.PrepareLobby();
            SceneManager.LoadScene(lobbySceneName);
        }

        private void OnSandboxClicked()
        {
            GameSessionSettings.PrepareSandbox();
            SceneManager.LoadScene(sandboxSceneName);
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
