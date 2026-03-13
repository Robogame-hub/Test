using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankGame.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusicPlayer : MonoBehaviour
    {
        private const string DefaultClipPath = "Assets/Game/AmbientAydio/steel_ambient_1.mp3";
        private static MenuMusicPlayer instance;

        [Header("Music")]
        [Tooltip("Музыкальный трек для главного меню и лобби.")]
        [SerializeField] private AudioClip menuMusicClip;
        [Tooltip("Базовая громкость трека (дополнительно умножается на MusicVolume из настроек).")]
        [Range(0f, 1f)]
        [SerializeField] private float baseVolume = 0.8f;

        [Header("Scenes")]
        [Tooltip("Сцены, в которых музыка должна играть.")]
        [SerializeField] private string[] menuSceneNames = { "MainMenu", "Lobby" };

        private AudioSource source;

        public static MenuMusicPlayer EnsureInstance()
        {
            if (instance != null)
                return instance;

            instance = Object.FindObjectOfType<MenuMusicPlayer>();
            if (instance != null)
                return instance;

            GameObject go = new GameObject("MenuMusic", typeof(AudioSource), typeof(MenuMusicPlayer));
            instance = go.GetComponent<MenuMusicPlayer>();
            return instance;
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

            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;

            if (menuMusicClip == null)
                menuMusicClip = LoadEditorFallbackClip();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            float musicVolume = 1f;
            AudioSettings audioSettings = AudioSettings.Instance;
            if (audioSettings != null)
                musicVolume = audioSettings.MusicVolume;

            source.volume = Mathf.Clamp01(baseVolume * musicVolume);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshForScene(scene.name);
        }

        private void RefreshForScene(string sceneName)
        {
            bool shouldPlay = IsMenuScene(sceneName);

            if (!shouldPlay)
            {
                if (source.isPlaying)
                    source.Stop();
                return;
            }

            if (menuMusicClip == null)
                menuMusicClip = LoadEditorFallbackClip();

            if (menuMusicClip == null)
                return;

            if (source.clip != menuMusicClip)
                source.clip = menuMusicClip;

            if (!source.isPlaying)
                source.Play();
        }

        private bool IsMenuScene(string sceneName)
        {
            if (menuSceneNames == null || menuSceneNames.Length == 0)
                return sceneName == "MainMenu" || sceneName == "Lobby";

            for (int i = 0; i < menuSceneNames.Length; i++)
            {
                if (string.Equals(menuSceneNames[i], sceneName, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static AudioClip LoadEditorFallbackClip()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultClipPath);
#else
            return null;
#endif
        }
    }
}

