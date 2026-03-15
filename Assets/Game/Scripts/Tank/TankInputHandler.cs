using UnityEngine;
using TankGame.Commands;
using TankGame.Settings;
using TankGame.Menu;

namespace TankGame.Tank
{
    /// <summary>
    /// Обработчик ввода для танка.
    /// </summary>
    public class TankInputHandler : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [SerializeField] private bool hideCursorInGame = true;
        [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;

        [Header("Aim Settings")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private Transform aimPlaneReference;

        private bool isAiming;
        private TankInputCommand lastCommand;
        private bool isCursorLocked = true;
        private InputSettings inputSettings;

        private void Start()
        {
            inputSettings = InputSettings.Instance;
            if (hideCursorInGame)
                LockCursor();
            else
                UnlockCursor();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleCursorKey))
            {
                if (BattlePauseMenuController.TryHandleEscapePressed())
                    return;

                ToggleCursor();
            }
        }

        public TankInputCommand GetCurrentInput()
        {
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            bool rightMouseHeld = Input.GetMouseButton(1);
            bool leftMouseHeld = Input.GetMouseButton(0);
            isAiming = rightMouseHeld;

            Vector2 mouseDelta = Vector2.zero;
            if (isAiming)
            {
                Vector2 rawDelta = new Vector2(
                    Input.GetAxis("Mouse X"),
                    Input.GetAxis("Mouse Y")
                );

                mouseDelta = inputSettings != null
                    ? inputSettings.ApplySensitivity(rawDelta)
                    : rawDelta;
            }

            bool isFiringPressed = isAiming && Input.GetMouseButtonDown(0);
            bool isFiringHeld = isAiming && leftMouseHeld;
            bool isFiringReleased = isAiming && Input.GetMouseButtonUp(0);
            bool isFiring = isFiringPressed || isFiringHeld;
            bool isReloadRequested = Input.GetKeyDown(KeyCode.R);
            bool isBoosting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            int weaponSlot = 0;
            if (Input.GetKeyDown(KeyCode.Alpha1))
                weaponSlot = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                weaponSlot = 2;
            else if (Input.GetKeyDown(KeyCode.Keypad1))
                weaponSlot = 1;
            else if (Input.GetKeyDown(KeyCode.Keypad2))
                weaponSlot = 2;

            int weaponScrollDelta = 0;
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.01f)
                weaponScrollDelta = 1;
            else if (scroll < -0.01f)
                weaponScrollDelta = -1;

            float rawYawInput = Input.GetMouseButton(2) ? Input.GetAxis("Mouse X") : 0f;
            float yawSensitivity = inputSettings != null ? inputSettings.GetEffectiveHorizontalSensitivity() : 1f;
            float cameraYawDelta = rawYawInput * yawSensitivity * Time.deltaTime;

            bool hasAimPoint = false;
            Vector3 aimPoint = Vector3.zero;
            if (isAiming)
                hasAimPoint = TryGetAimPoint(out aimPoint);

            lastCommand = new TankInputCommand(
                vertical,
                horizontal,
                mouseDelta,
                isAiming,
                isFiring,
                isReloadRequested,
                isBoosting,
                weaponSlot,
                isFiringPressed,
                isFiringHeld,
                isFiringReleased,
                weaponScrollDelta,
                hasAimPoint,
                aimPoint,
                cameraYawDelta
            );

            return lastCommand;
        }

        private bool TryGetAimPoint(out Vector3 worldPoint)
        {
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
            {
                worldPoint = transform.position + transform.forward * 100f;
                return false;
            }

            float planeY = aimPlaneReference != null
                ? aimPlaneReference.position.y
                : transform.root.position.y;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

            if (plane.Raycast(ray, out float distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }

            worldPoint = ray.origin + ray.direction * 500f;
            worldPoint.y = planeY;
            return true;
        }

        public bool IsAiming => isAiming;
        public TankInputCommand LastCommand => lastCommand;

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            isCursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isCursorLocked = false;
        }

        private void ToggleCursor()
        {
            if (isCursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        public void ForceLockCursor()
        {
            if (hideCursorInGame)
            {
                LockCursor();
            }
        }

        public void ForceUnlockCursor()
        {
            UnlockCursor();
        }
    }
}

