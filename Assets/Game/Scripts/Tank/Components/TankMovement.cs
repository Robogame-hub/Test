using UnityEngine;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент движения танка с реалистичной физикой
    /// Отвечает за перемещение, выравнивание по поверхности и наклоны
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TankMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Максимальная скорость движения танка")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Скорость поворота танка (градусов в секунду)")]
        [SerializeField] private float rotationSpeed = 90f;
        [Tooltip("Скорость разгона танка")]
        [SerializeField] private float acceleration = 10f;
        [Tooltip("Скорость торможения танка")]
        [SerializeField] private float deceleration = 15f;
        [Tooltip("Множитель скорости заднего хода (меньше 1 = назад медленнее)")]
        [SerializeField] private float reverseSpeedMultiplier = 0.65f;

        [Header("Boost / Drift Settings")]
        [Tooltip("Множитель скорости на форсаже")]
        [SerializeField] private float boostSpeedMultiplier = 1.6f;
        [Tooltip("Множитель скорости поворота на форсаже")]
        [SerializeField] private float boostRotationMultiplier = 1.35f;
        [Tooltip("Расход стамины в секунду на форсаже")]
        [SerializeField] private float boostStaminaDrainPerSecond = 20f;
        [Tooltip("Максимум стамины")]
        [SerializeField] private float maxStamina = 100f;
        [Tooltip("Linear Drag на форсаже (меньше = легче снос в дрифт)")]
        [SerializeField] private float boostLinearDrag = 0.08f;
        [Tooltip("Angular Drag на форсаже (меньше = легче прокрутка в дрифте)")]
        [SerializeField] private float boostAngularDrag = 0.7f;
        
        [Header("Physics Settings (Anti-Jitter)")]
        [Tooltip("Масса танка (больше = стабильнее)")]
        [SerializeField] private float tankMass = 1500f;
        [Tooltip("Linear Drag (сопротивление движению)")]
        [SerializeField] private float linearDrag = 0.5f;
        [Tooltip("Angular Drag (сопротивление вращению)")]
        [SerializeField] private float angularDrag = 5f;
        [Tooltip("Физический материал (для устранения трения с террейном)")]
        [SerializeField] private PhysicsMaterial physicMaterial;

        [Header("Ground Alignment - Suspension Points")]
        [Tooltip("Максимальная дистанция проверки земли")]
        [SerializeField] private float groundCheckDistance = 3f;
        [Tooltip("Скорость выравнивания танка по поверхности")]
        [SerializeField] private float groundAlignSpeed = 8f;
        [Tooltip("Маска слоев для определения земли")]
        [SerializeField] private LayerMask groundMask = -1;
        [Tooltip("Частота проверки земли (меньше = лучше производительность)")]
        [SerializeField] private int groundCheckFrequency = 1; // Каждый N кадр
        
        [Header("Suspension Points (Точки опоры на гусеницах)")]
        [Tooltip("Передняя левая точка опоры")]
        [SerializeField] private Transform frontLeftPoint;
        [Tooltip("Передняя правая точка опоры")]
        [SerializeField] private Transform frontRightPoint;
        [Tooltip("Задняя левая точка опоры")]
        [SerializeField] private Transform rearLeftPoint;
        [Tooltip("Задняя правая точка опоры")]
        [SerializeField] private Transform rearRightPoint;
        
        [Header("Auto-Create Points (если не назначены)")]
        [Tooltip("Автоматически создать точки опоры если они не назначены")]
        [SerializeField] private bool autoCreatePoints = true;
        [Tooltip("Расстояние от центра танка до автоматически созданных точек")]
        [SerializeField] private float autoPointOffset = 1f;
        
        [Header("Physics Tilt Settings")]
        [Tooltip("Включить физический наклон при движении и поворотах")]
        [SerializeField] private bool enablePhysicsTilt = true;
        [Tooltip("Величина наклона назад при ускорении (градусы)")]
        [SerializeField] private float accelerationTiltAmount = 5f;
        [Tooltip("Величина наклона вбок при повороте (градусы)")]
        [SerializeField] private float turnTiltAmount = 10f;
        [Tooltip("Скорость плавности наклонов")]
        [SerializeField] private float tiltSmoothSpeed = 3f;
        
        [Header("Stability Settings (Стабилизация)")]
        [Tooltip("Максимальный угол pitch (наклон вперед-назад)")]
        [SerializeField] private float maxPitchAngle = 45f;
        [Tooltip("Максимальный угол roll (наклон влево-вправо)")]
        [SerializeField] private float maxRollAngle = 35f;
        [Tooltip("Использовать улучшенный расчет углов (точнее, но дороже)")]
        [SerializeField] private bool useAdvancedAngleCalculation = true;
        [Tooltip("Инвертировать направление roll (если танк наклоняется не в ту сторону)")]
        [SerializeField] private bool invertRoll = false;
        
        [Header("Debug Visualization")]
        [Tooltip("Показывать нормали поверхности в точках контакта")]
        [SerializeField] private bool showSurfaceNormals = true;
        [Tooltip("Показывать расстояния до земли")]
        [SerializeField] private bool showGroundDistances = true;
        [Tooltip("Показывать целевые углы наклона")]
        [SerializeField] private bool showTargetAngles = true;

        private Rigidbody rb;
        private TankEngine tankEngine;  // Для проверки работы двигателя
        private float currentYaw;
        private Vector3 currentVelocity;
        private Vector3 targetVelocity;
        private float lastVerticalInput;
        private float lastHorizontalInput;
        
        private int groundCheckCounter; // Счетчик для пропуска проверок земли
        private Quaternion cachedGroundRotation; // Кэшированное вращение
        private float currentStamina;
        private bool isBoosting;

        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public float CurrentYaw => currentYaw;
        public Vector3 CurrentVelocity => currentVelocity;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaNormalized => maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
        public bool IsBoosting => isBoosting;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ConfigureRigidbody();
            cachedGroundRotation = rb != null ? rb.rotation : transform.rotation;
        }

        private void Start()
        {
            currentYaw = transform.eulerAngles.y;
            currentStamina = maxStamina;
            
            tankEngine = GetComponent<TankEngine>();
            
            // Автоматически создаем точки если не назначены
            if (autoCreatePoints)
            {
                CreateSuspensionPointsIfNeeded();
            }
            
        }
        
        /// <summary>
        /// Создает точки подвески автоматически если они не назначены
        /// </summary>
        private void CreateSuspensionPointsIfNeeded()
        {
            if (frontLeftPoint == null)
            {
                GameObject fl = new GameObject("FrontLeft_SuspensionPoint");
                fl.transform.SetParent(transform);
                fl.transform.localPosition = new Vector3(-autoPointOffset, 0, autoPointOffset);
                frontLeftPoint = fl.transform;
            }
            
            if (frontRightPoint == null)
            {
                GameObject fr = new GameObject("FrontRight_SuspensionPoint");
                fr.transform.SetParent(transform);
                fr.transform.localPosition = new Vector3(autoPointOffset, 0, autoPointOffset);
                frontRightPoint = fr.transform;
            }
            
            if (rearLeftPoint == null)
            {
                GameObject rl = new GameObject("RearLeft_SuspensionPoint");
                rl.transform.SetParent(transform);
                rl.transform.localPosition = new Vector3(-autoPointOffset, 0, -autoPointOffset);
                rearLeftPoint = rl.transform;
            }
            
            if (rearRightPoint == null)
            {
                GameObject rr = new GameObject("RearRight_SuspensionPoint");
                rr.transform.SetParent(transform);
                rr.transform.localPosition = new Vector3(autoPointOffset, 0, -autoPointOffset);
                rearRightPoint = rr.transform;
            }
        }
        

        private void ConfigureRigidbody()
        {
            // Основные настройки
            rb.mass = tankMass;
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            rb.useGravity = true;
            
            // Фиксируем вращение (управляем вручную)
            rb.freezeRotation = true;
            
            // Интерполяция для плавности (важно для предотвращения дергания!)
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Ограничение максимальной угловой скорости для предотвращения рывков
            rb.maxAngularVelocity = 7f; // Меньше значение = более плавное вращение
            
            // Заморозка локальной оси Z (крен) для предотвращения нежелательных вращений
            // Позволяем только Pitch (X) и Yaw (Y), но не Roll (Z) если танк должен быть стабильным
            // Примечание: Если вы хотите полный Roll от физики, закомментируйте эту строку
            // rb.constraints = RigidbodyConstraints.FreezeRotationZ;
            
            // Непрерывная проверка коллизий
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Применяем физический материал ко всем коллайдерам
            ApplyPhysicMaterial();
            
            // Если материала нет - создаем автоматически
            if (physicMaterial == null)
            {
                CreateDefaultPhysicMaterial();
            }
        }
        
        /// <summary>
        /// Применяет физический материал ко всем коллайдерам танка
        /// </summary>
        private void ApplyPhysicMaterial()
        {
            if (physicMaterial == null) return;
            
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.material = physicMaterial;
            }
        }
        
        /// <summary>
        /// Создает физический материал по умолчанию (без трения)
        /// </summary>
        private void CreateDefaultPhysicMaterial()
        {
            physicMaterial = new PhysicsMaterial("TankPhysicMaterial")
            {
                dynamicFriction = 0f,      // Нет динамического трения
                staticFriction = 0f,       // Нет статического трения
                bounciness = 0f,           // Не отскакивает
                frictionCombine = PhysicsMaterialCombine.Minimum,  // Минимальное трение
                bounceCombine = PhysicsMaterialCombine.Minimum     // Минимальный отскок
            };
            
            ApplyPhysicMaterial();
        }

        /// <summary>
        /// Применяет ввод для движения танка с плавным ускорением
        /// ВАЖНО: Используется в FixedUpdate для физики
        /// </summary>
        public void ApplyMovement(float vertical, float horizontal, bool boostRequested = false)
        {
            // ПРОВЕРКА: Двигатель должен быть запущен!
            if (tankEngine != null && !tankEngine.IsEngineRunning)
            {
                isBoosting = false;
                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;

                // Двигатель выключен - не применяем ввод, но замедляемся
                targetVelocity = Vector3.zero;
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    deceleration * Time.fixedDeltaTime
                );
                rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
                return;  // НЕ двигаемся!
            }

            bool hasControlInput = Mathf.Abs(vertical) > 0.01f || Mathf.Abs(horizontal) > 0.01f;
            bool canBoost = boostRequested && hasControlInput && currentStamina > 0f;
            isBoosting = canBoost;

            if (isBoosting)
            {
                currentStamina = Mathf.Max(0f, currentStamina - boostStaminaDrainPerSecond * Time.fixedDeltaTime);
                if (currentStamina <= 0f)
                {
                    isBoosting = false;
                }
            }

            rb.linearDamping = isBoosting ? boostLinearDrag : linearDrag;
            rb.angularDamping = isBoosting ? boostAngularDrag : angularDrag;

            float effectiveMoveSpeed = isBoosting ? moveSpeed * boostSpeedMultiplier : moveSpeed;
            float effectiveRotationSpeed = isBoosting ? rotationSpeed * boostRotationMultiplier : rotationSpeed;

            // В этой конфигурации vertical > 0 означает задний ход (из-за transform.forward * -vertical).
            bool isReverseInput = vertical > 0.01f;
            if (isReverseInput)
            {
                effectiveMoveSpeed *= reverseSpeedMultiplier;
            }
            
            // Движение вперед/назад с плавным ускорением
            Vector3 moveDirection = transform.forward * -vertical;
            targetVelocity = moveDirection * effectiveMoveSpeed;
            
            // Плавное изменение скорости (acceleration/deceleration)
            float accelRate = vertical != 0 ? acceleration : deceleration;
            currentVelocity = Vector3.Lerp(
                currentVelocity, 
                targetVelocity, 
                accelRate * Time.fixedDeltaTime
            );
            
            // Применяем velocity сохраняя Y компонент (гравитация)
            Vector3 newVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
            rb.linearVelocity = newVelocity;
            
            // Вращение танка
            currentYaw += horizontal * effectiveRotationSpeed * Time.fixedDeltaTime;
            
            // Сохраняем ввод для расчета наклонов
            lastVerticalInput = vertical;
            lastHorizontalInput = horizontal;
            
        }

        /// <summary>
        /// Выравнивание танка по поверхности земли с системой подвески
        /// Оптимизировано: проверка земли не каждый кадр
        /// </summary>
        public void AlignToGround()
        {
            // Проверяем что все точки назначены
            if (frontLeftPoint == null || frontRightPoint == null || 
                rearLeftPoint == null || rearRightPoint == null)
            {
                return;
            }

            // Оптимизация: делаем raycast не каждый кадр
            groundCheckCounter++;
            bool shouldUpdateRotation = groundCheckCounter >= groundCheckFrequency;
            
            if (shouldUpdateRotation)
            {
                groundCheckCounter = 0;
                
                // Используем назначенные точки опоры на гусеницах
                Vector3 frontLeft = frontLeftPoint.position;
                Vector3 frontRight = frontRightPoint.position;
                Vector3 rearLeft = rearLeftPoint.position;
                Vector3 rearRight = rearRightPoint.position;

                // Проверяем каждую точку подвески
                bool hitFL = Physics.Raycast(frontLeft + Vector3.up, Vector3.down, out RaycastHit hitFrontLeft, groundCheckDistance, groundMask);
                bool hitFR = Physics.Raycast(frontRight + Vector3.up, Vector3.down, out RaycastHit hitFrontRight, groundCheckDistance, groundMask);
                bool hitRL = Physics.Raycast(rearLeft + Vector3.up, Vector3.down, out RaycastHit hitRearLeft, groundCheckDistance, groundMask);
                bool hitRR = Physics.Raycast(rearRight + Vector3.up, Vector3.down, out RaycastHit hitRearRight, groundCheckDistance, groundMask);

                // Если хотя бы 3 точки касаются земли, выравниваем танк
                int hitCount = (hitFL ? 1 : 0) + (hitFR ? 1 : 0) + (hitRL ? 1 : 0) + (hitRR ? 1 : 0);
                if (hitCount >= 3)
                {
                    // Выбираем метод расчета углов
                    if (useAdvancedAngleCalculation)
                    {
                        cachedGroundRotation = CalculateAdvancedGroundRotation(
                            hitFL, hitFR, hitRL, hitRR,
                            hitFrontLeft, hitFrontRight, hitRearLeft, hitRearRight
                        );
                    }
                    else
                    {
                        cachedGroundRotation = CalculateSimpleGroundRotation(
                            hitFL, hitFR, hitRL, hitRR,
                            hitFrontLeft, hitFrontRight, hitRearLeft, hitRearRight,
                            hitCount
                        );
                    }

                    // Добавляем физические наклоны (тангаж и крен)
                    if (enablePhysicsTilt)
                    {
                        cachedGroundRotation = ApplyPhysicsTilt(cachedGroundRotation);
                    }
                }
            }

            // Плавно применяем кэшированное вращение ЧЕРЕЗ ФИЗИКУ для предотвращения дергания
            if (!IsValidQuaternion(cachedGroundRotation))
                cachedGroundRotation = rb.rotation;

            Quaternion targetRotation = Quaternion.Slerp(
                rb.rotation,
                cachedGroundRotation,
                groundAlignSpeed * Time.fixedDeltaTime
            );

            if (!IsValidQuaternion(targetRotation))
            {
                targetRotation = rb.rotation;
            }
            else
            {
                targetRotation = NormalizeQuaternion(targetRotation);
            }
            
            // Используем MoveRotation вместо прямого изменения transform
            rb.MoveRotation(targetRotation);
        }

        private static Quaternion NormalizeQuaternion(Quaternion q)
        {
            float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (magnitude < 1e-6f || float.IsNaN(magnitude) || float.IsInfinity(magnitude))
                return Quaternion.identity;

            float inv = 1f / magnitude;
            return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
        }

        private static bool IsValidQuaternion(Quaternion q)
        {
            return IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
        
        /// <summary>
        /// Простой расчет вращения по средней нормали (старый метод)
        /// </summary>
        private Quaternion CalculateSimpleGroundRotation(
            bool hitFL, bool hitFR, bool hitRL, bool hitRR,
            RaycastHit hitFrontLeft, RaycastHit hitFrontRight, RaycastHit hitRearLeft, RaycastHit hitRearRight,
            int hitCount)
        {
            // Вычисляем среднюю нормаль поверхности
            Vector3 averageNormal = Vector3.zero;
            if (hitFL) averageNormal += hitFrontLeft.normal;
            if (hitFR) averageNormal += hitFrontRight.normal;
            if (hitRL) averageNormal += hitRearLeft.normal;
            if (hitRR) averageNormal += hitRearRight.normal;
            averageNormal = (averageNormal / hitCount).normalized;

            // Создаем вращение на основе yaw
            Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 forward = yawRotation * Vector3.forward;

            // Проецируем направление на плоскость земли
            Vector3 alignedForward = Vector3.ProjectOnPlane(forward, averageNormal).normalized;
            if (alignedForward.sqrMagnitude < 0.001f)
                return transform.rotation;

            return Quaternion.LookRotation(alignedForward, averageNormal);
        }
        
        /// <summary>
        /// Улучшенный расчет вращения с точными углами pitch и roll
        /// </summary>
        private Quaternion CalculateAdvancedGroundRotation(
            bool hitFL, bool hitFR, bool hitRL, bool hitRR,
            RaycastHit hitFrontLeft, RaycastHit hitFrontRight, RaycastHit hitRearLeft, RaycastHit hitRearRight)
        {
            // Вычисляем высоты точек (относительно мировых координат)
            float heightFL = hitFL ? hitFrontLeft.point.y : transform.position.y;
            float heightFR = hitFR ? hitFrontRight.point.y : transform.position.y;
            float heightRL = hitRL ? hitRearLeft.point.y : transform.position.y;
            float heightRR = hitRR ? hitRearRight.point.y : transform.position.y;
            
            // Вычисляем средние высоты для передней и задней осей (для pitch)
            float frontHeight = (heightFL + heightFR) / 2f;
            float rearHeight = (heightRL + heightRR) / 2f;
            
            // Вычисляем средние высоты для левой и правой сторон (для roll)
            float leftHeight = (heightFL + heightRL) / 2f;
            float rightHeight = (heightFR + heightRR) / 2f;
            
            // Расстояния между точками (используем мировые координаты для точности)
            float frontRearDistance = Vector3.Distance(frontLeftPoint.position, rearLeftPoint.position);
            float leftRightDistance = Vector3.Distance(frontLeftPoint.position, frontRightPoint.position);
            
            // Защита от деления на ноль
            if (frontRearDistance < 0.01f) frontRearDistance = 2f;
            if (leftRightDistance < 0.01f) leftRightDistance = 2f;
            
            // Вычисляем углы
            // Pitch: положительный = нос вверх (передняя часть выше), отрицательный = нос вниз
            float targetPitch = Mathf.Atan2(frontHeight - rearHeight, frontRearDistance) * Mathf.Rad2Deg;
            
            // Roll: вычисляем наклон по разнице высот левой и правой сторон
            // Логика: танк наклоняется В СТОРОНУ более низкой точки
            // Если левая ниже -> отрицательный roll (наклон влево)
            // Если правая ниже -> положительный roll (наклон вправо)
            float heightDifference = leftHeight - rightHeight;
            float targetRoll = -Mathf.Atan2(heightDifference, leftRightDistance) * Mathf.Rad2Deg;
            
            // Опция инвертирования (если нужно)
            if (invertRoll)
                targetRoll = -targetRoll;
            
            // Ограничиваем углы для стабильности
            targetPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);
            targetRoll = Mathf.Clamp(targetRoll, -maxRollAngle, maxRollAngle);
            
            // Создаем итоговое вращение
            return Quaternion.Euler(targetPitch, currentYaw, targetRoll);
        }

        /// <summary>
        /// Применяет физические наклоны при ускорении и поворотах
        /// </summary>
        private Quaternion ApplyPhysicsTilt(Quaternion baseRotation)
        {
            // ИСПРАВЛЕНО: Наклон назад при ускорении вперед (как у настоящего танка)
            float pitchTilt = lastVerticalInput * accelerationTiltAmount;
            
            // ИСПРАВЛЕНО: Наклон в сторону при повороте
            float rollTilt = -lastHorizontalInput * turnTiltAmount;

            // Плавно применяем наклоны
            float currentPitch = Mathf.Lerp(0f, pitchTilt, tiltSmoothSpeed * Time.deltaTime);
            float currentRoll = Mathf.Lerp(0f, rollTilt, tiltSmoothSpeed * Time.deltaTime);

            // Добавляем наклоны к базовому вращению
            Quaternion tiltRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
            return baseRotation * tiltRotation;
        }

        /// <summary>
        /// Устанавливает позицию и поворот (для сетевой синхронизации)
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            currentYaw = rotation.eulerAngles.y;
        }

        
        #region Gizmos - Визуализация точек опоры
        
        private void OnDrawGizmos()
        {
            // Базовая визуализация только если объект не выбран
            if (frontLeftPoint != null && frontRightPoint != null && rearLeftPoint != null && rearRightPoint != null)
            {
                // Соединяем точки линиями (легкая операция)
                Gizmos.color = new Color(0, 1, 1, 0.3f); // Полупрозрачный cyan
                Gizmos.DrawLine(frontLeftPoint.position, frontRightPoint.position);
                Gizmos.DrawLine(rearLeftPoint.position, rearRightPoint.position);
                Gizmos.DrawLine(frontLeftPoint.position, rearLeftPoint.position);
                Gizmos.DrawLine(frontRightPoint.position, rearRightPoint.position);
            }
        }
        
        /// <summary>
        /// Рисует одну точку подвески с лучом
        /// </summary>
        private void DrawSuspensionPoint(Transform point, Color color, string label)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(point.position, 0.15f);
            Gizmos.DrawWireSphere(point.position, 0.2f);
            
            // Луч вниз
            Vector3 rayStart = point.position + Vector3.up * 0.5f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.5f));
            
            // Проверяем попадание луча
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask))
            {
                // Точка контакта с землей
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hit.point, 0.1f);
                
                // Линия от точки подвески до земли
                Gizmos.color = Color.white;
                Gizmos.DrawLine(point.position, hit.point);
                
                // Показываем нормаль поверхности
                if (showSurfaceNormals)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawRay(hit.point, hit.normal * 0.5f);
                }
                
                #if UNITY_EDITOR
                // Показываем расстояние до земли
                if (showGroundDistances)
                {
                    float distance = Vector3.Distance(point.position, hit.point);
                    UnityEditor.Handles.Label(
                        hit.point + Vector3.up * 0.2f, 
                        $"{label}\n{distance:F2}m"
                    );
                }
                #endif
            }
        }
        
        /// <summary>
        /// Рисует продвинутую информацию о подвеске (углы, оси)
        /// </summary>
        private void DrawAdvancedSuspensionInfo()
        {
            if (!useAdvancedAngleCalculation)
                return;
                
            // Проверяем все точки
            bool hitFL = Physics.Raycast(frontLeftPoint.position + Vector3.up, Vector3.down, out RaycastHit hitFrontLeft, groundCheckDistance, groundMask);
            bool hitFR = Physics.Raycast(frontRightPoint.position + Vector3.up, Vector3.down, out RaycastHit hitFrontRight, groundCheckDistance, groundMask);
            bool hitRL = Physics.Raycast(rearLeftPoint.position + Vector3.up, Vector3.down, out RaycastHit hitRearLeft, groundCheckDistance, groundMask);
            bool hitRR = Physics.Raycast(rearRightPoint.position + Vector3.up, Vector3.down, out RaycastHit hitRearRight, groundCheckDistance, groundMask);
            
            int hitCount = (hitFL ? 1 : 0) + (hitFR ? 1 : 0) + (hitRL ? 1 : 0) + (hitRR ? 1 : 0);
            if (hitCount < 3)
                return;
            
            // Вычисляем высоты
            float heightFL = hitFL ? hitFrontLeft.point.y : transform.position.y;
            float heightFR = hitFR ? hitFrontRight.point.y : transform.position.y;
            float heightRL = hitRL ? hitRearLeft.point.y : transform.position.y;
            float heightRR = hitRR ? hitRearRight.point.y : transform.position.y;
            
            float frontHeight = (heightFL + heightFR) / 2f;
            float rearHeight = (heightRL + heightRR) / 2f;
            float leftHeight = (heightFL + heightRL) / 2f;
            float rightHeight = (heightFR + heightRR) / 2f;
            
            // Вычисляем центры осей используя мировые позиции точек для правильной визуализации
            
            // Передняя-задняя ось (pitch) - вычисляем центр между передними точками и задними
            Vector3 frontCenter = Vector3.Lerp(frontLeftPoint.position, frontRightPoint.position, 0.5f);
            frontCenter.y = frontHeight;
            Vector3 rearCenter = Vector3.Lerp(rearLeftPoint.position, rearRightPoint.position, 0.5f);
            rearCenter.y = rearHeight;
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(frontCenter, rearCenter);
            Gizmos.DrawSphere(frontCenter, 0.2f);
            Gizmos.DrawSphere(rearCenter, 0.2f);
            
            // Левая-правая ось (roll) - вычисляем центр между левыми точками и правыми
            Vector3 leftCenter = Vector3.Lerp(frontLeftPoint.position, rearLeftPoint.position, 0.5f);
            leftCenter.y = leftHeight;
            Vector3 rightCenter = Vector3.Lerp(frontRightPoint.position, rearRightPoint.position, 0.5f);
            rightCenter.y = rightHeight;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftCenter, rightCenter);
            Gizmos.DrawSphere(leftCenter, 0.2f);
            Gizmos.DrawSphere(rightCenter, 0.2f);
            
            // Линии от центров к земле для наглядности
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Полупрозрачный пурпурный
            Gizmos.DrawLine(frontCenter, frontCenter - Vector3.up * Mathf.Abs(frontCenter.y - transform.position.y));
            Gizmos.DrawLine(rearCenter, rearCenter - Vector3.up * Mathf.Abs(rearCenter.y - transform.position.y));
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Полупрозрачный желтый
            Gizmos.DrawLine(leftCenter, leftCenter - Vector3.up * Mathf.Abs(leftCenter.y - transform.position.y));
            Gizmos.DrawLine(rightCenter, rightCenter - Vector3.up * Mathf.Abs(rightCenter.y - transform.position.y));
            
            #if UNITY_EDITOR
            // Показываем текущие углы
            if (showTargetAngles)
            {
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                float pitch = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
                float roll = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
                
                // Показываем разницу высот
                float heightDiffFrontRear = frontHeight - rearHeight;
                float heightDiffLeftRight = leftHeight - rightHeight;
                
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2.5f,
                    $"Pitch: {pitch:F1}° (max: ±{maxPitchAngle}°) | ΔH F-R: {heightDiffFrontRear:F2}m\n" +
                    $"Roll: {roll:F1}° (max: ±{maxRollAngle}°) | ΔH L-R: {heightDiffLeftRight:F2}m\n" +
                    $"Heights: FL:{heightFL:F2} FR:{heightFR:F2} RL:{heightRL:F2} RR:{heightRR:F2}"
                );
            }
            #endif
        }
        
        private void OnDrawGizmosSelected()
        {
            // Детальная визуализация только когда объект выбран (оптимизация)
            
            // Проверяем и визуализируем точки опоры
            if (frontLeftPoint != null && frontRightPoint != null && rearLeftPoint != null && rearRightPoint != null)
            {
                DrawSuspensionPoint(frontLeftPoint, Color.green, "FL");
                DrawSuspensionPoint(frontRightPoint, Color.green, "FR");
                DrawSuspensionPoint(rearLeftPoint, Color.blue, "RL");
                DrawSuspensionPoint(rearRightPoint, Color.blue, "RR");
                
                // В режиме игры показываем дополнительную информацию
                if (Application.isPlaying)
                {
                    DrawAdvancedSuspensionInfo();
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получить фактор движения (0 = стоит, 1 = максимальное движение/поворот)
        /// Используется для расчета разброса оружия
        /// </summary>
        public float GetMovementFactor()
        {
            if (rb == null)
                return 0f;
            
            // Скорость движения (0-1)
            float linearFactor = rb.linearVelocity.magnitude / moveSpeed;
            linearFactor = Mathf.Clamp01(linearFactor);
            
            // Скорость поворота (0-1)
            float angularFactor = Mathf.Abs(rb.angularVelocity.y) / (rotationSpeed * Mathf.Deg2Rad);
            angularFactor = Mathf.Clamp01(angularFactor);
            
            // Берем максимум из двух факторов
            return Mathf.Max(linearFactor, angularFactor);
        }
        
        #endregion
    }
}

