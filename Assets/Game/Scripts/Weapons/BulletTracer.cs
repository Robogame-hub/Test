using UnityEngine;

namespace TankGame.Weapons
{
    /// <summary>
    /// Управляет визуальным трассером пули
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class BulletTracer : MonoBehaviour
    {
        [Header("Tracer Settings")]
        [Tooltip("Длина трассера")]
        [SerializeField] private float tracerLength = 2f;
        
        [Tooltip("Ширина трассера")]
        [SerializeField] private float tracerWidth = 0.1f;
        
        [Tooltip("Время жизни трассера после остановки пули")]
        [SerializeField] private float fadeTime = 0.3f;
        
        [Header("Color Settings")]
        [Tooltip("Начальный цвет (горячий)")]
        [SerializeField] private Color startColor = new Color(1f, 1f, 0.5f, 1f);
        
        [Tooltip("Конечный цвет (холодный)")]
        [SerializeField] private Color endColor = new Color(1f, 0.3f, 0f, 0.5f);
        
        [Header("Advanced")]
        [Tooltip("Использовать продвинутый шейдер с анимацией")]
        [SerializeField] private bool useAdvancedShader = false;
        
        [Tooltip("Материал трассера (опционально)")]
        [SerializeField] private Material customTracerMaterial;
        
        private TrailRenderer trailRenderer;
        private bool isInitialized = false;

        private void Awake()
        {
            InitializeTracer();
        }

        /// <summary>
        /// Инициализирует трассер
        /// </summary>
        private void InitializeTracer()
        {
            trailRenderer = GetComponent<TrailRenderer>();
            
            if (trailRenderer == null)
            {
                Debug.LogError("BulletTracer: TrailRenderer component not found!");
                return;
            }

            // Настраиваем TrailRenderer
            trailRenderer.time = tracerLength / 10f; // Длина в секундах
            trailRenderer.startWidth = tracerWidth;
            trailRenderer.endWidth = tracerWidth * 0.2f; // Сужается к концу
            trailRenderer.emitting = true;
            trailRenderer.autodestruct = false; // Не уничтожать автоматически
            trailRenderer.Clear(); // Очищаем при инициализации
            
            // Градиент цвета
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] 
                { 
                    new GradientColorKey(startColor, 0.0f),
                    new GradientColorKey(endColor, 1.0f)
                },
                new GradientAlphaKey[] 
                { 
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trailRenderer.colorGradient = gradient;
            
            // Применяем материал
            if (customTracerMaterial != null)
            {
                trailRenderer.material = customTracerMaterial;
            }
            else
            {
                // Создаем материал автоматически
                CreateDefaultMaterial();
            }
            
            // Настройки рендеринга
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.alignment = LineAlignment.View; // Всегда смотрит на камеру
            trailRenderer.textureMode = LineTextureMode.Stretch;
            
            isInitialized = true;
        }

        /// <summary>
        /// Создает материал по умолчанию
        /// </summary>
        private void CreateDefaultMaterial()
        {
            string shaderName = useAdvancedShader 
                ? "TankGame/BulletTracerAdvanced" 
                : "TankGame/BulletTracerShader";
            
            Shader shader = Shader.Find(shaderName);
            
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.name = "BulletTracerMaterial_Auto";
                trailRenderer.material = mat;
            }
            else
            {
                Debug.LogWarning($"BulletTracer: Shader '{shaderName}' not found! Using default material.");
            }
        }

        /// <summary>
        /// Включает трассер
        /// </summary>
        public void EnableTracer()
        {
            if (!isInitialized) InitializeTracer();
            
            if (trailRenderer != null)
            {
                // ВАЖНО: Сначала очищаем, ПОТОМ включаем!
                trailRenderer.Clear();
                trailRenderer.emitting = false; // Останавливаем генерацию
                trailRenderer.enabled = false;
                
                // Ждем один кадр перед включением (Unity quirk)
                StartCoroutine(EnableAfterFrame());
            }
        }
        
        /// <summary>
        /// Включает трассер через кадр (исправляет артефакты Trail)
        /// </summary>
        private System.Collections.IEnumerator EnableAfterFrame()
        {
            yield return null; // Ждем один кадр
            
            if (trailRenderer != null)
            {
                trailRenderer.Clear(); // Еще раз очищаем
                trailRenderer.enabled = true;
                trailRenderer.emitting = true;
            }
        }

        /// <summary>
        /// Выключает трассер
        /// </summary>
        public void DisableTracer()
        {
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Обновляет настройки трассера в реальном времени
        /// </summary>
        public void UpdateTracerSettings(float length, float width)
        {
            tracerLength = length;
            tracerWidth = width;
            
            if (trailRenderer != null)
            {
                trailRenderer.time = tracerLength / 10f;
                trailRenderer.startWidth = tracerWidth;
                trailRenderer.endWidth = tracerWidth * 0.2f;
            }
        }

        /// <summary>
        /// Обновляет цвета трассера
        /// </summary>
        public void UpdateTracerColors(Color start, Color end)
        {
            startColor = start;
            endColor = end;
            
            if (trailRenderer != null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] 
                    { 
                        new GradientColorKey(startColor, 0.0f),
                        new GradientColorKey(endColor, 1.0f)
                    },
                    new GradientAlphaKey[] 
                    { 
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                trailRenderer.colorGradient = gradient;
            }
        }

        /// <summary>
        /// Вызывается при возврате пули в пул
        /// </summary>
        public void OnReturnToPool()
        {
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false; // Останавливаем генерацию
                trailRenderer.Clear();
                trailRenderer.enabled = false; // Выключаем полностью
            }
        }

        private void OnDisable()
        {
            // Полностью очищаем и выключаем трассер
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
                trailRenderer.enabled = false;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Обновляем настройки в редакторе
            if (Application.isPlaying && isInitialized)
            {
                InitializeTracer();
            }
        }
#endif
    }
}

