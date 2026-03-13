using TMPro;
using UnityEngine;
using TankGame.Tank;
using TankGame.Tank.Components;

namespace TankGame.UI
{
    /// <summary>
    /// UI-элемент состояния двигателя: показывает "ENGINE ON" / "ENGINE OFF" для локального танка.
    /// Повесьте на объект с TextMeshProUGUI и при необходимости укажите ссылки вручную.
    /// </summary>
    public class EngineStatusUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TankEngine tankEngine;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Texts")]
        [SerializeField] private string engineOnText = "ENGINE ON";
        [SerializeField] private string engineOffText = "ENGINE OFF";

        [Header("Colors")]
        [SerializeField] private Color engineOnColor = Color.green;
        [SerializeField] private Color engineOffColor = Color.red;

        private void Start()
        {
            if (statusText == null)
                statusText = GetComponent<TextMeshProUGUI>();

            if (tankEngine == null)
            {
                var localPlayer = TankRuntime.GetLocalPlayer();
                if (localPlayer != null)
                    tankEngine = localPlayer.GetComponent<TankEngine>();
            }
        }

        private void Update()
        {
            if (statusText == null)
                return;

            if (tankEngine == null)
            {
                statusText.text = engineOffText;
                statusText.color = engineOffColor;
                return;
            }

            bool isOn = tankEngine.IsEngineRunning;
            statusText.text = isOn ? engineOnText : engineOffText;
            statusText.color = isOn ? engineOnColor : engineOffColor;
        }
    }
}


