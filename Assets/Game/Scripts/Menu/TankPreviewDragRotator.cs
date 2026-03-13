using UnityEngine;
using UnityEngine.EventSystems;

namespace TankGame.Menu
{
    public class TankPreviewDragRotator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Tooltip("Контроллер выбора танка, которому передается вращение превью.")]
        public TankSelectionController selectionController;

        [Tooltip("Множитель скорости поворота при перетаскивании мышью.")]
        public float dragSensitivity = 0.25f;

        private bool isDragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || selectionController == null)
                return;

            float yawDelta = eventData.delta.x * dragSensitivity;
            selectionController.RotatePreview(yawDelta);
        }
    }
}
