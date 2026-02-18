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
        public bool IsReloadRequested;
        public bool IsBoosting;

        public TankInputCommand(float vertical, float horizontal, Vector2 mouseDelta, bool aiming, bool firing, bool reloadRequested = false, bool isBoosting = false)
        {
            Timestamp = Time.time;
            VerticalInput = vertical;
            HorizontalInput = horizontal;
            MouseDelta = mouseDelta;
            IsAiming = aiming;
            IsFiring = firing;
            IsReloadRequested = reloadRequested;
            IsBoosting = isBoosting;
        }
    }
}

