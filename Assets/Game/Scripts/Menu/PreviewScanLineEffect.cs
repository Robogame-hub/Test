using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Menu
{
    [DisallowMultipleComponent]
    public class PreviewScanLineEffect : MonoBehaviour
    {
        private const string ScanLineObjectName = "PreviewScanLine";

        [SerializeField]
        private Image scanLineImage;

        private Sprite scanLineSprite;
        private Color scanLineColor = new Color(1f, 0.67f, 0.25f, 0.45f);
        private float scanLineHeight = 12f;
        private float scanLineSpeed = 120f;

        private RectTransform hostRect;
        private RectTransform scanLineRect;
        private float currentY;
        private bool hasPosition;

        public void Configure(Sprite sprite, Color color, float height, float speed)
        {
            scanLineSprite = sprite;
            scanLineColor = color;
            scanLineHeight = Mathf.Max(1f, height);
            scanLineSpeed = Mathf.Max(0.01f, speed);

            EnsureReferences();
            ApplyVisuals();
        }

        private void Awake()
        {
            EnsureReferences();
            ApplyVisuals();
        }

        private void OnEnable()
        {
            EnsureReferences();
            ApplyVisuals();
        }

        private void Update()
        {
            if (scanLineImage == null || scanLineRect == null || !scanLineImage.gameObject.activeSelf)
                return;

            float hostHeight = hostRect != null ? hostRect.rect.height : 0f;
            if (hostHeight <= 0f)
                return;

            float halfRange = Mathf.Max(0f, (hostHeight - scanLineHeight) * 0.5f);
            if (!hasPosition || currentY > halfRange || currentY < -halfRange)
            {
                currentY = halfRange;
                hasPosition = true;
            }

            currentY -= scanLineSpeed * Time.unscaledDeltaTime;
            if (currentY < -halfRange)
                currentY = halfRange;

            scanLineRect.anchoredPosition = new Vector2(0f, currentY);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            scanLineHeight = Mathf.Max(1f, scanLineHeight);
            scanLineSpeed = Mathf.Max(0.01f, scanLineSpeed);
            EnsureReferences();
            ApplyVisuals();
        }
#endif

        private void EnsureReferences()
        {
            hostRect = transform as RectTransform;
            if (hostRect == null)
                return;

            if (scanLineImage == null)
            {
                Transform child = transform.Find(ScanLineObjectName);
                if (child != null)
                {
                    scanLineImage = child.GetComponent<Image>();
                    if (scanLineImage == null)
                        scanLineImage = child.gameObject.AddComponent<Image>();
                }
            }

            if (scanLineImage == null)
            {
                GameObject lineObject = new GameObject(ScanLineObjectName, typeof(RectTransform), typeof(Image));
                lineObject.transform.SetParent(transform, false);
                scanLineImage = lineObject.GetComponent<Image>();
            }

            if (scanLineImage != null)
                scanLineRect = scanLineImage.rectTransform;
        }

        private void ApplyVisuals()
        {
            if (scanLineImage == null || scanLineRect == null)
                return;

            bool isConfigured = scanLineSprite != null && scanLineColor.a > 0f;
            scanLineImage.gameObject.SetActive(isConfigured);
            scanLineImage.raycastTarget = false;
            scanLineImage.sprite = scanLineSprite;
            scanLineImage.color = scanLineColor;
            scanLineImage.type = Image.Type.Simple;
            scanLineImage.preserveAspect = false;

            scanLineRect.anchorMin = new Vector2(0f, 0.5f);
            scanLineRect.anchorMax = new Vector2(1f, 0.5f);
            scanLineRect.pivot = new Vector2(0.5f, 0.5f);
            scanLineRect.sizeDelta = new Vector2(0f, scanLineHeight);
            scanLineRect.SetAsLastSibling();

            hasPosition = false;
        }
    }
}
