using UnityEngine;

namespace TankGame.Tank.Animation
{
    /// <summary>
    /// Normalized runtime data for tank animation systems.
    /// </summary>
    public struct TankMotionState
    {
        public float Vertical;
        public float Horizontal;
        public bool EngineRunning;
        public bool IsAlive;
        public bool IsAiming;
        public bool IsBoosting;
        public Vector3 WorldVelocity;
    }
}
