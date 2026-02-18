using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TankGame.Tank.Components;

namespace TankGame.UI
{
    /// <summary>
    /// Показывает текущую стамину форсажа.
    /// Восполнение пока не реализовано — отображает только текущее падение.
    /// </summary>
    public class StaminaUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankMovement tankMovement;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.85f, 1f, 1f);
        [SerializeField] private Color lowColor = new Color(1f, 0.4f, 0.2f, 1f);
        [SerializeField] private float lowThreshold = 0.25f;

        private void Start()
        {
            if (tankMovement == null)
                tankMovement = FindObjectOfType<TankMovement>();

            if (staminaSlider != null)
            {
                staminaSlider.minValue = 0f;
                staminaSlider.maxValue = 1f;
            }

            Update();
        }

        private void Update()
        {
            if (tankMovement == null)
                return;

            float normalized = tankMovement.StaminaNormalized;

            if (staminaSlider != null)
                staminaSlider.value = normalized;

            if (staminaText != null)
                staminaText.text = $"{Mathf.CeilToInt(tankMovement.CurrentStamina)}/{Mathf.CeilToInt(tankMovement.MaxStamina)}";

            if (staminaFillImage != null)
                staminaFillImage.color = normalized <= lowThreshold ? lowColor : normalColor;
        }
    }
}
