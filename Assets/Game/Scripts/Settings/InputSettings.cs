using UnityEngine;

namespace TankGame.Settings
{
    /// <summary>
    /// Настройки управления (чувствительность мыши, инверсия и т.д.)
    /// Сохраняется через PlayerPrefs
    /// </summary>
    public class InputSettings : MonoBehaviour
    {
        private static InputSettings instance;
        private static bool isQuitting = false;
        
        public static InputSettings Instance
        {
            get
            {
                // ОПТИМИЗАЦИЯ: Не создаем instance при выходе из игры
                if (isQuitting)
                {
                    Debug.LogWarning("[InputSettings] Instance requested during application quit. Returning null.");
                    return null;
                }
                
                if (instance == null)
                {
                    // ИСПРАВЛЕНО: Кэшируем result FindObjectOfType
                    // FindObjectOfType - очень медленная операция!
                    InputSettings[] existingInstances = FindObjectsOfType<InputSettings>();
                    
                    if (existingInstances.Length > 1)
                    {
                        Debug.LogWarning($"[InputSettings] Found {existingInstances.Length} instances! Should only be one. Destroying extras.");
                        for (int i = 1; i < existingInstances.Length; i++)
                        {
                            Destroy(existingInstances[i].gameObject);
                        }
                    }
                    
                    if (existingInstances.Length > 0)
                    {
                        instance = existingInstances[0];
                    }
                    else
                    {
                        // Создаем новый instance только если не нашли
                        GameObject go = new GameObject("InputSettings");
                        instance = go.AddComponent<InputSettings>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[InputSettings] Created new InputSettings singleton");
                    }
                }
                return instance;
            }
        }

        [Header("Mouse Sensitivity")]
        [Tooltip("Чувствительность по горизонтали (башня)")]
        [SerializeField] private float horizontalSensitivity = 1.0f;
        
        [Tooltip("Чувствительность по вертикали (ствол)")]
        [SerializeField] private float verticalSensitivity = 1.0f;
        
        [Tooltip("Общий множитель чувствительности")]
        [SerializeField] private float masterSensitivity = 1.0f;

        [Header("Sensitivity Range")]
        [Tooltip("Минимальная чувствительность")]
        [SerializeField] private float minSensitivity = 0.1f;
        
        [Tooltip("Максимальная чувствительность")]
        [SerializeField] private float maxSensitivity = 5.0f;

        [Header("Invert")]
        [Tooltip("Инвертировать вертикальную ось")]
        [SerializeField] private bool invertY = false;
        
        [Tooltip("Инвертировать горизонтальную ось")]
        [SerializeField] private bool invertX = false;

        // PlayerPrefs ключи
        private const string KEY_HORIZONTAL_SENS = "Input_HorizontalSensitivity";
        private const string KEY_VERTICAL_SENS = "Input_VerticalSensitivity";
        private const string KEY_MASTER_SENS = "Input_MasterSensitivity";
        private const string KEY_INVERT_Y = "Input_InvertY";
        private const string KEY_INVERT_X = "Input_InvertX";

        // Публичные свойства
        public float HorizontalSensitivity
        {
            get => horizontalSensitivity;
            set
            {
                horizontalSensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity);
                SaveSettings();
            }
        }

        public float VerticalSensitivity
        {
            get => verticalSensitivity;
            set
            {
                verticalSensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity);
                SaveSettings();
            }
        }

        public float MasterSensitivity
        {
            get => masterSensitivity;
            set
            {
                masterSensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity);
                SaveSettings();
            }
        }

        public bool InvertY
        {
            get => invertY;
            set
            {
                invertY = value;
                SaveSettings();
            }
        }

        public bool InvertX
        {
            get => invertX;
            set
            {
                invertX = value;
                SaveSettings();
            }
        }

        public float MinSensitivity => minSensitivity;
        public float MaxSensitivity => maxSensitivity;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[InputSettings] Duplicate instance detected on {gameObject.name}. Destroying.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        
        private void OnApplicationQuit()
        {
            isQuitting = true;
            instance = null;
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Получить итоговую чувствительность по горизонтали с учетом инверсии
        /// </summary>
        public float GetEffectiveHorizontalSensitivity()
        {
            float sens = horizontalSensitivity * masterSensitivity;
            return invertX ? -sens : sens;
        }

        /// <summary>
        /// Получить итоговую чувствительность по вертикали с учетом инверсии
        /// </summary>
        public float GetEffectiveVerticalSensitivity()
        {
            float sens = verticalSensitivity * masterSensitivity;
            return invertY ? -sens : sens;
        }

        /// <summary>
        /// Применить дельту мыши с учетом всех настроек
        /// </summary>
        public Vector2 ApplySensitivity(Vector2 mouseDelta)
        {
            return new Vector2(
                mouseDelta.x * GetEffectiveHorizontalSensitivity(),
                mouseDelta.y * GetEffectiveVerticalSensitivity()
            );
        }

        /// <summary>
        /// Сохранить настройки
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_HORIZONTAL_SENS, horizontalSensitivity);
            PlayerPrefs.SetFloat(KEY_VERTICAL_SENS, verticalSensitivity);
            PlayerPrefs.SetFloat(KEY_MASTER_SENS, masterSensitivity);
            PlayerPrefs.SetInt(KEY_INVERT_Y, invertY ? 1 : 0);
            PlayerPrefs.SetInt(KEY_INVERT_X, invertX ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загрузить настройки
        /// </summary>
        public void LoadSettings()
        {
            horizontalSensitivity = PlayerPrefs.GetFloat(KEY_HORIZONTAL_SENS, horizontalSensitivity);
            verticalSensitivity = PlayerPrefs.GetFloat(KEY_VERTICAL_SENS, verticalSensitivity);
            masterSensitivity = PlayerPrefs.GetFloat(KEY_MASTER_SENS, masterSensitivity);
            invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
            invertX = PlayerPrefs.GetInt(KEY_INVERT_X, 0) == 1;
        }

        /// <summary>
        /// Сбросить настройки к значениям по умолчанию
        /// </summary>
        public void ResetToDefaults()
        {
            horizontalSensitivity = 1.0f;
            verticalSensitivity = 1.0f;
            masterSensitivity = 1.0f;
            invertY = false;
            invertX = false;
            SaveSettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            horizontalSensitivity = Mathf.Clamp(horizontalSensitivity, minSensitivity, maxSensitivity);
            verticalSensitivity = Mathf.Clamp(verticalSensitivity, minSensitivity, maxSensitivity);
            masterSensitivity = Mathf.Clamp(masterSensitivity, minSensitivity, maxSensitivity);
        }
#endif
    }
}

