using UnityEngine;

namespace TankGame.Menu
{
    [System.Serializable]
    public class TankDefinition
    {
        [Tooltip("Название танка, которое отображается в меню выбора.")]
        public string displayName;

        [Tooltip("Префаб танка игрока, который будет заспавнен в матче.")]
        public GameObject playerPrefab;

        [Tooltip("Отдельный префаб модели для 3D-превью (необязательно). Если пусто, используется playerPrefab.")]
        public GameObject previewModelPrefab;

        [Tooltip("Спрайт-превью (используется только как запасной вариант, если 3D-модель недоступна).")]
        public Sprite previewSprite;

        [Tooltip("Масштаб модели в окне 3D-превью.")]
        public float previewModelScale = 1f;

        [Tooltip("Характеристика скорости (заглушка для UI).")]
        [Range(0f, 1f)] public float speed = 0.5f;

        [Tooltip("Характеристика брони (заглушка для UI).")]
        [Range(0f, 1f)] public float armor = 0.5f;

        [Tooltip("Характеристика огневой мощи (заглушка для UI).")]
        [Range(0f, 1f)] public float firepower = 0.5f;

        [Tooltip("Характеристика управляемости (заглушка для UI).")]
        [Range(0f, 1f)] public float handling = 0.5f;
    }
}
