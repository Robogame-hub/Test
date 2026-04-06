using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankGame.Menu
{
    /// <summary>
    /// Applies a warm desert visual pass to MainMenu/Lobby/Core pause UI without changing scene object bindings.
    /// </summary>
    public static class MenuDesertTheme
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string LobbySceneName = "Lobby";
        private const string CoreSceneName = "Core";

        private static readonly Color PanelPrimary = new Color(0.13f, 0.08f, 0.05f, 0.78f);
        private static readonly Color PanelSecondary = new Color(0.1f, 0.07f, 0.05f, 0.48f);
        private static readonly Color PanelInset = new Color(0.18f, 0.11f, 0.06f, 0.56f);
        private static readonly Color PanelStats = new Color(0.2f, 0.12f, 0.06f, 0.52f);
        private static readonly Color AccentLine = new Color(0.97f, 0.72f, 0.34f, 0.65f);

        private static readonly Color ButtonBase = new Color(0.27f, 0.17f, 0.1f, 0.88f);
        private static readonly Color ButtonHover = new Color(0.39f, 0.24f, 0.12f, 0.92f);
        private static readonly Color ButtonPressed = new Color(0.54f, 0.31f, 0.13f, 0.96f);
        private static readonly Color ButtonDisabled = new Color(0.2f, 0.15f, 0.11f, 0.45f);

        private static readonly Color TextPrimary = new Color(0.96f, 0.86f, 0.67f, 1f);
        private static readonly Color TextSecondary = new Color(0.92f, 0.78f, 0.55f, 0.96f);
        private static readonly Color TextHeader = new Color(0.99f, 0.9f, 0.73f, 1f);
        private static readonly Color TextValue = new Color(1f, 0.72f, 0.36f, 1f);

        private static readonly Color SliderBackground = new Color(0.19f, 0.12f, 0.07f, 0.92f);
        private static readonly Color SliderFill = new Color(0.97f, 0.58f, 0.2f, 0.98f);
        private static readonly Color SliderHandle = new Color(0.99f, 0.79f, 0.44f, 1f);

        private static readonly Color StatBackground = new Color(0.62f, 0.24f, 0.12f, 0.5f);
        private static readonly Color StatFill = new Color(1f, 0.67f, 0.25f, 0.96f);

        public static void ApplyScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            if (scene.name == MainMenuSceneName)
            {
                ApplyMainMenu(scene);
                return;
            }

            if (scene.name == LobbySceneName)
            {
                ApplyLobby(scene);
                return;
            }

            if (scene.name == CoreSceneName)
                ApplyCorePause(scene);
        }

        public static void ApplyRoomEntry(GameObject row)
        {
            if (row == null)
                return;

            if (row.TryGetComponent(out Image rowImage))
                rowImage.color = PanelInset;

            if (row.TryGetComponent(out HorizontalLayoutGroup layout))
            {
                layout.spacing = 12f;
                layout.padding = IsPortrait()
                    ? new RectOffset(10, 10, 8, 8)
                    : new RectOffset(14, 14, 8, 8);
            }

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                bool isButtonLabel = text.transform.parent != null
                    && text.transform.parent.GetComponent<Button>() != null;
                text.color = isButtonLabel ? TextPrimary : TextSecondary;
            }

            Button joinButton = row.GetComponentInChildren<Button>(true);
            if (joinButton != null)
                StyleButton(joinButton);
        }

        private static void ApplyMainMenu(Scene scene)
        {
            float menuWidth = IsPortrait() ? 0.58f : 0.34f;
            SetPanelAnchorsX(scene, "LeftPanel", 0f, menuWidth);
            SetPanelAnchorsX(scene, "SettingsPanel", 0f, menuWidth);
            SetPanelAnchorsX(scene, "SandboxMatchPanel", 0f, menuWidth);
            SetPanelAnchorsX(scene, "RightPanel", menuWidth, 1f);

            GameObject leftPanel = FindSceneObjectByName(scene, "LeftPanel");
            GameObject settingsPanel = FindSceneObjectByName(scene, "SettingsPanel");
            GameObject sandboxPanel = FindSceneObjectByName(scene, "SandboxMatchPanel");
            GameObject rightPanel = FindSceneObjectByName(scene, "RightPanel");
            GameObject tankPanel = FindSceneObjectByName(scene, "TankPanel");
            GameObject previewBlock = FindSceneObjectByName(scene, "PreviewBlock");
            GameObject previewSquare = FindSceneObjectByName(scene, "TankPreviewSquare");
            GameObject statsBlock = FindSceneObjectByName(scene, "StatsBlock");

            SetImageColor(leftPanel, PanelPrimary);
            SetImageColor(settingsPanel, PanelPrimary);
            SetImageColor(sandboxPanel, PanelPrimary);
            SetImageColor(rightPanel, PanelSecondary);
            SetImageColor(tankPanel, PanelPrimary);
            SetImageColor(previewBlock, PanelInset);
            SetImageColor(previewSquare, PanelInset);
            SetImageColor(statsBlock, PanelStats);

            TuneVerticalLayout(leftPanel, IsPortrait() ? new RectOffset(24, 18, 34, 34) : new RectOffset(48, 18, 52, 52), IsPortrait() ? 10f : 14f, TextAnchor.MiddleLeft);
            TuneVerticalLayout(settingsPanel, IsPortrait() ? new RectOffset(20, 16, 24, 20) : new RectOffset(18, 16, 20, 16), 10f, TextAnchor.UpperLeft);
            TuneVerticalLayout(sandboxPanel, IsPortrait() ? new RectOffset(20, 16, 24, 20) : new RectOffset(18, 16, 20, 16), 12f, TextAnchor.UpperLeft);
            TuneVerticalLayout(tankPanel, IsPortrait() ? new RectOffset(14, 14, 16, 16) : new RectOffset(20, 20, 20, 20), 12f, TextAnchor.UpperCenter);
            TuneVerticalLayout(statsBlock, new RectOffset(12, 12, 12, 12), 8f, TextAnchor.UpperLeft);

            EnsureAccentLine(leftPanel, "DesertTopLine");
            EnsureAccentLine(settingsPanel, "DesertTopLine");
            EnsureAccentLine(sandboxPanel, "DesertTopLine");
            EnsureAccentLine(tankPanel, "DesertTopLine");

            StyleButtonByName(scene, "PlayButton");
            StyleButtonByName(scene, "SandboxButton");
            StyleButtonByName(scene, "SettingsButton");
            StyleButtonByName(scene, "ExitButton");
            StyleButtonByName(scene, "BackFromSettingsButton");
            StyleButtonByName(scene, "StartSandboxMatchButton");
            StyleButtonByName(scene, "BackFromSandboxMatchButton");
            StyleButtonByName(scene, "SandboxBotsPrevButton");
            StyleButtonByName(scene, "SandboxBotsNextButton");
            StyleButtonByName(scene, "LangPrev");
            StyleButtonByName(scene, "LangNext");
            StyleButtonByName(scene, "PrevTank");
            StyleButtonByName(scene, "NextTank");

            GameObject canvas = FindSceneObjectByName(scene, "MainMenuCanvas");
            StyleSlidersInHierarchy(canvas);
            StyleTextHierarchy(canvas);
            StyleStatBars(scene);
        }

        private static void ApplyLobby(Scene scene)
        {
            bool portrait = IsPortrait();
            float rightAnchor = portrait ? 0.96f : 0.58f;

            SetPanelAnchors(scene, "LobbyRoot", new Vector2(0.04f, 0.08f), new Vector2(rightAnchor, 0.92f));

            GameObject lobbyRoot = FindSceneObjectByName(scene, "LobbyRoot");
            GameObject roomList = FindSceneObjectByName(scene, "RoomList");
            GameObject viewport = FindSceneObjectByName(scene, "Viewport");
            GameObject template = FindSceneObjectByName(scene, "RoomEntryTemplate");

            SetImageColor(lobbyRoot, PanelPrimary);
            SetImageColor(roomList, PanelInset);
            SetImageColor(viewport, new Color(0.3f, 0.2f, 0.12f, 0.24f));
            EnsureAccentLine(lobbyRoot, "DesertTopLine");

            if (roomList != null)
            {
                LayoutElement roomListLayout = roomList.GetComponent<LayoutElement>();
                if (roomListLayout != null)
                {
                    float targetHeight = portrait ? 330f : 420f;
                    roomListLayout.minHeight = targetHeight;
                    roomListLayout.preferredHeight = targetHeight;
                }
            }

            TuneVerticalLayout(lobbyRoot, portrait ? new RectOffset(24, 24, 22, 22) : new RectOffset(36, 36, 30, 30), portrait ? 10f : 12f, TextAnchor.UpperLeft);

            StyleButtonByName(scene, "RefreshButton");
            StyleButtonByName(scene, "CreateButton");
            StyleButtonByName(scene, "BackButton");
            StyleButtonByName(scene, "JoinButton");

            if (template != null)
                ApplyRoomEntry(template);

            GameObject canvas = FindSceneObjectByName(scene, "LobbyCanvas");
            StyleTextHierarchy(canvas);

            GameObject nicknameInput = FindSceneObjectByName(scene, "NicknameInput");
            if (nicknameInput != null && nicknameInput.TryGetComponent(out TMP_InputField inputField))
                StyleInputField(inputField);
        }

        private static void ApplyCorePause(Scene scene)
        {
            GameObject pauseRoot = FindSceneObjectByName(scene, "PauseUIRoot");
            if (pauseRoot == null)
                return;

            SetImageColor(FindSceneObjectByName(scene, "PauseDimmer"), new Color(0.15f, 0.08f, 0.03f, 0.62f));
            SetImageColor(FindSceneObjectByName(scene, "PauseMenuPanel"), PanelPrimary);
            SetImageColor(FindSceneObjectByName(scene, "PauseSettingsPanel"), PanelPrimary);

            EnsureAccentLine(FindSceneObjectByName(scene, "PauseMenuPanel"), "DesertTopLine");
            EnsureAccentLine(FindSceneObjectByName(scene, "PauseSettingsPanel"), "DesertTopLine");

            StyleButtonByName(scene, "RestartButton");
            StyleButtonByName(scene, "SettingsButton");
            StyleButtonByName(scene, "MainMenuButton");
            StyleButtonByName(scene, "DesktopButton");
            StyleButtonByName(scene, "LanguagePrevButton");
            StyleButtonByName(scene, "LanguageNextButton");
            StyleButtonByName(scene, "BackFromSettingsButton");

            StyleSlidersInHierarchy(pauseRoot);
            StyleTextHierarchy(pauseRoot);
        }

        private static void StyleButtonByName(Scene scene, string name)
        {
            GameObject go = FindSceneObjectByName(scene, name);
            if (go == null)
                return;

            Button button = go.GetComponent<Button>();
            if (button != null)
                StyleButton(button);
        }

        private static void StyleButton(Button button)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = ButtonBase;
                EnsureShadow(image, new Color(0f, 0f, 0f, 0.36f), new Vector2(0f, -3f));
            }

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonBase;
            colors.highlightedColor = ButtonHover;
            colors.selectedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.disabledColor = ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.color = TextPrimary;
                EnsureShadow(text, new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, -2f));
            }
        }

        private static void StyleSlidersInHierarchy(GameObject root)
        {
            if (root == null)
                return;

            Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
                StyleSlider(sliders[i]);
        }

        private static void StyleSlider(Slider slider)
        {
            if (slider == null)
                return;

            Transform background = slider.transform.Find("Background");
            if (background != null && background.TryGetComponent(out Image bgImage))
            {
                bgImage.color = SliderBackground;
                bgImage.raycastTarget = false;
            }

            if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fillImage))
                fillImage.color = SliderFill;

            if (slider.handleRect != null && slider.handleRect.TryGetComponent(out Image handleImage))
            {
                handleImage.color = SliderHandle;
                EnsureShadow(handleImage, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -1f));
            }

            Transform fillArea = slider.transform.Find("Fill Area");
            if (fillArea != null && fillArea is RectTransform fillAreaRt)
            {
                fillAreaRt.offsetMin = new Vector2(10f, 4f);
                fillAreaRt.offsetMax = new Vector2(-10f, -4f);
            }
        }

        private static void StyleStatBars(Scene scene)
        {
            SegmentedStatBar[] bars = UnityEngine.Object.FindObjectsOfType<SegmentedStatBar>(true);
            for (int i = 0; i < bars.Length; i++)
            {
                SegmentedStatBar bar = bars[i];
                if (bar == null || bar.gameObject.scene != scene)
                    continue;

                bar.backgroundColor = StatBackground;
                bar.fillColor = StatFill;

                TintBarChildren(bar.backgroundContainer, StatBackground);
                TintBarChildren(bar.fillContainer, StatFill);

                if (bar.valueText != null)
                    bar.valueText.color = TextValue;
            }
        }

        private static void TintBarChildren(RectTransform root, Color color)
        {
            if (root == null)
                return;

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null)
                    image.color = color;
            }
        }

        private static void StyleInputField(TMP_InputField input)
        {
            if (input == null)
                return;

            if (input.TryGetComponent(out Image image))
                image.color = new Color(0.2f, 0.12f, 0.07f, 0.88f);

            if (input.textComponent != null)
                input.textComponent.color = TextPrimary;

            if (input.placeholder is TMP_Text placeholder)
                placeholder.color = new Color(TextSecondary.r, TextSecondary.g, TextSecondary.b, 0.55f);
        }

        private static void StyleTextHierarchy(GameObject root)
        {
            if (root == null)
                return;

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                bool isButtonText = text.transform.parent != null
                    && text.transform.parent.GetComponent<Button>() != null;
                if (isButtonText)
                {
                    text.color = TextPrimary;
                    continue;
                }

                string lowerName = text.name.ToLowerInvariant();
                bool isHeader = lowerName.Contains("title") || lowerName.Contains("header");
                bool isValue = lowerName.Contains("value") || lowerName.Contains("tankname");

                if (isHeader)
                {
                    text.color = TextHeader;
                    text.fontStyle |= FontStyles.Bold;
                    EnsureShadow(text, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -2f));
                }
                else if (isValue)
                {
                    text.color = TextValue;
                }
                else
                {
                    text.color = TextSecondary;
                }
            }
        }

        private static void TuneVerticalLayout(GameObject go, RectOffset padding, float spacing, TextAnchor alignment)
        {
            if (go == null)
                return;

            VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                return;

            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
        }

        private static void SetImageColor(GameObject go, Color color)
        {
            if (go == null)
                return;

            Image image = go.GetComponent<Image>();
            if (image == null)
                return;

            image.color = color;
        }

        private static void EnsureAccentLine(GameObject panel, string objectName)
        {
            if (panel == null)
                return;

            Transform existing = panel.transform.Find(objectName);
            Image image;
            if (existing == null)
            {
                GameObject line = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                line.transform.SetParent(panel.transform, false);
                RectTransform lineRt = line.GetComponent<RectTransform>();
                lineRt.anchorMin = new Vector2(0f, 1f);
                lineRt.anchorMax = new Vector2(1f, 1f);
                lineRt.pivot = new Vector2(0.5f, 1f);
                lineRt.offsetMin = new Vector2(0f, -3f);
                lineRt.offsetMax = Vector2.zero;
                image = line.GetComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                image = existing.GetComponent<Image>();
                if (image == null)
                    return;
            }

            image.color = AccentLine;
        }

        private static void EnsureShadow(Graphic target, Color color, Vector2 distance)
        {
            if (target == null)
                return;

            Shadow shadow = target.GetComponent<Shadow>();
            if (shadow == null)
                shadow = target.gameObject.AddComponent<Shadow>();

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void SetPanelAnchorsX(Scene scene, string objectName, float minX, float maxX)
        {
            GameObject go = FindSceneObjectByName(scene, objectName);
            if (go == null)
                return;

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(minX, rect.anchorMin.y);
            rect.anchorMax = new Vector2(maxX, rect.anchorMax.y);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }

        private static void SetPanelAnchors(Scene scene, string objectName, Vector2 min, Vector2 max)
        {
            GameObject go = FindSceneObjectByName(scene, objectName);
            if (go == null)
                return;

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool IsPortrait()
        {
            return Screen.height > Screen.width;
        }

        private static GameObject FindSceneObjectByName(Scene scene, string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(objectName))
                return null;

            GameObject active = GameObject.Find(objectName);
            if (active != null && active.scene == scene)
                return active;

            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform tr = all[i];
                if (tr == null)
                    continue;

                GameObject go = tr.gameObject;
                if (go == null)
                    continue;
                if (go.scene != scene)
                    continue;
                if (!string.Equals(go.name, objectName, StringComparison.Ordinal))
                    continue;
                return go;
            }

            return null;
        }
    }
}
