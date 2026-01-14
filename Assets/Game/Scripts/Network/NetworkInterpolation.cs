using UnityEngine;
using System.Collections.Generic;
using TankGame.Tank;

namespace TankGame.Network
{
    /// <summary>
    /// Сетевая интерполяция для плавного движения удаленных игроков
    /// </summary>
    public class NetworkInterpolation : MonoBehaviour
    {
        [Header("Interpolation Settings")]
        [SerializeField] private float interpolationBackTime = 0.1f; // 100ms задержка
        [SerializeField] private float extrapolationLimit = 0.5f; // Максимальное время экстраполяции

        private struct StateSnapshot
        {
            public float Timestamp;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
        }

        private LinkedList<StateSnapshot> stateBuffer = new LinkedList<StateSnapshot>();
        private TankController tankController;

        private void Awake()
        {
            tankController = GetComponent<TankController>();
        }

        /// <summary>
        /// Добавить новое состояние в буфер
        /// </summary>
        public void ReceiveState(TankNetworkState state)
        {
            StateSnapshot snapshot = new StateSnapshot
            {
                Timestamp = state.Timestamp,
                Position = state.Position,
                Rotation = state.Rotation,
                Velocity = state.Velocity
            };

            // Вставляем в правильную позицию (отсортировано по времени)
            var node = stateBuffer.Last;
            while (node != null && node.Value.Timestamp > snapshot.Timestamp)
            {
                node = node.Previous;
            }

            if (node == null)
                stateBuffer.AddFirst(snapshot);
            else
                stateBuffer.AddAfter(node, snapshot);

            // Удаляем старые состояния
            while (stateBuffer.Count > 30) // Храним ~0.5 секунды при 60 тиках
            {
                stateBuffer.RemoveFirst();
            }
        }

        private void Update()
        {
            if (!tankController.IsLocalPlayer && stateBuffer.Count >= 2)
            {
                InterpolateState();
            }
        }

        /// <summary>
        /// Интерполяция между состояниями
        /// </summary>
        private void InterpolateState()
        {
            float renderTime = Time.time - interpolationBackTime;

            StateSnapshot? fromState = null;
            StateSnapshot? toState = null;

            // Находим два состояния для интерполяции
            foreach (var state in stateBuffer)
            {
                if (state.Timestamp <= renderTime)
                {
                    fromState = state;
                }
                else
                {
                    toState = state;
                    break;
                }
            }

            // Интерполяция
            if (fromState.HasValue && toState.HasValue)
            {
                float duration = toState.Value.Timestamp - fromState.Value.Timestamp;
                float t = (renderTime - fromState.Value.Timestamp) / duration;

                Vector3 position = Vector3.Lerp(fromState.Value.Position, toState.Value.Position, t);
                Quaternion rotation = Quaternion.Slerp(fromState.Value.Rotation, toState.Value.Rotation, t);

                transform.SetPositionAndRotation(position, rotation);
            }
            // Экстраполяция (если нет новых данных)
            else if (fromState.HasValue)
            {
                float extrapolationTime = renderTime - fromState.Value.Timestamp;
                
                if (extrapolationTime < extrapolationLimit)
                {
                    Vector3 position = fromState.Value.Position + fromState.Value.Velocity * extrapolationTime;
                    transform.position = position;
                    transform.rotation = fromState.Value.Rotation;
                }
            }
        }
    }
}

