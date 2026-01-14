using UnityEngine;
using UnityEngine.UI;
using TankGame.Settings;
using TMPro; // Если используете TextMeshPro, иначе используйте Text

namespace TankGame.UI
{
    /// <summary>
    /// UI меню настроек
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        [Header("Sensitivity Sliders")]
        [Tooltip("Слайдер общей чувствительности")]
        [SerializeField] private Slider masterSensitivitySlider;
        
        [Tooltip("Слайдер горизонтальной чувствительности")]
        [SerializeField] private Slider horizontalSensitivitySlider;
        
        [Tooltip("Слайдер вертикальной чувствительности")]
        [SerializeField] private Slider verticalSensitivitySlider;

        [Header("Sensitivity Labels")]
        [Tooltip("Текст значения общей чувствительности")]
        [SerializeField] private TextMeshProUGUI masterSensitivityText;
        
        [Tooltip("Текст значения горизонтальной чувствительности")]
        [SerializeField] private TextMeshProUGUI horizontalSensitivityText;
        
        [Tooltip("Текст значения вертикальной чувствительности")]
        [SerializeField] private TextMeshProUGUI verticalSensitivityText;

        [Header("Invert Toggles")]
        [Tooltip("Toggle инверсии вертикальной оси")]
        [SerializeField] private Toggle invertYToggle;
        
        [Tooltip("Toggle инверсии горизонтальной оси")]
        [SerializeField] private Toggle invertXToggle;

        [Header("Buttons")]
        [Tooltip("Кнопка сброса к значениям по умолчанию")]
        [SerializeField] private Button resetButton;
        
        [Tooltip("Кнопка закрытия меню")]
        [SerializeField] private Button closeButton;

        private InputSettings settings;

        private void Start()
        {
            settings = InputSettings.Instance;
            
            // Инициализация слайдеров
            InitializeSliders();
            
            // Добавляем слушателей
            if (masterSensitivitySlider != null)
                masterSensitivitySlider.onValueChanged.AddListener(OnMasterSensitivityChanged);
            
            if (horizontalSensitivitySlider != null)
                horizontalSensitivitySlider.onValueChanged.AddListener(OnHorizontalSensitivityChanged);
            
            if (verticalSensitivitySlider != null)
                verticalSensitivitySlider.onValueChanged.AddListener(OnVerticalSensitivityChanged);
            
            if (invertYToggle != null)
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            
            if (invertXToggle != null)
                invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
            
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
            
            // Загружаем текущие значения
            LoadCurrentSettings();
        }

        /// <summary>
        /// Инициализирует диапазоны слайдеров
        /// </summary>
        private void InitializeSliders()
        {
            float minSens = settings.MinSensitivity;
            float maxSens = settings.MaxSensitivity;

            if (masterSensitivitySlider != null)
            {
                masterSensitivitySlider.minValue = minSens;
                masterSensitivitySlider.maxValue = maxSens;
            }

            if (horizontalSensitivitySlider != null)
            {
                horizontalSensitivitySlider.minValue = minSens;
                horizontalSensitivitySlider.maxValue = maxSens;
            }

            if (verticalSensitivitySlider != null)
            {
                verticalSensitivitySlider.minValue = minSens;
                verticalSensitivitySlider.maxValue = maxSens;
            }
        }

        /// <summary>
        /// Загружает текущие настройки в UI
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (masterSensitivitySlider != null)
                masterSensitivitySlider.value = settings.MasterSensitivity;
            
            if (horizontalSensitivitySlider != null)
                horizontalSensitivitySlider.value = settings.HorizontalSensitivity;
            
            if (verticalSensitivitySlider != null)
                verticalSensitivitySlider.value = settings.VerticalSensitivity;
            
            if (invertYToggle != null)
                invertYToggle.isOn = settings.InvertY;
            
            if (invertXToggle != null)
                invertXToggle.isOn = settings.InvertX;
            
            UpdateAllLabels();
        }

        /// <summary>
        /// Обновляет все текстовые метки
        /// </summary>
        private void UpdateAllLabels()
        {
            if (masterSensitivityText != null)
                masterSensitivityText.text = settings.MasterSensitivity.ToString("F2");
            
            if (horizontalSensitivityText != null)
                horizontalSensitivityText.text = settings.HorizontalSensitivity.ToString("F2");
            
            if (verticalSensitivityText != null)
                verticalSensitivityText.text = settings.VerticalSensitivity.ToString("F2");
        }

        #region Callbacks

        private void OnMasterSensitivityChanged(float value)
        {
            settings.MasterSensitivity = value;
            if (masterSensitivityText != null)
                masterSensitivityText.text = value.ToString("F2");
        }

        private void OnHorizontalSensitivityChanged(float value)
        {
            settings.HorizontalSensitivity = value;
            if (horizontalSensitivityText != null)
                horizontalSensitivityText.text = value.ToString("F2");
        }

        private void OnVerticalSensitivityChanged(float value)
        {
            settings.VerticalSensitivity = value;
            if (verticalSensitivityText != null)
                verticalSensitivityText.text = value.ToString("F2");
        }

        private void OnInvertYChanged(bool value)
        {
            settings.InvertY = value;
        }

        private void OnInvertXChanged(bool value)
        {
            settings.InvertX = value;
        }

        private void OnResetClicked()
        {
            settings.ResetToDefaults();
            LoadCurrentSettings();
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        #endregion

        private void OnDestroy()
        {
            // Удаляем слушателей
            if (masterSensitivitySlider != null)
                masterSensitivitySlider.onValueChanged.RemoveListener(OnMasterSensitivityChanged);
            
            if (horizontalSensitivitySlider != null)
                horizontalSensitivitySlider.onValueChanged.RemoveListener(OnHorizontalSensitivityChanged);
            
            if (verticalSensitivitySlider != null)
                verticalSensitivitySlider.onValueChanged.RemoveListener(OnVerticalSensitivityChanged);
            
            if (invertYToggle != null)
                invertYToggle.onValueChanged.RemoveListener(OnInvertYChanged);
            
            if (invertXToggle != null)
                invertXToggle.onValueChanged.RemoveListener(OnInvertXChanged);
            
            if (resetButton != null)
                resetButton.onClick.RemoveListener(OnResetClicked);
            
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}

