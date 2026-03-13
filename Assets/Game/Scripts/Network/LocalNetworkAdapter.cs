using System;
using TankGame.Commands;
using TankGame.Tank;
using UnityEngine;

namespace TankGame.Network
{
    /// <summary>
    /// Local transport stub for offline mode and integration testing.
    /// </summary>
    public sealed class LocalNetworkAdapter : MonoBehaviour, INetworkAdapter
    {
        public enum LoopbackMode
        {
            Disabled,
            Immediate
        }

        [SerializeField] private LoopbackMode mode = LoopbackMode.Disabled;

        public bool IsConnected => true;

        public event Action<TankController, TankInputCommand> OnRemoteCommand;

        public void SendInput(TankController tank, TankInputCommand command)
        {
            if (mode != LoopbackMode.Immediate)
                return;

            OnRemoteCommand?.Invoke(tank, command);
        }
    }
}
