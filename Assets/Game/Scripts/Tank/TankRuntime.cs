using System.Collections.Generic;

namespace TankGame.Tank
{
    /// <summary>
    /// Runtime access facade for tank lookup and registration.
    /// Uses scene context when available, otherwise falls back to TankRegistry.
    /// </summary>
    public static class TankRuntime
    {
        public static void Register(TankController tank)
        {
            TankMatchContext context = TankMatchContext.Active;
            if (context != null)
            {
                context.Register(tank);
                return;
            }

            TankRegistry.Register(tank);
        }

        public static void Unregister(TankController tank)
        {
            TankMatchContext context = TankMatchContext.Active;
            if (context != null)
            {
                context.Unregister(tank);
                return;
            }

            TankRegistry.Unregister(tank);
        }

        public static IReadOnlyList<TankController> GetAllTanks()
        {
            TankMatchContext context = TankMatchContext.Active;
            if (context != null)
                return context.GetAllTanks();

            return TankRegistry.GetAllTanks();
        }

        public static TankController GetLocalPlayer()
        {
            TankMatchContext context = TankMatchContext.Active;
            if (context != null)
                return context.GetLocalPlayer();

            return TankRegistry.GetLocalPlayer();
        }
    }
}
