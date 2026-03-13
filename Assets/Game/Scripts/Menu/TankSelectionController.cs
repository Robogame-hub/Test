using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TankGame.Session;

namespace TankGame.Menu
{
    public class TankSelectionController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Список доступных танков для выбора в меню.")]
        public List<TankDefinition> tanks = new List<TankDefinition>();

        [Header("Controls")]
        [Tooltip("Кнопка переключения на предыдущий танк.")]
        public Button previousButton;
        [Tooltip("Кнопка переключения на следующий танк.")]
        public Button nextButton;

        [Header("View")]
        [Tooltip("Текст с названием выбранного танка.")]
        public TMP_Text tankNameText;
        [Tooltip("Окно предпросмотра танка (старый Image-слот, используется как контейнер и fallback).")]
        public Image tankPreviewImage;
        [Tooltip("Заполнение индикатора скорости (Image Filled).")]
        public Image speedFill;
        [Tooltip("Заполнение индикатора брони (Image Filled).")]
        public Image armorFill;
        [Tooltip("Заполнение индикатора огневой мощи (Image Filled).")]
        public Image firepowerFill;
        [Tooltip("Заполнение индикатора управляемости (Image Filled).")]
        public Image handlingFill;

        [Header("3D Preview")]
        [Tooltip("RawImage для вывода 3D-превью. Если пусто, создается автоматически внутри окна preview.")]
        public RawImage previewRawImage;
        [Tooltip("Точка, в которой будет стоять 3D-модель для предпросмотра. Если пусто, создается автоматически.")]
        public Transform previewModelRoot;
        [Tooltip("Камера, рендерящая 3D-превью. Если пусто, создается автоматически.")]
        public Camera previewCamera;
        [Tooltip("Позиция камеры относительно previewModelRoot.")]
        public Vector3 previewCameraOffset = new Vector3(0f, 1.6f, -4f);
        [Tooltip("Скорость ручного вращения модели по оси Y.")]
        public float manualRotateSpeed = 1f;

        private int currentIndex;
        private GameObject currentPreviewInstance;
        private RenderTexture previewRenderTexture;

        private void Start()
        {
            if (previousButton != null)
                previousButton.onClick.AddListener(SelectPrevious);
            if (nextButton != null)
                nextButton.onClick.AddListener(SelectNext);

            currentIndex = Mathf.Clamp(GameSessionSettings.SelectedTankIndex, 0, Mathf.Max(0, tanks.Count - 1));

            EnsurePreviewInfrastructure();
            Refresh();
        }

        private void OnDestroy()
        {
            if (previousButton != null)
                previousButton.onClick.RemoveListener(SelectPrevious);
            if (nextButton != null)
                nextButton.onClick.RemoveListener(SelectNext);

            if (currentPreviewInstance != null)
                Destroy(currentPreviewInstance);

            if (previewCamera != null)
                previewCamera.targetTexture = null;

            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }
        }

        public void SelectPrevious()
        {
            if (tanks.Count == 0)
                return;

            currentIndex = (currentIndex - 1 + tanks.Count) % tanks.Count;
            GameSessionSettings.SelectedTankIndex = currentIndex;
            Refresh();
        }

        public void SelectNext()
        {
            if (tanks.Count == 0)
                return;

            currentIndex = (currentIndex + 1) % tanks.Count;
            GameSessionSettings.SelectedTankIndex = currentIndex;
            Refresh();
        }

        public GameObject GetSelectedTankPrefab()
        {
            if (tanks.Count == 0)
                return null;

            int index = Mathf.Clamp(currentIndex, 0, tanks.Count - 1);
            return tanks[index].playerPrefab;
        }

        public void RotatePreview(float yawDelta)
        {
            if (currentPreviewInstance == null)
                return;

            currentPreviewInstance.transform.Rotate(0f, yawDelta * manualRotateSpeed, 0f, Space.Self);
        }

        private void Refresh()
        {
            if (tanks.Count == 0)
            {
                if (tankNameText != null)
                    tankNameText.text = "Tank";

                DestroyPreviewInstance();
                SetFallbackSpriteVisible(false, null);
                SetFill(speedFill, 0f);
                SetFill(armorFill, 0f);
                SetFill(firepowerFill, 0f);
                SetFill(handlingFill, 0f);
                return;
            }

            TankDefinition selected = tanks[Mathf.Clamp(currentIndex, 0, tanks.Count - 1)];

            if (tankNameText != null)
                tankNameText.text = string.IsNullOrWhiteSpace(selected.displayName)
                    ? $"Tank {currentIndex + 1}"
                    : selected.displayName;

            bool built3DPreview = Build3DPreview(selected);
            if (!built3DPreview)
                SetFallbackSpriteVisible(true, selected.previewSprite);

            SetFill(speedFill, selected.speed);
            SetFill(armorFill, selected.armor);
            SetFill(firepowerFill, selected.firepower);
            SetFill(handlingFill, selected.handling);
        }

        private bool Build3DPreview(TankDefinition definition)
        {
            EnsurePreviewInfrastructure();
            if (previewModelRoot == null || previewCamera == null || previewRawImage == null)
                return false;

            GameObject prefab = definition.previewModelPrefab != null
                ? definition.previewModelPrefab
                : definition.playerPrefab;

            if (prefab == null)
                return false;

            DestroyPreviewInstance();

            currentPreviewInstance = Instantiate(prefab, previewModelRoot);
            currentPreviewInstance.name = prefab.name + "_Preview";
            currentPreviewInstance.transform.localPosition = Vector3.zero;
            currentPreviewInstance.transform.localRotation = Quaternion.identity;

            float scale = Mathf.Max(0.01f, definition.previewModelScale);
            currentPreviewInstance.transform.localScale = Vector3.one * scale;

            DisableNonVisualComponents(currentPreviewInstance);
            FocusPreviewCamera(currentPreviewInstance);

            if (previewRawImage != null)
                previewRawImage.enabled = true;
            if (tankPreviewImage != null)
                tankPreviewImage.enabled = false;

            return true;
        }

        private void DestroyPreviewInstance()
        {
            if (currentPreviewInstance == null)
                return;

            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }

        private void EnsurePreviewInfrastructure()
        {
            EnsureRawImageTarget();
            EnsurePreviewRoot();
            EnsurePreviewCamera();
            EnsureDragRotator();
        }

        private void EnsureRawImageTarget()
        {
            if (previewRawImage != null)
                return;

            Transform parent = tankPreviewImage != null ? tankPreviewImage.transform : transform;
            GameObject rawObj = new GameObject("TankPreview3D", typeof(RectTransform), typeof(RawImage));
            rawObj.transform.SetParent(parent, false);

            RectTransform rt = rawObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            previewRawImage = rawObj.GetComponent<RawImage>();
            previewRawImage.color = Color.white;
            previewRawImage.raycastTarget = true;
        }

        private void EnsurePreviewRoot()
        {
            if (previewModelRoot != null)
                return;

            GameObject root = new GameObject("TankPreviewRoot");
            root.transform.position = new Vector3(1000f, 1000f, 1000f);
            previewModelRoot = root.transform;
        }

        private void EnsurePreviewCamera()
        {
            if (previewCamera == null)
            {
                GameObject cameraObj = new GameObject("TankPreviewCamera", typeof(Camera));
                previewCamera = cameraObj.GetComponent<Camera>();
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                previewCamera.fieldOfView = 35f;
                previewCamera.nearClipPlane = 0.01f;
                previewCamera.farClipPlane = 200f;
                previewCamera.enabled = true;

                GameObject lightObj = new GameObject("TankPreviewLight", typeof(Light));
                lightObj.transform.SetParent(cameraObj.transform, false);
                lightObj.transform.localPosition = new Vector3(1f, 2f, -1f);
                lightObj.transform.localRotation = Quaternion.Euler(35f, -35f, 0f);
                Light light = lightObj.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
            }

            if (previewRenderTexture == null)
            {
                previewRenderTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32)
                {
                    name = "TankPreviewRT"
                };
                previewRenderTexture.Create();
            }

            previewCamera.targetTexture = previewRenderTexture;
            if (previewRawImage != null)
                previewRawImage.texture = previewRenderTexture;
        }

        private void EnsureDragRotator()
        {
            GameObject dragTarget = previewRawImage != null
                ? previewRawImage.gameObject
                : (tankPreviewImage != null ? tankPreviewImage.gameObject : null);

            if (dragTarget == null)
                return;

            TankPreviewDragRotator rotator = dragTarget.GetComponent<TankPreviewDragRotator>();
            if (rotator == null)
                rotator = dragTarget.AddComponent<TankPreviewDragRotator>();

            rotator.selectionController = this;
        }

        private void FocusPreviewCamera(GameObject model)
        {
            if (previewCamera == null || previewModelRoot == null || model == null)
                return;

            Bounds bounds = CalculateModelBounds(model);
            Vector3 center = bounds.center;
            previewCamera.transform.position = center + previewCameraOffset;
            previewCamera.transform.LookAt(center);
        }

        private static Bounds CalculateModelBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static void DisableNonVisualComponents(GameObject go)
        {
            MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < scripts.Length; i++)
            {
                MonoBehaviour script = scripts[i];
                if (script != null)
                    script.enabled = false;
            }

            Rigidbody[] rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                    rigidbodies[i].isKinematic = true;
            }

            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            AudioSource[] audios = go.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++)
            {
                if (audios[i] != null)
                    audios[i].enabled = false;
            }
        }

        private void SetFallbackSpriteVisible(bool visible, Sprite sprite)
        {
            if (previewRawImage != null)
                previewRawImage.enabled = !visible;

            if (tankPreviewImage == null)
                return;

            tankPreviewImage.enabled = visible;
            tankPreviewImage.sprite = sprite;
        }

        private static void SetFill(Image image, float value)
        {
            if (image == null)
                return;

            image.fillAmount = Mathf.Clamp01(value);
        }
    }
}
