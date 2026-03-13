using System.Collections.Generic;
using TankGame.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankGame.Menu
{
    public class LobbyController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("Имя сцены главного меню для кнопки 'Назад'.")]
        public string mainMenuSceneName = "MainMenu";
        [Tooltip("Имя сцены матча, которая открывается при входе в комнату или в соло.")]
        public string gameSceneName = "Core";

        [Header("UI")]
        [Tooltip("Контейнер (обычно Content в ScrollView) для строк комнат.")]
        public Transform roomListContainer;
        [Tooltip("Префаб/шаблон строки комнаты с текстом и кнопкой входа.")]
        public GameObject roomEntryPrefab;
        [Tooltip("Текст-заглушка, который показывается когда список комнат пуст.")]
        public TMP_Text emptyRoomsText;
        [Tooltip("Кнопка ручного обновления списка комнат.")]
        public Button refreshButton;
        [Tooltip("Кнопка создания новой комнаты (локальная заглушка).")]
        public Button createRoomButton;
        [Tooltip("Кнопка запуска одиночной игры с ботами.")]
        public Button playSoloButton;
        [Tooltip("Кнопка возврата в главное меню.")]
        public Button backButton;

        [Header("Nickname")]
        [Tooltip("Поле ввода ника игрока.")]
        public TMP_InputField nicknameInputField;

        [Header("Solo")]
        [Tooltip("Количество ботов при нажатии 'Играть одному'.")]
        public int soloBotCount = 3;

        private readonly List<string> roomNames = new List<string>();

        private void Start()
        {
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
                    joinButton.onClick.RemoveAllListeners();
                    joinButton.onClick.AddListener(() => JoinRoom(roomName));
                }
            }
        }

        private void OnNicknameChanged(string value)
        {
            GameSessionSettings.PlayerNickname = value;
        }

        private void JoinRoom(string roomName)
        {
            GameSessionSettings.PrepareLobby();
            SceneManager.LoadScene(gameSceneName);
        }

        private void PlaySolo()
        {
            GameSessionSettings.PrepareSolo(Mathf.Max(1, soloBotCount));
            SceneManager.LoadScene(gameSceneName);
        }

        private void BackToMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
