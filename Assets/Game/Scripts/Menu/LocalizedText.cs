using TMPro;
using UnityEngine;

namespace TankGame.Menu
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Ключ локализации из LocalizationService.Table (например: menu.play).")]
        [SerializeField] private string localizationKey;

        private TMP_Text textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LocalizationService.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationService.LanguageChanged -= Refresh;
        }

        public void SetKey(string key)
        {
            localizationKey = key;
            Refresh();
        }

        public void Refresh()
        {
            if (textComponent == null)
                textComponent = GetComponent<TMP_Text>();

            textComponent.text = LocalizationService.Get(localizationKey);
        }
    }
}
