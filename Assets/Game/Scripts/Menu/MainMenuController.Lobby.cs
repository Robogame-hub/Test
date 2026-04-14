using System;
using System.Collections.Generic;
using TMPro;
using TankGame.Session;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankGame.Menu
{
    public partial class MainMenuController
    {
        [Header("Lobby Panels")]
        [Tooltip("Основная панель лобби в MainMenu сцене.")]
        public GameObject lobbyPanel;
        [Tooltip("Панель создания комнаты в MainMenu сцене.")]
        public GameObject lobbyCreatePanel;
        [Tooltip("Панель ввода пароля для комнаты в MainMenu сцене.")]
        public GameObject lobbyPasswordPanel;

        [Header("Lobby Visuals")]
        [SerializeField] private Color lobbyRoomNormalColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color lobbyRoomSelectedColor = new Color(0.24f, 0.14f, 0.08f, 0.88f);

        private TMP_InputField lobbyNicknameInput;
        private Transform lobbyRoomListContent;
        private TMP_Text lobbyNoRoomsText;
        private Button lobbyRefreshButton;
        private Button lobbyCreateButton;
        private Button lobbyConnectButton;
        private Button lobbyBackButton;

        private TMP_InputField createRoomNameInput;
        private TMP_InputField createRoomPasswordInput;
        private GameObject createPasswordInputRoot;
        private Button createPasswordToggleButton;
        private TMP_Text createPasswordToggleText;
        private Button createConnectButton;
        private Button createBackButton;
        private TMP_Text createTitleText;
        private TMP_Text createRoomNameLabel;
        private TMP_Text createRoomPasswordLabel;

        private TMP_InputField passwordInput;
        private TMP_Text passwordTitleText;
        private TMP_Text passwordLabel;
        private TMP_Text passwordErrorText;
        private Button passwordConnectButton;
        private Button passwordBackButton;

        private LobbyMenuConfigData lobbyConfig;
        private readonly List<LobbyRoomData> lobbyRooms = new List<LobbyRoomData>();
        private readonly List<GameObject> lobbyStaticRows = new List<GameObject>();
        private readonly List<LobbyRoomRowView> lobbyRowViews = new List<LobbyRoomRowView>();

        private LobbyRoomData selectedLobbyRoom;
        private LobbyRoomData pendingPasswordRoom;
        private bool createRoomIsPasswordProtected;
        private bool createHasSeparatePasswordInput;

        private sealed class LobbyRoomData
        {
            public string roomName;
            public int currentPlayers;
            public int maxPlayers;
            public bool isPasswordProtected;
            public string password;
        }

        private sealed class LobbyRoomRowView
        {
            public GameObject root;
            public Image background;
            public TMP_Text roomText;
            public Button selectButton;
            public TMP_Text selectButtonText;
            public LobbyRoomData data;
        }

        private void InitializeLobbyMenu()
        {
            lobbyConfig = LobbyMenuConfig.LoadOrDefault();
            EnsureLobbyPanelReferences();
            ResolveLobbyUiReferences();
            ConfigureLobbyPanelsLayout();
            ApplyLobbyLocalizationKeys();
            HookLobbyEvents();

            if (lobbyNicknameInput != null)
            {
                string nickname = string.IsNullOrWhiteSpace(GameSessionSettings.PlayerNickname)
                    ? lobbyConfig.defaultNickname
                    : GameSessionSettings.PlayerNickname;
                lobbyNicknameInput.SetTextWithoutNotify(nickname);
                GameSessionSettings.PlayerNickname = nickname;
            }

            CreateDebugRoomsFromConfig();
            RebuildLobbyRoomList();
            UpdateLobbyConnectState();
            ResetCreatePanelState();
            ResetPasswordPanelState();
            HideAllLobbyPanels();
        }

        private void DisposeLobbyMenu()
        {
            UnhookLobbyEvents();
            ClearLobbyRoomRows();
        }

        public void ShowLobbyPanel()
        {
            EnsureLobbyPanelReferences();

            if (mainPanel != null)
                mainPanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (sandboxMatchPanel != null)
                sandboxMatchPanel.SetActive(false);

            if (lobbyCreatePanel != null)
                lobbyCreatePanel.SetActive(false);
            if (lobbyPasswordPanel != null)
                lobbyPasswordPanel.SetActive(false);
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);

            UpdateLobbyConnectState();
            ApplyLobbySelectionVisuals();
        }

        private void HideAllLobbyPanels()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
            if (lobbyCreatePanel != null)
                lobbyCreatePanel.SetActive(false);
            if (lobbyPasswordPanel != null)
                lobbyPasswordPanel.SetActive(false);
        }

        private void ShowLobbyCreatePanel()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
            if (lobbyPasswordPanel != null)
                lobbyPasswordPanel.SetActive(false);
            if (lobbyCreatePanel != null)
                lobbyCreatePanel.SetActive(true);

            ResetCreatePanelState();
        }

        private void ShowLobbyPasswordPanel(LobbyRoomData targetRoom)
        {
            pendingPasswordRoom = targetRoom;

            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
            if (lobbyCreatePanel != null)
                lobbyCreatePanel.SetActive(false);
            if (lobbyPasswordPanel != null)
                lobbyPasswordPanel.SetActive(true);

            ResetPasswordPanelState();
        }

        private void EnsureLobbyPanelReferences()
        {
            if (lobbyPanel == null)
                lobbyPanel = FindSceneObjectByName("LobbyPanel");
            if (lobbyCreatePanel == null)
                lobbyCreatePanel = FindSceneObjectByName("LobbyCreatePanel");
            if (lobbyPasswordPanel == null)
                lobbyPasswordPanel = FindSceneObjectByName("LobbyPasswordPanel");
        }

        private GameObject FindSceneObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            if (mainPanel != null)
            {
                Transform mainParent = mainPanel.transform.parent;
                Transform fromMainParent = FindInChildrenByName<Transform>(mainParent, objectName);
                if (fromMainParent != null)
                    return fromMainParent.gameObject;
            }

            Scene scene = gameObject.scene;
            if (scene.IsValid())
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    Transform found = FindInChildrenByName<Transform>(roots[i].transform, objectName);
                    if (found != null)
                        return found.gameObject;
                }
            }

            return GameObject.Find(objectName);
        }

        private void ResolveLobbyUiReferences()
        {
            ResolveMainLobbyPanelReferences();
            ResolveCreateLobbyPanelReferences();
            ResolvePasswordLobbyPanelReferences();
        }

        private void ResolveMainLobbyPanelReferences()
        {
            if (lobbyPanel == null)
                return;

            Transform root = lobbyPanel.transform;

            if (lobbyNicknameInput == null)
                lobbyNicknameInput = FindInChildrenByName<TMP_InputField>(root, "NicknameInput");

            if (lobbyRoomListContent == null)
            {
                RectTransform contentTransform = FindInChildrenByName<RectTransform>(root, "Content");
                lobbyRoomListContent = contentTransform;
            }

            if (lobbyNoRoomsText == null)
                lobbyNoRoomsText = FindInChildrenByName<TMP_Text>(root, "NoRooms");

            if (lobbyRefreshButton == null)
                lobbyRefreshButton = FindInChildrenByName<Button>(root, "RefreshButton");
            if (lobbyCreateButton == null)
                lobbyCreateButton = FindInChildrenByName<Button>(root, "CreateButton");
            if (lobbyConnectButton == null)
                lobbyConnectButton = FindInChildrenByName<Button>(root, "PlaySoloButton")
                    ?? FindInChildrenByName<Button>(root, "ConnectButton");
            if (lobbyConnectButton != null)
                lobbyConnectButton.gameObject.SetActive(true);
            if (lobbyBackButton == null)
                lobbyBackButton = FindInChildrenByName<Button>(root, "BackButton");
        }

        private void ResolveCreateLobbyPanelReferences()
        {
            if (lobbyCreatePanel == null)
                return;

            Transform root = lobbyCreatePanel.transform;

            if (createTitleText == null)
                createTitleText = FindInChildrenByName<TMP_Text>(root, "LobbyTitle");

            if (createRoomNameLabel == null)
                createRoomNameLabel = FindInChildrenByName<TMP_Text>(root, "NicknameLabel");

            if (createRoomNameInput == null)
                createRoomNameInput = FindInChildrenByName<TMP_InputField>(root, "NicknameInput");

            if (createRoomPasswordInput == null)
                createRoomPasswordInput = FindInChildrenByName<TMP_InputField>(root, "PasswordInput");

            if (createRoomPasswordInput == null)
            {
                TMP_InputField[] inputs = root.GetComponentsInChildren<TMP_InputField>(true);
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i] != null && inputs[i] != createRoomNameInput)
                    {
                        createRoomPasswordInput = inputs[i];
                        break;
                    }
                }
            }

            createHasSeparatePasswordInput = createRoomPasswordInput != null && createRoomPasswordInput != createRoomNameInput;

            if (createRoomPasswordLabel == null)
                createRoomPasswordLabel = FindInChildrenByName<TMP_Text>(root, "PasswordLabel");

            if (createPasswordInputRoot == null && createRoomPasswordInput != null)
            {
                Transform row = createRoomPasswordInput.transform.parent;
                if (row != null)
                    row = row.parent;
                if (row != null)
                    createPasswordInputRoot = row.gameObject;
            }

            if (createPasswordToggleButton == null)
                createPasswordToggleButton = FindInChildrenByName<Button>(root, "RefreshButton")
                    ?? FindInChildrenByName<Button>(root, "PasswordToggleButton");

            if (createPasswordToggleText == null && createPasswordToggleButton != null)
                createPasswordToggleText = createPasswordToggleButton.GetComponentInChildren<TMP_Text>(true);

            if (createConnectButton == null)
                createConnectButton = FindInChildrenByName<Button>(root, "CreateButton")
                    ?? FindInChildrenByName<Button>(root, "ConnectButton");

            if (createBackButton == null)
                createBackButton = FindInChildrenByName<Button>(root, "BackButton");
        }

        private void ResolvePasswordLobbyPanelReferences()
        {
            if (lobbyPasswordPanel == null)
                return;

            Transform root = lobbyPasswordPanel.transform;

            if (passwordTitleText == null)
                passwordTitleText = FindInChildrenByName<TMP_Text>(root, "LobbyTitle");

            if (passwordLabel == null)
                passwordLabel = FindInChildrenByName<TMP_Text>(root, "NicknameLabel");

            if (passwordInput == null)
                passwordInput = FindInChildrenByName<TMP_InputField>(root, "NicknameInput")
                    ?? FindInChildrenByName<TMP_InputField>(root, "PasswordInput");

            if (passwordErrorText == null)
                passwordErrorText = FindInChildrenByName<TMP_Text>(root, "NoRooms")
                    ?? FindInChildrenByName<TMP_Text>(root, "PasswordErrorText");

            if (passwordConnectButton == null)
                passwordConnectButton = FindInChildrenByName<Button>(root, "CreateButton")
                    ?? FindInChildrenByName<Button>(root, "ConnectButton");

            if (passwordBackButton == null)
                passwordBackButton = FindInChildrenByName<Button>(root, "BackButton");
        }

        private void ConfigureLobbyPanelsLayout()
        {
            CacheLobbyStaticRows();
        }

        private void CacheLobbyStaticRows()
        {
            lobbyStaticRows.Clear();
            if (lobbyRoomListContent == null)
                return;

            for (int i = 0; i < lobbyRoomListContent.childCount; i++)
            {
                Transform child = lobbyRoomListContent.GetChild(i);
                if (child == null)
                    continue;

                if (!child.name.StartsWith("RoomEntryTemplate", StringComparison.Ordinal) &&
                    !child.name.StartsWith("LobbyRoomRow_", StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject row = child.gameObject;
                if (!lobbyStaticRows.Contains(row))
                    lobbyStaticRows.Add(row);

                row.SetActive(false);
            }
        }

        private void ApplyLobbyLocalizationKeys()
        {
            SetLocalizedButtonLabel(lobbyConnectButton, "lobby.connect");
            SetLocalizedButtonLabel(lobbyCreateButton, "lobby.create_room");
            SetLocalizedButtonLabel(lobbyBackButton, "menu.back");
            SetLocalizedButtonLabel(lobbyRefreshButton, "lobby.refresh");

            SetLocalizedText(createTitleText, "lobby.create_room");
            SetLocalizedText(createRoomNameLabel, "lobby.room_name");
            if (createRoomPasswordLabel != null)
                SetLocalizedText(createRoomPasswordLabel, "lobby.password");
            SetLocalizedButtonLabel(createConnectButton, "lobby.connect");
            SetLocalizedButtonLabel(createBackButton, "menu.back");

            SetLocalizedText(passwordTitleText, "lobby.password");
            SetLocalizedText(passwordLabel, "lobby.password");
            SetLocalizedButtonLabel(passwordConnectButton, "lobby.connect");
            SetLocalizedButtonLabel(passwordBackButton, "menu.back");

            if (passwordErrorText != null)
            {
                SetLocalizedText(passwordErrorText, "lobby.wrong_password");
                passwordErrorText.color = Color.red;
            }

            RefreshCreatePasswordToggleLabel();
        }

        private void HookLobbyEvents()
        {
            if (lobbyNicknameInput != null)
            {
                lobbyNicknameInput.onValueChanged.RemoveListener(OnLobbyNicknameChanged);
                lobbyNicknameInput.onValueChanged.AddListener(OnLobbyNicknameChanged);
            }

            if (lobbyRefreshButton != null)
            {
                lobbyRefreshButton.onClick.RemoveListener(OnLobbyRefreshClicked);
                lobbyRefreshButton.onClick.AddListener(OnLobbyRefreshClicked);
                ConfigureButtonFeedback(lobbyRefreshButton);
            }

            if (lobbyCreateButton != null)
            {
                lobbyCreateButton.onClick.RemoveListener(OnLobbyCreateClicked);
                lobbyCreateButton.onClick.AddListener(OnLobbyCreateClicked);
                ConfigureButtonFeedback(lobbyCreateButton);
            }

            if (lobbyConnectButton != null)
            {
                lobbyConnectButton.onClick.RemoveListener(OnLobbyConnectClicked);
                lobbyConnectButton.onClick.AddListener(OnLobbyConnectClicked);
                ConfigureButtonFeedback(lobbyConnectButton);
            }

            if (lobbyBackButton != null)
            {
                lobbyBackButton.onClick.RemoveListener(ShowMainPanel);
                lobbyBackButton.onClick.AddListener(ShowMainPanel);
                ConfigureButtonFeedback(lobbyBackButton);
            }

            if (createPasswordToggleButton != null)
            {
                createPasswordToggleButton.onClick.RemoveListener(OnCreatePasswordToggleClicked);
                createPasswordToggleButton.onClick.AddListener(OnCreatePasswordToggleClicked);
                ConfigureButtonFeedback(createPasswordToggleButton);
            }

            if (createConnectButton != null)
            {
                createConnectButton.onClick.RemoveListener(OnCreateConnectClicked);
                createConnectButton.onClick.AddListener(OnCreateConnectClicked);
                ConfigureButtonFeedback(createConnectButton);
            }

            if (createBackButton != null)
            {
                createBackButton.onClick.RemoveListener(ShowLobbyPanel);
                createBackButton.onClick.AddListener(ShowLobbyPanel);
                ConfigureButtonFeedback(createBackButton);
            }

            if (createRoomNameInput != null)
            {
                createRoomNameInput.onValueChanged.RemoveListener(OnCreateInputChanged);
                createRoomNameInput.onValueChanged.AddListener(OnCreateInputChanged);
            }

            if (createRoomPasswordInput != null && createRoomPasswordInput != createRoomNameInput)
            {
                createRoomPasswordInput.onValueChanged.RemoveListener(OnCreateInputChanged);
                createRoomPasswordInput.onValueChanged.AddListener(OnCreateInputChanged);
            }

            if (passwordInput != null)
            {
                passwordInput.onValueChanged.RemoveListener(OnPasswordInputChanged);
                passwordInput.onValueChanged.AddListener(OnPasswordInputChanged);
            }

            if (passwordConnectButton != null)
            {
                passwordConnectButton.onClick.RemoveListener(OnPasswordConnectClicked);
                passwordConnectButton.onClick.AddListener(OnPasswordConnectClicked);
                ConfigureButtonFeedback(passwordConnectButton);
            }

            if (passwordBackButton != null)
            {
                passwordBackButton.onClick.RemoveListener(ShowLobbyPanel);
                passwordBackButton.onClick.AddListener(ShowLobbyPanel);
                ConfigureButtonFeedback(passwordBackButton);
            }
        }

        private void UnhookLobbyEvents()
        {
            if (lobbyNicknameInput != null)
                lobbyNicknameInput.onValueChanged.RemoveListener(OnLobbyNicknameChanged);

            if (lobbyRefreshButton != null)
                lobbyRefreshButton.onClick.RemoveListener(OnLobbyRefreshClicked);
            if (lobbyCreateButton != null)
                lobbyCreateButton.onClick.RemoveListener(OnLobbyCreateClicked);
            if (lobbyConnectButton != null)
                lobbyConnectButton.onClick.RemoveListener(OnLobbyConnectClicked);
            if (lobbyBackButton != null)
                lobbyBackButton.onClick.RemoveListener(ShowMainPanel);

            if (createPasswordToggleButton != null)
                createPasswordToggleButton.onClick.RemoveListener(OnCreatePasswordToggleClicked);
            if (createConnectButton != null)
                createConnectButton.onClick.RemoveListener(OnCreateConnectClicked);
            if (createBackButton != null)
                createBackButton.onClick.RemoveListener(ShowLobbyPanel);
            if (createRoomNameInput != null)
                createRoomNameInput.onValueChanged.RemoveListener(OnCreateInputChanged);
            if (createRoomPasswordInput != null && createRoomPasswordInput != createRoomNameInput)
                createRoomPasswordInput.onValueChanged.RemoveListener(OnCreateInputChanged);

            if (passwordInput != null)
                passwordInput.onValueChanged.RemoveListener(OnPasswordInputChanged);
            if (passwordConnectButton != null)
                passwordConnectButton.onClick.RemoveListener(OnPasswordConnectClicked);
            if (passwordBackButton != null)
                passwordBackButton.onClick.RemoveListener(ShowLobbyPanel);
        }

        private void CreateDebugRoomsFromConfig()
        {
            lobbyRooms.Clear();
            selectedLobbyRoom = null;
            pendingPasswordRoom = null;

            LobbyMenuRoomConfigEntry[] debugRooms = lobbyConfig != null ? lobbyConfig.initialDebugRooms : null;
            if (debugRooms == null)
                return;

            for (int i = 0; i < debugRooms.Length; i++)
            {
                LobbyMenuRoomConfigEntry entry = debugRooms[i];
                if (entry == null)
                    continue;

                string name = ResolveRoomName(entry.roomNameKey, entry.fallbackRoomName);
                int maxPlayers = Mathf.Max(1, entry.maxPlayers);
                int currentPlayers = Mathf.Clamp(entry.currentPlayers, 0, maxPlayers);

                lobbyRooms.Add(new LobbyRoomData
                {
                    roomName = name,
                    currentPlayers = currentPlayers,
                    maxPlayers = maxPlayers,
                    isPasswordProtected = entry.isPasswordProtected,
                    password = entry.password ?? string.Empty
                });
            }
        }

        private string ResolveRoomName(string key, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                string localized = LocalizationService.Get(key);
                if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, key, StringComparison.Ordinal))
                    return localized;
            }

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return "Room";
        }

        private void RebuildLobbyRoomList()
        {
            ClearLobbyRoomRows();

            if (lobbyRoomListContent == null || lobbyStaticRows.Count == 0)
            {
                UpdateLobbyConnectState();
                return;
            }

            if (lobbyNoRoomsText != null)
                lobbyNoRoomsText.gameObject.SetActive(lobbyRooms.Count == 0);

            int visibleRows = Mathf.Min(lobbyRooms.Count, lobbyStaticRows.Count);
            for (int i = 0; i < visibleRows; i++)
            {
                LobbyRoomData room = lobbyRooms[i];
                GameObject row = lobbyStaticRows[i];
                if (row == null)
                    continue;

                row.name = "LobbyRoomRow_" + i;
                row.SetActive(true);

                LobbyRoomRowView view = BuildLobbyRowView(row, room, i);
                lobbyRowViews.Add(view);
            }

            if (lobbyRooms.Count > lobbyStaticRows.Count)
                Debug.LogWarning("[MainMenuController] Not enough prebuilt room rows in LobbyPanel for all rooms.");

            ApplyLobbySelectionVisuals();
            UpdateLobbyConnectState();
        }

        private LobbyRoomRowView BuildLobbyRowView(GameObject row, LobbyRoomData room, int rowIndex)
        {
            LobbyRoomRowView view = new LobbyRoomRowView
            {
                root = row,
                data = room,
                background = row.GetComponent<Image>()
            };

            TMP_Text[] allTexts = row.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text text = allTexts[i];
                if (text == null)
                    continue;

                if (text.GetComponentInParent<Button>() == null && view.roomText == null)
                    view.roomText = text;
            }

            view.selectButton = row.GetComponentInChildren<Button>(true);
            if (view.selectButton != null)
            {
                view.selectButtonText = view.selectButton.GetComponentInChildren<TMP_Text>(true);
                view.selectButton.onClick.RemoveAllListeners();
                view.selectButton.onClick.AddListener(() => OnRoomRowSelected(rowIndex));
                ConfigureButtonFeedback(view.selectButton);
            }

            if (view.roomText != null)
            {
                string passwordLabel = LocalizationService.Get("lobby.password");
                string lockSuffix = room.isPasswordProtected ? $" [{passwordLabel}]" : string.Empty;
                view.roomText.text = $"{room.roomName} ({room.currentPlayers}/{room.maxPlayers}){lockSuffix}";
            }

            if (view.selectButtonText != null)
                view.selectButtonText.text = LocalizationService.Get("lobby.select_room");

            return view;
        }

        private void ClearLobbyRoomRows()
        {
            for (int i = 0; i < lobbyStaticRows.Count; i++)
            {
                GameObject row = lobbyStaticRows[i];
                if (row == null)
                    continue;

                Button rowButton = row.GetComponentInChildren<Button>(true);
                if (rowButton != null)
                    rowButton.onClick.RemoveAllListeners();

                row.SetActive(false);
            }

            lobbyRowViews.Clear();
        }

        private void OnRoomRowSelected(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= lobbyRooms.Count)
                return;

            selectedLobbyRoom = lobbyRooms[rowIndex];
            ApplyLobbySelectionVisuals();
            UpdateLobbyConnectState();
        }

        private void ApplyLobbySelectionVisuals()
        {
            for (int i = 0; i < lobbyRowViews.Count; i++)
            {
                LobbyRoomRowView view = lobbyRowViews[i];
                bool isSelected = selectedLobbyRoom != null && ReferenceEquals(selectedLobbyRoom, view.data);

                if (view.background != null)
                    view.background.color = isSelected ? lobbyRoomSelectedColor : lobbyRoomNormalColor;

                if (view.selectButtonText != null)
                    view.selectButtonText.text = isSelected
                        ? LocalizationService.Get("lobby.selected")
                        : LocalizationService.Get("lobby.select_room");
            }
        }

        private void UpdateLobbyConnectState()
        {
            if (lobbyConnectButton != null)
                lobbyConnectButton.interactable = selectedLobbyRoom != null;
        }

        private void OnLobbyRefreshClicked()
        {
            lobbyRooms.Clear();
            selectedLobbyRoom = null;
            RebuildLobbyRoomList();
        }

        private void OnLobbyCreateClicked()
        {
            ShowLobbyCreatePanel();
        }

        private void OnLobbyConnectClicked()
        {
            if (selectedLobbyRoom == null)
                return;

            if (selectedLobbyRoom.isPasswordProtected)
            {
                ShowLobbyPasswordPanel(selectedLobbyRoom);
                return;
            }

            StartLobbyMatch();
        }

        private void OnLobbyNicknameChanged(string value)
        {
            GameSessionSettings.PlayerNickname = value;
        }

        private void OnCreatePasswordToggleClicked()
        {
            if (!createHasSeparatePasswordInput)
            {
                createRoomIsPasswordProtected = false;
                RefreshCreatePasswordToggleLabel();
                UpdateCreateConnectState();
                return;
            }

            createRoomIsPasswordProtected = !createRoomIsPasswordProtected;
            if (createPasswordInputRoot != null)
                createPasswordInputRoot.SetActive(createRoomIsPasswordProtected);

            if (!createRoomIsPasswordProtected && createRoomPasswordInput != null)
                createRoomPasswordInput.SetTextWithoutNotify(string.Empty);

            RefreshCreatePasswordToggleLabel();
            UpdateCreateConnectState();
        }

        private void OnCreateInputChanged(string _)
        {
            UpdateCreateConnectState();
        }

        private void UpdateCreateConnectState()
        {
            if (createConnectButton == null)
                return;

            bool hasRoomName = createRoomNameInput != null && !string.IsNullOrWhiteSpace(createRoomNameInput.text);
            if (!createRoomIsPasswordProtected)
            {
                createConnectButton.interactable = hasRoomName;
                return;
            }

            if (!createHasSeparatePasswordInput || createRoomPasswordInput == null)
            {
                createConnectButton.interactable = false;
                return;
            }

            bool hasPassword = !string.IsNullOrWhiteSpace(createRoomPasswordInput.text);
            createConnectButton.interactable = hasRoomName && hasPassword;
        }

        private void OnCreateConnectClicked()
        {
            string roomName = createRoomNameInput != null ? createRoomNameInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(roomName))
                return;

            string password = string.Empty;
            if (createRoomIsPasswordProtected)
            {
                if (!createHasSeparatePasswordInput || createRoomPasswordInput == null)
                    return;

                password = createRoomPasswordInput.text != null ? createRoomPasswordInput.text.Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(password))
                    return;
            }

            lobbyRooms.Add(new LobbyRoomData
            {
                roomName = roomName,
                currentPlayers = 0,
                maxPlayers = 6,
                isPasswordProtected = createRoomIsPasswordProtected,
                password = password
            });

            StartLobbyMatch();
        }

        private void ResetCreatePanelState()
        {
            createRoomIsPasswordProtected = false;

            if (createRoomNameInput != null)
                createRoomNameInput.SetTextWithoutNotify(string.Empty);

            if (createRoomPasswordInput != null && createRoomPasswordInput != createRoomNameInput)
                createRoomPasswordInput.SetTextWithoutNotify(string.Empty);

            if (createPasswordInputRoot != null)
                createPasswordInputRoot.SetActive(false);

            RefreshCreatePasswordToggleLabel();
            UpdateCreateConnectState();
        }

        private void RefreshCreatePasswordToggleLabel()
        {
            if (createPasswordToggleText == null)
                return;

            string key;
            if (!createHasSeparatePasswordInput)
                key = "lobby.password_field_missing";
            else
                key = createRoomIsPasswordProtected ? "lobby.password_toggle_on" : "lobby.password_toggle_off";

            SetLocalizedText(createPasswordToggleText, key);
        }

        private void OnPasswordInputChanged(string _)
        {
            if (passwordErrorText != null)
                passwordErrorText.gameObject.SetActive(false);

            UpdatePasswordConnectState();
        }

        private void UpdatePasswordConnectState()
        {
            if (passwordConnectButton == null)
                return;

            if (pendingPasswordRoom == null || passwordInput == null)
            {
                passwordConnectButton.interactable = false;
                return;
            }

            string entered = passwordInput.text != null ? passwordInput.text.Trim() : string.Empty;
            string expected = pendingPasswordRoom.password != null ? pendingPasswordRoom.password.Trim() : string.Empty;
            passwordConnectButton.interactable = !string.IsNullOrEmpty(expected) && string.Equals(entered, expected, StringComparison.Ordinal);
        }

        private void OnPasswordConnectClicked()
        {
            if (pendingPasswordRoom == null || passwordInput == null)
                return;

            string entered = passwordInput.text != null ? passwordInput.text.Trim() : string.Empty;
            string expected = pendingPasswordRoom.password != null ? pendingPasswordRoom.password.Trim() : string.Empty;

            if (!string.Equals(entered, expected, StringComparison.Ordinal))
            {
                if (passwordErrorText != null)
                    passwordErrorText.gameObject.SetActive(true);
                UpdatePasswordConnectState();
                return;
            }

            StartLobbyMatch();
        }

        private void ResetPasswordPanelState()
        {
            if (passwordInput != null)
                passwordInput.SetTextWithoutNotify(string.Empty);

            if (passwordErrorText != null)
                passwordErrorText.gameObject.SetActive(false);

            UpdatePasswordConnectState();
        }

        private void StartLobbyMatch()
        {
            GameSessionSettings.PrepareLobby();
            string battleSceneName = lobbyConfig != null ? lobbyConfig.battleSceneName : gameSceneName;
            if (string.IsNullOrWhiteSpace(battleSceneName))
                battleSceneName = gameSceneName;

            LoadConfiguredScene(battleSceneName, "Assets/Scenes/Core.unity");
        }

        private static void SetLocalizedButtonLabel(Button button, string key)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            SetLocalizedText(text, key);
        }

        private static void SetLocalizedText(TMP_Text text, string key)
        {
            if (text == null || string.IsNullOrWhiteSpace(key))
                return;

            LocalizedText localized = text.GetComponent<LocalizedText>();
            if (localized != null)
            {
                localized.SetKey(key);
                return;
            }

            text.text = LocalizationService.Get(key);
        }
    }
}
