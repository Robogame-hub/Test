using UnityEngine;
using UnityEngine.UI;
using TankGame.Tank.Components;
#if UNITY_TMPRO
using TMPro;
#endif

namespace TankGame.UI
{
    /// <summary>
    /// UI компонент для отображения здоровья танка в виде цифр
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Ссылка на TankHealth для отслеживания здоровья")]
        [SerializeField] private TankHealth tankHealth;
        
        [Header("UI Elements")]
        [Tooltip("Text компонент для отображения здоровья (Unity UI Text - используйте ОДИН из вариантов)")]
        [SerializeField] private Text healthText;
        
#if UNITY_TMPRO
        [Tooltip("TextMeshProUGUI компонент для отображения здоровья (если используете TextMeshPro вместо Text)")]
        [SerializeField] private TextMeshProUGUI healthTextTMP;
#endif
        
        [Header("Display Settings")]
        [Tooltip("Формат отображения здоровья (например: \"HP: {0}/{1}\" или \"{0}\")")]
        [SerializeField] private string healthFormat = "HP: {0}/{1}";
        
        [Tooltip("Показывать максимальное здоровье")]
        [SerializeField] private bool showMaxHealth = true;
        
        [Tooltip("Цвет текста при полном здоровье")]
        [SerializeField] private Color fullHealthColor = Color.green;
        
        [Tooltip("Цвет текста при низком здоровье")]
        [SerializeField] private Color lowHealthColor = Color.red;
        
        [Tooltip("Порог низкого здоровья (0-1, например 0.3 = 30%)")]
        [Range(0f, 1f)]
        [SerializeField] private float lowHealthThreshold = 0.3f;

        private void Start()
        {
            // Проверка компонентов
            if (tankHealth == null)
            {
                Debug.LogWarning("[HealthUI] TankHealth not assigned! Assign manually in Inspector.");
            }
            
            // Проверяем наличие текстового компонента
#if UNITY_TMPRO
            if (healthText == null && healthTextTMP == null)
#else
            if (healthText == null)
#endif
            {
                Debug.LogWarning("[HealthUI] Health Text not assigned! Create a UI Text element (Text or TextMeshPro) and assign it manually in Inspector.");
            }
            
            // Подписываемся на события изменения здоровья
            if (tankHealth != null)
            {
                tankHealth.OnHealthChanged.AddListener(OnHealthChanged);
                // Обновляем UI сразу
                UpdateHealthDisplay(tankHealth.CurrentHealth, tankHealth.MaxHealth);
            }
        }
        
        private void OnDestroy()
        {
            // Отписываемся от событий
            if (tankHealth != null)
            {
                tankHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            }
        }
        
        /// <summary>
        /// Обработчик изменения здоровья
        /// </summary>
        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            UpdateHealthDisplay(currentHealth, maxHealth);
        }
        
        /// <summary>
        /// Обновляет отображение здоровья
        /// </summary>
        private void UpdateHealthDisplay(float currentHealth, float maxHealth)
        {
            // Формируем текст
            string healthString;
            if (showMaxHealth)
            {
                healthString = string.Format(healthFormat, Mathf.CeilToInt(currentHealth), Mathf.CeilToInt(maxHealth));
            }
            else
            {
                healthString = string.Format(healthFormat.Replace("/{1}", "").Replace("{1}", ""), Mathf.CeilToInt(currentHealth));
            }
            
            // Вычисляем цвет на основе процента здоровья
            // Плавный переход от зеленого (100% HP) к красному (0% HP)
            float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            Color healthColor = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
            
            // Обновляем UI (поддержка и Text, и TextMeshPro)
            if (healthText != null)
            {
                healthText.text = healthString;
                healthText.color = healthColor;
            }
#if UNITY_TMPRO
            else if (healthTextTMP != null)
            {
                healthTextTMP.text = healthString;
                healthTextTMP.color = healthColor;
            }
#endif
        }
    }
}

