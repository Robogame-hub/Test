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
        public int WeaponScrollDelta;
        public bool HasAimPoint;
        public Vector3 AimPoint;
        public float CameraYawDelta;

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
            bool isFiringReleased = false,
            int weaponScrollDelta = 0,
            bool hasAimPoint = false,
            Vector3 aimPoint = default,
            float cameraYawDelta = 0f)
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
            WeaponScrollDelta = weaponScrollDelta;
            HasAimPoint = hasAimPoint;
            AimPoint = aimPoint;
            CameraYawDelta = cameraYawDelta;
        }
    }
}
