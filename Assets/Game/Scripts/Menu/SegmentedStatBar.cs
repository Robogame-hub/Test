using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Menu
{
    public class SegmentedStatBar : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Контейнер фона (красные палочки).")]
        public RectTransform backgroundContainer;
        [Tooltip("Контейнер заполнения (зеленые палочки).")]
        public RectTransform fillContainer;
        [Tooltip("Шаблон одной палочки (Image).")]
        public Image segmentTemplate;
        [Tooltip("Текст числового значения справа от палочек.")]
        public TMP_Text valueText;

        [Header("Segments")]
        [Tooltip("Количество палочек в шкале.")]
        [Min(1)]
        public int segmentCount = 10;

        [Header("Colors")]
        [Tooltip("Цвет фоновых палочек.")]
        public Color backgroundColor = new Color(0.9f, 0.15f, 0.15f, 0.55f);
        [Tooltip("Цвет активных палочек.")]
        public Color fillColor = new Color(1f, 0.67f, 0.25f, 0.96f);

        private readonly List<Image> backgroundSegments = new List<Image>();
        private readonly List<Image> fillSegments = new List<Image>();
        private bool isBuilt;

        private void Awake()
        {
            RebuildIfNeeded();
        }

        public void SetNormalized(float normalizedValue)
        {
            RebuildIfNeeded();

            float clamped = Mathf.Clamp01(normalizedValue);
            int filled = Mathf.Clamp(Mathf.RoundToInt(clamped * segmentCount), 0, segmentCount);

            for (int i = 0; i < fillSegments.Count; i++)
            {
                Image seg = fillSegments[i];
                if (seg != null)
                    seg.enabled = i < filled;
            }

            if (valueText != null)
                valueText.text = Mathf.RoundToInt(clamped * 100f).ToString();
        }

        public void SetIntValue(int current, int minValue, int maxValue)
        {
            float normalized = maxValue <= minValue ? 0f : Mathf.InverseLerp(minValue, maxValue, current);
            SetNormalized(normalized);
            if (valueText != null)
                valueText.text = current.ToString();
        }

        public void SetBoolValue(bool state)
        {
            SetNormalized(state ? 1f : 0f);
            if (valueText != null)
                valueText.text = state ? "1" : "0";
        }

        private void RebuildIfNeeded()
        {
            if (isBuilt)
                return;
            if (backgroundContainer == null || fillContainer == null || segmentTemplate == null)
                return;

            BuildSegments(backgroundContainer, backgroundSegments, backgroundColor);
            BuildSegments(fillContainer, fillSegments, fillColor);

            segmentTemplate.gameObject.SetActive(false);
            isBuilt = true;
        }

        private void BuildSegments(Transform parent, List<Image> target, Color color)
        {
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (target[i] != null)
                    Destroy(target[i].gameObject);
            }
            target.Clear();

            for (int i = 0; i < segmentCount; i++)
            {
                Image seg = Instantiate(segmentTemplate, parent);
                seg.gameObject.SetActive(true);
                seg.color = color;
                target.Add(seg);
            }
        }
    }
}
