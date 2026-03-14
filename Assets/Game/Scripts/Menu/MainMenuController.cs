using System.IO;
using System;
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
        [Tooltip("Панель условий матча для песочницы.")]
        public GameObject sandboxMatchPanel;

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
        [Tooltip("Кнопка старта матча из панели песочницы.")]
        public Button startSandboxMatchButton;
        [Tooltip("Кнопка возврата из панели песочницы.")]
        public Button backFromSandboxMatchButton;

        [Header("Sandbox Match Settings")]
        [Tooltip("Кнопка уменьшения количества ботов (влево).") ]
        public Button sandboxBotsPrevButton;
        [Tooltip("Текст со значением количества ботов в песочнице.")]
        public TMP_Text sandboxBotCountValueText;
        [Tooltip("Кнопка увеличения количества ботов (вправо).") ]
        public Button sandboxBotsNextButton;

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
        private int currentSandboxBotCount;

        private void Start()
        {
            MenuMusicPlayer.EnsureInstance();
            EnsureSettingsPanelAsSibling();
            EnsureSandboxPanelAsSibling();
            EnsureSandboxMatchUiReferences();
            inputSettings = InputSettings.Instance;
            audioSettings = AudioSettings.Instance;

            ApplyMenuTextColor();
            SetupButtonFeedbacks();
            HookButtons();
            InitSettingsPanel();
            InitSandboxMatchPanel();
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
            if (sandboxButton != null) sandboxButton.onClick.AddListener(ShowSandboxMatchPanel);
            if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
            if (backFromSettingsButton != null) backFromSettingsButton.onClick.AddListener(ShowMainPanel);
            if (startSandboxMatchButton != null) startSandboxMatchButton.onClick.AddListener(StartSandboxMatch);
            if (backFromSandboxMatchButton != null) backFromSandboxMatchButton.onClick.AddListener(ShowMainPanel);
            if (sandboxBotsPrevButton != null) sandboxBotsPrevButton.onClick.AddListener(OnSandboxBotCountPrev);
            if (sandboxBotsNextButton != null) sandboxBotsNextButton.onClick.AddListener(OnSandboxBotCountNext);

            if (languagePrevButton != null) languagePrevButton.onClick.AddListener(OnPrevLanguage);
            if (languageNextButton != null) languageNextButton.onClick.AddListener(OnNextLanguage);
        }

        private void UnhookButtons()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (sandboxButton != null) sandboxButton.onClick.RemoveListener(ShowSandboxMatchPanel);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(ShowSettingsPanel);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
            if (backFromSettingsButton != null) backFromSettingsButton.onClick.RemoveListener(ShowMainPanel);
            if (startSandboxMatchButton != null) startSandboxMatchButton.onClick.RemoveListener(StartSandboxMatch);
            if (backFromSandboxMatchButton != null) backFromSandboxMatchButton.onClick.RemoveListener(ShowMainPanel);
            if (sandboxBotsPrevButton != null) sandboxBotsPrevButton.onClick.RemoveListener(OnSandboxBotCountPrev);
            if (sandboxBotsNextButton != null) sandboxBotsNextButton.onClick.RemoveListener(OnSandboxBotCountNext);

            if (languagePrevButton != null) languagePrevButton.onClick.RemoveListener(OnPrevLanguage);
            if (languageNextButton != null) languageNextButton.onClick.RemoveListener(OnNextLanguage);
        }

        private void InitSandboxMatchPanel()
        {
            EnsureSandboxMatchPanelExists();
            EnsureSandboxPanelAsSibling();
            EnsureSandboxMatchUiReferences();
            RebindSandboxMatchButtons();

            int maxBots = Mathf.Max(0, GameSessionSettings.MaxPlayers - 1);
            currentSandboxBotCount = Mathf.Clamp(GameSessionSettings.SandboxBotCount, 0, maxBots);
            GameSessionSettings.SandboxBotCount = currentSandboxBotCount;
            RefreshSandboxBotCountLabel();
        }

        private void OnSandboxBotCountPrev()
        {
            int maxBots = Mathf.Max(0, GameSessionSettings.MaxPlayers - 1);
            currentSandboxBotCount = Mathf.Clamp(currentSandboxBotCount - 1, 0, maxBots);
            GameSessionSettings.SandboxBotCount = currentSandboxBotCount;
            RefreshSandboxBotCountLabel();
        }

        private void OnSandboxBotCountNext()
        {
            int maxBots = Mathf.Max(0, GameSessionSettings.MaxPlayers - 1);
            currentSandboxBotCount = Mathf.Clamp(currentSandboxBotCount + 1, 0, maxBots);
            GameSessionSettings.SandboxBotCount = currentSandboxBotCount;
            RefreshSandboxBotCountLabel();
        }

        private void EnsureSandboxMatchPanelExists()
        {
            if (sandboxMatchPanel != null || mainPanel == null)
                return;

            Transform parent = mainPanel.transform.parent;
            if (parent == null)
                return;

            sandboxMatchPanel = new GameObject("SandboxMatchPanel", typeof(RectTransform), typeof(Image));
            sandboxMatchPanel.transform.SetParent(parent, false);

            RectTransform panelRt = sandboxMatchPanel.GetComponent<RectTransform>();
            RectTransform mainRt = mainPanel.GetComponent<RectTransform>();
            if (mainRt != null)
            {
                panelRt.anchorMin = mainRt.anchorMin;
                panelRt.anchorMax = mainRt.anchorMax;
                panelRt.pivot = mainRt.pivot;
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.sizeDelta = Vector2.zero;
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;
            }

            Image panelImage = sandboxMatchPanel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.45f);

            VerticalLayoutGroup layout = sandboxMatchPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 16f;
            layout.padding = new RectOffset(48, 16, 60, 60);
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            CreateSandboxPanelDefaultUi();
            sandboxMatchPanel.SetActive(false);
        }

        private void EnsureSandboxMatchUiReferences()
        {
            if (sandboxMatchPanel == null)
                return;

            if (sandboxBotsPrevButton == null)
                sandboxBotsPrevButton = FindInChildrenByName<Button>(sandboxMatchPanel.transform, "SandboxBotsPrevButton")
                    ?? FindInChildrenByName<Button>(sandboxMatchPanel.transform, "BotCountPrevButton");

            if (sandboxBotCountValueText == null)
                sandboxBotCountValueText = FindInChildrenByName<TMP_Text>(sandboxMatchPanel.transform, "SandboxBotCountValueText")
                    ?? FindInChildrenByName<TMP_Text>(sandboxMatchPanel.transform, "BotCountValueText");

            if (sandboxBotsNextButton == null)
                sandboxBotsNextButton = FindInChildrenByName<Button>(sandboxMatchPanel.transform, "SandboxBotsNextButton")
                    ?? FindInChildrenByName<Button>(sandboxMatchPanel.transform, "BotCountNextButton");

            if (startSandboxMatchButton == null)
                startSandboxMatchButton = FindInChildrenByName<Button>(sandboxMatchPanel.transform, "StartSandboxMatchButton");

            if (backFromSandboxMatchButton == null)
                backFromSandboxMatchButton = FindInChildrenByName<Button>(sandboxMatchPanel.transform, "BackFromSandboxMatchButton");

            bool hasAll = sandboxBotsPrevButton != null
                && sandboxBotCountValueText != null
                && sandboxBotsNextButton != null
                && startSandboxMatchButton != null
                && backFromSandboxMatchButton != null;

            if (!hasAll)
                CreateSandboxPanelDefaultUi();
        }

        private void CreateSandboxPanelDefaultUi()
        {
            if (sandboxMatchPanel == null)
                return;

            Transform panel = sandboxMatchPanel.transform;
            TMP_Text title = FindInChildrenByName<TMP_Text>(panel, "SandboxTitle");
            if (title == null)
            {
                title = CreateRuntimeLabel(panel, "SandboxTitle", LocalizationService.Get("sandbox.title"), 48f, FontStyles.Bold);
                AddLocalizedKey(title.gameObject, "sandbox.title");
                SetPreferredHeight(title.gameObject, 72f);
            }

            Transform row = panel.Find("SandboxBotCountRow");
            if (row == null)
            {
                GameObject rowObj = new GameObject("SandboxBotCountRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                rowObj.transform.SetParent(panel, false);
                HorizontalLayoutGroup rowLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.spacing = 10f;
                rowLayout.childControlWidth = false;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
                SetPreferredHeight(rowObj, 62f);

                TMP_Text label = CreateRuntimeLabel(rowObj.transform, "SandboxBotCountLabel", LocalizationService.Get("sandbox.bot_count"), 30f, FontStyles.Normal);
                AddLocalizedKey(label.gameObject, "sandbox.bot_count");
                SetPreferredWidth(label.gameObject, 160f);
                row = rowObj.transform;
            }

            if (sandboxBotsPrevButton == null)
                sandboxBotsPrevButton = CreateRuntimeButton(row, "SandboxBotsPrevButton", "<", 70f, 56f, 30f);

            if (sandboxBotCountValueText == null)
            {
                sandboxBotCountValueText = CreateRuntimeLabel(row, "SandboxBotCountValueText", "0", 30f, FontStyles.Bold);
                sandboxBotCountValueText.alignment = TextAlignmentOptions.Center;
                SetPreferredWidth(sandboxBotCountValueText.gameObject, 90f);
            }

            if (sandboxBotsNextButton == null)
                sandboxBotsNextButton = CreateRuntimeButton(row, "SandboxBotsNextButton", ">", 70f, 56f, 30f);

            if (startSandboxMatchButton == null)
            {
                startSandboxMatchButton = CreateRuntimeButton(panel, "StartSandboxMatchButton", LocalizationService.Get("menu.start_match"), 360f, 74f, 28f);
                AddLocalizedKey(startSandboxMatchButton.GetComponentInChildren<TMP_Text>(true)?.gameObject, "menu.start_match");
            }

            if (backFromSandboxMatchButton == null)
            {
                backFromSandboxMatchButton = CreateRuntimeButton(panel, "BackFromSandboxMatchButton", LocalizationService.Get("menu.back"), 260f, 74f, 28f);
                AddLocalizedKey(backFromSandboxMatchButton.GetComponentInChildren<TMP_Text>(true)?.gameObject, "menu.back");
            }
        }

        private void RebindSandboxMatchButtons()
        {
            // Ensure same behavior and feedback as the rest of menu buttons, even if controls were created at runtime.
            if (sandboxBotsPrevButton != null)
            {
                sandboxBotsPrevButton.onClick.RemoveListener(OnSandboxBotCountPrev);
                sandboxBotsPrevButton.onClick.AddListener(OnSandboxBotCountPrev);
                ApplyButtonTextColor(sandboxBotsPrevButton);
                ConfigureButtonFeedback(sandboxBotsPrevButton);
            }

            if (sandboxBotsNextButton != null)
            {
                sandboxBotsNextButton.onClick.RemoveListener(OnSandboxBotCountNext);
                sandboxBotsNextButton.onClick.AddListener(OnSandboxBotCountNext);
                ApplyButtonTextColor(sandboxBotsNextButton);
                ConfigureButtonFeedback(sandboxBotsNextButton);
            }

            if (startSandboxMatchButton != null)
            {
                startSandboxMatchButton.onClick.RemoveListener(StartSandboxMatch);
                startSandboxMatchButton.onClick.AddListener(StartSandboxMatch);
                ApplyButtonTextColor(startSandboxMatchButton);
                ConfigureButtonFeedback(startSandboxMatchButton);
            }

            if (backFromSandboxMatchButton != null)
            {
                backFromSandboxMatchButton.onClick.RemoveListener(ShowMainPanel);
                backFromSandboxMatchButton.onClick.AddListener(ShowMainPanel);
                ApplyButtonTextColor(backFromSandboxMatchButton);
                ConfigureButtonFeedback(backFromSandboxMatchButton);
            }
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            if (target == null)
                return;

            LayoutElement le = target.GetComponent<LayoutElement>();
            if (le == null)
                le = target.AddComponent<LayoutElement>();

            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static void SetPreferredWidth(GameObject target, float width)
        {
            if (target == null)
                return;

            LayoutElement le = target.GetComponent<LayoutElement>();
            if (le == null)
                le = target.AddComponent<LayoutElement>();

            le.minWidth = width;
            le.preferredWidth = width;
        }

        private TMP_Text CreateRuntimeLabel(Transform parent, string name, string text, float fontSize, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            TMP_Text label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = menuButtonTextColor;

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = Mathf.Max(32f, fontSize + 8f);
            le.preferredHeight = Mathf.Max(32f, fontSize + 8f);
            return label;
        }

        private Button CreateRuntimeButton(Transform parent, string name, string text, float width = 320f, float height = 64f, float fontSize = 28f)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(parent, false);
            buttonObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            LayoutElement buttonLe = buttonObj.GetComponent<LayoutElement>();
            buttonLe.minHeight = height;
            buttonLe.preferredHeight = height;
            buttonLe.minWidth = width;
            buttonLe.preferredWidth = width;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TMP_Text tmp = textObj.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = menuButtonTextColor;

            return buttonObj.GetComponent<Button>();
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

        private static void AddLocalizedKey(GameObject target, string key)
        {
            if (target == null || string.IsNullOrWhiteSpace(key))
                return;

            LocalizedText localized = target.GetComponent<LocalizedText>();
            if (localized == null)
                localized = target.AddComponent<LocalizedText>();
            localized.SetKey(key);
        }

        private void RefreshSandboxBotCountLabel()
        {
            if (sandboxBotCountValueText == null)
                return;

            sandboxBotCountValueText.text = currentSandboxBotCount.ToString();
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
            ApplyButtonTextColor(sandboxBotsPrevButton);
            ApplyButtonTextColor(sandboxBotsNextButton);
            ApplyButtonTextColor(startSandboxMatchButton);
            ApplyButtonTextColor(backFromSandboxMatchButton);
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
            ConfigureButtonFeedback(sandboxBotsPrevButton);
            ConfigureButtonFeedback(sandboxBotsNextButton);
            ConfigureButtonFeedback(startSandboxMatchButton);
            ConfigureButtonFeedback(backFromSandboxMatchButton);
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

        private void EnsureSandboxPanelAsSibling()
        {
            if (mainPanel == null || sandboxMatchPanel == null)
                return;

            Transform mainParent = mainPanel.transform.parent;
            if (mainParent == null)
                return;

            if (sandboxMatchPanel.transform.parent == mainParent)
                return;

            sandboxMatchPanel.transform.SetParent(mainParent, false);

            RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
            RectTransform sandboxRect = sandboxMatchPanel.GetComponent<RectTransform>();
            if (mainRect == null || sandboxRect == null)
                return;

            sandboxRect.anchorMin = mainRect.anchorMin;
            sandboxRect.anchorMax = mainRect.anchorMax;
            sandboxRect.pivot = mainRect.pivot;
            sandboxRect.anchoredPosition = Vector2.zero;
            sandboxRect.sizeDelta = Vector2.zero;
            sandboxRect.offsetMin = Vector2.zero;
            sandboxRect.offsetMax = Vector2.zero;
        }

        public void ShowMainPanel()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (sandboxMatchPanel != null)
                sandboxMatchPanel.SetActive(false);
            if (mainPanel != null)
                mainPanel.SetActive(true);
        }

        public void ShowSettingsPanel()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
            if (sandboxMatchPanel != null)
                sandboxMatchPanel.SetActive(false);
            if (mainPanel != null)
                mainPanel.SetActive(false);
        }

        public void ShowSandboxMatchPanel()
        {
            EnsureSandboxMatchPanelExists();
            EnsureSandboxPanelAsSibling();
            EnsureSandboxMatchUiReferences();
            RebindSandboxMatchButtons();

            if (sandboxMatchPanel == null)
            {
                if (mainPanel != null)
                    mainPanel.SetActive(true);
                return;
            }

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            sandboxMatchPanel.SetActive(true);
            if (mainPanel != null)
                mainPanel.SetActive(false);

            // Mark sandbox mode immediately so bot settings survive any legacy persistent scene-load listeners.
            GameSessionSettings.PrepareSandbox(currentSandboxBotCount);
        }

        private void OnPlayClicked()
        {
            GameSessionSettings.PrepareLobby();
            LoadConfiguredScene(lobbySceneName, "Assets/Scenes/Lobby.unity");
        }

        private void StartSandboxMatch()
        {
            int bots = currentSandboxBotCount;
            GameSessionSettings.PrepareSandbox(bots);
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






















