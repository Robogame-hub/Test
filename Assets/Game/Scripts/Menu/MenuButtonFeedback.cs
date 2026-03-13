using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TankGame.Menu
{
    public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Targets")]
        [Tooltip("Кнопка, для которой применяется фидбек.")]
        public Button button;
        [Tooltip("Текст кнопки, который меняет цвет и масштаб.")]
        public TMP_Text targetText;

        [Header("Text Colors")]
        [Tooltip("Базовый цвет текста.")]
        public Color normalTextColor = new Color32(0x0F, 0xF3, 0x00, 0xFF);
        [Tooltip("Цвет текста при наведении.")]
        public Color hoverTextColor = Color.red;
        [Tooltip("Цвет текста при нажатии.")]
        public Color pressedTextColor = Color.white;

        [Header("Scale")]
        [Tooltip("Масштаб текста при наведении.")]
        [Min(1f)]
        public float hoverTextScale = 1.08f;
        [Tooltip("Скорость анимации масштаба текста.")]
        [Min(1f)]
        public float scaleLerpSpeed = 16f;

        [Header("Audio")]
        [Tooltip("Источник для проигрывания UI-звуков.")]
        public AudioSource audioSource;
        [Tooltip("Звук при наведении на кнопку.")]
        public AudioClip hoverSound;
        [Tooltip("Звук при нажатии на кнопку.")]
        public AudioClip clickSound;

        private Vector3 initialTextScale = Vector3.one;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (targetText == null)
                targetText = GetComponentInChildren<TMP_Text>(true);

            if (targetText != null)
                initialTextScale = targetText.rectTransform.localScale;

            if (button != null)
                button.transition = Selectable.Transition.None;

            ApplyVisualState();
        }

        private void Update()
        {
            if (targetText == null)
                return;

            float targetMul = isHovered ? hoverTextScale : 1f;
            Vector3 targetScale = initialTextScale * targetMul;
            targetText.rectTransform.localScale = Vector3.Lerp(
                targetText.rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * scaleLerpSpeed);
        }

        public void Configure(
            Color normalColor,
            Color hoverColor,
            Color pressedColor,
            float hoverScale,
            float lerpSpeed,
            AudioSource sharedAudioSource,
            AudioClip hoverClip,
            AudioClip clickClip)
        {
            normalTextColor = normalColor;
            hoverTextColor = hoverColor;
            pressedTextColor = pressedColor;
            hoverTextScale = Mathf.Max(1f, hoverScale);
            scaleLerpSpeed = Mathf.Max(1f, lerpSpeed);
            audioSource = sharedAudioSource;
            hoverSound = hoverClip;
            clickSound = clickClip;
            ApplyVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            isPressed = false;
            PlayOneShot(hoverSound);
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            ApplyVisualState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            PlayOneShot(clickSound);
            ApplyVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (targetText == null)
                return;

            if (isPressed)
                targetText.color = pressedTextColor;
            else if (isHovered)
                targetText.color = hoverTextColor;
            else
                targetText.color = normalTextColor;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }
    }
}
