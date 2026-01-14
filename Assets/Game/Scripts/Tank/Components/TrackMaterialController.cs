using UnityEngine;
using TankGame.Tank.Components;

namespace TankGame.Tank
{
    /// <summary>
    /// Контроллер материала гусениц - управляет анимацией текстуры
    /// </summary>
    [RequireComponent(typeof(TankMovement))]
    public class TrackMaterialController : MonoBehaviour
    {
        [Header("Track Materials")]
        [Tooltip("Материалы гусениц (с шейдером ToonTrackShader)")]
        [SerializeField] private Material[] trackMaterials;
        
        [Header("Animation Settings")]
        [Tooltip("Скорость анимации текстуры гусениц (константа)")]
        [SerializeField] private float trackAnimationSpeed = 1.0f;
        [Tooltip("Минимальный ввод для начала анимации")]
        [SerializeField] private float minInputThreshold = 0.01f;
        [Tooltip("Направление движения текстуры (обычно (0,1) для вертикального)")]
        [SerializeField] private Vector2 trackDirection = new Vector2(0, 1);
        
        private TankMovement tankMovement;
        private TankInputHandler inputHandler;
        private float currentTrackSpeed; // Текущая скорость анимации (с плавным переходом)
        private static readonly int TrackSpeedProperty = Shader.PropertyToID("_TrackSpeed");
        private static readonly int TrackDirectionProperty = Shader.PropertyToID("_TrackDirection");

        private void Awake()
        {
            tankMovement = GetComponent<TankMovement>();
            inputHandler = GetComponent<TankInputHandler>();
        }

        private void Update()
        {
            if (tankMovement == null || trackMaterials == null || trackMaterials.Length == 0)
                return;

            // Определяем есть ли движение или поворот
            bool isMoving = false;
            
            if (inputHandler != null)
            {
                var input = inputHandler.LastCommand;
                float verticalInput = Mathf.Abs(input.VerticalInput);
                float horizontalInput = Mathf.Abs(input.HorizontalInput);
                
                // Танк движется если есть любой ввод (движение или поворот)
                isMoving = verticalInput > minInputThreshold || horizontalInput > minInputThreshold;
            }
            
            // Целевая скорость: либо константа из инспектора, либо 0
            // Без плавного перехода - мгновенная остановка
            currentTrackSpeed = isMoving ? trackAnimationSpeed : 0f;
            
            // Применяем к всем материалам гусениц
            foreach (Material mat in trackMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(TrackSpeedProperty, currentTrackSpeed);
                    mat.SetVector(TrackDirectionProperty, new Vector4(trackDirection.x, trackDirection.y, 0, 0));
                }
            }
        }
    }
}

