using UnityEngine;

namespace TankGame.Settings
{
    /// <summary>
    /// Input settings singleton.
    /// Horizontal sensitivity is the only sensitivity setting in use.
    /// Legacy vertical/master properties are kept for compatibility and mapped to horizontal.
    /// </summary>
    public class InputSettings : MonoBehaviour
    {
        private static InputSettings instance;
        private static bool isQuitting;

        public static InputSettings Instance
        {
            get
            {
                if (isQuitting)
                    return null;

                if (instance == null)
                {
                    InputSettings[] existing = FindObjectsOfType<InputSettings>();
                    if (existing.Length > 0)
                    {
                        instance = existing[0];
                        for (int i = 1; i < existing.Length; i++)
                        {
                            if (existing[i] != null)
                                Destroy(existing[i].gameObject);
                        }
                    }
                    else
                    {
                        GameObject go = new GameObject("InputSettings");
                        instance = go.AddComponent<InputSettings>();
                        DontDestroyOnLoad(go);
                    }
                }

                return instance;
            }
        }

        [Header("Mouse Sensitivity")]
        [SerializeField] private float horizontalSensitivity = 1f;

        [Header("Legacy Sensitivity (compatibility only)")]
        [SerializeField] private float verticalSensitivity = 1f;
        [SerializeField] private float masterSensitivity = 1f;

        [Header("Sensitivity Range")]
        [SerializeField] private float minSensitivity = 0.1f;
        [SerializeField] private float maxSensitivity = 5f;

        [Header("Invert")]
        [SerializeField] private bool invertY;
        [SerializeField] private bool invertX;

        private const string KEY_HORIZONTAL_SENS = "Input_HorizontalSensitivity";
        private const string KEY_VERTICAL_SENS = "Input_VerticalSensitivity";
        private const string KEY_MASTER_SENS = "Input_MasterSensitivity";
        private const string KEY_INVERT_Y = "Input_InvertY";
        private const string KEY_INVERT_X = "Input_InvertX";

        public float HorizontalSensitivity
        {
            get => horizontalSensitivity;
            set
            {
                horizontalSensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity);
                // keep legacy mirrors in sync
                verticalSensitivity = horizontalSensitivity;
                masterSensitivity = horizontalSensitivity;
                SaveSettings();
            }
        }

        // Backward compatibility: mapped to horizontal.
        public float VerticalSensitivity
        {
            get => horizontalSensitivity;
            set => HorizontalSensitivity = value;
        }

        // Backward compatibility: mapped to horizontal.
        public float MasterSensitivity
        {
            get => horizontalSensitivity;
            set => HorizontalSensitivity = value;
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
                instance = null;
        }

        public float GetEffectiveHorizontalSensitivity()
        {
            float sens = horizontalSensitivity;
            return invertX ? -sens : sens;
        }

        public float GetEffectiveVerticalSensitivity()
        {
            float sens = horizontalSensitivity;
            return invertY ? -sens : sens;
        }

        public Vector2 ApplySensitivity(Vector2 mouseDelta)
        {
            return new Vector2(
                mouseDelta.x * GetEffectiveHorizontalSensitivity(),
                mouseDelta.y * GetEffectiveVerticalSensitivity()
            );
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_HORIZONTAL_SENS, horizontalSensitivity);
            // keep legacy keys for old scenes/components
            PlayerPrefs.SetFloat(KEY_VERTICAL_SENS, horizontalSensitivity);
            PlayerPrefs.SetFloat(KEY_MASTER_SENS, horizontalSensitivity);
            PlayerPrefs.SetInt(KEY_INVERT_Y, invertY ? 1 : 0);
            PlayerPrefs.SetInt(KEY_INVERT_X, invertX ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            horizontalSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KEY_HORIZONTAL_SENS, horizontalSensitivity), minSensitivity, maxSensitivity);
            verticalSensitivity = horizontalSensitivity;
            masterSensitivity = horizontalSensitivity;
            invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
            invertX = PlayerPrefs.GetInt(KEY_INVERT_X, 0) == 1;
        }

        public void ResetToDefaults()
        {
            horizontalSensitivity = 1f;
            verticalSensitivity = horizontalSensitivity;
            masterSensitivity = horizontalSensitivity;
            invertY = false;
            invertX = false;
            SaveSettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            horizontalSensitivity = Mathf.Clamp(horizontalSensitivity, minSensitivity, maxSensitivity);
            verticalSensitivity = horizontalSensitivity;
            masterSensitivity = horizontalSensitivity;
        }
#endif
    }
}
