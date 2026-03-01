using System.Collections.Generic;
using UnityEngine;

namespace TankGame.Tank
{
    /// <summary>
    /// Глобальный реестр танков. Регистрация при OnEnable, снятие при OnDisable.
    /// Используется миникартой и AI вместо FindObjectsOfType каждый кадр.
    /// </summary>
    public static class TankRegistry
    {
        private static readonly List<TankController> All = new List<TankController>(32);
        private static TankController _localPlayerCache;
        private static int _cacheFrame = -1;

        /// <summary>Все зарегистрированные танки (активные в сцене).</summary>
        public static IReadOnlyList<TankController> GetAllTanks()
        {
            return All;
        }

        /// <summary>Локальный игрок (первый танк с IsLocalPlayer == true). Кэш обновляется при запросе.</summary>
        public static TankController GetLocalPlayer()
        {
            int frame = Time.frameCount;
            if (_localPlayerCache != null && _cacheFrame == frame)
                return _localPlayerCache;

            _cacheFrame = frame;
            for (int i = 0; i < All.Count; i++)
            {
                var t = All[i];
                if (t != null && t.IsLocalPlayer)
                {
                    _localPlayerCache = t;
                    return t;
                }
            }
            _localPlayerCache = null;
            return null;
        }

        /// <summary>Вызывается из TankController.OnEnable.</summary>
        public static void Register(TankController tank)
        {
            if (tank == null)
                return;
            if (!All.Contains(tank))
                All.Add(tank);
            if (tank.IsLocalPlayer)
                _localPlayerCache = tank;
        }

        /// <summary>Вызывается из TankController.OnDisable.</summary>
        public static void Unregister(TankController tank)
        {
            if (tank == null)
                return;
            All.Remove(tank);
            if (_localPlayerCache == tank)
                _localPlayerCache = null;
        }
    }
}
