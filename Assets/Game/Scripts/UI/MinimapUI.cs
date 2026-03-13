using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TankGame.Tank;

namespace TankGame.UI
{
    /// <summary>
    /// Мини-карта на статичной текстуре. Игрок и боты (танки с IsLocalPlayer == false) отображаются иконками.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        [Header("Map Texture")]
        [SerializeField] private Texture2D mapTexture;
        [SerializeField] private Sprite mapSprite;
        [SerializeField] private bool flipVertical;

        [Header("Игровая область (мир XZ)")]
        [Tooltip("Задать маркерами: поставьте два пустых объекта в углах игровой зоны и перетащите сюда")]
        [SerializeField] private Transform worldCornerMin;
        [SerializeField] private Transform worldCornerMax;
        [Tooltip("Если маркеры не заданы — используются эти границы (World Min = левый нижний угол по XZ)")]
        [SerializeField] private Vector2 worldMinFallback = new Vector2(-50f, -50f);
        [Tooltip("World Max = правый верхний угол по XZ")]
        [SerializeField] private Vector2 worldMaxFallback = new Vector2(50f, 50f);
        [Tooltip("Взять границы из Terrain в сцене (если есть и маркеры не заданы)")]
        [SerializeField] private bool useTerrainBoundsIfNoMarkers = true;

        [Header("UI")]
        [Tooltip("Image или RawImage с текстурой карты (обязательно тот, на котором видна карта)")]
        [SerializeField] private Graphic mapGraphic;
        [Tooltip("Иконка игрока — будет дочерней к карте и двигаться по ней")]
        [SerializeField] private RectTransform playerIcon;
        [SerializeField] private bool rotateIconWithPlayer = true;

        [Header("Боты на мини-карте")]
        [SerializeField] private bool showBots = true;
        [Tooltip("Крутить иконку бота по направлению танка")]
        [SerializeField] private bool rotateBotIcons = true;
        [Tooltip("Префаб иконки бота (RectTransform с Image). Если пусто — создаётся простая точка")]
        [SerializeField] private RectTransform botIconPrefab;
        [Tooltip("Цвет иконки бота, если префаб не задан")]
        [SerializeField] private Color botIconColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        [SerializeField] private int botIconPoolSize = 24;

        [Header("Player")]
        [SerializeField] private Transform playerTarget;

        [Header("Debug")]
        [SerializeField] private bool logMappingOnce;

        private RectTransform _mapRect;
        private float _worldSizeX, _worldSizeZ;
        private float _worldMinX, _worldMinZ, _worldMaxX, _worldMaxZ;
        private bool _boundsComputed;
        private readonly List<RectTransform> _botIconPool = new List<RectTransform>();
        private int _botIconsUsed;

        private void Start()
        {
            if (mapGraphic == null)
                mapGraphic = GetComponentInChildren<RawImage>() ?? (Graphic)GetComponentInChildren<Image>();

            if (mapGraphic != null)
            {
                _mapRect = mapGraphic.rectTransform;
                ApplyMapTexture();
            }
            else
                Debug.LogWarning("[MinimapUI] Map Image/RawImage not assigned.");

            ComputeWorldBounds();
            FindPlayerIfNeeded();

            if (playerIcon != null && _mapRect != null)
            {
                if (playerIcon.parent != _mapRect)
                    playerIcon.SetParent(_mapRect, false);
                playerIcon.anchorMin = new Vector2(0.5f, 0.5f);
                playerIcon.anchorMax = new Vector2(0.5f, 0.5f);
                playerIcon.pivot = new Vector2(0.5f, 0.5f);
            }

            if (showBots && _mapRect != null)
                BuildBotIconPool();
        }

        private void BuildBotIconPool()
        {
            _botIconPool.Clear();
            for (int i = 0; i < botIconPoolSize; i++)
            {
                RectTransform rt;
                if (botIconPrefab != null)
                {
                    rt = Instantiate(botIconPrefab, _mapRect, false);
                }
                else
                {
                    var go = new GameObject("MinimapBotIcon");
                    rt = go.AddComponent<RectTransform>();
                    rt.SetParent(_mapRect, false);
                    var img = go.AddComponent<Image>();
                    img.color = botIconColor;
                    img.raycastTarget = false;
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(8f, 8f);
                }
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.gameObject.SetActive(false);
                _botIconPool.Add(rt);
            }
        }

        private void ComputeWorldBounds()
        {
            if (worldCornerMin != null && worldCornerMax != null)
            {
                _worldMinX = Mathf.Min(worldCornerMin.position.x, worldCornerMax.position.x);
                _worldMaxX = Mathf.Max(worldCornerMin.position.x, worldCornerMax.position.x);
                _worldMinZ = Mathf.Min(worldCornerMin.position.z, worldCornerMax.position.z);
                _worldMaxZ = Mathf.Max(worldCornerMin.position.z, worldCornerMax.position.z);
            }
            else if (useTerrainBoundsIfNoMarkers)
            {
                var terrain = FindObjectOfType<Terrain>();
                if (terrain != null)
                {
                    var td = terrain.terrainData;
                    var pos = terrain.transform.position;
                    _worldMinX = pos.x;
                    _worldMinZ = pos.z;
                    _worldMaxX = pos.x + td.size.x;
                    _worldMaxZ = pos.z + td.size.z;
                }
                else
                {
                    _worldMinX = worldMinFallback.x;
                    _worldMinZ = worldMinFallback.y;
                    _worldMaxX = worldMaxFallback.x;
                    _worldMaxZ = worldMaxFallback.y;
                }
            }
            else
            {
                _worldMinX = worldMinFallback.x;
                _worldMinZ = worldMinFallback.y;
                _worldMaxX = worldMaxFallback.x;
                _worldMaxZ = worldMaxFallback.y;
            }

            _worldSizeX = _worldMaxX - _worldMinX;
            _worldSizeZ = _worldMaxZ - _worldMinZ;
            if (_worldSizeX <= 0.001f) _worldSizeX = 1f;
            if (_worldSizeZ <= 0.001f) _worldSizeZ = 1f;
            _boundsComputed = true;
        }

        private void FindPlayerIfNeeded()
        {
            if (playerTarget != null) return;
            var local = TankRuntime.GetLocalPlayer();
            if (local != null)
                playerTarget = local.transform;
            if (playerTarget == null)
                Debug.LogWarning("[MinimapUI] Player target not set and no TankController found.");
        }

        private void ApplyMapTexture()
        {
            var tex = mapTexture != null ? mapTexture : (mapSprite != null ? mapSprite.texture : null);
            if (tex == null) return;

            if (mapGraphic is RawImage rawImage)
            {
                rawImage.texture = tex;
                rawImage.uvRect = new Rect(0, flipVertical ? 1f : 0f, 1f, flipVertical ? -1f : 1f);
            }
            else if (mapGraphic is Image img && mapSprite != null)
            {
                img.sprite = mapSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
            }
        }

        private void LateUpdate()
        {
            if (!_boundsComputed)
                ComputeWorldBounds();
            if (playerTarget == null || _mapRect == null || playerIcon == null)
                return;

            // Обновляем границы, если заданы маркеры (можно двигать в редакторе)
            if (worldCornerMin != null && worldCornerMax != null)
            {
                _worldMinX = Mathf.Min(worldCornerMin.position.x, worldCornerMax.position.x);
                _worldMaxX = Mathf.Max(worldCornerMin.position.x, worldCornerMax.position.x);
                _worldMinZ = Mathf.Min(worldCornerMin.position.z, worldCornerMax.position.z);
                _worldMaxZ = Mathf.Max(worldCornerMin.position.z, worldCornerMax.position.z);
                _worldSizeX = _worldMaxX - _worldMinX;
                _worldSizeZ = _worldMaxZ - _worldMinZ;
                if (_worldSizeX <= 0.001f) _worldSizeX = 1f;
                if (_worldSizeZ <= 0.001f) _worldSizeZ = 1f;
            }

            float x = playerTarget.position.x;
            float z = playerTarget.position.z;

            // Нормализованная позиция в игровой области: 0 = левый/нижний край, 1 = правый/верхний
            float normX = Mathf.Clamp01((x - _worldMinX) / _worldSizeX);
            float normZ = Mathf.Clamp01((z - _worldMinZ) / _worldSizeZ);
            if (flipVertical) normZ = 1f - normZ;

            if (logMappingOnce)
            {
                Debug.Log($"[MinimapUI] World: ({x:F1}, {z:F1}) | Bounds: X=[{_worldMinX:F0},{_worldMaxX:F0}] Z=[{_worldMinZ:F0},{_worldMaxZ:F0}] | Norm: ({normX:F2}, {normZ:F2})");
                logMappingOnce = false;
            }

            Vector2 mapPos = WorldToMinimapPosition(x, z);
            playerIcon.anchoredPosition = mapPos;

            if (rotateIconWithPlayer)
                playerIcon.localRotation = Quaternion.Euler(0f, 0f, -playerTarget.eulerAngles.y);

            // Боты: из реестра (без FindObjectsOfType каждый кадр)
            if (showBots && _botIconPool.Count > 0)
            {
                var allTanks = TankRuntime.GetAllTanks();
                int used = 0;
                for (int i = 0; i < allTanks.Count; i++)
                {
                    var tc = allTanks[i];
                    if (tc == null || tc.transform == playerTarget || !tc.gameObject.activeInHierarchy)
                        continue;
                    if (used >= _botIconPool.Count) break;
                    RectTransform icon = _botIconPool[used];
                    icon.gameObject.SetActive(true);
                    icon.anchoredPosition = WorldToMinimapPosition(tc.transform.position.x, tc.transform.position.z);
                    if (rotateBotIcons)
                        icon.localRotation = Quaternion.Euler(0f, 0f, -tc.transform.eulerAngles.y);
                    used++;
                }
                _botIconsUsed = used;
                for (int i = used; i < _botIconPool.Count; i++)
                    _botIconPool[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Мировая позиция (X, Z) → anchoredPosition на мини-карте.</summary>
        private Vector2 WorldToMinimapPosition(float worldX, float worldZ)
        {
            float normX = Mathf.Clamp01((worldX - _worldMinX) / _worldSizeX);
            float normZ = Mathf.Clamp01((worldZ - _worldMinZ) / _worldSizeZ);
            if (flipVertical) normZ = 1f - normZ;
            Rect r = _mapRect.rect;
            Vector2 pivot = _mapRect.pivot;
            return new Vector2((normX - pivot.x) * r.width, (normZ - pivot.y) * r.height);
        }

        /// <summary>Задать границы игровой области вручную (X и Z min/max).</summary>
        public void SetWorldBounds(Vector2 min, Vector2 max)
        {
            worldCornerMin = null;
            worldCornerMax = null;
            worldMinFallback = min;
            worldMaxFallback = max;
            _worldMinX = min.x;
            _worldMinZ = min.y;
            _worldMaxX = max.x;
            _worldMaxZ = max.y;
            _worldSizeX = _worldMaxX - _worldMinX;
            _worldSizeZ = _worldMaxZ - _worldMinZ;
            if (_worldSizeX <= 0f) _worldSizeX = 1f;
            if (_worldSizeZ <= 0f) _worldSizeZ = 1f;
            _boundsComputed = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mapGraphic == null)
                mapGraphic = GetComponentInChildren<RawImage>() ?? (Graphic)GetComponentInChildren<Image>();
        }
#endif
    }
}

