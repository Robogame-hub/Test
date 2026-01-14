using UnityEngine;

namespace TankGame.Tank.States
{
    /// <summary>
    /// Состояние "Оглушен" - танк временно не может двигаться
    /// </summary>
    public class TankStunnedState : ITankState
    {
        private readonly TankController tank;
        private readonly float stunDuration;
        private float stunStartTime;

        public TankStunnedState(TankController tank, float duration = 2f)
        {
            this.tank = tank;
            this.stunDuration = duration;
        }

        public void Enter()
        {
            stunStartTime = Time.time;
            
            // Останавливаем танк
            Rigidbody rb = tank.Movement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            Debug.Log("Tank stunned!");
        }

        public void Update()
        {
            // Проверяем завершение оглушения
            if (Time.time - stunStartTime >= stunDuration)
            {
                // Возвращаемся в живое состояние
                // (это должно управляться извне через TankStateMachine)
            }
        }

        public void FixedUpdate()
        {
            // Танк не может двигаться
        }

        public void Exit()
        {
            Debug.Log("Tank recovered from stun!");
        }
    }
}

