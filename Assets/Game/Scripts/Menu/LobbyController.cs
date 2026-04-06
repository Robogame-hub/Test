using System.Collections.Generic;
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
    public class LobbyController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("РРјСЏ СЃС†РµРЅС‹ РіР»Р°РІРЅРѕРіРѕ РјРµРЅСЋ РґР»СЏ РєРЅРѕРїРєРё 'РќР°Р·Р°Рґ'.")]
        public string mainMenuSceneName = "MainMenu";
        [Tooltip("РРјСЏ СЃС†РµРЅС‹ РјР°С‚С‡Р°, РєРѕС‚РѕСЂР°СЏ РѕС‚РєСЂС‹РІР°РµС‚СЃСЏ РїСЂРё РІС…РѕРґРµ РІ РєРѕРјРЅР°С‚Сѓ РёР»Рё РІ СЃРѕР»Рѕ.")]
        public string gameSceneName = "Core";

        [Header("UI")]
        [Tooltip("РљРѕРЅС‚РµР№РЅРµСЂ (РѕР±С‹С‡РЅРѕ Content РІ ScrollView) РґР»СЏ СЃС‚СЂРѕРє РєРѕРјРЅР°С‚.")]
        public Transform roomListContainer;
        [Tooltip("РџСЂРµС„Р°Р±/С€Р°Р±Р»РѕРЅ СЃС‚СЂРѕРєРё РєРѕРјРЅР°С‚С‹ СЃ С‚РµРєСЃС‚РѕРј Рё РєРЅРѕРїРєРѕР№ РІС…РѕРґР°.")]
        public GameObject roomEntryPrefab;
        [Tooltip("РўРµРєСЃС‚-Р·Р°РіР»СѓС€РєР°, РєРѕС‚РѕСЂС‹Р№ РїРѕРєР°Р·С‹РІР°РµС‚СЃСЏ РєРѕРіРґР° СЃРїРёСЃРѕРє РєРѕРјРЅР°С‚ РїСѓСЃС‚.")]
        public TMP_Text emptyRoomsText;
        [Tooltip("РљРЅРѕРїРєР° СЂСѓС‡РЅРѕРіРѕ РѕР±РЅРѕРІР»РµРЅРёСЏ СЃРїРёСЃРєР° РєРѕРјРЅР°С‚.")]
        public Button refreshButton;
        [Tooltip("РљРЅРѕРїРєР° СЃРѕР·РґР°РЅРёСЏ РЅРѕРІРѕР№ РєРѕРјРЅР°С‚С‹ (Р»РѕРєР°Р»СЊРЅР°СЏ Р·Р°РіР»СѓС€РєР°).")]
        public Button createRoomButton;
        [Tooltip("РљРЅРѕРїРєР° РІРѕР·РІСЂР°С‚Р° РІ РіР»Р°РІРЅРѕРµ РјРµРЅСЋ.")]
        public Button backButton;
        [Header("Button Feedback")]
        [Tooltip("Источник звука для фидбека кнопок (hover/click).")]
        public AudioSource buttonFeedbackAudioSource;
        [Header("Shared Feedback Config")]
        [Tooltip("Общий конфиг параметров фидбека кнопок. Если не задан, пробуем загрузить Resources/Menu/MenuButtonFeedbackConfig.")]
        public MenuButtonFeedbackConfig sharedButtonFeedbackConfig;

        [Header("Nickname")]
        [Tooltip("РџРѕР»Рµ РІРІРѕРґР° РЅРёРєР° РёРіСЂРѕРєР°.")]
        public TMP_InputField nicknameInputField;

        private readonly List<string> roomNames = new List<string>();

        private void Start()
        {
            MenuMusicPlayer.EnsureInstance();
            ApplySharedButtonFeedbackConfig();
            SetupButtonFeedbacks();

            if (refreshButton != null) refreshButton.onClick.AddListener(RefreshRooms);
            if (createRoomButton != null) createRoomButton.onClick.AddListener(CreateRoom);
            if (backButton != null) backButton.onClick.AddListener(BackToMenu);

            if (nicknameInputField != null)
            {
                nicknameInputField.onValueChanged.AddListener(OnNicknameChanged);
                nicknameInputField.text = GameSessionSettings.PlayerNickname;
            }

            RefreshRooms();
            MenuDesertTheme.ApplyScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(RefreshRooms);
            if (createRoomButton != null) createRoomButton.onClick.RemoveListener(CreateRoom);
            if (backButton != null) backButton.onClick.RemoveListener(BackToMenu);

            if (nicknameInputField != null)
                nicknameInputField.onValueChanged.RemoveListener(OnNicknameChanged);
        }

        public void RefreshRooms()
        {
            roomNames.Clear();
            RebuildRoomList();
        }

        public void CreateRoom()
        {
            roomNames.Add($"Room {roomNames.Count + 1}");
            RebuildRoomList();
        }

        private void RebuildRoomList()
        {
            if (roomListContainer == null)
                return;

            for (int i = roomListContainer.childCount - 1; i >= 0; i--)
                Destroy(roomListContainer.GetChild(i).gameObject);

            if (emptyRoomsText != null)
                emptyRoomsText.gameObject.SetActive(roomNames.Count == 0);

            if (roomEntryPrefab == null)
                return;

            for (int i = 0; i < roomNames.Count; i++)
            {
                string roomName = roomNames[i];
                GameObject row = Instantiate(roomEntryPrefab, roomListContainer);
                row.SetActive(true);
                MenuDesertTheme.ApplyRoomEntry(row);
                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                TMP_FontAsset configuredFont = GetConfiguredUiFont();
                if (texts.Length > 0)
                    texts[0].text = roomName;
                if (configuredFont != null)
                {
                    for (int t = 0; t < texts.Length; t++)
                    {
                        if (texts[t] != null)
                            texts[t].font = configuredFont;
                    }
                }

                Button joinButton = row.GetComponentInChildren<Button>(true);
                if (joinButton != null)
                {
                    ConfigureButtonFeedback(joinButton);
                    joinButton.onClick.RemoveAllListeners();
                    joinButton.onClick.AddListener(() => JoinRoom(roomName));
                }
            }
        }
        private void SetupButtonFeedbacks()
        {
            EnsureButtonFeedbackAudioSource();
            ConfigureButtonFeedback(refreshButton);
            ConfigureButtonFeedback(createRoomButton);
            ConfigureButtonFeedback(backButton);
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
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.normalTextColor : new Color(0.96f, 0.86f, 0.67f, 1f);
        }

        private Color GetButtonHoverColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.hoverTextColor : new Color(1f, 0.74f, 0.37f, 1f);
        }

        private Color GetButtonPressedColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.pressedTextColor : new Color(1f, 0.96f, 0.87f, 1f);
        }

        private float GetButtonHoverScale()
        {
            return sharedButtonFeedbackConfig != null ? Mathf.Max(1f, sharedButtonFeedbackConfig.hoverTextScale) : 1.08f;
        }

        private float GetButtonScaleLerpSpeed()
        {
            return sharedButtonFeedbackConfig != null ? Mathf.Max(1f, sharedButtonFeedbackConfig.scaleLerpSpeed) : 16f;
        }

        private AudioClip GetButtonHoverSound()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.hoverSound : null;
        }

        private AudioClip GetButtonClickSound()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.clickSound : null;
        }

        private TMP_FontAsset GetConfiguredUiFont()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.uiFont : null;
        }
        private void OnNicknameChanged(string value)
        {
            GameSessionSettings.PlayerNickname = value;
        }

        private void JoinRoom(string roomName)
        {
            GameSessionSettings.PrepareLobby();
            LoadConfiguredScene(gameSceneName, "Assets/Scenes/Core.unity");
        }

        private void BackToMenu()
        {
            LoadConfiguredScene(mainMenuSceneName, "Assets/Scenes/MainMenu.unity");
        }

        private static void LoadConfiguredScene(string sceneName, string editorFallbackPath)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[LobbyController] Scene name is empty.");
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

            Debug.LogError($"[LobbyController] Scene '{sceneName}' is not in active Build Profile/shared scene list and fallback path '{editorFallbackPath}' was not found.");
        }
    }
}




