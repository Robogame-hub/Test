using UnityEngine;
using TankGame.Commands;

namespace TankGame.Tank
{
    /// <summary>
    /// Обработчик ввода для танка
    /// Собирает ввод и преобразует в команды
    /// </summary>
    public class TankInputHandler : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [Tooltip("Скрывать курсор мыши в режиме игры")]
        [SerializeField] private bool hideCursorInGame = true;
        [Tooltip("Клавиша для показа/скрытия курсора (обычно Escape)")]
        [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;
        
        private bool isAiming;
        private TankInputCommand lastCommand;
        private bool isCursorLocked = true;

        private void Start()
        {
            // В топдаун шутере курсор видимый (для прицела)
            // Не скрываем курсор, так как прицел следует за ним
            UnlockCursor();
        }

        private void Update()
        {
            // Проверяем нажатие клавиши переключения курсора
            if (Input.GetKeyDown(toggleCursorKey))
            {
                ToggleCursor();
            }
        }

        public TankInputCommand GetCurrentInput()
        {
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            // Обработка прицеливания
            if (Input.GetMouseButtonDown(1))
            {
                isAiming = true;
            }

            if (Input.GetMouseButtonUp(1))
            {
                isAiming = false;
            }

            // Расчет дельты мыши
            // Используем Input.GetAxis для работы с заблокированным курсором
            Vector2 mouseDelta = Vector2.zero;
            if (isAiming)
            {
                // GetAxis("Mouse X/Y") работает независимо от состояния курсора
                mouseDelta = new Vector2(
                    Input.GetAxis("Mouse X"),
                    Input.GetAxis("Mouse Y")
                );
            }

            bool isFiring = isAiming && Input.GetMouseButtonDown(0);
            
            // Debug для отслеживания ввода
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"[TankInputHandler] LMB pressed! isAiming={isAiming}, isFiring={isFiring}, Frame={Time.frameCount}");
            }

            lastCommand = new TankInputCommand(vertical, horizontal, mouseDelta, isAiming, isFiring);
            return lastCommand;
        }

        public bool IsAiming => isAiming;
        public TankInputCommand LastCommand => lastCommand;
        
        /// <summary>
        /// Блокирует и скрывает курсор
        /// </summary>
        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isCursorLocked = true;
        }
        
        /// <summary>
        /// Разблокирует и показывает курсор
        /// </summary>
        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isCursorLocked = false;
        }
        
        /// <summary>
        /// Переключает состояние курсора
        /// </summary>
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
        
        /// <summary>
        /// Принудительно скрывает курсор (для вызова из других скриптов)
        /// </summary>
        public void ForceLockCursor()
        {
            if (hideCursorInGame)
            {
                LockCursor();
            }
        }
        
        /// <summary>
        /// Принудительно показывает курсор (для вызова из других скриптов)
        /// </summary>
        public void ForceUnlockCursor()
        {
            UnlockCursor();
        }
    }
}

