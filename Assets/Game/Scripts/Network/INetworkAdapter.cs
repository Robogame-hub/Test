using System;
using TankGame.Commands;
using TankGame.Tank;

namespace TankGame.Network
{
    /// <summary>
    /// Transport-agnostic adapter for forwarding player input commands over network.
    /// </summary>
    public interface INetworkAdapter
    {
        bool IsConnected { get; }
        event Action<TankController, TankInputCommand> OnRemoteCommand;
        void SendInput(TankController tank, TankInputCommand command);
    }
}
