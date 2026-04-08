using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TankGame.Session;

namespace TankGame.Menu
{
    public class TankSelectionController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Р РЋР С—Р С‘РЎРѓР С•Р С” Р Т‘Р С•РЎРѓРЎвЂљРЎС“Р С—Р Р…РЎвЂ№РЎвЂ¦ РЎвЂљР В°Р Р…Р С”Р С•Р Р† Р Т‘Р В»РЎРЏ Р Р†РЎвЂ№Р В±Р С•РЎР‚Р В° Р Р† Р СР ВµР Р…РЎР‹.")]
        public List<TankDefinition> tanks = new List<TankDefinition>();

        [Header("Controls")]
        [Tooltip("Р С™Р Р…Р С•Р С—Р С”Р В° Р С—Р ВµРЎР‚Р ВµР С”Р В»РЎР‹РЎвЂЎР ВµР Р…Р С‘РЎРЏ Р Р…Р В° Р С—РЎР‚Р ВµР Т‘РЎвЂ№Р Т‘РЎС“РЎвЂ°Р С‘Р в„– РЎвЂљР В°Р Р…Р С”.")]
        public Button previousButton;
        [Tooltip("Р С™Р Р…Р С•Р С—Р С”Р В° Р С—Р ВµРЎР‚Р ВµР С”Р В»РЎР‹РЎвЂЎР ВµР Р…Р С‘РЎРЏ Р Р…Р В° РЎРѓР В»Р ВµР Т‘РЎС“РЎР‹РЎвЂ°Р С‘Р в„– РЎвЂљР В°Р Р…Р С”.")]
        public Button nextButton;

        [Header("View")]
        [Tooltip("Р СћР ВµР С”РЎРѓРЎвЂљ РЎРѓ Р Р…Р В°Р В·Р Р†Р В°Р Р…Р С‘Р ВµР С Р Р†РЎвЂ№Р В±РЎР‚Р В°Р Р…Р Р…Р С•Р С–Р С• РЎвЂљР В°Р Р…Р С”Р В°.")]
        public TMP_Text tankNameText;
        [Tooltip("Р С›Р С”Р Р…Р С• Р С—РЎР‚Р ВµР Т‘Р С—РЎР‚Р С•РЎРѓР СР С•РЎвЂљРЎР‚Р В° РЎвЂљР В°Р Р…Р С”Р В° (РЎРѓРЎвЂљР В°РЎР‚РЎвЂ№Р в„– Image-РЎРѓР В»Р С•РЎвЂљ, Р С‘РЎРѓР С—Р С•Р В»РЎРЉР В·РЎС“Р ВµРЎвЂљРЎРѓРЎРЏ Р С”Р В°Р С” Р С”Р С•Р Р…РЎвЂљР ВµР в„–Р Р…Р ВµРЎР‚ Р С‘ fallback).")]
        public Image tankPreviewImage;
        [Tooltip("Р РЋР ВµР С–Р СР ВµР Р…РЎвЂљР Р…Р В°РЎРЏ РЎв‚¬Р С”Р В°Р В»Р В° РЎРѓР С”Р С•РЎР‚Р С•РЎРѓРЎвЂљР С‘ (Р С—Р В°Р В»Р С•РЎвЂЎР С”Р С‘ + РЎвЂЎР С‘РЎРѓР В»Р С•).")]
        public SegmentedStatBar speedBar;
        [Tooltip("Р РЋР ВµР С–Р СР ВµР Р…РЎвЂљР Р…Р В°РЎРЏ РЎв‚¬Р С”Р В°Р В»Р В° Р В±РЎР‚Р С•Р Р…Р С‘ (Р С—Р В°Р В»Р С•РЎвЂЎР С”Р С‘ + РЎвЂЎР С‘РЎРѓР В»Р С•).")]
        public SegmentedStatBar armorBar;
        [Tooltip("Р РЋР ВµР С–Р СР ВµР Р…РЎвЂљР Р…Р В°РЎРЏ РЎв‚¬Р С”Р В°Р В»Р В° Р С•Р С–Р Р…Р ВµР Р†Р С•Р в„– Р СР С•РЎвЂ°Р С‘ (Р С—Р В°Р В»Р С•РЎвЂЎР С”Р С‘ + РЎвЂЎР С‘РЎРѓР В»Р С•).")]
        public SegmentedStatBar firepowerBar;
        [Tooltip("Р РЋР ВµР С–Р СР ВµР Р…РЎвЂљР Р…Р В°РЎРЏ РЎв‚¬Р С”Р В°Р В»Р В° РЎС“Р С—РЎР‚Р В°Р Р†Р В»РЎРЏР ВµР СР С•РЎРѓРЎвЂљР С‘ (Р С—Р В°Р В»Р С•РЎвЂЎР С”Р С‘ + РЎвЂЎР С‘РЎРѓР В»Р С•).")]
        public SegmentedStatBar handlingBar;

        [Header("3D Preview")]
        [Tooltip("RawImage Р Т‘Р В»РЎРЏ Р Р†РЎвЂ№Р Р†Р С•Р Т‘Р В° 3D-Р С—РЎР‚Р ВµР Р†РЎРЉРЎР‹. Р вЂўРЎРѓР В»Р С‘ Р С—РЎС“РЎРѓРЎвЂљР С•, РЎРѓР С•Р В·Р Т‘Р В°Р ВµРЎвЂљРЎРѓРЎРЏ Р В°Р Р†РЎвЂљР С•Р СР В°РЎвЂљР С‘РЎвЂЎР ВµРЎРѓР С”Р С‘ Р Р†Р Р…РЎС“РЎвЂљРЎР‚Р С‘ Р С•Р С”Р Р…Р В° preview.")]
        public RawImage previewRawImage;
        [Tooltip("Р СћР С•РЎвЂЎР С”Р В°, Р Р† Р С”Р С•РЎвЂљР С•РЎР‚Р С•Р в„– Р В±РЎС“Р Т‘Р ВµРЎвЂљ РЎРѓРЎвЂљР С•РЎРЏРЎвЂљРЎРЉ 3D-Р СР С•Р Т‘Р ВµР В»РЎРЉ Р Т‘Р В»РЎРЏ Р С—РЎР‚Р ВµР Т‘Р С—РЎР‚Р С•РЎРѓР СР С•РЎвЂљРЎР‚Р В°. Р вЂўРЎРѓР В»Р С‘ Р С—РЎС“РЎРѓРЎвЂљР С•, РЎРѓР С•Р В·Р Т‘Р В°Р ВµРЎвЂљРЎРѓРЎРЏ Р В°Р Р†РЎвЂљР С•Р СР В°РЎвЂљР С‘РЎвЂЎР ВµРЎРѓР С”Р С‘.")]
        public Transform previewModelRoot;
        [Tooltip("Р С™Р В°Р СР ВµРЎР‚Р В°, РЎР‚Р ВµР Р…Р Т‘Р ВµРЎР‚РЎРЏРЎвЂ°Р В°РЎРЏ 3D-Р С—РЎР‚Р ВµР Р†РЎРЉРЎР‹. Р вЂўРЎРѓР В»Р С‘ Р С—РЎС“РЎРѓРЎвЂљР С•, РЎРѓР С•Р В·Р Т‘Р В°Р ВµРЎвЂљРЎРѓРЎРЏ Р В°Р Р†РЎвЂљР С•Р СР В°РЎвЂљР С‘РЎвЂЎР ВµРЎРѓР С”Р С‘.")]
        public Camera previewCamera;
        [Tooltip("Р СџР С•Р В·Р С‘РЎвЂ Р С‘РЎРЏ Р С”Р В°Р СР ВµРЎР‚РЎвЂ№ Р С•РЎвЂљР Р…Р С•РЎРѓР С‘РЎвЂљР ВµР В»РЎРЉР Р…Р С• previewModelRoot.")]
        public Vector3 previewCameraOffset = new Vector3(0f, 1.6f, -4f);
        [Tooltip("Р РЋР С”Р С•РЎР‚Р С•РЎРѓРЎвЂљРЎРЉ РЎР‚РЎС“РЎвЂЎР Р…Р С•Р С–Р С• Р Р†РЎР‚Р В°РЎвЂ°Р ВµР Р…Р С‘РЎРЏ Р СР С•Р Т‘Р ВµР В»Р С‘ Р С—Р С• Р С•РЎРѓР С‘ Y.")]
        public float manualRotateSpeed = 1f;
        [Header("Button Feedback")]
        [Tooltip("Источник звука для фидбека кнопок переключения танка.")]
        public AudioSource buttonFeedbackAudioSource;
        [Tooltip("Общий конфиг параметров фидбека кнопок. Если не задан, пробуем загрузить Resources/Menu/MenuButtonFeedbackConfig.")]
        public MenuButtonFeedbackConfig sharedButtonFeedbackConfig;

        [Header("Stats Hover")]
        [Tooltip("Множитель масштаба строки характеристики при наведении.")]
        [Min(1f)]
        public float statRowHoverScale = 1.03f;
        [Tooltip("Скорость анимации масштаба строки характеристики.")]
        [Min(1f)]
        public float statRowHoverLerpSpeed = 14f;

        private int currentIndex;
        private GameObject currentPreviewInstance;
        private RenderTexture previewRenderTexture;
        private PreviewScanLineEffect previewScanLineEffect;

        private enum TankStatType
        {
            Speed,
            Armor,
            Firepower,
            Handling
        }

        private void Start()
        {
            ApplySharedButtonFeedbackConfig();

            if (previousButton != null)
                previousButton.onClick.AddListener(SelectPrevious);
            if (nextButton != null)
                nextButton.onClick.AddListener(SelectNext);

            currentIndex = Mathf.Clamp(GameSessionSettings.SelectedTankIndex, 0, Mathf.Max(0, tanks.Count - 1));

            EnsureStatBars();
            ApplyStatColors();
            ConfigureButtonFeedbacks();
            EnsurePreviewInfrastructure();
            Refresh();
        }
        public void ApplyButtonFeedbackSettings(
            AudioSource audioSource,
            MenuButtonFeedbackConfig config)
        {
            buttonFeedbackAudioSource = audioSource;
            if (config != null)
                sharedButtonFeedbackConfig = config;
            else if (sharedButtonFeedbackConfig == null)
                sharedButtonFeedbackConfig = MenuButtonFeedbackConfig.LoadDefault();

            ApplyStatColors();
            ConfigureButtonFeedbacks();
            EnsurePreviewInfrastructure();
        }

        private void ConfigureButtonFeedbacks()
        {
            EnsureButtonFeedbackAudioSource();
            ConfigureButtonFeedback(previousButton);
            ConfigureButtonFeedback(nextButton);
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
            feedback.targetButtonImage = button.targetGraphic as Image;
            feedback.Configure(
                GetButtonNormalColor(),
                GetButtonHoverColor(),
                GetButtonPressedColor(),
                GetButtonBackgroundNormalColor(),
                GetButtonBackgroundHoverColor(),
                GetButtonBackgroundPressedColor(),
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

        private Color GetButtonBackgroundNormalColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.normalButtonColor : new Color(0.27f, 0.17f, 0.1f, 0.88f);
        }

        private Color GetButtonBackgroundHoverColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.hoverButtonColor : new Color(0.39f, 0.24f, 0.12f, 0.92f);
        }

        private Color GetButtonBackgroundPressedColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.pressedButtonColor : new Color(0.54f, 0.31f, 0.13f, 0.96f);
        }

        private Color GetStaticTextColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.staticTextColor : new Color(0.92f, 0.78f, 0.55f, 0.96f);
        }

        private Color GetStatLabelTextColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.statLabelTextColor : new Color(0.92f, 0.78f, 0.55f, 0.96f);
        }

        private Color GetStatValueTextColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.statValueTextColor : new Color(1f, 0.72f, 0.36f, 1f);
        }

        private Color GetStatBackgroundColor(TankStatType statType)
        {
            if (sharedButtonFeedbackConfig == null)
                return new Color(0.62f, 0.24f, 0.12f, 0.5f);

            switch (statType)
            {
                case TankStatType.Speed:
                    return sharedButtonFeedbackConfig.speedStatBackgroundColor;
                case TankStatType.Armor:
                    return sharedButtonFeedbackConfig.armorStatBackgroundColor;
                case TankStatType.Firepower:
                    return sharedButtonFeedbackConfig.firepowerStatBackgroundColor;
                case TankStatType.Handling:
                    return sharedButtonFeedbackConfig.handlingStatBackgroundColor;
                default:
                    return sharedButtonFeedbackConfig.statBackgroundColor;
            }
        }

        private Color GetStatFillColor(TankStatType statType)
        {
            if (sharedButtonFeedbackConfig == null)
                return new Color(1f, 0.67f, 0.25f, 0.96f);

            switch (statType)
            {
                case TankStatType.Speed:
                    return sharedButtonFeedbackConfig.speedStatFillColor;
                case TankStatType.Armor:
                    return sharedButtonFeedbackConfig.armorStatFillColor;
                case TankStatType.Firepower:
                    return sharedButtonFeedbackConfig.firepowerStatFillColor;
                case TankStatType.Handling:
                    return sharedButtonFeedbackConfig.handlingStatFillColor;
                default:
                    return sharedButtonFeedbackConfig.statFillColor;
            }
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

        private Sprite GetPreviewScanLineSprite()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.previewScanLineSprite : null;
        }

        private Color GetPreviewScanLineColor()
        {
            return sharedButtonFeedbackConfig != null ? sharedButtonFeedbackConfig.previewScanLineColor : new Color(1f, 0.67f, 0.25f, 0.45f);
        }

        private float GetPreviewScanLineHeight()
        {
            return sharedButtonFeedbackConfig != null ? Mathf.Max(1f, sharedButtonFeedbackConfig.previewScanLineHeight) : 12f;
        }

        private float GetPreviewScanLineSpeed()
        {
            return sharedButtonFeedbackConfig != null ? Mathf.Max(0.01f, sharedButtonFeedbackConfig.previewScanLineSpeed) : 120f;
        }

        private void EnsureStatBars()
        {
            speedBar = EnsureStatBar(speedBar, "tank.speed_Row", TankStatType.Speed);
            armorBar = EnsureStatBar(armorBar, "tank.armor_Row", TankStatType.Armor);
            firepowerBar = EnsureStatBar(firepowerBar, "tank.firepower_Row", TankStatType.Firepower);
            handlingBar = EnsureStatBar(handlingBar, "tank.handling_Row", TankStatType.Handling);
        }

        private void ApplyStatColors()
        {
            if (tankNameText != null)
                tankNameText.color = GetStaticTextColor();

            ApplyStatBarColors(speedBar, TankStatType.Speed);
            ApplyStatBarColors(armorBar, TankStatType.Armor);
            ApplyStatBarColors(firepowerBar, TankStatType.Firepower);
            ApplyStatBarColors(handlingBar, TankStatType.Handling);
        }

        private void ApplyStatBarColors(SegmentedStatBar statBar, TankStatType statType)
        {
            if (statBar == null)
                return;

            Color background = GetStatBackgroundColor(statType);
            Color label = GetStatLabelTextColor();
            Color fill = GetStatFillColor(statType);
            Color value = GetStatValueTextColor();

            statBar.backgroundColor = background;
            statBar.fillColor = fill;

            if (statBar.backgroundContainer != null)
                TintBarContainer(statBar.backgroundContainer, background);

            if (statBar.fillContainer != null)
                TintBarContainer(statBar.fillContainer, fill);

            TintStatRowText(statBar, label, value);
        }

        private static void TintBarContainer(RectTransform container, Color color)
        {
            if (container == null)
                return;

            Image[] images = container.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null)
                    image.color = color;
            }
        }

        private static void TintStatRowText(SegmentedStatBar statBar, Color labelColor, Color valueColor)
        {
            if (statBar == null)
                return;

            TMP_Text[] texts = statBar.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                text.color = statBar.valueText == text ? valueColor : labelColor;
            }
        }

        private SegmentedStatBar EnsureStatBar(SegmentedStatBar current, string rowName, TankStatType statType)
        {
            if (current != null)
                return current;

            Transform scope = transform.parent != null ? transform.parent : transform;
            Transform row = FindChildRecursive(scope, rowName);
            if (row == null)
                return null;

            SegmentedStatBar existing = row.GetComponent<SegmentedStatBar>();
            if (existing != null)
            {
                EnsureStatRowHoverFeedback(row);
                return existing;
            }

            RectTransform barRoot = row.Find("BarsRoot") as RectTransform;
            if (barRoot == null)
                barRoot = row.Find("BarRoot") as RectTransform;
            if (barRoot == null)
            {
                GameObject barRootObj = new GameObject("BarsRoot", typeof(RectTransform), typeof(LayoutElement));
                barRootObj.transform.SetParent(row, false);
                barRoot = barRootObj.GetComponent<RectTransform>();
                LayoutElement barLayout = barRootObj.GetComponent<LayoutElement>();
                barLayout.minWidth = 320f;
                barLayout.preferredWidth = 320f;
                barLayout.minHeight = 20f;
                barLayout.preferredHeight = 20f;
            }

            Image legacyRootImage = barRoot.GetComponent<Image>();
            if (legacyRootImage != null)
                legacyRootImage.color = Color.clear;

            for (int i = barRoot.childCount - 1; i >= 0; i--)
                Destroy(barRoot.GetChild(i).gameObject);

            RectTransform bg = CreateBarsContainer("BackgroundBars", barRoot);
            RectTransform fill = CreateBarsContainer("FillBars", barRoot);

            GameObject segmentTemplateObj = new GameObject("SegmentTemplate", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            segmentTemplateObj.transform.SetParent(fill, false);
            LayoutElement segmentLayout = segmentTemplateObj.GetComponent<LayoutElement>();
            segmentLayout.minWidth = 8f;
            segmentLayout.preferredWidth = 8f;
            Image segmentTemplate = segmentTemplateObj.GetComponent<Image>();
            segmentTemplate.color = GetStatFillColor(statType);
            segmentTemplateObj.SetActive(false);

            TMP_Text valueText = row.Find("Value") != null ? row.Find("Value").GetComponent<TMP_Text>() : null;
            if (valueText == null)
            {
                GameObject valueObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                valueObj.transform.SetParent(row, false);
                TextMeshProUGUI tmp = valueObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "0";
                tmp.fontSize = 20f;
                tmp.alignment = TextAlignmentOptions.Right;
                TMP_Text rowLabel = row.GetComponentInChildren<TMP_Text>(true);
                tmp.color = rowLabel != null ? rowLabel.color : GetStatValueTextColor();
                LayoutElement valueLayout = valueObj.GetComponent<LayoutElement>();
                valueLayout.minWidth = 52f;
                valueLayout.preferredWidth = 52f;
                valueText = tmp;
            }

            SegmentedStatBar statBar = row.gameObject.AddComponent<SegmentedStatBar>();
            statBar.backgroundContainer = bg;
            statBar.fillContainer = fill;
            statBar.segmentTemplate = segmentTemplate;
            statBar.valueText = valueText;
            statBar.segmentCount = 10;
            statBar.backgroundColor = GetStatBackgroundColor(statType);
            statBar.fillColor = GetStatFillColor(statType);
            if (statBar.valueText != null)
                statBar.valueText.color = GetStatValueTextColor();
            ApplyStatBarColors(statBar, statType);
            EnsureStatRowHoverFeedback(row);
            return statBar;
        }
        private void EnsureStatRowHoverFeedback(Transform row)
        {
            if (row == null)
                return;

            Image raycastImage = row.GetComponent<Image>();
            if (raycastImage == null)
            {
                raycastImage = row.gameObject.AddComponent<Image>();
                raycastImage.color = new Color(0f, 0f, 0f, 0f);
                raycastImage.raycastTarget = true;
            }

            UiHoverScaleFeedback hover = row.GetComponent<UiHoverScaleFeedback>();
            if (hover == null)
                hover = row.gameObject.AddComponent<UiHoverScaleFeedback>();

            hover.target = row as RectTransform;
            hover.Configure(statRowHoverScale, statRowHoverLerpSpeed);
        }
        private static RectTransform CreateBarsContainer(string name, Transform parent)
        {
            GameObject container = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            container.transform.SetParent(parent, false);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return rt;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void OnDestroy()
        {
            if (previousButton != null)
                previousButton.onClick.RemoveListener(SelectPrevious);
            if (nextButton != null)
                nextButton.onClick.RemoveListener(SelectNext);

            if (currentPreviewInstance != null)
                Destroy(currentPreviewInstance);

            if (previewCamera != null)
                previewCamera.targetTexture = null;

            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }
        }

        public void SelectPrevious()
        {
            if (tanks.Count == 0)
                return;

            currentIndex = (currentIndex - 1 + tanks.Count) % tanks.Count;
            GameSessionSettings.SelectedTankIndex = currentIndex;
            Refresh();
        }

        public void SelectNext()
        {
            if (tanks.Count == 0)
                return;

            currentIndex = (currentIndex + 1) % tanks.Count;
            GameSessionSettings.SelectedTankIndex = currentIndex;
            Refresh();
        }

        public GameObject GetSelectedTankPrefab()
        {
            if (tanks.Count == 0)
                return null;

            int index = Mathf.Clamp(currentIndex, 0, tanks.Count - 1);
            return tanks[index].playerPrefab;
        }

        public void RotatePreview(float yawDelta)
        {
            if (currentPreviewInstance == null)
                return;

            currentPreviewInstance.transform.Rotate(0f, yawDelta * manualRotateSpeed, 0f, Space.Self);
        }

        private void Refresh()
        {
            if (tanks.Count == 0)
            {
                if (tankNameText != null)
                    tankNameText.text = "Tank";

                DestroyPreviewInstance();
                SetFallbackSpriteVisible(false, null);
                SetStat(speedBar, 0f);
                SetStat(armorBar, 0f);
                SetStat(firepowerBar, 0f);
                SetStat(handlingBar, 0f);
                return;
            }

            TankDefinition selected = tanks[Mathf.Clamp(currentIndex, 0, tanks.Count - 1)];

            if (tankNameText != null)
                tankNameText.text = string.IsNullOrWhiteSpace(selected.displayName)
                    ? $"Tank {currentIndex + 1}"
                    : selected.displayName;

            bool built3DPreview = Build3DPreview(selected);
            if (!built3DPreview)
                SetFallbackSpriteVisible(true, selected.previewSprite);

            SetStat(speedBar, selected.speed);
            SetStat(armorBar, selected.armor);
            SetStat(firepowerBar, selected.firepower);
            SetStat(handlingBar, selected.handling);
        }

        private bool Build3DPreview(TankDefinition definition)
        {
            EnsurePreviewInfrastructure();
            if (previewModelRoot == null || previewCamera == null || previewRawImage == null)
                return false;

            GameObject prefab = definition.previewModelPrefab != null
                ? definition.previewModelPrefab
                : definition.playerPrefab;

            if (prefab == null)
                return false;

            DestroyPreviewInstance();

            currentPreviewInstance = Instantiate(prefab, previewModelRoot);
            currentPreviewInstance.name = prefab.name + "_Preview";
            currentPreviewInstance.transform.localPosition = Vector3.zero;
            currentPreviewInstance.transform.localRotation = Quaternion.identity;

            float scale = Mathf.Max(0.01f, definition.previewModelScale);
            currentPreviewInstance.transform.localScale = Vector3.one * scale;

            DisableNonVisualComponents(currentPreviewInstance);
            FocusPreviewCamera(currentPreviewInstance);

            if (previewRawImage != null)
                previewRawImage.enabled = true;
            if (tankPreviewImage != null)
                tankPreviewImage.enabled = false;

            return true;
        }

        private void DestroyPreviewInstance()
        {
            if (currentPreviewInstance == null)
                return;

            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }

        private void EnsurePreviewInfrastructure()
        {
            EnsureRawImageTarget();
            EnsurePreviewScanLine();
            EnsurePreviewRoot();
            EnsurePreviewCamera();
            EnsureDragRotator();
        }

        private void EnsureRawImageTarget()
        {
            if (previewRawImage != null)
                return;

            Transform parent = tankPreviewImage != null ? tankPreviewImage.transform : transform;
            GameObject rawObj = new GameObject("TankPreview3D", typeof(RectTransform), typeof(RawImage));
            rawObj.transform.SetParent(parent, false);

            RectTransform rt = rawObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            previewRawImage = rawObj.GetComponent<RawImage>();
            previewRawImage.color = Color.white;
            previewRawImage.raycastTarget = true;
        }

        private void EnsurePreviewScanLine()
        {
            RectTransform host = tankPreviewImage != null
                ? tankPreviewImage.rectTransform
                : (previewRawImage != null ? previewRawImage.rectTransform : null);

            if (host == null)
                return;

            if (previewScanLineEffect != null && previewScanLineEffect.transform != host)
                previewScanLineEffect = null;

            if (previewScanLineEffect == null)
            {
                previewScanLineEffect = host.GetComponent<PreviewScanLineEffect>();
                if (previewScanLineEffect == null)
                    previewScanLineEffect = host.gameObject.AddComponent<PreviewScanLineEffect>();
            }

            ApplyPreviewScanLineConfig();
        }

        private void ApplyPreviewScanLineConfig()
        {
            if (previewScanLineEffect == null)
                return;

            previewScanLineEffect.Configure(
                GetPreviewScanLineSprite(),
                GetPreviewScanLineColor(),
                GetPreviewScanLineHeight(),
                GetPreviewScanLineSpeed());
        }

        private void EnsurePreviewRoot()
        {
            if (previewModelRoot != null)
                return;

            GameObject root = new GameObject("TankPreviewRoot");
            root.transform.position = new Vector3(1000f, 1000f, 1000f);
            previewModelRoot = root.transform;
        }

        private void EnsurePreviewCamera()
        {
            if (previewCamera == null)
            {
                GameObject cameraObj = new GameObject("TankPreviewCamera", typeof(Camera));
                previewCamera = cameraObj.GetComponent<Camera>();
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                previewCamera.fieldOfView = 35f;
                previewCamera.nearClipPlane = 0.01f;
                previewCamera.farClipPlane = 200f;
                previewCamera.enabled = true;

                GameObject lightObj = new GameObject("TankPreviewLight", typeof(Light));
                lightObj.transform.SetParent(cameraObj.transform, false);
                lightObj.transform.localPosition = new Vector3(1f, 2f, -1f);
                lightObj.transform.localRotation = Quaternion.Euler(35f, -35f, 0f);
                Light light = lightObj.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
            }

            if (previewRenderTexture == null)
            {
                previewRenderTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32)
                {
                    name = "TankPreviewRT"
                };
                previewRenderTexture.Create();
            }

            previewCamera.targetTexture = previewRenderTexture;
            if (previewRawImage != null)
                previewRawImage.texture = previewRenderTexture;
        }

        private void EnsureDragRotator()
        {
            GameObject dragTarget = previewRawImage != null
                ? previewRawImage.gameObject
                : (tankPreviewImage != null ? tankPreviewImage.gameObject : null);

            if (dragTarget == null)
                return;

            TankPreviewDragRotator rotator = dragTarget.GetComponent<TankPreviewDragRotator>();
            if (rotator == null)
                rotator = dragTarget.AddComponent<TankPreviewDragRotator>();

            rotator.selectionController = this;
        }

        private void FocusPreviewCamera(GameObject model)
        {
            if (previewCamera == null || previewModelRoot == null || model == null)
                return;

            Bounds bounds = CalculateModelBounds(model);
            Vector3 center = bounds.center;
            previewCamera.transform.position = center + previewCameraOffset;
            previewCamera.transform.LookAt(center);
        }

        private static Bounds CalculateModelBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static void DisableNonVisualComponents(GameObject go)
        {
            MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < scripts.Length; i++)
            {
                MonoBehaviour script = scripts[i];
                if (script != null)
                    script.enabled = false;
            }

            Rigidbody[] rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                    rigidbodies[i].isKinematic = true;
            }

            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            AudioSource[] audios = go.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++)
            {
                if (audios[i] != null)
                    audios[i].enabled = false;
            }
        }

        private void SetFallbackSpriteVisible(bool visible, Sprite sprite)
        {
            if (previewRawImage != null)
                previewRawImage.enabled = !visible;

            if (tankPreviewImage == null)
                return;

            tankPreviewImage.enabled = visible;
            tankPreviewImage.sprite = sprite;
        }

        private static void SetStat(SegmentedStatBar statBar, float value)
        {
            if (statBar == null)
                return;

            statBar.SetNormalized(value);
        }
    }
}










