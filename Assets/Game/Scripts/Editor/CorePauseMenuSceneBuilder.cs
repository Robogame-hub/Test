using System.IO;
using TankGame.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankGame.Editor
{
    public static class CorePauseMenuSceneBuilder
    {
        private const string CoreScenePath = "Assets/Scenes/Core.unity";
        private const string CanvasName = "UIManager";
        private const string PauseRootName = "PauseUIRoot";

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.88f);
        private static readonly Color ButtonColor = new Color(0f, 0f, 0f, 0.74f);
        private static readonly Color AccentColor = new Color32(0x0F, 0xF3, 0x00, 0xFF);
        private static readonly Color SliderBackground = new Color(0.08f, 0.08f, 0.08f, 1f);

        [MenuItem("TankGame/Scenes/Create Core Pause Menu Panel")]
        public static void CreateCorePauseMenuPanel()
        {
            Scene scene = EnsureCoreSceneLoaded();
            if (!scene.IsValid())
                return;

            Canvas canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                Debug.LogError("[CorePauseMenuSceneBuilder] Could not find/create canvas.");
                return;
            }

            RebuildPauseUi(canvas.transform);
            EnsurePauseControllerExists();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CorePauseMenuSceneBuilder] Core pause menu rebuilt from scratch.");
        }

        private static Scene EnsureCoreSceneLoaded()
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.path == CoreScenePath)
                return current;

            if (!File.Exists(CoreScenePath))
            {
                Debug.LogError($"[CorePauseMenuSceneBuilder] Scene not found: {CoreScenePath}");
                return default;
            }

            return EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
        }

        private static Canvas FindOrCreateCanvas()
        {
            GameObject existing = GameObject.Find(CanvasName);
            if (existing != null)
                return existing.GetComponent<Canvas>();

            GameObject go = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void RebuildPauseUi(Transform canvasTransform)
        {
            DeleteIfExists(PauseRootName);
            DeleteIfExists("PauseMenuPanel");
            DeleteIfExists("PauseSettingsPanel");

            GameObject root = CreateUiObject(PauseRootName, canvasTransform);
            Stretch(root.GetComponent<RectTransform>());

            GameObject dimmer = CreateUiObject("PauseDimmer", root.transform, typeof(Image));
            Stretch(dimmer.GetComponent<RectTransform>());
            Image dimmerImage = dimmer.GetComponent<Image>();
            dimmerImage.color = new Color(0f, 0f, 0f, 0.6f);
            dimmerImage.raycastTarget = false;

            GameObject menuPanel = CreatePanel("PauseMenuPanel", root.transform, new Vector2(620f, 620f), true);
            BuildPauseMainPanel(menuPanel.transform);

            GameObject settingsPanel = CreatePanel("PauseSettingsPanel", root.transform, new Vector2(760f, 820f), false);
            BuildPauseSettingsPanel(settingsPanel.transform);
        }

        private static void BuildPauseMainPanel(Transform parent)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            CreateLabel(parent, "PauseTitle", "pause.title", 44f, FontStyles.Bold, TextAlignmentOptions.Center, 58f);
            CreateMenuButton(parent, "RestartButton", "pause.restart");
            CreateMenuButton(parent, "SettingsButton", "menu.settings");
            CreateMenuButton(parent, "MainMenuButton", "pause.main_menu");
            CreateMenuButton(parent, "DesktopButton", "pause.desktop");
        }

        private static void BuildPauseSettingsPanel(Transform parent)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            CreateLabel(parent, "SettingsTitle", "settings.title", 36f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 50f);
            CreateLabel(parent, "SensitivityHeader", "settings.sensitivity", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 42f);
            CreateSliderRow(parent, "MasterSensitivity", "settings.master_sens");
            CreateSliderRow(parent, "HorizontalSensitivity", "settings.horizontal_sens");
            CreateSliderRow(parent, "VerticalSensitivity", "settings.vertical_sens");

            CreateLabel(parent, "SoundHeader", "settings.sound", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 42f);
            CreateSliderRow(parent, "MasterVolume", "settings.master_volume");
            CreateSliderRow(parent, "MusicVolume", "settings.music_volume");
            CreateSliderRow(parent, "SfxVolume", "settings.sfx_volume");

            CreateLanguageRow(parent);
            CreateMenuButton(parent, "BackFromSettingsButton", "menu.back");
        }

        private static void CreateSliderRow(Transform parent, string rowName, string labelKey)
        {
            GameObject row = CreateUiObject(rowName + "Row", parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            VerticalLayoutGroup rowLayout = row.GetComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 4f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = false;
            SetHeight(row, 86f);

            CreateLabel(row.transform, rowName + "Label", labelKey, 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 30f);

            GameObject controls = CreateUiObject(rowName + "Controls", row.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            HorizontalLayoutGroup controlsLayout = controls.GetComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 10f;
            controlsLayout.childAlignment = TextAnchor.MiddleLeft;
            controlsLayout.childControlWidth = false;
            controlsLayout.childControlHeight = true;
            controlsLayout.childForceExpandWidth = false;
            controlsLayout.childForceExpandHeight = false;
            SetHeight(controls, 34f);

            Slider slider = CreateStyledSlider(rowName + "Slider", controls.transform);
            SetWidth(slider.gameObject, 510f, 320f);

            GameObject value = CreateUiObject(rowName + "Value", controls.transform, typeof(TextMeshProUGUI), typeof(LayoutElement));
            TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
            valueText.text = "0";
            valueText.fontSize = 22f;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.color = AccentColor;
            SetWidth(value, 76f, 76f);
            SetHeight(value, 30f);
        }

        private static Slider CreateStyledSlider(string name, Transform parent)
        {
            GameObject root = CreateUiObject(name, parent, typeof(Slider), typeof(LayoutElement));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 28f);

            GameObject bg = CreateUiObject("Background", root.transform, typeof(Image));
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = SliderBackground;

            GameObject fillArea = CreateUiObject("Fill Area", root.transform);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(10f, 4f);
            fillAreaRt.offsetMax = new Vector2(-10f, -4f);

            GameObject fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = AccentColor;

            GameObject handle = CreateUiObject("Handle", root.transform, typeof(Image));
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0f);
            handleRt.anchorMax = new Vector2(0f, 1f);
            handleRt.sizeDelta = new Vector2(16f, 0f);
            handle.GetComponent<Image>().color = AccentColor;

            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            SetHeight(root, 30f);
            return slider;
        }

        private static void CreateLanguageRow(Transform parent)
        {
            GameObject row = CreateUiObject("LanguageRow", parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            SetHeight(row, 56f);

            GameObject label = CreateLabel(row.transform, "LanguageLabel", "settings.language", 26f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 44f);
            SetWidth(label, 170f, 170f);

            CreateArrowButton(row.transform, "LanguagePrevButton", "<");

            GameObject value = CreateUiObject("LanguageValue", row.transform, typeof(TextMeshProUGUI), typeof(LayoutElement));
            TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
            valueText.text = "Language";
            valueText.fontSize = 24f;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = AccentColor;
            SetWidth(value, 180f, 180f);

            CreateArrowButton(row.transform, "LanguageNextButton", ">");
        }

        private static GameObject CreateArrowButton(Transform parent, string name, string textValue)
        {
            GameObject button = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            button.GetComponent<Image>().color = ButtonColor;
            SetWidth(button, 56f, 56f);
            SetHeight(button, 44f);

            GameObject textObj = CreateUiObject("Text", button.transform, typeof(TextMeshProUGUI));
            Stretch(textObj.GetComponent<RectTransform>());
            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = AccentColor;
            return button;
        }

        private static GameObject CreateMenuButton(Transform parent, string name, string key)
        {
            GameObject button = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            button.GetComponent<Image>().color = ButtonColor;
            SetHeight(button, 70f);

            GameObject textObj = CreateUiObject("Text", button.transform, typeof(TextMeshProUGUI), typeof(LocalizedText));
            Stretch(textObj.GetComponent<RectTransform>());

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.fontSize = 30f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = AccentColor;

            textObj.GetComponent<LocalizedText>().SetKey(key);
            return button;
        }

        private static GameObject CreateLabel(Transform parent, string name, string key, float size, FontStyles style, TextAlignmentOptions alignment, float height)
        {
            GameObject go = CreateUiObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement), typeof(LocalizedText));
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = AccentColor;
            go.GetComponent<LocalizedText>().SetKey(key);
            SetHeight(go, height);
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 size, bool active)
        {
            GameObject panel = CreateUiObject(name, parent, typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = PanelColor;
            panel.SetActive(active);
            return panel;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] extraComponents)
        {
            System.Type[] components;
            if (extraComponents == null || extraComponents.Length == 0)
            {
                components = new[] { typeof(RectTransform) };
            }
            else
            {
                components = new System.Type[extraComponents.Length + 1];
                components[0] = typeof(RectTransform);
                for (int i = 0; i < extraComponents.Length; i++)
                    components[i + 1] = extraComponents[i];
            }

            GameObject go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetHeight(GameObject go, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static void SetWidth(GameObject go, float width, float minWidth)
        {
            LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = minWidth;
            le.preferredWidth = width;
        }

        private static void EnsurePauseControllerExists()
        {
            BattlePauseMenuController controller = Object.FindObjectOfType<BattlePauseMenuController>(true);
            if (controller != null)
                return;

            GameObject go = new GameObject("BattlePauseMenuController");
            go.AddComponent<BattlePauseMenuController>();
        }

        private static void DeleteIfExists(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }
    }
}
