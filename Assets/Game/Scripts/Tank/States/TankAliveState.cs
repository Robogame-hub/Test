using UnityEngine;

namespace TankGame.Tank.States
{
    /// <summary>
    /// Состояние "Живой" - танк может двигаться и стрелять
    /// </summary>
    public class TankAliveState : ITankState
    {
        private readonly TankController tank;

        public TankAliveState(TankController tank)
        {
            this.tank = tank;
        }

        public void Enter()
        {
            // Включаем управление
            tank.enabled = true;
        }

        public void Update()
        {
            // Логика обновления в активном состоянии
        }

        public void FixedUpdate()
        {
            // Физика в активном состоянии
        }

        public void Exit()
        {
            // Выключаем управление
            tank.enabled = false;
        }
    }
}

