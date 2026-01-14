using UnityEngine;
using System.Collections.Generic;
using TankGame.Commands;
using TankGame.Tank;

namespace TankGame.Network
{
    /// <summary>
    /// Клиентское предсказание (Client-Side Prediction)
    /// Уменьшает задержку между вводом и реакцией
    /// </summary>
    public class ClientPrediction : MonoBehaviour
    {
        private const int MAX_HISTORY_SIZE = 60; // 1 секунда при 60 FPS

        private Queue<TankInputCommand> inputHistory = new Queue<TankInputCommand>();
        private TankController tankController;

        private void Awake()
        {
            tankController = GetComponent<TankController>();
        }

        /// <summary>
        /// Сохранить команду в истории
        /// </summary>
        public void RecordInput(TankInputCommand command)
        {
            inputHistory.Enqueue(command);

            // Ограничиваем размер истории
            while (inputHistory.Count > MAX_HISTORY_SIZE)
            {
                inputHistory.Dequeue();
            }
        }

        /// <summary>
        /// Сверка с сервером и коррекция если нужно
        /// </summary>
        public void ReconcileWithServer(TankNetworkState serverState)
        {
            // Проверяем расхождение позиции
            float positionError = Vector3.Distance(transform.position, serverState.Position);
            
            if (positionError > 0.1f) // Порог коррекции
            {
                // Применяем состояние с сервера
                tankController.Movement.SetPositionAndRotation(serverState.Position, serverState.Rotation);

                // Повторно применяем все команды после серверной метки времени
                ReplayInputs(serverState.Timestamp);
            }
        }

        /// <summary>
        /// Переиграть команды ввода для коррекции предсказания
        /// </summary>
        private void ReplayInputs(float serverTimestamp)
        {
            foreach (var command in inputHistory)
            {
                if (command.Timestamp > serverTimestamp)
                {
                    tankController.ProcessCommand(command);
                }
            }
        }

        /// <summary>
        /// Очистить старую историю
        /// </summary>
        public void ClearOldHistory(float beforeTimestamp)
        {
            while (inputHistory.Count > 0)
            {
                var command = inputHistory.Peek();
                if (command.Timestamp < beforeTimestamp)
                {
                    inputHistory.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }
    }
}

