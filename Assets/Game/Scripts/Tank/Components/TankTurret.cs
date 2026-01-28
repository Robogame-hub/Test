using UnityEngine;
using TankGame.Settings;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент башни танка
    /// Отвечает за логику прицеливания и визуализацию линии выстрела
    /// Поворот башни будет реализован отдельно
    /// </summary>
    public class TankTurret : MonoBehaviour
    {
        [Header("Turret Settings")]
        [Tooltip("Transform башни танка")]
        [SerializeField] private Transform turret;

        [Header("Cannon Settings")]
        [Tooltip("Transform пушки танка (используется только для поиска FirePoint, не вращается в топдаун шутере)")]
        [SerializeField] private Transform cannon;

        [Header("Aiming Settings")]
        [Tooltip("Объект прицела (UI элемент)")]
        [SerializeField] private GameObject crosshair;
        [Tooltip("Максимальная стабильность прицеливания (1.0 = полная точность)")]
        [SerializeField] private float maxAimStability = 1f;
        [Tooltip("Скорость увеличения стабильности")]
        [SerializeField] private float stabilityIncreaseRate = 0.5f;
        
        [Header("Turret Rotation Settings")]
        [Tooltip("Скорость поворота башни (градусы в секунду)")]
        [SerializeField] private float turretRotationSpeed = 90f;
        [Tooltip("Плавность поворота башни (0-1, где 1 = мгновенный поворот)")]
        [SerializeField] [Range(0f, 1f)] private float turretRotationSmoothness = 0.1f;
        
        [Header("Camera Settings")]
        [Tooltip("Камера которая следует за танком и поворачивается с башней")]
        [SerializeField] private Camera turretCamera;
        [Tooltip("Высота камеры над танком")]
        [SerializeField] private float cameraHeight = 20f;
        [Tooltip("Скорость следования камеры за танком")]
        [SerializeField] private float cameraFollowSpeed = 10f;
        [Tooltip("Скорость поворота камеры")]
        [SerializeField] private float cameraRotationSpeed = 5f;

        private float currentStability;
        private bool isAiming;

        public Transform Turret => turret;
        public Transform Cannon => cannon;
        public float CurrentStability => currentStability;
        public bool IsAiming => isAiming;
        
        private Transform tankTransform;
        private TankWeapon weapon;
        private LineRenderer fireLineRenderer;

        private void Awake()
        {
            Debug.Log($"[TankTurret] Awake STARTED on {gameObject.name}");
            
            // Получаем TankController для определения корневого объекта танка
            // Сначала пытаемся найти на том же объекте, затем в родителях
            TankController tankController = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
            
            if (tankController != null)
            {
                tankTransform = tankController.transform;
                // Weapon может быть еще не инициализирован в Awake, получим его позже
                Debug.Log($"[TankTurret] Found TankController on {tankController.gameObject.name}");
            }
            else
            {
                tankTransform = transform;
                Debug.LogWarning($"[TankTurret] TankController not found! Using own transform.");
            }
            
            InitializeTransforms();
            
            // Инициализация камеры
            if (turretCamera == null)
            {
                turretCamera = Camera.main;
            }
            
            // Отладка: проверяем результат инициализации
            Debug.Log($"[TankTurret] Awake completed - turret={(turret != null ? turret.name : "NULL")}, tankTransform={tankTransform.name}, isAiming={isAiming}, gameObject={gameObject.name}");
        }
        
        private void Start()
        {
            Debug.Log($"[TankTurret] Start called on {gameObject.name}");
            
            // Получаем weapon в Start, когда все компоненты уже инициализированы
            if (weapon == null)
            {
                TankController tankController = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
                if (tankController != null)
                {
                    weapon = tankController.Weapon;
                    Debug.Log($"[TankTurret] Got weapon from TankController: {(weapon != null ? "OK" : "NULL")}");
                }
            }
            
            if (crosshair)
                crosshair.SetActive(false);
            
            // Создаем LineRenderer для визуализации линии выстрела после инициализации weapon
            CreateFireLineRenderer();
        }
        
        /// <summary>
        /// Создает LineRenderer для визуализации линии выстрела
        /// </summary>
        private void CreateFireLineRenderer()
        {
            if (weapon == null || weapon.FirePoint == null)
                return;
            
            GameObject lineObj = new GameObject("FireLineRenderer");
            lineObj.transform.SetParent(weapon.FirePoint);
            lineObj.transform.localPosition = Vector3.zero;
            
            fireLineRenderer = lineObj.AddComponent<LineRenderer>();
            fireLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            fireLineRenderer.startWidth = 0.1f;
            fireLineRenderer.endWidth = 0.05f;
            fireLineRenderer.positionCount = 2;
            fireLineRenderer.useWorldSpace = true;
            fireLineRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            // Отладка: проверяем вызов LateUpdate раз в секунду
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TankTurret] LateUpdate called - isAiming={isAiming}, turret={(turret != null ? turret.name : "NULL")}, gameObject={gameObject.name}");
            }
            
            // Обновление камеры (всегда)
            UpdateCamera();
            
            // Обновление линии выстрела и поворот башни (только при прицеливании)
            if (isAiming)
            {
                UpdateFireLine();
                UpdateTurretRotation();
            }
        }
        
        /// <summary>
        /// Обновляет визуализацию линии выстрела
        /// Линия рисуется горизонтально (по XZ плоскости) и движется вместе с танком
        /// </summary>
        private void UpdateFireLine()
        {
            if (fireLineRenderer == null || weapon == null || weapon.FirePoint == null)
                return;
            
            if (!isAiming)
            {
                fireLineRenderer.enabled = false;
                return;
            }
            
            fireLineRenderer.enabled = true;
            
            // Получаем точку прицела на земле
            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 firePointPos = weapon.FirePoint.position;
            
            // Вычисляем направление от FirePoint к точке прицела
            Vector3 directionToAim = aimPoint - firePointPos;
            
            // ВАЖНО: Проецируем направление на горизонтальную плоскость (XZ)
            // Убираем вертикальную компоненту (Y), чтобы линия была горизонтальной
            directionToAim.y = 0f;
            
            // Нормализуем направление
            if (directionToAim.magnitude < 0.001f)
            {
                // Если направление нулевое, используем forward от FirePoint
                directionToAim = weapon.FirePoint.forward;
                directionToAim.y = 0f;
                directionToAim.Normalize();
            }
            else
            {
                directionToAim.Normalize();
            }
            
            // Вычисляем конечную точку линии (горизонтально от FirePoint)
            float lineLength = 50f;
            Vector3 endPoint = firePointPos + directionToAim * lineLength;
            
            // Устанавливаем позиции линии (в мировых координатах, так как useWorldSpace = true)
            // Линия будет двигаться вместе с танком, так как FirePoint движется с танком
            fireLineRenderer.SetPosition(0, firePointPos);
            fireLineRenderer.SetPosition(1, endPoint);
            
            // Цвет линии в зависимости от состояния
            if (!weapon.CanFire)
            {
                // Перезарядка - желтая
                fireLineRenderer.startColor = Color.yellow;
                fireLineRenderer.endColor = Color.yellow;
            }
            else
            {
                // Можно стрелять - белая
                fireLineRenderer.startColor = Color.white;
                fireLineRenderer.endColor = Color.white;
            }
        }

        private void InitializeTransforms()
        {
            if (turret == null)
            {
                // Сначала пытаемся найти по имени ZUBR_TURRET (рекурсивно), затем по имени Turret
                // Ищем в текущем объекте и во всех дочерних объектах
                turret = FindChildRecursive(transform, "ZUBR_TURRET") ?? FindChildRecursive(transform, "Turret");
                
                // Если не нашли, пытаемся найти в корневом объекте танка
                if (turret == null)
                {
                    Transform rootTransform = transform.root;
                    if (rootTransform != transform)
                    {
                        turret = FindChildRecursive(rootTransform, "ZUBR_TURRET") ?? FindChildRecursive(rootTransform, "Turret");
                    }
                }
                
                // Если все еще не нашли, пытаемся найти через GetComponentInChildren
                if (turret == null)
                {
                    Transform[] allChildren = GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in allChildren)
                    {
                        if (child.name == "ZUBR_TURRET" || child.name == "Turret")
                        {
                            turret = child;
                            break;
                        }
                    }
                }
                
                if (turret == null)
                {
                    Debug.LogError($"[TankTurret] Turret not found! Searched for 'ZUBR_TURRET' and 'Turret' in {gameObject.name} and root {transform.root.name}. Please assign turret manually in inspector.");
                }
                else
                {
                    Debug.Log($"[TankTurret] Turret found: {turret.name} (path: {GetTransformPath(turret)})");
                }
            }

            if (cannon == null && turret != null)
                cannon = turret.Find("Cannon") ?? turret;
        }
        
        /// <summary>
        /// Получает полный путь к Transform в иерархии (для отладки)
        /// </summary>
        private string GetTransformPath(Transform t)
        {
            string path = t.name;
            Transform parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
        
        /// <summary>
        /// Рекурсивно ищет дочерний объект по имени
        /// </summary>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            // Сначала проверяем прямых потомков
            Transform found = parent.Find(name);
            if (found != null)
                return found;
            
            // Затем рекурсивно проверяем всех потомков
            foreach (Transform child in parent)
            {
                found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        /// <summary>
        /// Начинает прицеливание
        /// </summary>
        public void StartAiming()
        {
            Debug.Log($"[TankTurret] StartAiming called - turret={(turret != null ? turret.name : "NULL")}");
            isAiming = true;
            if (crosshair)
                crosshair.SetActive(true);
        }

        /// <summary>
        /// Прекращает прицеливание
        /// </summary>
        public void StopAiming()
        {
            Debug.Log($"[TankTurret] StopAiming called");
            isAiming = false;
            if (crosshair)
                crosshair.SetActive(false);
            currentStability = 0f;
        }
        
        /// <summary>
        /// Получить точку прицеливания от курсора мыши
        /// В топдаун шутере проецируем курсор на плоскость земли (НЕ на камеру!)
        /// </summary>
        public Vector3 GetAimPointFromMouse()
        {
            Camera mainCamera = turretCamera != null ? turretCamera : Camera.main;
            if (mainCamera == null)
            {
                return transform.position + transform.forward * 100f;
            }
            
            // ВАЖНО: В топдаун шутере проецируем курсор мыши на плоскость ЗЕМЛИ, а не на камеру
            // Получаем высоту танка для плоскости (используем позицию танка, НЕ камеры)
            float groundHeight = 0f;
            if (tankTransform != null)
            {
                groundHeight = tankTransform.position.y;
            }
            else if (weapon != null && weapon.FirePoint != null)
            {
                // Fallback: используем высоту FirePoint
                groundHeight = weapon.FirePoint.position.y;
            }
            else
            {
                groundHeight = transform.position.y;
            }
            
            // Создаем плоскость на уровне земли (XZ плоскость)
            Plane groundPlane = new Plane(Vector3.up, groundHeight);
            
            // Raycast от камеры через курсор мыши
            Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Пересекаем луч с плоскостью земли
            if (groundPlane.Raycast(mouseRay, out float distance))
            {
                // Точка пересечения с плоскостью земли - это точка прицеливания
                Vector3 aimPoint = mouseRay.GetPoint(distance);
                return aimPoint;
            }
            else
            {
                // Если луч не пересекает плоскость (не должно происходить в топдаун камере),
                // используем проекцию на максимальную дистанцию
                float maxDistance = 500f;
                Vector3 farPoint = mouseRay.origin + mouseRay.direction * maxDistance;
                // Проецируем на плоскость земли
                farPoint.y = groundHeight;
                return farPoint;
            }
        }
        
        /// <summary>
        /// Обновляет поворот башни в сторону точки прицеливания
        /// Поворачивает башню только вокруг оси Y
        /// </summary>
        private void UpdateTurretRotation()
        {
            // Отладка: проверяем вызов метода раз в секунду
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TankTurret] UpdateTurretRotation called - turret={(turret != null ? turret.name : "NULL")}, isAiming={isAiming}");
            }
            
            if (turret == null)
            {
                // Отладка: проверяем раз в секунду
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"[TankTurret] UpdateTurretRotation: turret is NULL! Trying to reinitialize...");
                    InitializeTransforms();
                }
                return;
            }
            
            // Получаем точку прицеливания
            Vector3 aimPoint = GetAimPointFromMouse();
            
            // Вычисляем направление от башни к точке прицеливания (в мировых координатах)
            Vector3 directionToAim = aimPoint - turret.position;
            
            // Проецируем направление на горизонтальную плоскость (XZ)
            directionToAim.y = 0f;
            
            // Проверяем, что направление не нулевое
            if (directionToAim.magnitude < 0.001f)
                return;
            
            // Нормализуем направление
            directionToAim.Normalize();
            
            // Инвертируем направление для правильного поворота башни
            directionToAim = -directionToAim;
            
            // Вычисляем целевой поворот (только вокруг оси Y) в мировых координатах
            Quaternion targetWorldRotation = Quaternion.LookRotation(directionToAim);
            
            // Определяем, является ли башня дочерним объектом танка
            bool isChildOfTank = turret.parent != null && turret.parent == tankTransform;
            
            float currentY;
            float targetY;
            
            if (isChildOfTank)
            {
                // Если башня является дочерним объектом танка, работаем с локальной ротацией
                Vector3 currentLocalEuler = turret.localEulerAngles;
                currentY = currentLocalEuler.y;
                
                // Преобразуем целевой поворот в локальные координаты танка
                Quaternion targetLocalRotation = Quaternion.Inverse(tankTransform.rotation) * targetWorldRotation;
                targetY = targetLocalRotation.eulerAngles.y;
            }
            else
            {
                // Если башня не является дочерним объектом, работаем с мировой ротацией
                Vector3 currentEuler = turret.eulerAngles;
                currentY = currentEuler.y;
                targetY = targetWorldRotation.eulerAngles.y;
            }
            
            // Применяем поворот с учетом скорости и плавности
            if (turretRotationSmoothness > 0.99f)
            {
                // Мгновенный поворот (если плавность = 1)
                if (isChildOfTank)
                {
                    Vector3 currentLocalEuler = turret.localEulerAngles;
                    turret.localRotation = Quaternion.Euler(currentLocalEuler.x, targetY, currentLocalEuler.z);
                }
                else
                {
                    Vector3 currentEuler = turret.eulerAngles;
                    turret.rotation = Quaternion.Euler(currentEuler.x, targetY, currentEuler.z);
                }
            }
            else
            {
                // Плавный поворот с ограничением скорости
                float maxRotationAngle = turretRotationSpeed * Time.deltaTime;
                
                // Вычисляем новый угол с учетом плавности
                float smoothedAngle = Mathf.LerpAngle(currentY, targetY, turretRotationSmoothness);
                
                // Ограничиваем максимальный угол поворота за кадр
                float smoothedDifference = Mathf.DeltaAngle(currentY, smoothedAngle);
                if (Mathf.Abs(smoothedDifference) > maxRotationAngle)
                {
                    smoothedAngle = currentY + Mathf.Sign(smoothedDifference) * maxRotationAngle;
                }
                
                // Применяем поворот только вокруг оси Y
                if (isChildOfTank)
                {
                    Vector3 currentLocalEuler = turret.localEulerAngles;
                    turret.localRotation = Quaternion.Euler(currentLocalEuler.x, smoothedAngle, currentLocalEuler.z);
                }
                else
                {
                    Vector3 currentEuler = turret.eulerAngles;
                    turret.rotation = Quaternion.Euler(currentEuler.x, smoothedAngle, currentEuler.z);
                }
            }
            
            // Отладка: показываем информацию раз в секунду
            if (Time.frameCount % 60 == 0)
            {
                float angleDiff = Mathf.DeltaAngle(currentY, targetY);
                Debug.Log($"[TankTurret] Rotating turret: CurrentY={currentY:F1}°, TargetY={targetY:F1}°, Diff={angleDiff:F1}°, Speed={turretRotationSpeed}°/s, Smoothness={turretRotationSmoothness}, IsChild={isChildOfTank}");
            }
        }
        
        /// <summary>
        /// Обновление камеры - следует за танком
        /// </summary>
        private void UpdateCamera()
        {
            if (turretCamera == null || tankTransform == null)
                return;
            
            // Позиция камеры над танком
            Vector3 targetPosition = tankTransform.position + Vector3.up * cameraHeight;
            turretCamera.transform.position = Vector3.Lerp(
                turretCamera.transform.position,
                targetPosition,
                Time.deltaTime * cameraFollowSpeed
            );
            
            // В топдаун шутере камера смотрит сверху (90 градусов по X)
            float currentPitch = turretCamera.transform.eulerAngles.x;
            if (Mathf.Abs(currentPitch - 90f) > 1f && Mathf.Abs(currentPitch - 270f) > 1f)
            {
                currentPitch = 90f;
            }
            
            // Камера не поворачивается вместе с башней (башня будет управляться отдельно)
            Quaternion targetRotation = Quaternion.Euler(currentPitch, 0f, 0f);
            turretCamera.transform.rotation = Quaternion.Lerp(
                turretCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * cameraRotationSpeed
            );
        }

        /// <summary>
        /// Сбрасывает стабильность после выстрела
        /// </summary>
        public void ResetStability()
        {
            currentStability = 0f;
        }

    }
}

