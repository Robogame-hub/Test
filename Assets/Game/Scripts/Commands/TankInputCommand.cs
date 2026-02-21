using UnityEngine;

namespace TankGame.Commands
{
    /// <summary>
    /// Команда ввода для танка - содержит данные о вводе игрока
    /// Может быть сериализована и отправлена по сети
    /// </summary>
    public struct TankInputCommand
    {
        public float Timestamp;
        public float VerticalInput;
        public float HorizontalInput;
        public Vector2 MouseDelta;
        public bool IsAiming;
        public bool IsFiring;
        public bool IsFiringPressed;
        public bool IsFiringHeld;
        public bool IsFiringReleased;
        public bool IsReloadRequested;
        public bool IsBoosting;
        public int WeaponSlot;

        public TankInputCommand(
            float vertical,
            float horizontal,
            Vector2 mouseDelta,
            bool aiming,
            bool firing,
            bool reloadRequested = false,
            bool isBoosting = false,
            int weaponSlot = 0,
            bool isFiringPressed = false,
            bool isFiringHeld = false,
            bool isFiringReleased = false)
        {
            Timestamp = Time.time;
            VerticalInput = vertical;
            HorizontalInput = horizontal;
            MouseDelta = mouseDelta;
            IsAiming = aiming;
            IsFiring = firing;
            IsFiringPressed = isFiringPressed;
            IsFiringHeld = isFiringHeld;
            IsFiringReleased = isFiringReleased;
            IsReloadRequested = reloadRequested;
            IsBoosting = isBoosting;
            WeaponSlot = weaponSlot;
        }
    }
}

