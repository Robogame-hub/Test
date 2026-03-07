using UnityEngine;
using TankGame.Tank;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Управление двигателем танка и системой выхлопа.
    /// У ботов двигатель всегда заглушен, звуки двигателя не воспроизводятся.
    /// </summary>
    [RequireComponent(typeof(TankMovement))]
    public class TankEngine : MonoBehaviour
    {
        [Header("Engine Settings")]
        [Tooltip("Двигатель заведен по умолчанию")]
        [SerializeField] private bool startEngineOnAwake = false;
        
        [Tooltip("Клавиша для включения/выключения двигателя")]
        [SerializeField] private KeyCode engineToggleKey = KeyCode.F;
        
        [Header("Exhaust Particles")]
        [Tooltip("Системы частиц выхлопа (дым из труб) - можно несколько!")]
        [SerializeField] private ParticleSystem[] exhaustParticles;
        
        [Tooltip("Автоматически найти все ParticleSystem в дочерних объектах")]
        [SerializeField] private bool autoFindParticles = true;
        
        [Header("Emission Settings")]
        [Tooltip("Количество частиц на холостом ходу (engine idle)")]
        [SerializeField] private float idleEmissionRate = 10f;
        
        [Tooltip("Максимальное количество частиц при полном газу")]
        [SerializeField] private float maxEmissionRate = 50f;
        
        [Tooltip("Скорость изменения интенсивности дыма")]
        [SerializeField] private float emissionTransitionSpeed = 3f;
        
        [Header("Particle Properties")]
        [Tooltip("Размер частиц на холостом ходу")]
        [SerializeField] private float idleParticleSize = 0.5f;
        
        [Tooltip("Размер частиц при движении")]
        [SerializeField] private float movingParticleSize = 1.0f;
        
        [Tooltip("Скорость частиц на холостом ходу")]
        [SerializeField] private float idleParticleSpeed = 1f;
        
        [Tooltip("Скорость частиц при движении")]
        [SerializeField] private float movingParticleSpeed = 3f;
        
        [Header("Audio (Optional)")]
        [Tooltip("Звук работы двигателя (опционально)")]
        [SerializeField] private AudioSource engineAudioSource;
        
        [Tooltip("Звук запуска двигателя")]
        [SerializeField] private AudioClip engineStartSound;
        
        [Tooltip("Звук остановки двигателя")]
        [SerializeField] private AudioClip engineStopSound;
        
        [Tooltip("Звук работы двигателя (loop) - на скорости")]
        [SerializeField] private AudioClip engineLoopSound;
        
        [Tooltip("Минимальный pitch звука двигателя (холостой ход)")]
        [SerializeField] private float minEnginePitch = 0.8f;
        
        [Tooltip("Максимальный pitch звука двигателя (полный газ)")]
        [SerializeField] private float maxEnginePitch = 1.5f;
        
        [Tooltip("Минимальная громкость звука двигателя")]
        [SerializeField] private float minEngineVolume = 0.3f;
        
        [Tooltip("Максимальная громкость звука двигателя")]
        [SerializeField] private float maxEngineVolume = 0.8f;
        
        // Компоненты
        private TankMovement tankMovement;
        private TankController tankController;
        private ParticleSystem.EmissionModule[] emissionModules;
        private ParticleSystem.MainModule[] mainModules;
        
        // Состояние
        private bool isEngineRunning = false;
        private float currentEmissionRate = 0f;
        private float targetEmissionRate = 0f;
        
        // Свойства
        public bool IsEngineRunning => isEngineRunning;
        /// <summary> true = можно анимировать гусеницы (двигатель заведён или это бот) </summary>
        public bool ShouldAnimateTracks => IsEngineRunning || !IsLocalPlayerTank();
        
        private void Awake()
        {
            tankMovement = GetComponent<TankMovement>();
            tankController = GetComponent<TankController>();
            
            // Автопоиск всех ParticleSystem
            if (autoFindParticles && (exhaustParticles == null || exhaustParticles.Length == 0))
            {
                exhaustParticles = GetComponentsInChildren<ParticleSystem>();
            }
            
            // Инициализация массивов модулей
            if (exhaustParticles != null && exhaustParticles.Length > 0)
            {
                // Подсчет валидных систем частиц
                int validCount = 0;
                foreach (var ps in exhaustParticles)
                {
                    if (ps != null)
                        validCount++;
                }
                
                emissionModules = new ParticleSystem.EmissionModule[exhaustParticles.Length];
                mainModules = new ParticleSystem.MainModule[exhaustParticles.Length];
                
                for (int i = 0; i < exhaustParticles.Length; i++)
                {
                    if (exhaustParticles[i] != null)
                    {
                        emissionModules[i] = exhaustParticles[i].emission;
                        mainModules[i] = exhaustParticles[i].main;
                        Debug.Log($"[TankEngine] Initialized exhaust #{i + 1}: {exhaustParticles[i].name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TankEngine] Exhaust #{i + 1} is NULL!");
                    }
                }
                
                Debug.Log($"[TankEngine] Total: {exhaustParticles.Length} slots, {validCount} valid systems");
            }
            else
            {
                Debug.LogWarning("[TankEngine] Exhaust ParticleSystem not found! Assign manually.");
            }
        }
        
        private void Start()
        {
            bool isLocal = IsLocalPlayerTank();
            if (!isLocal)
            {
                // Бот: двигатель всегда заглушен, без звуков
                isEngineRunning = false;
                if (exhaustParticles != null)
                {
                    foreach (var ps in exhaustParticles)
                    {
                        if (ps != null) ps.Stop();
                    }
                }
                if (engineAudioSource != null && engineAudioSource.isPlaying)
                    engineAudioSource.Stop();
                return;
            }
            if (startEngineOnAwake)
                StartEngine();
            else
                StopEngine();
        }
        
        private void Update()
        {
            if (!IsLocalPlayerTank())
                return;
            if (Input.GetKeyDown(engineToggleKey))
                ToggleEngine();
            if (isEngineRunning)
                UpdateExhaust();
        }

        private bool IsLocalPlayerTank()
        {
            if (tankController != null)
                return tankController.IsLocalPlayer;
            return true;
        }
        
        /// <summary>
        /// Переключить состояние двигателя (вкл/выкл)
        /// </summary>
        private void ToggleEngine()
        {
            if (isEngineRunning)
            {
                StopEngine();
            }
            else
            {
                StartEngine();
            }
        }
        
        /// <summary>
        /// Запустить двигатель
        /// </summary>
        public void StartEngine()
        {
            if (isEngineRunning)
                return;
            
            isEngineRunning = true;
            
            // Включить все системы частиц
            if (exhaustParticles != null)
            {
                foreach (var ps in exhaustParticles)
                {
                    if (ps != null)
                    {
                        ps.Play();
                    }
                }
            }
            
            if (IsLocalPlayerTank())
            {
                if (engineAudioSource != null && engineStartSound != null)
                    engineAudioSource.PlayOneShot(engineStartSound);
                if (engineAudioSource != null && engineLoopSound != null)
                {
                    engineAudioSource.clip = engineLoopSound;
                    engineAudioSource.loop = true;
                    engineAudioSource.pitch = minEnginePitch;
                    engineAudioSource.volume = minEngineVolume;
                    engineAudioSource.Play();
                }
            }
        }
        
        /// <summary>
        /// Заглушить двигатель
        /// </summary>
        public void StopEngine()
        {
            if (!isEngineRunning)
                return;
            
            isEngineRunning = false;
            
            // Остановить все системы частиц
            if (exhaustParticles != null)
            {
                foreach (var ps in exhaustParticles)
                {
                    if (ps != null)
                    {
                        ps.Stop();
                    }
                }
            }
            
            if (engineAudioSource != null && engineAudioSource.isPlaying)
                engineAudioSource.Stop();
            if (IsLocalPlayerTank() && engineAudioSource != null && engineStopSound != null)
                engineAudioSource.PlayOneShot(engineStopSound);
        }
        
        /// <summary>
        /// Обновление интенсивности выхлопа и звука двигателя
        /// </summary>
        private void UpdateExhaust()
        {
            if (exhaustParticles == null || exhaustParticles.Length == 0 || tankMovement == null)
                return;
            
            // Получить фактор движения (0 = стоит, 1 = полный газ)
            float movementFactor = tankMovement.GetMovementFactor();
            
            // Целевая интенсивность выхлопа
            // Idle (стоит) = idleEmissionRate
            // Moving (движется) = Lerp между idle и max
            targetEmissionRate = Mathf.Lerp(idleEmissionRate, maxEmissionRate, movementFactor);
            
            // Плавный переход
            currentEmissionRate = Mathf.Lerp(
                currentEmissionRate,
                targetEmissionRate,
                Time.deltaTime * emissionTransitionSpeed
            );
            
            // Применить ко всем системам частиц
            for (int i = 0; i < exhaustParticles.Length; i++)
            {
                if (exhaustParticles[i] != null)
                {
                    // Emission rate
                    emissionModules[i].rateOverTime = currentEmissionRate;
                    
                    // Размер частиц
                    float particleSize = Mathf.Lerp(idleParticleSize, movingParticleSize, movementFactor);
                    mainModules[i].startSize = particleSize;
                    
                    // Скорость частиц
                    float particleSpeed = Mathf.Lerp(idleParticleSpeed, movingParticleSpeed, movementFactor);
                    mainModules[i].startSpeed = particleSpeed;
                }
            }
            
            if (IsLocalPlayerTank() && engineAudioSource != null && engineAudioSource.isPlaying)
            {
                engineAudioSource.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, movementFactor);
                engineAudioSource.volume = Mathf.Lerp(minEngineVolume, maxEngineVolume, movementFactor);
            }
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Проверка значений
            if (idleEmissionRate < 0f) idleEmissionRate = 0f;
            if (maxEmissionRate < idleEmissionRate) maxEmissionRate = idleEmissionRate;
            if (emissionTransitionSpeed < 0.1f) emissionTransitionSpeed = 0.1f;
        }
        #endif
    }
}

