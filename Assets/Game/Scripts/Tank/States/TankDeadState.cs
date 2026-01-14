using UnityEngine;

namespace TankGame.Tank.States
{
    /// <summary>
    /// Состояние "Мертв" - танк уничтожен
    /// </summary>
    public class TankDeadState : ITankState
    {
        private readonly TankController tank;
        private readonly float respawnTime;
        private float deathTime;

        public TankDeadState(TankController tank, float respawnTime = 5f)
        {
            this.tank = tank;
            this.respawnTime = respawnTime;
        }

        public void Enter()
        {
            deathTime = Time.time;
            
            // Отключаем коллайдеры
            foreach (var collider in tank.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            // Можно добавить эффект взрыва
            Debug.Log("Tank destroyed!");
        }

        public void Update()
        {
            // Проверяем время для респавна
            if (Time.time - deathTime >= respawnTime)
            {
                Respawn();
            }
        }

        public void FixedUpdate()
        {
            // Ничего не делаем
        }

        public void Exit()
        {
            // Включаем коллайдеры
            foreach (var collider in tank.GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }
        }

        private void Respawn()
        {
            // Восстанавливаем здоровье
            tank.Health.Respawn();
            
            // Можно добавить телепорт на точку спавна
            Debug.Log("Tank respawned!");
        }
    }
}

