namespace TankGame.Tank.States
{
    /// <summary>
    /// Интерфейс состояния танка (State Pattern)
    /// </summary>
    public interface ITankState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }
}

