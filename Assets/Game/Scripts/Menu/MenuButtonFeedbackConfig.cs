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
        public Color normalTextColor = new Color32(0x0F, 0xF3, 0x00, 0xFF);
        [Tooltip("Button text color on hover.")]
        public Color hoverTextColor = Color.red;
        [Tooltip("Button text color on press.")]
        public Color pressedTextColor = Color.white;

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
