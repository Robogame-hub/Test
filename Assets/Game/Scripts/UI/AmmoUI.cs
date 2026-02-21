using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TankGame.Tank;
using TankGame.Tank.Components;

namespace TankGame.UI
{
    /// <summary>
    /// UI отображения боезапаса (магазин и резерв).
    /// </summary>
    public class AmmoUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankWeapon tankWeapon;
        [SerializeField] private TankController tankController;

        [Header("UI Elements")]
        [SerializeField] private Text ammoText;
        [SerializeField] private TextMeshProUGUI ammoTextTMP;

        [Header("Display")]
        [SerializeField] private string ammoFormat = "{0}/{1} | {2}";
        [SerializeField] private string reloadingLabel = "  RELOAD...";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lowAmmoColor = new Color(1f, 0.45f, 0.2f, 1f);
        [SerializeField] private Color emptyAmmoColor = Color.red;

        private void Start()
        {
            if (tankWeapon == null)
            {
                tankWeapon = FindObjectOfType<TankWeapon>();
            }

            if (tankController == null)
                tankController = FindObjectOfType<TankController>();

            if (tankController != null)
            {
                if (tankWeapon == null)
                    tankWeapon = tankController.Weapon;
                tankController.OnWeaponChanged.AddListener(OnWeaponChanged);
            }

            if (tankWeapon == null)
                return;

            tankWeapon.OnAmmoChanged.AddListener(OnAmmoChanged);
            Refresh();
        }

        private void Update()
        {
            // Отображаем состояние перезарядки без задержки.
            Refresh();
        }

        private void OnDestroy()
        {
            if (tankWeapon != null)
                tankWeapon.OnAmmoChanged.RemoveListener(OnAmmoChanged);
            if (tankController != null)
                tankController.OnWeaponChanged.RemoveListener(OnWeaponChanged);
        }

        private void OnAmmoChanged(int currentMagazine, int magazineSize, int reserveAmmo)
        {
            UpdateDisplay(currentMagazine, magazineSize, reserveAmmo);
        }

        private void Refresh()
        {
            if (tankWeapon == null)
                return;

            UpdateDisplay(tankWeapon.CurrentAmmoInMagazine, tankWeapon.MagazineSize, tankWeapon.ReserveAmmo);
        }

        private void UpdateDisplay(int currentMagazine, int magazineSize, int reserveAmmo)
        {
            string textValue = string.Format(ammoFormat, currentMagazine, magazineSize, reserveAmmo);
            if (tankWeapon != null && tankWeapon.IsReloading)
                textValue += reloadingLabel;

            Color color = normalColor;
            if (currentMagazine <= 0 && reserveAmmo <= 0)
                color = emptyAmmoColor;
            else if (currentMagazine <= Mathf.Max(1, magazineSize / 4))
                color = lowAmmoColor;

            if (ammoText != null)
            {
                ammoText.text = textValue;
                ammoText.color = color;
            }

            if (ammoTextTMP != null)
            {
                ammoTextTMP.text = textValue;
                ammoTextTMP.color = color;
            }
        }

        private void OnWeaponChanged(WeaponType weaponType, TankWeapon newWeapon)
        {
            if (tankWeapon != null)
                tankWeapon.OnAmmoChanged.RemoveListener(OnAmmoChanged);

            tankWeapon = newWeapon;

            if (tankWeapon != null)
                tankWeapon.OnAmmoChanged.AddListener(OnAmmoChanged);

            Refresh();
        }
    }
}
