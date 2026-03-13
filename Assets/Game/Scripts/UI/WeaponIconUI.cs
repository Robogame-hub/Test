using TankGame.Tank;
using TankGame.Tank.Components;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.UI
{
    /// <summary>
    /// Меняет иконку оружия в UI при переключении слота оружия.
    /// Повесьте этот компонент на объект с RawImage (или укажите RawImage вручную),
    /// задайте текстуры для пушки и пулемёта.
    /// </summary>
    public class WeaponIconUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankController tankController;
        [SerializeField] private RawImage iconImage;

        [Header("Textures")]
        [SerializeField] private Texture cannonTexture;
        [SerializeField] private Texture machineGunTexture;

        [Header("Behavior")]
        [SerializeField] private bool hideWhenNoWeapon = true;

        private void Start()
        {
            if (iconImage == null)
                iconImage = GetComponent<RawImage>();

            if (tankController == null)
            {
                // Пытаемся взять локального игрока из реестра
                var local = TankRuntime.GetLocalPlayer();
                if (local != null)
                    tankController = local;
                else
                    tankController = FindObjectOfType<TankController>();
            }

            if (tankController == null)
            {
                ApplyTextureInternal(null);
                return;
            }

            tankController.OnWeaponChanged.AddListener(OnWeaponChanged);
            ApplyTexture(tankController.ActiveWeaponType);
        }

        private void OnDestroy()
        {
            if (tankController != null)
                tankController.OnWeaponChanged.RemoveListener(OnWeaponChanged);
        }

        private void OnWeaponChanged(WeaponType type, TankWeapon _)
        {
            ApplyTexture(type);
        }

        private void ApplyTexture(WeaponType type)
        {
            Texture tex = null;
            switch (type)
            {
                case WeaponType.Cannon:
                    tex = cannonTexture;
                    break;
                case WeaponType.MachineGun:
                    tex = machineGunTexture;
                    break;
            }

            ApplyTextureInternal(tex);
        }

        private void ApplyTextureInternal(Texture tex)
        {
            if (iconImage == null)
                return;

            iconImage.texture = tex;

            if (hideWhenNoWeapon)
                iconImage.enabled = tex != null;
        }
    }
}


