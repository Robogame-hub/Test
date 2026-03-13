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
        [Tooltip("РљРЅРѕРїРєР° Р·Р°РїСѓСЃРєР° РѕРґРёРЅРѕС‡РЅРѕР№ РёРіСЂС‹ СЃ Р±РѕС‚Р°РјРё.")]
        public Button playSoloButton;
        [Tooltip("РљРЅРѕРїРєР° РІРѕР·РІСЂР°С‚Р° РІ РіР»Р°РІРЅРѕРµ РјРµРЅСЋ.")]
        public Button backButton;
        [Header("Button Feedback")]
        [Tooltip("Источник звука для фидбека кнопок (hover/click).")]
        public AudioSource buttonFeedbackAudioSource;
        [Tooltip("Звук при наведении на кнопку.")]
        public AudioClip buttonHoverSound;
        [Tooltip("Звук при нажатии на кнопку.")]
        public AudioClip buttonClickSound;
        [Tooltip("Базовый цвет текста кнопки.")]
        public Color buttonNormalTextColor = new Color32(0x0F, 0xF3, 0x00, 0xFF);
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

        [Header("Nickname")]
        [Tooltip("РџРѕР»Рµ РІРІРѕРґР° РЅРёРєР° РёРіСЂРѕРєР°.")]
        public TMP_InputField nicknameInputField;

        [Header("Solo")]
        [Tooltip("РљРѕР»РёС‡РµСЃС‚РІРѕ Р±РѕС‚РѕРІ РїСЂРё РЅР°Р¶Р°С‚РёРё 'РРіСЂР°С‚СЊ РѕРґРЅРѕРјСѓ'.")]
        public int soloBotCount = 3;

        private readonly List<string> roomNames = new List<string>();

        private void Start()
        {
            MenuMusicPlayer.EnsureInstance();
            SetupButtonFeedbacks();

            if (refreshButton != null) refreshButton.onClick.AddListener(RefreshRooms);
            if (createRoomButton != null) createRoomButton.onClick.AddListener(CreateRoom);
            if (playSoloButton != null) playSoloButton.onClick.AddListener(PlaySolo);
            if (backButton != null) backButton.onClick.AddListener(BackToMenu);

            if (nicknameInputField != null)
            {
                nicknameInputField.onValueChanged.AddListener(OnNicknameChanged);
                nicknameInputField.text = GameSessionSettings.PlayerNickname;
            }

            RefreshRooms();
        }

        private void OnDestroy()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(RefreshRooms);
            if (createRoomButton != null) createRoomButton.onClick.RemoveListener(CreateRoom);
            if (playSoloButton != null) playSoloButton.onClick.RemoveListener(PlaySolo);
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
                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length > 0)
                    texts[0].text = roomName;

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
            ConfigureButtonFeedback(playSoloButton);
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
                buttonNormalTextColor,
                buttonHoverTextColor,
                buttonPressedTextColor,
                buttonHoverTextScale,
                buttonScaleLerpSpeed,
                buttonFeedbackAudioSource,
                buttonHoverSound,
                buttonClickSound);
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

        private void PlaySolo()
        {
            GameSessionSettings.PrepareSolo(GameSessionSettings.MaxPlayers);
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




