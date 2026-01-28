using UnityEngine;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент башни танка
    /// Отвечает за логику прицеливания, поворот башни и визуализацию линии выстрела
    /// </summary>
    public class TankTurret : MonoBehaviour
    {
        [Header("Turret Settings")]
        [Tooltip("Transform башни танка")]
        [SerializeField] private Transform turret;

        [Header("Cannon Settings")]
        [Tooltip("Transform пушки танка (используется только для поиска FirePoint)")]
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
        [Tooltip("Камера которая следует за танком")]
        [SerializeField] private Camera turretCamera;
        [Tooltip("Высота камеры над танком")]
        [SerializeField] private float cameraHeight = 20f;
        [Tooltip("Скорость следования камеры за танком")]
        [SerializeField] private float cameraFollowSpeed = 10f;
        [Tooltip("Скорость поворота камеры")]
        [SerializeField] private float cameraRotationSpeed = 5f;
        [Tooltip("Угол наклона камеры по оси X (градусы)")]
        [SerializeField] private float cameraPitchAngle = 90f;
        [Tooltip("Скорость поворота камеры по оси Y при зажатой центральной кнопке мыши")]
        [SerializeField] private float cameraYawRotationSpeed = 50f;
        [Tooltip("Максимальное расстояние перемещения камеры когда прицел за экраном")]
        [SerializeField] private float cameraMaxOffsetDistance = 10f;
        [Tooltip("Скорость перемещения камеры к прицелу когда он за экраном")]
        [SerializeField] private float cameraOffsetSpeed = 5f;
        [Tooltip("Отступ от края экрана для определения когда прицел за экраном (в пикселях)")]
        [SerializeField] private float screenEdgeMargin = 50f;
        
        [Header("Fire Line Settings")]
        [Tooltip("Толщина линии выстрела в начале")]
        [SerializeField] private float fireLineStartWidth = 0.1f;
        [Tooltip("Толщина линии выстрела в конце")]
        [SerializeField] private float fireLineEndWidth = 0.05f;
        [Tooltip("Максимальная длина линии выстрела")]
        [SerializeField] private float fireLineMaxLength = 50f;
        [Tooltip("Угол отклонения FirePoint от направления прицела для блокировки выстрела (градусы)")]
        [SerializeField] private float firePointAlignmentThreshold = 30f;
        
        [Header("Spread Settings")]
        [Tooltip("Влияние движения танка на разброс (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float movementSpreadInfluence = 0.3f;
        [Tooltip("Влияние поворота башни на разброс (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float turretRotationSpreadInfluence = 0.2f;
        [Tooltip("Влияние расстояния до прицела на разброс (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float distanceSpreadInfluence = 0.2f;
        [Tooltip("Время восстановления стабильности после выстрела (секунды)")]
        [SerializeField] private float postFireRecoveryTime = 1f;

        private float currentStability;
        private bool isAiming;
        private float cameraYawRotation;
        private float lastFireTime;
        private float lastTurretYAngle;
        private float currentTurretRotationSpeed;
        private TankMovement tankMovement;
        private Vector3 cameraOffset;

        public Transform Turret => turret;
        public Transform Cannon => cannon;
        public float CurrentStability => currentStability;
        public bool IsAiming => isAiming;
        public bool IsFirePointAligned => isFirePointAligned;
        
        private Transform tankTransform;
        private TankWeapon weapon;
        private LineRenderer fireLineRenderer;
        private bool isFirePointAligned;

        private void Awake()
        {
            InitializeComponents();
            InitializeTransforms();
            
            if (turretCamera == null)
                turretCamera = Camera.main;
        }
        
        private void Start()
        {
            TankController tankController = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
            if (tankController != null)
            {
                if (weapon == null)
                    weapon = tankController.Weapon;
                
                if (tankMovement == null)
                    tankMovement = tankController.Movement;
            }
            
            if (crosshair)
                crosshair.SetActive(false);
            
            CreateFireLineRenderer();
            
            // Инициализируем угол башни для отслеживания поворота
            if (turret != null)
            {
                bool isChildOfTank = turret.parent != null && turret.parent == tankTransform;
                lastTurretYAngle = isChildOfTank ? turret.localEulerAngles.y : turret.eulerAngles.y;
            }
        }

        private void InitializeComponents()
        {
            TankController tankController = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
            tankTransform = tankController != null ? tankController.transform : transform;
            
            if (tankController != null)
                tankMovement = tankController.Movement;
        }

        private void InitializeTransforms()
        {
            if (turret == null)
            {
                turret = FindChildRecursive(transform, "ZUBR_TURRET") ?? FindChildRecursive(transform, "Turret");
                
                if (turret == null)
                {
                    Transform rootTransform = transform.root;
                    if (rootTransform != transform)
                        turret = FindChildRecursive(rootTransform, "ZUBR_TURRET") ?? FindChildRecursive(rootTransform, "Turret");
                }
                
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
            }

            if (cannon == null && turret != null)
                cannon = turret.Find("Cannon") ?? turret;
        }
        
        private Transform FindChildRecursive(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null)
                return found;
            
            foreach (Transform child in parent)
            {
                found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        private void CreateFireLineRenderer()
        {
            if (weapon == null || weapon.FirePoint == null)
                return;
            
            GameObject lineObj = new GameObject("FireLineRenderer");
            lineObj.transform.SetParent(weapon.FirePoint);
            lineObj.transform.localPosition = Vector3.zero;
            
            fireLineRenderer = lineObj.AddComponent<LineRenderer>();
            fireLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            fireLineRenderer.startWidth = fireLineStartWidth;
            fireLineRenderer.endWidth = fireLineEndWidth;
            fireLineRenderer.positionCount = 2;
            fireLineRenderer.useWorldSpace = true;
            fireLineRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            UpdateCamera();
            
            if (isAiming)
            {
                UpdateFireLine();
                UpdateTurretRotation();
                UpdateStability();
            }
            else
            {
                // Отключаем линию при выходе из режима прицеливания
                if (fireLineRenderer != null)
                    fireLineRenderer.enabled = false;
            }
        }
        
        /// <summary>
        /// Обновляет стабильность прицеливания
        /// </summary>
        private void UpdateStability()
        {
            currentStability = Mathf.Min(maxAimStability, currentStability + stabilityIncreaseRate * Time.deltaTime);
        }
        
        /// <summary>
        /// Обновляет визуализацию линии выстрела
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
            
            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 firePointPos = weapon.FirePoint.position;
            Vector3 firePointForward = weapon.FirePoint.forward;
            firePointForward.y = 0f;
            firePointForward.Normalize();
            
            // Вычисляем направление от FirePoint к точке прицела
            Vector3 directionToAim = aimPoint - firePointPos;
            directionToAim.y = 0f;
            
            if (directionToAim.magnitude < 0.001f)
            {
                directionToAim = firePointForward;
            }
            else
            {
                directionToAim.Normalize();
            }
            
            // Проверяем выравнивание FirePoint с направлением прицела
            float alignmentAngle = Vector3.Angle(firePointForward, directionToAim);
            isFirePointAligned = alignmentAngle <= firePointAlignmentThreshold;
            
            // Вычисляем длину линии (не больше максимальной)
            float distanceToAim = Vector3.Distance(firePointPos, aimPoint);
            float lineLength = Mathf.Min(distanceToAim, fireLineMaxLength);
            Vector3 endPoint = firePointPos + directionToAim * lineLength;
            
            fireLineRenderer.SetPosition(0, firePointPos);
            fireLineRenderer.SetPosition(1, endPoint);
            
            // Определяем цвет линии
            if (!isFirePointAligned)
            {
                // FirePoint не смотрит в сторону прицела - красная линия
                fireLineRenderer.startColor = Color.red;
                fireLineRenderer.endColor = Color.red;
            }
            else if (!weapon.CanFire)
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

        /// <summary>
        /// Начинает прицеливание
        /// </summary>
        public void StartAiming()
        {
            isAiming = true;
            if (crosshair)
                crosshair.SetActive(true);
        }

        /// <summary>
        /// Прекращает прицеливание
        /// </summary>
        public void StopAiming()
        {
            isAiming = false;
            if (crosshair)
                crosshair.SetActive(false);
            currentStability = 0f;
        }
        
        /// <summary>
        /// Получить точку прицеливания от курсора мыши
        /// </summary>
        public Vector3 GetAimPointFromMouse()
        {
            Camera mainCamera = turretCamera != null ? turretCamera : Camera.main;
            if (mainCamera == null)
                return transform.position + transform.forward * 100f;
            
            float groundHeight = tankTransform != null ? tankTransform.position.y : 
                                 (weapon != null && weapon.FirePoint != null ? weapon.FirePoint.position.y : transform.position.y);
            
            Plane groundPlane = new Plane(Vector3.up, groundHeight);
            Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (groundPlane.Raycast(mouseRay, out float distance))
                return mouseRay.GetPoint(distance);
            
            float maxDistance = 500f;
            Vector3 farPoint = mouseRay.origin + mouseRay.direction * maxDistance;
            farPoint.y = groundHeight;
            return farPoint;
        }
        
        /// <summary>
        /// Обновляет поворот башни в сторону точки прицеливания
        /// </summary>
        private void UpdateTurretRotation()
        {
            if (turret == null)
                return;
            
            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 directionToAim = aimPoint - turret.position;
            directionToAim.y = 0f;
            
            if (directionToAim.magnitude < 0.001f)
                return;
            
            directionToAim.Normalize();
            directionToAim = -directionToAim;
            
            Quaternion targetWorldRotation = Quaternion.LookRotation(directionToAim);
            bool isChildOfTank = turret.parent != null && turret.parent == tankTransform;
            
            float currentY, targetY;
            
            if (isChildOfTank)
            {
                Vector3 currentLocalEuler = turret.localEulerAngles;
                currentY = currentLocalEuler.y;
                Quaternion targetLocalRotation = Quaternion.Inverse(tankTransform.rotation) * targetWorldRotation;
                targetY = targetLocalRotation.eulerAngles.y;
            }
            else
            {
                Vector3 currentEuler = turret.eulerAngles;
                currentY = currentEuler.y;
                targetY = targetWorldRotation.eulerAngles.y;
            }
            
            // Отслеживаем скорость поворота башни для расчета разброса
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(lastTurretYAngle, currentY));
            currentTurretRotationSpeed = angleDifference / Time.deltaTime;
            lastTurretYAngle = currentY;
            
            if (turretRotationSmoothness > 0.99f)
            {
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
                float maxRotationAngle = turretRotationSpeed * Time.deltaTime;
                float smoothedAngle = Mathf.LerpAngle(currentY, targetY, turretRotationSmoothness);
                float smoothedDifference = Mathf.DeltaAngle(currentY, smoothedAngle);
                
                if (Mathf.Abs(smoothedDifference) > maxRotationAngle)
                    smoothedAngle = currentY + Mathf.Sign(smoothedDifference) * maxRotationAngle;
                
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
        }
        
        /// <summary>
        /// Обновление камеры - следует за танком и обрабатывает поворот
        /// </summary>
        private void UpdateCamera()
        {
            if (turretCamera == null || tankTransform == null)
                return;
            
            // Базовая позиция камеры над танком
            Vector3 basePosition = tankTransform.position + Vector3.up * cameraHeight;
            
            // Проверяем, находится ли прицел за пределами экрана
            UpdateCameraOffset();
            
            // Целевая позиция с учетом смещения
            Vector3 targetPosition = basePosition + cameraOffset;
            
            turretCamera.transform.position = Vector3.Lerp(
                turretCamera.transform.position,
                targetPosition,
                Time.deltaTime * cameraFollowSpeed
            );
            
            // Поворот камеры по оси Y при зажатой центральной кнопке мыши
            if (Input.GetMouseButton(2))
            {
                float mouseX = Input.GetAxis("Mouse X");
                cameraYawRotation += mouseX * cameraYawRotationSpeed * Time.deltaTime;
            }
            
            // Применяем поворот камеры
            Quaternion targetRotation = Quaternion.Euler(cameraPitchAngle, cameraYawRotation, 0f);
            turretCamera.transform.rotation = Quaternion.Lerp(
                turretCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * cameraRotationSpeed
            );
        }
        
        /// <summary>
        /// Обновляет смещение камеры когда прицел за пределами экрана
        /// </summary>
        private void UpdateCameraOffset()
        {
            if (turretCamera == null || !isAiming)
            {
                // Плавно возвращаем камеру в исходное положение
                cameraOffset = Vector3.Lerp(cameraOffset, Vector3.zero, Time.deltaTime * cameraOffsetSpeed);
                return;
            }
            
            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 screenPoint = turretCamera.WorldToScreenPoint(aimPoint);
            
            // Получаем размеры экрана
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // Проверяем, находится ли точка за пределами экрана (с учетом отступа)
            bool isOffScreen = screenPoint.x < screenEdgeMargin || 
                              screenPoint.x > screenWidth - screenEdgeMargin ||
                              screenPoint.y < screenEdgeMargin || 
                              screenPoint.y > screenHeight - screenEdgeMargin;
            
            if (isOffScreen)
            {
                // Вычисляем направление от центра экрана к точке прицела
                Vector3 screenCenter = new Vector3(screenWidth * 0.5f, screenHeight * 0.5f, screenPoint.z);
                Vector3 screenDirection = (screenPoint - screenCenter).normalized;
                
                // Инвертируем направление движения камеры
                // Если прицел справа от центра экрана, камера движется влево (инвертировано)
                // чтобы "подтянуть" прицел к центру экрана
                Vector3 worldDirection = turretCamera.transform.right * -screenDirection.x + 
                                         turretCamera.transform.up * -screenDirection.y;
                
                // Вычисляем силу смещения в зависимости от расстояния от края экрана
                float offsetStrength = CalculateOffsetStrength(screenPoint, screenWidth, screenHeight);
                
                // Нормализуем и ограничиваем максимальным расстоянием
                worldDirection = worldDirection.normalized * (cameraMaxOffsetDistance * offsetStrength);
                
                // Плавно перемещаем камеру к целевому смещению
                cameraOffset = Vector3.Lerp(
                    cameraOffset,
                    worldDirection,
                    Time.deltaTime * cameraOffsetSpeed
                );
            }
            else
            {
                // Плавно возвращаем камеру в исходное положение
                cameraOffset = Vector3.Lerp(cameraOffset, Vector3.zero, Time.deltaTime * cameraOffsetSpeed);
            }
        }
        
        /// <summary>
        /// Вычисляет силу смещения камеры в зависимости от расстояния прицела от края экрана
        /// Возвращает значение от 0 до 1
        /// </summary>
        private float CalculateOffsetStrength(Vector3 screenPoint, float screenWidth, float screenHeight)
        {
            // Вычисляем расстояние от края экрана
            float distanceFromEdgeX = Mathf.Min(
                screenPoint.x - screenEdgeMargin,
                screenWidth - screenEdgeMargin - screenPoint.x
            );
            float distanceFromEdgeY = Mathf.Min(
                screenPoint.y - screenEdgeMargin,
                screenHeight - screenEdgeMargin - screenPoint.y
            );
            
            // Используем минимальное расстояние (ближайший край)
            float minDistance = Mathf.Min(distanceFromEdgeX, distanceFromEdgeY);
            
            // Если расстояние отрицательное, значит точка за экраном
            // Чем дальше за экраном, тем больше сила смещения
            if (minDistance < 0)
            {
                // Нормализуем силу смещения (0 = на краю, 1 = максимально далеко)
                float maxDistance = Mathf.Max(screenWidth, screenHeight);
                return Mathf.Clamp01(Mathf.Abs(minDistance) / maxDistance);
            }
            
            return 0f;
        }

        /// <summary>
        /// Получить стабильность выстрела с учетом всех факторов разброса
        /// Возвращает значение от 0 до 1, где 1 = полная точность (минимальный разброс)
        /// </summary>
        public float GetFireStability()
        {
            if (weapon == null || weapon.FirePoint == null)
                return 0f;
            
            float stability = currentStability;
            
            // Фактор 1: Влияние движения танка
            if (tankMovement != null)
            {
                float movementFactor = tankMovement.GetMovementFactor();
                stability *= (1f - movementFactor * movementSpreadInfluence);
            }
            
            // Фактор 2: Влияние поворота башни
            // Нормализуем скорость поворота относительно максимальной скорости поворота башни
            float maxRotationSpeed = turretRotationSpeed; // Максимальная скорость из настроек
            float normalizedRotationSpeed = Mathf.Clamp01(currentTurretRotationSpeed / maxRotationSpeed);
            stability *= (1f - normalizedRotationSpeed * turretRotationSpreadInfluence);
            
            // Фактор 3: Влияние расстояния до прицела
            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 firePointPos = weapon.FirePoint.position;
            float distanceToAim = Vector3.Distance(firePointPos, aimPoint);
            
            if (distanceToAim > fireLineMaxLength)
            {
                // Если прицел дальше максимальной длины линии - максимальный разброс
                stability = 0f;
            }
            else
            {
                // Чем дальше прицел, тем больше разброс
                float distanceFactor = distanceToAim / fireLineMaxLength;
                stability *= (1f - distanceFactor * distanceSpreadInfluence);
            }
            
            // Фактор 4: Влияние времени после выстрела
            float timeSinceFire = Time.time - lastFireTime;
            if (timeSinceFire < postFireRecoveryTime)
            {
                float recoveryFactor = timeSinceFire / postFireRecoveryTime;
                stability *= recoveryFactor;
            }
            
            return Mathf.Clamp01(stability);
        }

        /// <summary>
        /// Сбрасывает стабильность после выстрела
        /// </summary>
        public void ResetStability()
        {
            currentStability = 0f;
            lastFireTime = Time.time;
        }
    }
}
