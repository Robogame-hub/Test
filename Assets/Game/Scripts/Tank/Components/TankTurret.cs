using UnityEngine;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Компонент башни танка.
    /// Отвечает за прицеливание, поворот башни, визуализацию линии выстрела и поведение камеры.
    /// </summary>
    public class TankTurret : MonoBehaviour
    {
        // ─── Turret ───────────────────────────────────────────────────────────
        [Header("Turret")]
        [Tooltip("Transform башни (назначь ZUBR_TURRET вручную или будет найден автоматически)")]
        [SerializeField] private Transform turret;

        [Header("Turret Rotation")]
        [Tooltip("Скорость поворота башни, градусов в секунду")]
        [SerializeField] private float turretRotationSpeed = 90f;

        [Tooltip("Плавность поворота башни: 0 = очень медленно, 1 = мгновенно")]
        [SerializeField][Range(0f, 1f)] private float turretRotationSmoothness = 0.1f;

        [Tooltip("Мёртвая зона: башня останавливается если угол до цели меньше этого значения (градусы).\n⚠ Держи значение < 1°. При большом значении башня не доворачивает до прицела!")]
        [SerializeField][Range(0f, 1f)] private float turretAimDeadZone = 0.1f;

        // ─── Aiming ───────────────────────────────────────────────────────────
        [Header("Aiming")]
        [Tooltip("UI-объект прицела")]
        [SerializeField] private GameObject crosshair;

        [Tooltip("Максимальная стабильность (1 = полная точность)")]
        [SerializeField] private float maxAimStability = 1f;

        [Tooltip("Скорость накопления стабильности")]
        [SerializeField] private float stabilityIncreaseRate = 0.5f;

        // ─── Camera ───────────────────────────────────────────────────────────
        [Header("Camera")]
        [Tooltip("Камера, следящая за танком")]
        [SerializeField] private Camera turretCamera;

        [Tooltip("Высота камеры над танком")]
        [SerializeField] private float cameraHeight = 20f;

        [Tooltip("Скорость следования камеры за танком")]
        [SerializeField] private float cameraFollowSpeed = 10f;

        [Tooltip("Скорость интерполяции поворота камеры")]
        [SerializeField] private float cameraRotationSpeed = 5f;

        [Tooltip("Угол наклона камеры по X (90 = вид сверху)")]
        [SerializeField] private float cameraPitchAngle = 90f;

        [Tooltip("Скорость поворота камеры по Y (средняя кнопка мыши)")]
        [SerializeField] private float cameraYawRotationSpeed = 50f;

        [Tooltip("Начальный сдвиг угла камеры относительно танка (180 = вид сзади)")]
        [SerializeField] private float cameraStartYawOffset = 180f;

        [Tooltip("Максимальное смещение камеры когда прицел уходит за экран")]
        [SerializeField] private float cameraMaxOffsetDistance = 10f;

        [Tooltip("Скорость плавного сдвига камеры")]
        [SerializeField] private float cameraOffsetSpeed = 5f;

        [Tooltip("Отступ от края экрана, при котором считается что прицел вышел за экран (пиксели)")]
        [SerializeField] private float screenEdgeMargin = 50f;

        // ─── Fire Line ────────────────────────────────────────────────────────
        [Header("Fire Line")]
        [Tooltip("Ширина линии у основания")]
        [SerializeField] private float fireLineStartWidth = 0.1f;

        [Tooltip("Ширина линии у конца")]
        [SerializeField] private float fireLineEndWidth = 0.05f;

        [Tooltip("Максимальная длина линии выстрела")]
        [SerializeField] private float fireLineMaxLength = 50f;

        [Tooltip("Допустимое отклонение FirePoint от прицела для разрешения выстрела (градусы)")]
        [SerializeField] private float firePointAlignmentThreshold = 30f;

        // ─── Spread ───────────────────────────────────────────────────────────
        [Header("Spread")]
        [Tooltip("Влияние движения на разброс (0–1)")]
        [SerializeField][Range(0f, 1f)] private float movementSpreadInfluence = 0.3f;

        [Tooltip("Влияние поворота башни на разброс (0–1)")]
        [SerializeField][Range(0f, 1f)] private float turretRotationSpreadInfluence = 0.2f;

        [Tooltip("Влияние дистанции до цели на разброс (0–1)")]
        [SerializeField][Range(0f, 1f)] private float distanceSpreadInfluence = 0.2f;

        [Tooltip("Время восстановления после выстрела (секунды)")]
        [SerializeField] private float postFireRecoveryTime = 1f;

        // ─── Private state ────────────────────────────────────────────────────
        private float          currentStability;
        private bool           isAiming;
        private float          cameraYawRotation;
        private float          lastFireTime;
        private float          currentTurretRotationSpeed;
        private Vector3        cameraOffset;
        private bool           isFirePointAligned;

        private Transform      tankTransform;
        private TankWeapon     weapon;
        private TankMovement   tankMovement;
        private LineRenderer   fireLineRenderer;

        // ─── Public API ───────────────────────────────────────────────────────
        public Transform Turret          => turret;
        public float     CurrentStability => currentStability;
        public bool      IsAiming         => isAiming;
        public bool      IsFirePointAligned => isFirePointAligned;

        // ─── Reset (сбрасывает инспектор к правильным дефолтам) ──────────────
        private void Reset()
        {
            turretRotationSpeed      = 90f;
            turretRotationSmoothness = 0.5f;
            turretAimDeadZone        = 0.1f;
            cameraHeight             = 20f;
            cameraFollowSpeed        = 10f;
            cameraRotationSpeed      = 5f;
            cameraPitchAngle         = 90f;
            cameraYawRotationSpeed   = 50f;
            cameraStartYawOffset     = 180f;
            cameraMaxOffsetDistance  = 10f;
            cameraOffsetSpeed        = 5f;
            screenEdgeMargin         = 50f;
            fireLineStartWidth       = 0.1f;
            fireLineEndWidth         = 0.05f;
            fireLineMaxLength        = 50f;
            firePointAlignmentThreshold = 30f;
            maxAimStability          = 1f;
            stabilityIncreaseRate    = 0.5f;
            movementSpreadInfluence  = 0.3f;
            turretRotationSpreadInfluence = 0.2f;
            distanceSpreadInfluence  = 0.2f;
            postFireRecoveryTime     = 1f;
        }

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            TankController tc = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
            tankTransform = tc != null ? tc.transform : transform;

            if (tc != null)
                tankMovement = tc.Movement;

            if (turretCamera == null)
                turretCamera = Camera.main;

            InitializeTurretTransform();
        }

        private void Start()
        {
            TankController tc = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
            if (tc != null)
            {
                if (weapon      == null) weapon      = tc.Weapon;
                if (tankMovement == null) tankMovement = tc.Movement;
            }

            if (crosshair != null)
                crosshair.SetActive(false);

            cameraYawRotation = (tankTransform != null ? tankTransform.eulerAngles.y : 0f)
                                + cameraStartYawOffset;

            CreateFireLineRenderer();
        }

        // ─── Initialization ───────────────────────────────────────────────────
        private void InitializeTurretTransform()
        {
            if (turret != null) return;

            // Поиск по имени в иерархии
            turret = FindChildRecursive(transform.root, "ZUBR_TURRET")
                  ?? FindChildRecursive(transform.root, "Turret");

            if (turret == null)
                Debug.LogWarning("[TankTurret] Не найден объект башни. Назначь Transform в инспекторе.");
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent.name == childName) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, childName);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateFireLineRenderer()
        {
            if (weapon == null || weapon.FirePoint == null) return;

            GameObject lineObj = new GameObject("FireLineRenderer");
            lineObj.transform.SetParent(weapon.FirePoint);
            lineObj.transform.localPosition = Vector3.zero;

            fireLineRenderer = lineObj.AddComponent<LineRenderer>();
            fireLineRenderer.material      = new Material(Shader.Find("Sprites/Default"));
            fireLineRenderer.startWidth    = fireLineStartWidth;
            fireLineRenderer.endWidth      = fireLineEndWidth;
            fireLineRenderer.positionCount = 2;
            fireLineRenderer.useWorldSpace = true;
            fireLineRenderer.enabled       = false;
        }

        // ─── Update ───────────────────────────────────────────────────────────
        private void LateUpdate()
        {
            UpdateCamera();

            if (isAiming)
            {
                UpdateTurretRotation();
                UpdateFireLine();
                UpdateStability();
            }
            else
            {
                if (fireLineRenderer != null)
                    fireLineRenderer.enabled = false;
            }
        }

        // ─── Turret rotation ──────────────────────────────────────────────────
        /// <summary>
        /// Foxhole-style поворот: башня вращается по мировой Y, ориентируясь на реальный forward ствола (FirePoint).
        /// Это устраняет проблемы с импортированными осями меша и инверсиями.
        /// </summary>
        private void UpdateTurretRotation()
        {
            if (turret == null) return;

            Transform aimForwardTransform = (weapon != null && weapon.FirePoint != null) ? weapon.FirePoint : turret;
            Vector3 aimPivot = aimForwardTransform.position;
            Vector3 rotationAxis = turret.parent != null ? turret.parent.up : Vector3.up;

            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 dirToAim = Vector3.ProjectOnPlane(aimPoint - aimPivot, rotationAxis);
            if (dirToAim.sqrMagnitude < 0.001f) return;
            dirToAim.Normalize();

            Vector3 currentForward = Vector3.ProjectOnPlane(aimForwardTransform.forward, rotationAxis);
            if (currentForward.sqrMagnitude < 0.001f) return;
            currentForward.Normalize();

            float angleDiff = Vector3.SignedAngle(currentForward, dirToAim, rotationAxis);

            if (Mathf.Abs(angleDiff) <= turretAimDeadZone)
            {
                currentTurretRotationSpeed = 0f;
                return;
            }

            float smoothMul = Mathf.Lerp(0.1f, 1f, turretRotationSmoothness);
            float maxStep   = turretRotationSpeed * smoothMul * Time.deltaTime;
            float step      = Mathf.Sign(angleDiff) * Mathf.Min(Mathf.Abs(angleDiff), maxStep);

            currentTurretRotationSpeed = Mathf.Abs(step) / Mathf.Max(Time.deltaTime, 0.0001f);

            // Крутим вокруг локальной оси "вверх" корпуса/родителя башни.
            // Это убирает перекосы на склонах: башня вращается только влево/вправо.
            turret.rotation = Quaternion.AngleAxis(step, rotationAxis) * turret.rotation;
        }

        // ─── Camera ───────────────────────────────────────────────────────────
        private void UpdateCamera()
        {
            if (turretCamera == null || tankTransform == null) return;

            // Поворот по Y при зажатой средней кнопке мыши
            if (Input.GetMouseButton(2))
                cameraYawRotation += Input.GetAxis("Mouse X") * cameraYawRotationSpeed * Time.deltaTime;

            // Целевая позиция = над танком + смещение к прицелу
            UpdateCameraOffset();
            Vector3 targetPos = tankTransform.position + Vector3.up * cameraHeight + cameraOffset;

            turretCamera.transform.position = Vector3.Lerp(
                turretCamera.transform.position, targetPos, cameraFollowSpeed * Time.deltaTime);

            Quaternion targetRot = Quaternion.Euler(cameraPitchAngle, cameraYawRotation, 0f);
            turretCamera.transform.rotation = Quaternion.Lerp(
                turretCamera.transform.rotation, targetRot, cameraRotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Когда прицел уходит за пределы экрана — плавно сдвигаем камеру в ту же сторону,
        /// чтобы прицел вернулся в зону видимости.
        /// Направление вычисляется в XZ-плоскости через cameraYawRotation —
        /// корректно при любом cameraPitchAngle.
        /// </summary>
        private void UpdateCameraOffset()
        {
            if (turretCamera == null || !isAiming)
            {
                cameraOffset = Vector3.Lerp(cameraOffset, Vector3.zero, cameraOffsetSpeed * Time.deltaTime);
                return;
            }

            Vector3 aimPoint    = GetAimPointFromMouse();
            Vector3 screenPoint = turretCamera.WorldToScreenPoint(aimPoint);

            float sw = Screen.width;
            float sh = Screen.height;

            bool offScreen = screenPoint.x < screenEdgeMargin
                          || screenPoint.x > sw - screenEdgeMargin
                          || screenPoint.y < screenEdgeMargin
                          || screenPoint.y > sh - screenEdgeMargin;

            if (offScreen)
            {
                // Нормализованное направление от центра экрана к прицелу
                Vector2 screenDir = new Vector2(
                    screenPoint.x - sw * 0.5f,
                    screenPoint.y - sh * 0.5f).normalized;

                // Горизонтальные оси камеры в XZ-плоскости (независимо от pitch)
                Vector3 camRight   = Quaternion.Euler(0f, cameraYawRotation, 0f) * Vector3.right;
                Vector3 camForward = Quaternion.Euler(0f, cameraYawRotation, 0f) * Vector3.forward;

                // Камера сдвигается В СТОРОНУ прицела — прицел возвращается к центру
                float strength     = CalculateOffsetStrength(screenPoint, sw, sh);
                Vector3 targetOffset = (camRight * screenDir.x + camForward * screenDir.y).normalized
                                       * (cameraMaxOffsetDistance * strength);

                cameraOffset = Vector3.Lerp(cameraOffset, targetOffset, cameraOffsetSpeed * Time.deltaTime);
            }
            else
            {
                cameraOffset = Vector3.Lerp(cameraOffset, Vector3.zero, cameraOffsetSpeed * Time.deltaTime);
            }
        }

        private float CalculateOffsetStrength(Vector3 screenPoint, float sw, float sh)
        {
            float dx = Mathf.Min(screenPoint.x - screenEdgeMargin, sw - screenEdgeMargin - screenPoint.x);
            float dy = Mathf.Min(screenPoint.y - screenEdgeMargin, sh - screenEdgeMargin - screenPoint.y);
            float minDist = Mathf.Min(dx, dy);

            if (minDist >= 0f) return 0f;

            return Mathf.Clamp01(Mathf.Abs(minDist) / Mathf.Max(sw, sh));
        }

        // ─── Fire line ────────────────────────────────────────────────────────
        private void UpdateFireLine()
        {
            if (fireLineRenderer == null || weapon == null || weapon.FirePoint == null) return;

            fireLineRenderer.enabled = true;

            Vector3 aimPoint      = GetAimPointFromMouse();
            Vector3 fpPos         = weapon.FirePoint.position;
            Vector3 fpFwdH        = weapon.FirePoint.forward;
            fpFwdH.y = 0f;
            if (fpFwdH.sqrMagnitude > 0.001f) fpFwdH.Normalize();
            else fpFwdH = Vector3.forward;

            Vector3 dirToAim = aimPoint - fpPos;
            dirToAim.y = 0f;
            Vector3 drawDir = dirToAim.sqrMagnitude > 0.001f ? dirToAim.normalized : fpFwdH;

            float alignAngle = Vector3.Angle(fpFwdH, drawDir);
            isFirePointAligned = alignAngle <= firePointAlignmentThreshold;

            float lineLen  = Mathf.Min(Vector3.Distance(fpPos, aimPoint), fireLineMaxLength);
            Vector3 endPt  = fpPos + drawDir * lineLen;

            fireLineRenderer.SetPosition(0, fpPos);
            fireLineRenderer.SetPosition(1, endPt);

            Color lineColor = !isFirePointAligned ? Color.red
                            : !weapon.CanFire      ? Color.yellow
                            :                        Color.white;
            fireLineRenderer.startColor = lineColor;
            fireLineRenderer.endColor   = lineColor;
        }

        // ─── Stability ────────────────────────────────────────────────────────
        private void UpdateStability()
        {
            currentStability = Mathf.Min(maxAimStability,
                currentStability + stabilityIncreaseRate * Time.deltaTime);
        }

        // ─── Public ───────────────────────────────────────────────────────────
        public void StartAiming()
        {
            isAiming = true;
            if (crosshair != null) crosshair.SetActive(true);
        }

        public void StopAiming()
        {
            isAiming         = false;
            currentStability = 0f;
            if (crosshair != null) crosshair.SetActive(false);
        }

        public Vector3 GetAimPointFromMouse()
        {
            Camera cam = turretCamera != null ? turretCamera : Camera.main;
            if (cam == null) return transform.position + transform.forward * 100f;

            float groundY = tankTransform != null        ? tankTransform.position.y
                          : weapon?.FirePoint != null    ? weapon.FirePoint.position.y
                          :                               transform.position.y;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, groundY);

            if (ground.Raycast(ray, out float dist))
                return ray.GetPoint(dist);

            Vector3 far = ray.origin + ray.direction * 500f;
            far.y = groundY;
            return far;
        }

        public float GetFireStability()
        {
            if (weapon == null || weapon.FirePoint == null) return 0f;

            float s = currentStability;

            // Движение
            if (tankMovement != null)
                s *= 1f - tankMovement.GetMovementFactor() * movementSpreadInfluence;

            // Поворот башни
            float normRotSpeed = Mathf.Clamp01(currentTurretRotationSpeed / Mathf.Max(turretRotationSpeed, 0.001f));
            s *= 1f - normRotSpeed * turretRotationSpreadInfluence;

            // Расстояние
            float dist = Vector3.Distance(weapon.FirePoint.position, GetAimPointFromMouse());
            if (dist > fireLineMaxLength)
                s = 0f;
            else
                s *= 1f - (dist / fireLineMaxLength) * distanceSpreadInfluence;

            // Время после выстрела
            float timeSinceFire = Time.time - lastFireTime;
            if (timeSinceFire < postFireRecoveryTime)
                s *= timeSinceFire / postFireRecoveryTime;

            return Mathf.Clamp01(s);
        }

        public void ResetStability()
        {
            currentStability = 0f;
            lastFireTime     = Time.time;
        }

        // ─── Editor gizmos ────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (turret == null) return;

            Vector3 origin = turret.position + Vector3.up * 0.3f;

            // GREEN — реальный forward меша (XZ-проекция)
            Vector3 fwdH = turret.forward;
            fwdH.y = 0f;
            if (fwdH.sqrMagnitude < 0.001f) fwdH = Vector3.forward;
            fwdH.Normalize();

            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + fwdH * 4f);
            Gizmos.DrawWireSphere(origin + fwdH * 4f, 0.15f);

            if (!Application.isPlaying) return;

            // CYAN — raw направление к прицелу
            Vector3 aimPoint = GetAimPointFromMouse();
            Vector3 toAimH   = aimPoint - turret.position;
            toAimH.y = 0f;
            if (toAimH.sqrMagnitude < 0.001f) return;
            toAimH.Normalize();

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + toAimH * 4f);
            Gizmos.DrawWireSphere(origin + toAimH * 4f, 0.1f);

            // YELLOW — целевое направление (куда должен смотреть FirePoint/ствол)
            Vector3 targetDir = toAimH;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + targetDir * 5f);
            Gizmos.DrawWireSphere(origin + targetDir * 5f, 0.18f);

            float diff = Vector3.Angle(fwdH, targetDir);
            Gizmos.color = diff <= turretAimDeadZone ? Color.green : Color.red;
            Gizmos.DrawLine(origin + fwdH * 2f, origin + targetDir * 2f);

            UnityEditor.Handles.Label(origin + Vector3.up * 0.6f,
                $"GREEN=forward  CYAN=aim  YELLOW=target\n" +
                $"Diff: {diff:F1}°");
        }
#endif
    }
}
