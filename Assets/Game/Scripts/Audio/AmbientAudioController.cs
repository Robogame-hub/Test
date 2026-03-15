using System.Collections;
using UnityEngine;

namespace TankGame.Audio
{
    /// <summary>
    /// Проигрывает эмбиент-треки в случайном порядке и со случайными паузами.
    /// Можно настроить шанс "пропуска" цикла (тишина), чтобы эмбиент включался не всегда.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class AmbientAudioController : MonoBehaviour
    {
        [Header("Tracks")]
        [Tooltip("Список эмбиент-треков. Выбирается случайный трек.")]
        [SerializeField] private AudioClip[] ambientTracks;

        [Header("Playback")]
        [Tooltip("Автоматически запускать систему эмбиента при старте сцены.")]
        [SerializeField] private bool playOnStart = true;
        [Tooltip("Не повторять один и тот же трек подряд, если доступно 2+ трека.")]
        [SerializeField] private bool avoidImmediateRepeat = true;
        [Tooltip("Шанс запуска трека в цикле (1 = всегда, 0.5 = примерно каждый второй цикл).")]
        [SerializeField] [Range(0f, 1f)] private float playChancePerCycle = 1f;

        [Header("Random Delays")]
        [Tooltip("Минимальная пауза перед следующим решением (сек).")]
        [SerializeField] private float minDelayBetweenCycles = 2f;
        [Tooltip("Максимальная пауза перед следующим решением (сек).")]
        [SerializeField] private float maxDelayBetweenCycles = 8f;

        [Header("Volume")]
        [Tooltip("Громкость эмбиента.")]
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

        private AudioSource ambientSource;
        private Coroutine ambientRoutine;
        private int lastTrackIndex = -1;
        private bool isRunning;
        private float lastAppliedMusicVolume = -1f;

        public bool IsRunning => isRunning;

        private void Awake()
        {
            ambientSource = GetComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = false;
            ambientSource.spatialBlend = 0f; // 2D ambience
            ApplyEffectiveVolume();

            if (maxDelayBetweenCycles < minDelayBetweenCycles)
                maxDelayBetweenCycles = minDelayBetweenCycles;
        }

        private void Update()
        {
            float musicVolume = 1f;
            TankGame.Menu.AudioSettings settings = TankGame.Menu.AudioSettings.Instance;
            if (settings != null)
                musicVolume = Mathf.Clamp01(settings.MusicVolume);

            if (!Mathf.Approximately(lastAppliedMusicVolume, musicVolume))
                ApplyEffectiveVolume();
        }

        private void Start()
        {
            if (playOnStart)
                StartAmbient();
        }

        public void StartAmbient()
        {
            if (isRunning)
                return;

            if (ambientTracks == null || ambientTracks.Length == 0)
            {
                Debug.LogWarning("[AmbientAudioController] Нет треков в ambientTracks.");
                return;
            }

            isRunning = true;
            ambientRoutine = StartCoroutine(AmbientLoopRoutine());
        }

        public void StopAmbient()
        {
            if (!isRunning)
                return;

            isRunning = false;

            if (ambientRoutine != null)
            {
                StopCoroutine(ambientRoutine);
                ambientRoutine = null;
            }

            if (ambientSource != null && ambientSource.isPlaying)
                ambientSource.Stop();
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            ApplyEffectiveVolume();
        }

        private void ApplyEffectiveVolume()
        {
            if (ambientSource == null)
                return;

            float musicVolume = 1f;
            TankGame.Menu.AudioSettings settings = TankGame.Menu.AudioSettings.Instance;
            if (settings != null)
                musicVolume = Mathf.Clamp01(settings.MusicVolume);

            lastAppliedMusicVolume = musicVolume;
            ambientSource.volume = Mathf.Clamp01(volume * musicVolume);
        }

        private IEnumerator AmbientLoopRoutine()
        {
            while (isRunning)
            {
                float delay = Random.Range(minDelayBetweenCycles, maxDelayBetweenCycles);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);

                if (!isRunning)
                    yield break;

                if (Random.value > playChancePerCycle)
                    continue;

                int nextIndex = GetNextTrackIndex();
                if (nextIndex < 0 || nextIndex >= ambientTracks.Length)
                    continue;

                AudioClip nextClip = ambientTracks[nextIndex];
                if (nextClip == null)
                    continue;

                lastTrackIndex = nextIndex;
                ambientSource.clip = nextClip;
                ambientSource.Play();

                yield return new WaitForSeconds(nextClip.length);
            }
        }

        private int GetNextTrackIndex()
        {
            if (ambientTracks == null || ambientTracks.Length == 0)
                return -1;

            if (!avoidImmediateRepeat || ambientTracks.Length == 1)
                return Random.Range(0, ambientTracks.Length);

            int index = Random.Range(0, ambientTracks.Length);
            if (index == lastTrackIndex)
                index = (index + 1) % ambientTracks.Length;

            return index;
        }

        private void OnDisable()
        {
            StopAmbient();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxDelayBetweenCycles < minDelayBetweenCycles)
                maxDelayBetweenCycles = minDelayBetweenCycles;

            if (ambientSource != null)
                ApplyEffectiveVolume();
        }
#endif
    }
}
