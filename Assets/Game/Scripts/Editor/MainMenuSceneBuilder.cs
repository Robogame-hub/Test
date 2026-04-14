#if UNITY_EDITOR
using System.Collections.Generic;
using TankGame.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankGame.EditorTools
{
    public static class MainMenuSceneBuilder
    {
        private static readonly Color PanelColor = new Color(0.13f, 0.08f, 0.05f, 0.76f);
        private static readonly Color ButtonColor = new Color(0.27f, 0.17f, 0.10f, 0.88f);
        private static readonly Color TextGreen = new Color(0.96f, 0.86f, 0.67f, 1f);

        [MenuItem("TankGame/Scenes/Create MainMenu (Legacy)")]
        public static void CreateMainMenuLegacy()
        {
            CreateMainMenuScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MainMenuSceneBuilder] Created/updated MainMenu scene.");
        }

        [MenuItem("TankGame/Scenes/Create MainMenu")]
        public static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            EnsureEventSystem();
            CreateCamera();

            Canvas canvas = CreateCanvas("MainMenuCanvas");

            GameObject leftPanel = CreatePanel("LeftPanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0.4f, 1f));
            GameObject rightPanel = CreatePanel("RightPanel", canvas.transform, new Vector2(0.4f, 0f), new Vector2(1f, 1f));

            VerticalLayoutGroup leftLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftLayout.childAlignment = TextAnchor.MiddleLeft;
            leftLayout.spacing = 16f;
            leftLayout.padding = new RectOffset(48, 16, 60, 60);
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = false;

            ContentSizeFitter fitter = leftPanel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            TMP_Text title = CreateLabel("Title", leftPanel.transform, "menu.title", 64f, FontStyles.Bold);
            SetElementHeight(title.gameObject, 90f);

            Button playButton = CreateMenuButton(leftPanel.transform, "PlayButton", "menu.play");
            Button sandboxButton = CreateMenuButton(leftPanel.transform, "SandboxButton", "menu.sandbox");
            Button settingsButton = CreateMenuButton(leftPanel.transform, "SettingsButton", "menu.settings");
            Button exitButton = CreateMenuButton(leftPanel.transform, "ExitButton", "menu.exit");

            GameObject settingsPanel = CreatePanel("SettingsPanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0.4f, 1f));
            settingsPanel.SetActive(false);
            VerticalLayoutGroup settingsLayout = settingsPanel.AddComponent<VerticalLayoutGroup>();
            settingsLayout.spacing = 8f;
            settingsLayout.padding = new RectOffset(8, 8, 8, 8);
            settingsLayout.childControlWidth = true;
            settingsLayout.childControlHeight = false;
            settingsLayout.childAlignment = TextAnchor.UpperLeft;

            TMP_Text settingsTitle = CreateLabel("SettingsTitle", settingsPanel.transform, "settings.title", 34f, FontStyles.Bold);
            SetElementHeight(settingsTitle.gameObject, 48f);

            TMP_Text sensHeader = CreateLabel("SensitivityHeader", settingsPanel.transform, "settings.sensitivity", 26f, FontStyles.Bold);
            SetElementHeight(sensHeader.gameObject, 34f);
            TMP_Text ms = CreateLabel("MasterSensLabel", settingsPanel.transform, "settings.master_sens", 22f, FontStyles.Normal);
            Slider masterSens = CreateSlider("MasterSensSlider", settingsPanel.transform);
            TMP_Text hs = CreateLabel("HorizontalSensLabel", settingsPanel.transform, "settings.horizontal_sens", 22f, FontStyles.Normal);
            Slider horizontalSens = CreateSlider("HorizontalSensSlider", settingsPanel.transform);
            TMP_Text vs = CreateLabel("VerticalSensLabel", settingsPanel.transform, "settings.vertical_sens", 22f, FontStyles.Normal);
            Slider verticalSens = CreateSlider("VerticalSensSlider", settingsPanel.transform);

            TMP_Text soundHeader = CreateLabel("SoundHeader", settingsPanel.transform, "settings.sound", 26f, FontStyles.Bold);
            SetElementHeight(soundHeader.gameObject, 34f);
            TMP_Text mv = CreateLabel("MasterVolumeLabel", settingsPanel.transform, "settings.master_volume", 22f, FontStyles.Normal);
            Slider masterVol = CreateSlider("MasterVolumeSlider", settingsPanel.transform);
            TMP_Text musicV = CreateLabel("MusicVolumeLabel", settingsPanel.transform, "settings.music_volume", 22f, FontStyles.Normal);
            Slider musicVol = CreateSlider("MusicVolumeSlider", settingsPanel.transform);
            TMP_Text sfxV = CreateLabel("SfxVolumeLabel", settingsPanel.transform, "settings.sfx_volume", 22f, FontStyles.Normal);
            Slider sfxVol = CreateSlider("SfxVolumeSlider", settingsPanel.transform);

            TMP_Text langLabel = CreateLabel("LanguageLabel", settingsPanel.transform, "settings.language", 22f, FontStyles.Normal);
            GameObject langRow = CreateRow("LanguageRow", settingsPanel.transform);
            Button prevLang = CreateSmallButton(langRow.transform, "LangPrev", "<");
            TMP_Text langValue = CreatePlainLabel(langRow.transform, "LanguageValue", "Р В РЎС“РЎРѓРЎРѓР С”Р С‘Р в„–", 22f, FontStyles.Bold);
            Button nextLang = CreateSmallButton(langRow.transform, "LangNext", ">");

            Button backSettingsButton = CreateMenuButton(settingsPanel.transform, "BackFromSettingsButton", "menu.back");

            GameObject sandboxMatchPanel = CreatePanel("SandboxMatchPanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0.4f, 1f));
            sandboxMatchPanel.SetActive(false);
            VerticalLayoutGroup sandboxLayout = sandboxMatchPanel.AddComponent<VerticalLayoutGroup>();
            sandboxLayout.spacing = 12f;
            sandboxLayout.padding = new RectOffset(8, 8, 8, 8);
            sandboxLayout.childControlWidth = true;
            sandboxLayout.childControlHeight = false;
            sandboxLayout.childAlignment = TextAnchor.UpperLeft;

            TMP_Text sandboxTitle = CreateLabel("SandboxTitle", sandboxMatchPanel.transform, "sandbox.title", 34f, FontStyles.Bold);
            SetElementHeight(sandboxTitle.gameObject, 48f);

            GameObject sandboxBotRow = CreateRow("SandboxBotCountRow", sandboxMatchPanel.transform);
            SetElementHeight(sandboxBotRow, 58f);
            TMP_Text botCountLabel = CreateLabel("SandboxBotCountLabel", sandboxBotRow.transform, "sandbox.bot_count", 24f, FontStyles.Normal);
            LayoutElement botCountLabelLe = botCountLabel.gameObject.GetComponent<LayoutElement>();
            botCountLabelLe.minWidth = 170f;
            botCountLabelLe.preferredWidth = 170f;

            Button sandboxBotsPrevButton = CreateSmallButton(sandboxBotRow.transform, "SandboxBotsPrevButton", "<", 64f, 54f);
            TMP_Text sandboxBotCountValueText = CreatePlainLabel(sandboxBotRow.transform, "SandboxBotCountValueText", "3", 24f, FontStyles.Bold);
            LayoutElement sandboxBotValueLe = sandboxBotCountValueText.gameObject.GetComponent<LayoutElement>();
            sandboxBotValueLe.minWidth = 72f;
            sandboxBotValueLe.preferredWidth = 72f;
            sandboxBotCountValueText.alignment = TextAlignmentOptions.Center;
            Button sandboxBotsNextButton = CreateSmallButton(sandboxBotRow.transform, "SandboxBotsNextButton", ">", 64f, 54f);

            Button startSandboxMatchButton = CreateMenuButton(sandboxMatchPanel.transform, "StartSandboxMatchButton", "menu.start_match");
            Button backFromSandboxMatchButton = CreateMenuButton(sandboxMatchPanel.transform, "BackFromSandboxMatchButton", "menu.back");

            BuildRightTankPanel(rightPanel.transform, out TankSelectionController tankSelectionController);

            MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            controller.transform.SetParent(canvas.transform, false);

            controller.mainPanel = leftPanel;
            controller.settingsPanel = settingsPanel;
            controller.sandboxMatchPanel = sandboxMatchPanel;
            controller.playButton = playButton;
            controller.sandboxButton = sandboxButton;
            controller.settingsButton = settingsButton;
            controller.exitButton = exitButton;
            controller.backFromSettingsButton = backSettingsButton;
            controller.startSandboxMatchButton = startSandboxMatchButton;
            controller.backFromSandboxMatchButton = backFromSandboxMatchButton;
            controller.sandboxBotsPrevButton = sandboxBotsPrevButton;
            controller.sandboxBotCountValueText = sandboxBotCountValueText;
            controller.sandboxBotsNextButton = sandboxBotsNextButton;

            controller.masterSensitivitySlider = masterSens;
            controller.horizontalSensitivitySlider = horizontalSens;
            controller.verticalSensitivitySlider = verticalSens;

            controller.masterVolumeSlider = masterVol;
            controller.musicVolumeSlider = musicVol;
            controller.sfxVolumeSlider = sfxVol;

            controller.languagePrevButton = prevLang;
            controller.languageNextButton = nextLang;
            controller.languageValueText = langValue;
            controller.tankSelection = tankSelectionController;

            string tankPrefabPath = "Assets/Game/Prefab/TANK_1 (1).prefab";
            GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tankPrefabPath);
            if (tankPrefab != null && tankSelectionController != null)
            {
                tankSelectionController.tanks = new List<TankDefinition>
                {
                    new TankDefinition
                    {
                        displayName = "TANK 1",
                        playerPrefab = tankPrefab,
                        speed = 0.55f,
                        armor = 0.65f,
                        firepower = 0.70f,
                        handling = 0.50f
                    }
                };
            }

            SetupMenuMusic();

            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void BuildRightTankPanel(Transform parent, out TankSelectionController tankSelection)
        {
            GameObject container = CreatePanel("TankPanel", parent, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            TMP_Text header = CreateLabel("TankSelectTitle", container.transform, "tank.select", 34f, FontStyles.Bold);
            SetElementHeight(header.gameObject, 48f);

            GameObject selectorRow = CreateRow("TankSelectorRow", container.transform);
            Button prev = CreateSmallButton(selectorRow.transform, "PrevTank", "<", 80f, 80f);
            TMP_Text tankName = CreatePlainLabel(selectorRow.transform, "TankName", "TANK 1", 30f, FontStyles.Bold);
            Button next = CreateSmallButton(selectorRow.transform, "NextTank", ">", 80f, 80f);

            GameObject previewBlock = CreatePanel("PreviewBlock", container.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            previewBlock.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
            SetElementHeight(previewBlock, 320f);

            GameObject previewSquareObj = new GameObject("TankPreviewSquare", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            previewSquareObj.transform.SetParent(previewBlock.transform, false);
            RectTransform previewSquareRt = previewSquareObj.GetComponent<RectTransform>();
            previewSquareRt.anchorMin = new Vector2(0.5f, 0.5f);
            previewSquareRt.anchorMax = new Vector2(0.5f, 0.5f);
            previewSquareRt.pivot = new Vector2(0.5f, 0.5f);
            previewSquareRt.sizeDelta = new Vector2(280f, 280f);

            AspectRatioFitter previewAspect = previewSquareObj.GetComponent<AspectRatioFitter>();
            previewAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            previewAspect.aspectRatio = 1f;

            Image preview = previewSquareObj.GetComponent<Image>();
            preview.color = new Color(0f, 0f, 0f, 0.30f);

            GameObject statsBlock = CreatePanel("StatsBlock", container.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            statsBlock.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            VerticalLayoutGroup statsLayout = statsBlock.AddComponent<VerticalLayoutGroup>();
            statsLayout.padding = new RectOffset(10, 10, 10, 10);
            statsLayout.spacing = 8f;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = false;
            statsLayout.childAlignment = TextAnchor.UpperLeft;

            SegmentedStatBar speedBar = CreateStatRow(statsBlock.transform, "tank.speed");
            SegmentedStatBar armorBar = CreateStatRow(statsBlock.transform, "tank.armor");
            SegmentedStatBar fireBar = CreateStatRow(statsBlock.transform, "tank.firepower");
            SegmentedStatBar handlingBar = CreateStatRow(statsBlock.transform, "tank.handling");

            tankSelection = new GameObject("TankSelectionController").AddComponent<TankSelectionController>();
            tankSelection.transform.SetParent(container.transform, false);
            tankSelection.previousButton = prev;
            tankSelection.nextButton = next;
            tankSelection.tankNameText = tankName;
            tankSelection.tankPreviewImage = preview;
            tankSelection.speedBar = speedBar;
            tankSelection.armorBar = armorBar;
            tankSelection.firepowerBar = fireBar;
            tankSelection.handlingBar = handlingBar;
        }

        private static void SetupMenuMusic()
        {
            GameObject musicObj = new GameObject("MenuMusic", typeof(AudioSource), typeof(MenuMusicPlayer));
            AudioSource source = musicObj.GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            MenuMusicPlayer player = musicObj.GetComponent<MenuMusicPlayer>();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/AmbientAydio/steel_ambient_1.mp3");
            if (clip != null)
            {
                SerializedObject so = new SerializedObject(player);
                SerializedProperty clipProp = so.FindProperty("menuMusicClip");
                clipProp.objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        private static SegmentedStatBar CreateStatRow(Transform parent, string key)
        {
            GameObject row = CreateRow($"{key}_Row", parent);
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 32f;
            rowLayout.preferredHeight = 32f;

            TMP_Text label = CreateLabel($"{key}_Label", row.transform, key, 20f, FontStyles.Normal);
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minWidth = 170f;
            labelLayout.preferredWidth = 170f;

            GameObject barRoot = new GameObject("BarsRoot", typeof(RectTransform), typeof(LayoutElement));
            barRoot.transform.SetParent(row.transform, false);
            LayoutElement barRootLayout = barRoot.GetComponent<LayoutElement>();
            barRootLayout.minWidth = 320f;
            barRootLayout.preferredWidth = 320f;
            barRootLayout.minHeight = 20f;
            barRootLayout.preferredHeight = 20f;

            GameObject bgObj = new GameObject("BackgroundBars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bgObj.transform.SetParent(barRoot.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            HorizontalLayoutGroup bgLayout = bgObj.GetComponent<HorizontalLayoutGroup>();
            bgLayout.spacing = 4f;
            bgLayout.childControlWidth = true;
            bgLayout.childControlHeight = true;
            bgLayout.childForceExpandWidth = true;
            bgLayout.childForceExpandHeight = true;

            GameObject fillObj = new GameObject("FillBars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            fillObj.transform.SetParent(barRoot.transform, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            HorizontalLayoutGroup fillLayout = fillObj.GetComponent<HorizontalLayoutGroup>();
            fillLayout.spacing = 4f;
            fillLayout.childControlWidth = true;
            fillLayout.childControlHeight = true;
            fillLayout.childForceExpandWidth = true;
            fillLayout.childForceExpandHeight = true;

            GameObject segmentTemplateObj = new GameObject("SegmentTemplate", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            segmentTemplateObj.transform.SetParent(fillObj.transform, false);
            LayoutElement segmentLayout = segmentTemplateObj.GetComponent<LayoutElement>();
            segmentLayout.minWidth = 8f;
            segmentLayout.preferredWidth = 8f;
            Image segmentTemplate = segmentTemplateObj.GetComponent<Image>();
            segmentTemplate.color = TextGreen;
            segmentTemplateObj.SetActive(false);

            TMP_Text valueText = CreatePlainLabel(row.transform, "Value", "50", 20f, FontStyles.Bold);
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.minWidth = 52f;
            valueLayout.preferredWidth = 52f;

            SegmentedStatBar statBar = row.AddComponent<SegmentedStatBar>();
            statBar.backgroundContainer = bgRt;
            statBar.fillContainer = fillRt;
            statBar.segmentTemplate = segmentTemplate;
            statBar.valueText = valueText;
            statBar.segmentCount = 10;
            statBar.backgroundColor = new Color(0.62f, 0.24f, 0.12f, 0.5f);
            statBar.fillColor = new Color(1f, 0.67f, 0.25f, 0.96f);
            return statBar;
        }

        private static GameObject CreateRoomEntryTemplate(Transform parent)
        {
            GameObject row = CreateRow("RoomEntryTemplate", parent);
            SetElementHeight(row, 56f);
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            TMP_Text roomName = CreatePlainLabel(row.transform, "RoomName", "Room 1", 20f, FontStyles.Normal);
            LayoutElement nameLe = roomName.gameObject.AddComponent<LayoutElement>();
            nameLe.minWidth = 420f;
            nameLe.preferredWidth = 420f;

            Button join = CreateMenuButton(row.transform, "JoinButton", "lobby.join", 160f, 38f);
            return row;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void CreateCamera()
        {
            GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            Camera camera = cam.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.24f, 0.18f, 0.12f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();
            img.color = PanelColor;
            return go;
        }

        private static GameObject CreateRow(string name, Transform parent)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            SetElementHeight(row, 48f);
            return row;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, string key, float fontSize, FontStyles style)
        {
            TMP_Text label = CreatePlainLabel(parent, name, LocalizationService.Get(key), fontSize, style);
            LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
            localized.SetKey(key);
            return label;
        }

        private static TMP_Text CreatePlainLabel(Transform parent, string name, string text, float fontSize, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = TextGreen;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = Mathf.Max(32f, fontSize + 8f);
            le.minHeight = Mathf.Max(32f, fontSize + 8f);
            return label;
        }

        private static Button CreateMenuButton(Transform parent, string name, string localizationKey, float width = 360f, float height = 74f)
        {
            Button button = CreateButtonBase(parent, name, width, height);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text.text = LocalizationService.Get(localizationKey);
            LocalizedText localized = text.gameObject.AddComponent<LocalizedText>();
            localized.SetKey(localizationKey);
            return button;
        }

        private static Button CreateSmallButton(Transform parent, string name, string text, float width = 52f, float height = 52f)
        {
            Button button = CreateButtonBase(parent, name, width, height);
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            tmp.text = text;
            return button;
        }

        private static Button CreateButtonBase(Transform parent, string name, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = ButtonColor;

            ColorBlock colors = go.GetComponent<Button>().colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = new Color(ButtonColor.r + 0.1f, ButtonColor.g + 0.1f, ButtonColor.b + 0.1f, 0.9f);
            colors.pressedColor = new Color(ButtonColor.r + 0.2f, ButtonColor.g + 0.2f, ButtonColor.b + 0.2f, 1f);
            go.GetComponent<Button>().colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(go.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TMP_Text tmp = textObj.GetComponent<TMP_Text>();
            tmp.text = name;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = Mathf.Max(20f, height * 0.34f);
            tmp.color = TextGreen;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            le.preferredHeight = height;
            le.minHeight = height;

            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, float width = 320f, float height = 42f)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(width, height);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject textViewport = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
            textViewport.transform.SetParent(root.transform, false);
            RectTransform viewportRt = textViewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(10f, 6f);
            viewportRt.offsetMax = new Vector2(-10f, -6f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textViewport.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = 22f;
            text.color = TextGreen;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(textViewport.transform, false);
            RectTransform placeholderRt = placeholderObj.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholder.text = "Player";
            placeholder.fontSize = 22f;
            placeholder.color = new Color(TextGreen.r, TextGreen.g, TextGreen.b, 0.45f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = viewportRt;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 24;

            LayoutElement le = root.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            le.preferredHeight = height;
            le.minHeight = height;

            return input;
        }
        private static Slider CreateSlider(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 28f);

            GameObject bg = CreatePanel("Background", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(10f, 4f);
            fillAreaRt.offsetMax = new Vector2(-10f, -4f);

            GameObject fill = CreatePanel("Fill", fillArea.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = TextGreen;

            GameObject handle = CreatePanel("Handle", root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f));
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16f, 0f);
            handle.GetComponent<Image>().color = TextGreen;

            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            LayoutElement le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 30f;
            le.minHeight = 30f;

            return slider;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 200f;
            le.minHeight = 200f;
            return image;
        }

        private static void SetElementHeight(GameObject go, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }
    }
}
#endif














