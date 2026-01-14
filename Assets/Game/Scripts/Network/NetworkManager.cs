using UnityEngine;
using System.Collections.Generic;
using TankGame.Commands;

namespace TankGame.Network
{
    /// <summary>
    /// Базовый сетевой менеджер
    /// Готов к интеграции с Mirror, Netcode for GameObjects или Photon
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager instance;
        public static NetworkManager Instance => instance;

        [Header("Network Settings")]
        [SerializeField] private float clientSendRate = 30f; // Гц
        [SerializeField] private float serverTickRate = 60f; // Гц
        [SerializeField] private int maxPlayers = 16;

        private Queue<TankInputCommand> inputBuffer = new Queue<TankInputCommand>();
        private float lastSendTime;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Добавить команду ввода в буфер для отправки
        /// </summary>
        public void SendInput(TankInputCommand command)
        {
            inputBuffer.Enqueue(command);

            float sendInterval = 1f / clientSendRate;
            if (Time.time - lastSendTime >= sendInterval)
            {
                FlushInputBuffer();
                lastSendTime = Time.time;
            }
        }

        /// <summary>
        /// Отправить все накопленные команды серверу
        /// </summary>
        private void FlushInputBuffer()
        {
            if (inputBuffer.Count == 0)
                return;

            // Здесь будет код для отправки команд серверу
            // Например через:
            // - Mirror: NetworkClient.Send(new InputMessage(inputBuffer));
            // - Netcode: NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(...);
            // - Photon: PhotonNetwork.RaiseEvent(...);

            inputBuffer.Clear();
        }

        /// <summary>
        /// Получить состояние игры от сервера
        /// </summary>
        public void ReceiveGameState(byte[] data)
        {
            // Десериализация состояния игры
            // Применение к локальным объектам
        }
    }
}

