using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TankGame.Tank;
using TankGame.Tank.Components;

namespace TankGame.UI
{
    /// <summary>
    /// Шкала стамины форсажа: полоска на Image (fillAmount) с фоном, без Slider.
    /// Рекомендуемая иерархия: Panel → Background (Image) → Fill (Image, Filled).
    /// </summary>
    public class StaminaUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankMovement tankMovement;
        [Tooltip("Фон полоски (тёмный прямоугольник)")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("Текст, например «85 / 100»")]
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Bars")]
        [Tooltip("Родитель для вертикальных полосок стамины")]
        [SerializeField] private RectTransform barsContainer;
        [Tooltip("Префаб одной вертикальной полоски (Image)")]
        [SerializeField] private Image barPrefab;
        [Tooltip("Сколько единиц стамины даёт одна полоска")]
        [SerializeField] private float unitsPerBar = 10f;

        [Header("Appearance")]
        [SerializeField] private Color fullColor = new Color(0.25f, 0.88f, 1f, 0.95f);
        [SerializeField] private Color lowColor = new Color(1f, 0.5f, 0.2f, 0.95f);
        [SerializeField] private float lowThreshold = 0.25f;

        [Header("Visibility")]
        [Tooltip("Скрывать только когда игрок мёртв или отсутствует. Не скрывать при нулевой стамине.")]
        [SerializeField] private bool hideWhenInactive = true;

        private CanvasGroup _canvasGroup;
        private readonly List<Image> _bars = new List<Image>();
        private int _totalBars;
        private bool _barsInitialized;

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null && hideWhenInactive)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            RefreshMovement();
            TryInitBars();
        }

        private void RefreshMovement()
        {
            if (tankMovement != null) return;
            var player = TankRegistry.GetLocalPlayer();
            if (player != null) tankMovement = player.Movement;
            if (tankMovement != null)
                TryInitBars();
        }

        private void TryInitBars()
        {
            if (_barsInitialized)
                return;
            if (tankMovement == null || barsContainer == null || barPrefab == null)
                return;

            float maxStamina = Mathf.Max(1f, tankMovement.MaxStamina);
            _totalBars = Mathf.Max(1, Mathf.CeilToInt(maxStamina / unitsPerBar));

            _bars.Clear();
            for (int i = 0; i < _totalBars; i++)
            {
                var bar = Instantiate(barPrefab, barsContainer);
                bar.gameObject.SetActive(true);
                _bars.Add(bar);
            }

            // Если префаб находился на сцене как пример — прячем его
            if (barPrefab.gameObject.activeSelf)
                barPrefab.gameObject.SetActive(false);

            _barsInitialized = true;
        }

        private void Update()
        {
            RefreshMovement();

            if (tankMovement == null)
            {
                if (hideWhenInactive) SetVisible(false);
                return;
            }

            var health = tankMovement.GetComponent<TankHealth>();
            if (hideWhenInactive && health != null && !health.IsAlive())
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            float current = tankMovement.CurrentStamina;
            float max = Mathf.Max(1f, tankMovement.MaxStamina);
            float norm = Mathf.Clamp01(current / max);

            if (_barsInitialized)
            {
                int barsToShow = Mathf.Clamp(Mathf.CeilToInt(current / unitsPerBar), 0, _totalBars);
                Color visibleColor = norm <= lowThreshold ? lowColor : fullColor;

                for (int i = 0; i < _bars.Count; i++)
                {
                    var bar = _bars[i];
                    if (bar == null) continue;

                    bool filled = i < barsToShow;

                    if (filled)
                    {
                        bar.color = visibleColor;
                    }
                    else
                    {
                        var c = visibleColor;
                        c.a = 0f; // делаем полоску полностью прозрачной, чтобы не влиять на лейаут
                        bar.color = c;
                    }
                }
            }

            if (staminaText != null)
                staminaText.text = $"{Mathf.CeilToInt(tankMovement.CurrentStamina)} / {Mathf.CeilToInt(tankMovement.MaxStamina)}";
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
            else if (hideWhenInactive)
                gameObject.SetActive(visible);
        }
    }
}
