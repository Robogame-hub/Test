using UnityEngine;
using UnityEngine.UI;

namespace TankGame.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public class RawImageUvScroll : MonoBehaviour
    {
        [Header("Scroll")]
        [SerializeField] private Vector2 uvSpeed = new Vector2(0f, -0.03f);
        [SerializeField] private bool useUnscaledTime = true;

        private RawImage targetImage;
        private Rect currentUvRect;

        private void Awake()
        {
            targetImage = GetComponent<RawImage>();
            currentUvRect = targetImage.uvRect;
        }

        private void OnEnable()
        {
            if (targetImage == null)
                targetImage = GetComponent<RawImage>();

            currentUvRect = targetImage.uvRect;
        }

        private void Update()
        {
            if (targetImage == null)
                return;

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            currentUvRect.position += uvSpeed * deltaTime;
            targetImage.uvRect = currentUvRect;
        }

        public void SetSpeed(Vector2 speed)
        {
            uvSpeed = speed;
        }
    }
}
