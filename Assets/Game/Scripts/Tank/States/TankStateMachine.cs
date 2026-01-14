using UnityEngine;

namespace TankGame.Tank.States
{
    /// <summary>
    /// Машина состояний танка
    /// </summary>
    public class TankStateMachine : MonoBehaviour
    {
        private ITankState currentState;

        public ITankState CurrentState => currentState;

        public void ChangeState(ITankState newState)
        {
            if (currentState != null)
            {
                currentState.Exit();
            }

            currentState = newState;

            if (currentState != null)
            {
                currentState.Enter();
            }
        }

        private void Update()
        {
            currentState?.Update();
        }

        private void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }
    }
}

