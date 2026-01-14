namespace TankGame.Commands
{
    /// <summary>
    /// Базовый интерфейс команды (Command Pattern)
    /// Используется для записи, воспроизведения и сетевой синхронизации действий
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}

