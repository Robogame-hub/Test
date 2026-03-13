using UnityEngine;

namespace TankGame.Menu
{
    public class AudioSettings : MonoBehaviour
    {
        private static AudioSettings instance;
        private static bool isQuitting;

        private const string KeyMaster = "Audio.Master";
        private const string KeyMusic = "Audio.Music";
        private const string KeySfx = "Audio.Sfx";

        [Header("Default Volumes")]
        [Tooltip("Начальное значение общей громкости (0..1).")]
        [SerializeField] private float masterVolume = 1f;
        [Tooltip("Начальное значение громкости музыки (0..1).")]
        [SerializeField] private float musicVolume = 1f;
        [Tooltip("Начальное значение громкости эффектов (0..1).")]
        [SerializeField] private float sfxVolume = 1f;

        public static AudioSettings Instance
        {
            get
            {
                if (isQuitting)
                    return null;

                if (instance == null)
                {
                    AudioSettings found = FindObjectOfType<AudioSettings>();
                    if (found != null)
                    {
                        instance = found;
                    }
                    else
                    {
                        GameObject go = new GameObject("AudioSettings");
                        instance = go.AddComponent<AudioSettings>();
                        DontDestroyOnLoad(go);
                    }
                }

                return instance;
            }
        }

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                Apply();
                Save();
            }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                Save();
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                Save();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            Apply();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            instance = null;
        }

        private void Apply()
        {
            AudioListener.volume = masterVolume;
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(KeyMaster, masterVolume);
            PlayerPrefs.SetFloat(KeyMusic, musicVolume);
            PlayerPrefs.SetFloat(KeySfx, sfxVolume);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMaster, masterVolume));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, musicVolume));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, sfxVolume));
        }
    }
}
