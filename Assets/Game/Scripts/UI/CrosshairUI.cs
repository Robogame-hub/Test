using UnityEngine;
using UnityEngine.UI;
using TankGame.Tank.Components;

namespace TankGame.UI
{
    /// <summary>
    /// UI прицел для игры от третьего лица
    /// Показывает разброс и перезарядку
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Ссылка на TankWeapon для отслеживания перезарядки")]
        [SerializeField] private TankWeapon weapon;
        
        [Tooltip("Ссылка на TankTurret для отслеживания стабильности")]
        [SerializeField] private TankTurret turret;
        
        [Tooltip("Ссылка на TankMovement для отслеживания движения танка")]
        [SerializeField] private TankMovement tankMovement;
        
        [Header("Crosshair Elements")]
        [Tooltip("Центральная точка прицела")]
        [SerializeField] private RectTransform centerDot;
        
        [Tooltip("Верхняя линия прицела")]
        [SerializeField] private RectTransform topLine;
        
        [Tooltip("Нижняя линия прицела")]
        [SerializeField] private RectTransform bottomLine;
        
        [Tooltip("Левая линия прицела")]
        [SerializeField] private RectTransform leftLine;
        
        [Tooltip("Правая линия прицела")]
        [SerializeField] private RectTransform rightLine;
        
        [Header("Spread Settings")]
        [Tooltip("Минимальное расстояние линий от центра (при максимальной стабильности)")]
        [SerializeField] private float minSpread = 20f;
        
        [Tooltip("Максимальное расстояние линий от центра (при нулевой стабильности)")]
        [SerializeField] private float maxSpread = 80f;
        
        [Tooltip("Дополнительный разброс от движения танка (UI множитель)")]
        [SerializeField] private float movementSpreadMultiplier = 30f;
        
        [Tooltip("Скорость анимации изменения разброса")]
        [SerializeField] private float spreadAnimationSpeed = 10f;
        
        [Header("Cooldown Indicator")]
        [Tooltip("Круговая шкала перезарядки вокруг прицела")]
        [SerializeField] private Image cooldownCircle;
        
        [Tooltip("Цвет шкалы при перезарядке")]
        [SerializeField] private Color cooldownColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        
        [Tooltip("Цвет шкалы когда можно стрелять")]
        [SerializeField] private Color readyColor = new Color(0.3f, 1f, 0.3f, 0.8f);
        
        [Header("Visual Feedback")]
        [Tooltip("Цвет прицела при максимальной стабильности")]
        [SerializeField] private Color stableColor = Color.green;
        
        [Tooltip("Цвет прицела при нулевой стабильности")]
        [SerializeField] private Color unstableColor = Color.red;
        
        [Tooltip("Цвет прицела когда наведен на цель")]
        [SerializeField] private Color targetColor = Color.red;
        
        [Tooltip("Применять цвет в зависимости от стабильности")]
        [SerializeField] private bool colorByStability = true;
        
        [Tooltip("Показывать когда прицел наведен на что-то")]
        [SerializeField] private bool highlightTarget = true;
        
        [Tooltip("Максимальная дистанция проверки цели")]
        [SerializeField] private float maxTargetDistance = 500f;
        
        [Tooltip("Слои которые считаются целями (оставить пустым = все слои)")]
        [SerializeField] private LayerMask targetLayers = -1;
        
        [Header("Crosshair Images")]
        [Tooltip("Массив всех Image компонентов прицела (для изменения цвета)")]
        [SerializeField] private Image[] crosshairImages;

        private float currentSpread;

        private void Start()
        {
            InitializeCrosshair();
            
            // Начальное состояние
            currentSpread = minSpread;
            
            // Проверка назначения компонентов
            if (weapon == null)
                Debug.LogWarning("[CrosshairUI] TankWeapon not assigned! Assign manually in Inspector.");
            if (turret == null)
                Debug.LogWarning("[CrosshairUI] TankTurret not assigned! Assign manually in Inspector.");
            if (tankMovement == null)
                Debug.LogWarning("[CrosshairUI] TankMovement not assigned! Movement spread will not be shown.");
        }

        private void InitializeCrosshair()
        {
            // Если не назначены вручную, собираем автоматически (только для обратной совместимости)
            if (crosshairImages == null || crosshairImages.Length == 0)
            {
                crosshairImages = GetComponentsInChildren<Image>();
            }
            
            // Настраиваем cooldown circle
            if (cooldownCircle != null)
            {
                cooldownCircle.type = Image.Type.Filled;
                cooldownCircle.fillMethod = Image.FillMethod.Radial360;
                cooldownCircle.fillOrigin = (int)Image.Origin360.Top;
                cooldownCircle.fillAmount = 0f;
            }
        }

        private void Update()
        {
            if (weapon != null)
            {
                UpdateSpread();
                UpdateCooldown();
            }
            
            // UpdateColor всегда вызывается (не зависит от weapon)
            UpdateColor();
        }

        private void UpdateSpread()
        {
            // Получаем стабильность от турели (0-1)
            float stability = turret != null ? turret.CurrentStability : 1f;
            
            // Базовый разброс от стабильности пушки
            float targetSpread = Mathf.Lerp(maxSpread, minSpread, stability);
            
            // ИСПРАВЛЕНО: Добавляем разброс от движения танка
            if (tankMovement != null)
            {
                float movementFactor = tankMovement.GetMovementFactor();
                targetSpread += movementFactor * movementSpreadMultiplier;
            }
            
            // Плавная анимация
            currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * spreadAnimationSpeed);
            
            // Применяем к линиям прицела
            UpdateCrosshairLines();
        }

        private void UpdateCrosshairLines()
        {
            if (topLine != null)
                topLine.anchoredPosition = new Vector2(0f, currentSpread);
            
            if (bottomLine != null)
                bottomLine.anchoredPosition = new Vector2(0f, -currentSpread);
            
            if (leftLine != null)
                leftLine.anchoredPosition = new Vector2(-currentSpread, 0f);
            
            if (rightLine != null)
                rightLine.anchoredPosition = new Vector2(currentSpread, 0f);
        }

        private void UpdateCooldown()
        {
            if (cooldownCircle == null || weapon == null)
                return;
            
            // Вычисляем прогресс перезарядки
            float timeSinceLastFire = Time.time - weapon.LastFireTime;
            float cooldownProgress = Mathf.Clamp01(timeSinceLastFire / weapon.FireCooldown);
            
            // Обновляем fill amount (0 = перезарядка, 1 = готов)
            cooldownCircle.fillAmount = 1f - cooldownProgress;
            
            // Меняем цвет в зависимости от готовности
            if (weapon.CanFire)
            {
                cooldownCircle.color = readyColor;
            }
            else
            {
                cooldownCircle.color = cooldownColor;
            }
            
            // Скрываем когда полностью заряжено
            cooldownCircle.enabled = !weapon.CanFire;
        }

        private void UpdateColor()
        {
            Color finalColor = Color.white;
            
            // Проверяем есть ли цель под прицелом
            bool hasTarget = highlightTarget && CheckForTarget();
            
            if (hasTarget)
            {
                // Наведено на цель - используем target color
                finalColor = targetColor;
            }
            else if (colorByStability && turret != null)
            {
                // Цвет в зависимости от стабильности
                float stability = turret.CurrentStability;
                finalColor = Color.Lerp(unstableColor, stableColor, stability);
            }
            
            // Применяем ко всем элементам прицела (кроме cooldown circle)
            foreach (Image img in crosshairImages)
            {
                if (img != null && img != cooldownCircle)
                {
                    img.color = finalColor;
                }
            }
        }
        
        /// <summary>
        /// Проверяет есть ли цель под прицелом (raycast от камеры)
        /// </summary>
        private bool CheckForTarget()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return false;
            
            // Raycast от центра экрана
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            // Проверяем есть ли что-то под прицелом (с учетом layerMask)
            if (Physics.Raycast(ray, out RaycastHit hit, maxTargetDistance, targetLayers))
            {
                // Можно добавить дополнительную проверку по тегам или компонентам
                // Например: return hit.collider.CompareTag("Enemy");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Показать прицел
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Скрыть прицел
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Визуальная обратная связь при выстреле
        /// </summary>
        public void OnFire()
        {
            // Можно добавить анимацию отдачи, вспышку и т.д.
            StartCoroutine(FireFeedback());
        }

        private System.Collections.IEnumerator FireFeedback()
        {
            // Кратковременное увеличение разброса
            float originalSpread = currentSpread;
            currentSpread = maxSpread;
            
            yield return new WaitForSeconds(0.1f);
            
            currentSpread = originalSpread;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minSpread = Mathf.Max(0f, minSpread);
            maxSpread = Mathf.Max(minSpread, maxSpread);
        }
#endif
    }
}

