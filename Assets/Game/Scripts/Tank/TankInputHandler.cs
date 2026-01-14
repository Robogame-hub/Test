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
        private Vector2 lastMousePosition;
        private bool isAiming;

        public TankInputCommand GetCurrentInput()
        {
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            // Обработка прицеливания
            if (Input.GetMouseButtonDown(1))
            {
                isAiming = true;
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1))
            {
                isAiming = false;
            }

            // Расчет дельты мыши
            Vector2 mouseDelta = Vector2.zero;
            if (isAiming)
            {
                mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;
            }

            bool isFiring = isAiming && Input.GetMouseButtonDown(0);

            return new TankInputCommand(vertical, horizontal, mouseDelta, isAiming, isFiring);
        }

        public bool IsAiming => isAiming;
    }
}

