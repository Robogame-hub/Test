using TankGame.Tank;
using TankGame.Tank.Components;
using UnityEngine;

namespace TankGame.UI
{
    /// <summary>
    /// Переключает UI оружия при смене активного слота.
    /// </summary>
    public class WeaponUIStateSwitcher : MonoBehaviour
    {
        [SerializeField] private TankController tankController;
        [SerializeField] private GameObject cannonUIRoot;
        [SerializeField] private GameObject machineGunUIRoot;

        private void Start()
        {
            if (tankController == null)
                tankController = FindObjectOfType<TankController>();

            if (tankController == null)
                return;

            tankController.OnWeaponChanged.AddListener(OnWeaponChanged);
            ApplyState(tankController.ActiveWeaponType);
        }

        private void OnDestroy()
        {
            if (tankController != null)
                tankController.OnWeaponChanged.RemoveListener(OnWeaponChanged);
        }

        private void OnWeaponChanged(WeaponType type, TankWeapon _)
        {
            ApplyState(type);
        }

        private void ApplyState(WeaponType type)
        {
            if (cannonUIRoot != null)
                cannonUIRoot.SetActive(type == WeaponType.Cannon);
            if (machineGunUIRoot != null)
                machineGunUIRoot.SetActive(type == WeaponType.MachineGun);
        }
    }
}
