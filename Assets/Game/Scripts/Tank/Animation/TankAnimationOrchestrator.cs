using UnityEngine;
using TankGame.Tank.Components;

namespace TankGame.Tank.Animation
{
    /// <summary>
    /// Single animation entry point for both player and AI motion.
    /// </summary>
    [RequireComponent(typeof(TankMovement))]
    public sealed class TankAnimationOrchestrator : MonoBehaviour, ITankAnimationSink
    {
        [Header("References")]
        [SerializeField] private TrackAnimationController trackAnimation;
        [SerializeField] private TankMovement movement;
        [SerializeField] private TankEngine engine;
        [SerializeField] private TankHealth health;
        [SerializeField] private TankTurret turret;

        private void Awake()
        {
            if (movement == null)
                movement = GetComponent<TankMovement>();
            if (engine == null)
                engine = GetComponent<TankEngine>();
            if (health == null)
                health = GetComponent<TankHealth>();
            if (turret == null)
                turret = GetComponent<TankTurret>();
            if (trackAnimation == null)
                trackAnimation = GetComponent<TrackAnimationController>();
        }

        public void ApplyInput(float vertical, float horizontal, bool isBoosting)
        {
            TankMotionState state = new TankMotionState
            {
                Vertical = vertical,
                Horizontal = horizontal,
                IsBoosting = isBoosting,
                IsAiming = turret != null && turret.IsAiming,
                IsAlive = health == null || health.IsAlive(),
                EngineRunning = engine == null || engine.IsEngineRunning,
                WorldVelocity = movement != null ? movement.CurrentVelocity : Vector3.zero
            };

            Apply(state);
        }

        public void Apply(in TankMotionState state)
        {
            if (trackAnimation == null)
                return;

            if (!state.IsAlive || !state.EngineRunning)
            {
                trackAnimation.UpdateTrackAnimation(0f, 0f);
                return;
            }

            trackAnimation.UpdateTrackAnimation(state.Vertical, state.Horizontal);
        }
    }
}
