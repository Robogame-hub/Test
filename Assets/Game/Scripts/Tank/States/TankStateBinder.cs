using UnityEngine;
using TankGame.Tank.Components;

namespace TankGame.Tank.States
{
    /// <summary>
    /// Binds TankHealth lifecycle events to TankStateMachine transitions.
    /// </summary>
    [RequireComponent(typeof(TankController))]
    [RequireComponent(typeof(TankHealth))]
    [RequireComponent(typeof(TankStateMachine))]
    public sealed class TankStateBinder : MonoBehaviour
    {
        [SerializeField] private TankController tankController;
        [SerializeField] private TankHealth tankHealth;
        [SerializeField] private TankStateMachine stateMachine;

        private void Awake()
        {
            if (tankController == null)
                tankController = GetComponent<TankController>();
            if (tankHealth == null)
                tankHealth = GetComponent<TankHealth>();
            if (stateMachine == null)
                stateMachine = GetComponent<TankStateMachine>();
        }

        private void OnEnable()
        {
            if (tankHealth != null)
            {
                tankHealth.OnDeath.AddListener(HandleDeath);
                tankHealth.OnRespawn.AddListener(HandleRespawn);
            }
        }

        private void Start()
        {
            if (stateMachine != null && tankController != null)
                stateMachine.ChangeState(new TankAliveState(tankController));
        }

        private void OnDisable()
        {
            if (tankHealth != null)
            {
                tankHealth.OnDeath.RemoveListener(HandleDeath);
                tankHealth.OnRespawn.RemoveListener(HandleRespawn);
            }
        }

        private void HandleDeath()
        {
            if (stateMachine == null || tankController == null)
                return;

            // TankHealth already controls respawn timing via Invoke(Respawn),
            // so keep this state passive to avoid duplicate respawn triggers.
            stateMachine.ChangeState(new TankDeadState(tankController, float.MaxValue));
        }

        private void HandleRespawn()
        {
            if (stateMachine == null || tankController == null)
                return;

            stateMachine.ChangeState(new TankAliveState(tankController));
        }
    }
}
