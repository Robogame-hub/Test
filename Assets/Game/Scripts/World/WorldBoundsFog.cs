using UnityEngine;

namespace TankGame.World
{
    /// <summary>
    /// Создаёт «туман войны» (тёмные поля) вокруг прямоугольной игровой области.
    /// Внутри прямоугольника всё видно, снаружи — затемнено.
    ///
    /// Использование:
    /// 1. Поставьте два маркера-объекта по углам игровой зоны (минимальный и максимальный X/Z).
    /// 2. Создайте пустой объект "WorldBoundsFog" и повесьте на него этот скрипт.
    /// 3. Заполните поля World Corner Min / Max и материал тумана (полупрозрачная чёрная текстура/цвет).
    /// </summary>
    public class WorldBoundsFog : MonoBehaviour
    {
        [Header("World Bounds (XZ)")]
        [Tooltip("Левый нижний угол игровой зоны (минимальные X и Z)")]
        [SerializeField] private Transform worldCornerMin;
        [Tooltip("Правый верхний угол игровой зоны (максимальные X и Z)")]
        [SerializeField] private Transform worldCornerMax;

        [Header("Fog Settings")]
        [Tooltip("Материал тумана (полупрозрачный)")]
        [SerializeField] private Material fogMaterial;
        [Tooltip("Высота тумана над землёй")]
        [SerializeField] private float fogHeight = 5f;
        [Tooltip("Толщина полос тумана вокруг карты (внешний отступ)")]
        [SerializeField] private float fogThickness = 50f;

        [Header("Debug")]
        [SerializeField] private bool rebuildOnStart = true;

        private const string LeftName = "Fog_Left";
        private const string RightName = "Fog_Right";
        private const string TopName = "Fog_Top";
        private const string BottomName = "Fog_Bottom";

        private void Start()
        {
            if (rebuildOnStart)
                RebuildFog();
        }

        /// <summary>
        /// Пересоздаёт 4 полосы тумана вокруг заданного прямоугольника.
        /// </summary>
        public void RebuildFog()
        {
            if (worldCornerMin == null || worldCornerMax == null || fogMaterial == null)
                return;

            float minX = Mathf.Min(worldCornerMin.position.x, worldCornerMax.position.x);
            float maxX = Mathf.Max(worldCornerMin.position.x, worldCornerMax.position.x);
            float minZ = Mathf.Min(worldCornerMin.position.z, worldCornerMax.position.z);
            float maxZ = Mathf.Max(worldCornerMin.position.z, worldCornerMax.position.z);

            float sizeX = maxX - minX;
            float sizeZ = maxZ - minZ;
            if (sizeX <= 0.1f || sizeZ <= 0.1f)
                return;

            // Центр и базовая высота.
            float centerY = (worldCornerMin.position.y + worldCornerMax.position.y) * 0.5f + 0.1f;

            CreateOrUpdateStrip(
                LeftName,
                // Центр слева от карты
                new Vector3(minX - fogThickness * 0.5f, centerY + fogHeight, (minZ + maxZ) * 0.5f),
                // Ширина по X, длина по Z
                new Vector2(fogThickness, sizeZ + fogThickness * 2f)
            );

            CreateOrUpdateStrip(
                RightName,
                // Центр справа от карты
                new Vector3(maxX + fogThickness * 0.5f, centerY + fogHeight, (minZ + maxZ) * 0.5f),
                new Vector2(fogThickness, sizeZ + fogThickness * 2f)
            );

            CreateOrUpdateStrip(
                TopName,
                // Центр впереди карты (по +Z)
                new Vector3((minX + maxX) * 0.5f, centerY + fogHeight, maxZ + fogThickness * 0.5f),
                new Vector2(sizeX, fogThickness)
            );

            CreateOrUpdateStrip(
                BottomName,
                // Центр позади карты (по -Z)
                new Vector3((minX + maxX) * 0.5f, centerY + fogHeight, minZ - fogThickness * 0.5f),
                new Vector2(sizeX, fogThickness)
            );
        }

        private void CreateOrUpdateStrip(string childName, Vector3 worldCenter, Vector2 size)
        {
            Transform child = transform.Find(childName);
            GameObject go;
            if (child == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = childName;
                go.transform.SetParent(transform, false);

                // Удаляем коллайдер, он не нужен.
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
            }
            else
            {
                go = child.gameObject;
            }

            var t = go.transform;
            t.position = worldCenter;
            // Поворачиваем quad, чтобы он лежал в XZ-плоскости и смотрел вверх.
            t.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Quad имеет размер 1x1 → масштабируем по ширине/длине.
            t.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = fogMaterial;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            // Для удобства — обновляем расположение в редакторе при смене параметров.
            if (worldCornerMin != null && worldCornerMax != null && fogMaterial != null)
            {
                RebuildFog();
            }
        }
#endif
    }
}

