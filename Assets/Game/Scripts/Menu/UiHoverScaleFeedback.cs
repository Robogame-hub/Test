using UnityEngine;
using UnityEngine.EventSystems;

namespace TankGame.Menu
{
    public class UiHoverScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Какой Transform масштабировать при наведении. Если не задан, используется текущий.")]
        public RectTransform target;
        [Tooltip("Множитель масштаба при наведении.")]
        [Min(1f)]
        public float hoverScale = 1.03f;
        [Tooltip("Скорость плавной анимации масштаба.")]
        [Min(1f)]
        public float lerpSpeed = 14f;

        private Vector3 initialScale = Vector3.one;
        private bool hovered;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            if (target != null)
                initialScale = target.localScale;
        }

        private void Update()
        {
            if (target == null)
                return;

            float mul = hovered ? hoverScale : 1f;
            Vector3 to = initialScale * mul;
            target.localScale = Vector3.Lerp(target.localScale, to, Time.unscaledDeltaTime * lerpSpeed);
        }

        public void Configure(float scale, float speed)
        {
            hoverScale = Mathf.Max(1f, scale);
            lerpSpeed = Mathf.Max(1f, speed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }
    }
}
