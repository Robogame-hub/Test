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
        [Tooltip("Image кнопки, который меняет цвет по состояниям.")]
        public Image targetButtonImage;

        [Header("Text Colors")]
        [Tooltip("Базовый цвет текста.")]
        public Color normalTextColor = new Color(0.96f, 0.86f, 0.67f, 1f);
        [Tooltip("Цвет текста при наведении.")]
        public Color hoverTextColor = new Color(1f, 0.74f, 0.37f, 1f);
        [Tooltip("Цвет текста при нажатии.")]
        public Color pressedTextColor = new Color(1f, 0.96f, 0.87f, 1f);

        [Header("Button Colors")]
        [Tooltip("Базовый цвет кнопки.")]
        public Color normalButtonColor = new Color(0.27f, 0.17f, 0.1f, 0.88f);
        [Tooltip("Цвет кнопки при наведении.")]
        public Color hoverButtonColor = new Color(0.39f, 0.24f, 0.12f, 0.92f);
        [Tooltip("Цвет кнопки при нажатии.")]
        public Color pressedButtonColor = new Color(0.54f, 0.31f, 0.13f, 0.96f);

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
            if (targetButtonImage == null)
                targetButtonImage = ResolveButtonImage();

            if (targetText != null)
                initialTextScale = targetText.rectTransform.localScale;

            ApplyVisualState();
        }

        private void OnEnable()
        {
            ResetState(immediateScale: true);
        }

        private void OnDisable()
        {
            ResetState(immediateScale: true);
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
            Color normalBgColor,
            Color hoverBgColor,
            Color pressedBgColor,
            float hoverScale,
            float lerpSpeed,
            AudioSource sharedAudioSource,
            AudioClip hoverClip,
            AudioClip clickClip)
        {
            normalTextColor = normalColor;
            hoverTextColor = hoverColor;
            pressedTextColor = pressedColor;
            normalButtonColor = normalBgColor;
            hoverButtonColor = hoverBgColor;
            pressedButtonColor = pressedBgColor;
            hoverTextScale = Mathf.Max(1f, hoverScale);
            scaleLerpSpeed = Mathf.Max(1f, lerpSpeed);
            audioSource = sharedAudioSource;
            hoverSound = hoverClip;
            clickSound = clickClip;

            if (targetButtonImage == null)
                targetButtonImage = ResolveButtonImage();

            if (button != null)
            {
                button.transition = Selectable.Transition.ColorTint;
                if (button.targetGraphic == null && targetButtonImage != null)
                    button.targetGraphic = targetButtonImage;

                ColorBlock colors = button.colors;
                colors.normalColor = normalButtonColor;
                colors.highlightedColor = hoverButtonColor;
                colors.selectedColor = hoverButtonColor;
                colors.pressedColor = pressedButtonColor;
                colors.disabledColor = new Color(normalButtonColor.r, normalButtonColor.g, normalButtonColor.b, Mathf.Clamp01(normalButtonColor.a * 0.5f));
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.06f;
                button.colors = colors;
            }

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

        public void ResetState(bool immediateScale = false)
        {
            isHovered = false;
            isPressed = false;
            ApplyVisualState();

            if (immediateScale && targetText != null)
                targetText.rectTransform.localScale = initialTextScale;
        }

        private void ApplyVisualState()
        {
            Color textColor;
            Color buttonColor;

            if (isPressed)
            {
                textColor = pressedTextColor;
                buttonColor = pressedButtonColor;
            }
            else if (isHovered)
            {
                textColor = hoverTextColor;
                buttonColor = hoverButtonColor;
            }
            else
            {
                textColor = normalTextColor;
                buttonColor = normalButtonColor;
            }

            if (targetText != null)
                targetText.color = textColor;

            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = buttonColor;

            if (targetButtonImage != null && (button == null || targetButtonImage != button.targetGraphic))
                targetButtonImage.color = buttonColor;
        }

        private Image ResolveButtonImage()
        {
            if (button == null)
                return null;

            if (button.targetGraphic is Image targetGraphicImage)
                return targetGraphicImage;

            Image ownImage = button.GetComponent<Image>();
            if (ownImage != null)
                return ownImage;

            Image[] images = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    return images[i];
            }

            return null;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }
    }
}
