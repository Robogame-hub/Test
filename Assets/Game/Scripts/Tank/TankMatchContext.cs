using System.Collections.Generic;
using UnityEngine;

namespace TankGame.Tank
{
    /// <summary>
    /// Optional scene-scoped registry. If absent, TankRegistry is used as fallback.
    /// </summary>
    public sealed class TankMatchContext : MonoBehaviour
    {
        private static TankMatchContext active;

        [SerializeField] private bool setAsActiveOnAwake = true;

        private readonly List<TankController> all = new List<TankController>(32);
        private TankController localPlayer;

        public static TankMatchContext Active => active;

        private void Awake()
        {
            if (setAsActiveOnAwake || active == null)
                active = this;
        }

        private void OnDestroy()
        {
            if (active == this)
                active = null;
        }

        public void Register(TankController tank)
        {
            if (tank == null)
                return;
            if (!all.Contains(tank))
                all.Add(tank);
            if (tank.IsLocalPlayer)
                localPlayer = tank;
        }

        public void Unregister(TankController tank)
        {
            if (tank == null)
                return;
            all.Remove(tank);
            if (localPlayer == tank)
                localPlayer = null;
        }

        public IReadOnlyList<TankController> GetAllTanks()
        {
            return all;
        }

        public TankController GetLocalPlayer()
        {
            if (localPlayer != null)
                return localPlayer;

            for (int i = 0; i < all.Count; i++)
            {
                TankController tank = all[i];
                if (tank != null && tank.IsLocalPlayer)
                {
                    localPlayer = tank;
                    return localPlayer;
                }
            }

            return null;
        }
    }
}
