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
