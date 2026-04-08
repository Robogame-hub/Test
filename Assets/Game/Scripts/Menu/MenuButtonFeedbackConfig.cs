using UnityEngine;
using TMPro;

namespace TankGame.Menu
{
    [CreateAssetMenu(fileName = "MenuButtonFeedbackConfig", menuName = "TankGame/UI/Menu Button Feedback Config")]
    public class MenuButtonFeedbackConfig : ScriptableObject
    {
        public const string DefaultResourcesPath = "Menu/MenuButtonFeedbackConfig";

        [Header("Text Colors")]
        [Tooltip("Base button text color.")]
        public Color normalTextColor = new Color(0.96f, 0.86f, 0.67f, 1f);
        [Tooltip("Button text color on hover.")]
        public Color hoverTextColor = new Color(1f, 0.74f, 0.37f, 1f);
        [Tooltip("Button text color on press.")]
        public Color pressedTextColor = new Color(1f, 0.96f, 0.87f, 1f);

        [Header("Button Colors")]
        [Tooltip("Base button background color.")]
        public Color normalButtonColor = new Color(0.27f, 0.17f, 0.1f, 0.88f);
        [Tooltip("Button background color on hover.")]
        public Color hoverButtonColor = new Color(0.39f, 0.24f, 0.12f, 0.92f);
        [Tooltip("Button background color on press.")]
        public Color pressedButtonColor = new Color(0.54f, 0.31f, 0.13f, 0.96f);

        [Header("Static Text")]
        [Tooltip("Color for non-clickable UI text.")]
        public Color staticTextColor = new Color(0.92f, 0.78f, 0.55f, 0.96f);

        [Header("Stats Colors")]
        [Tooltip("Label text color for stat rows.")]
        public Color statLabelTextColor = new Color(0.92f, 0.78f, 0.55f, 0.96f);
        [Tooltip("Background segments color for stat bars.")]
        public Color statBackgroundColor = new Color(0.62f, 0.24f, 0.12f, 0.5f);
        [Tooltip("Fill segments color for stat bars.")]
        public Color statFillColor = new Color(1f, 0.67f, 0.25f, 0.96f);
        [Tooltip("Numeric value color for stat bars.")]
        public Color statValueTextColor = new Color(1f, 0.72f, 0.36f, 1f);

        [Header("Stats Colors Per Parameter")]
        [Tooltip("Speed stat background color.")]
        public Color speedStatBackgroundColor = new Color(0.62f, 0.24f, 0.12f, 0.5f);
        [Tooltip("Speed stat fill color.")]
        public Color speedStatFillColor = new Color(1f, 0.67f, 0.25f, 0.96f);
        [Tooltip("Armor stat background color.")]
        public Color armorStatBackgroundColor = new Color(0.62f, 0.24f, 0.12f, 0.5f);
        [Tooltip("Armor stat fill color.")]
        public Color armorStatFillColor = new Color(1f, 0.67f, 0.25f, 0.96f);
        [Tooltip("Firepower stat background color.")]
        public Color firepowerStatBackgroundColor = new Color(0.62f, 0.24f, 0.12f, 0.5f);
        [Tooltip("Firepower stat fill color.")]
        public Color firepowerStatFillColor = new Color(1f, 0.67f, 0.25f, 0.96f);
        [Tooltip("Handling stat background color.")]
        public Color handlingStatBackgroundColor = new Color(0.62f, 0.24f, 0.12f, 0.5f);
        [Tooltip("Handling stat fill color.")]
        public Color handlingStatFillColor = new Color(1f, 0.67f, 0.25f, 0.96f);

        [Header("Slider Colors")]
        [Tooltip("Slider track background color.")]
        public Color sliderBackgroundColor = new Color(0.19f, 0.12f, 0.07f, 0.92f);
        [Tooltip("Slider fill color.")]
        public Color sliderFillColor = new Color(0.97f, 0.58f, 0.2f, 0.98f);
        [Tooltip("Slider handle color.")]
        public Color sliderHandleColor = new Color(0.99f, 0.79f, 0.44f, 1f);

        [Header("Tank Preview ScanLine")]
        [Tooltip("Sprite used for the animated scan line in tank preview.")]
        public Sprite previewScanLineSprite;
        [Tooltip("Tint color for the tank preview scan line.")]
        public Color previewScanLineColor = new Color(1f, 0.67f, 0.25f, 0.45f);
        [Tooltip("Scan line height in pixels.")]
        [Min(1f)]
        public float previewScanLineHeight = 12f;
        [Tooltip("Scan line speed in pixels per second.")]
        [Min(0.01f)]
        public float previewScanLineSpeed = 120f;

        [Header("Audio")]
        [Tooltip("Звук при наведении на кнопку.")]
        public AudioClip hoverSound;
        [Tooltip("Звук при нажатии на кнопку.")]
        public AudioClip clickSound;

        [Header("Scale")]
        [Tooltip("Множитель масштаба текста при наведении.")]
        [Min(1f)]
        public float hoverTextScale = 1.08f;
        [Tooltip("Скорость анимации масштаба текста кнопки.")]
        [Min(1f)]
        public float scaleLerpSpeed = 16f;

        [Header("Typography")]
        [Tooltip("Шрифт, который будет принудительно применяться ко всем TMP-элементам UI.")]
        public TMP_FontAsset uiFont;

        public static MenuButtonFeedbackConfig LoadDefault()
        {
            return Resources.Load<MenuButtonFeedbackConfig>(DefaultResourcesPath);
        }
    }
}
