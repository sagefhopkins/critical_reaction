using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Workstations.GraduatedCylinder
{
    public class CylinderTriggerZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private FluidContainer targetContainer;

        private PourController currentPourController;

        public FluidContainer TargetContainer => targetContainer;

        private void Awake()
        {
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
            }
            else if (!graphic.raycastTarget)
            {
                graphic.raycastTarget = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            PourController pourController = eventData.pointerDrag.GetComponent<PourController>();
            if (pourController != null)
            {
                currentPourController = pourController;
                pourController.SetOverTarget(targetContainer);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (currentPourController != null)
            {
                currentPourController.ClearOverTarget();
                currentPourController = null;
            }
        }

        private void OnDisable()
        {
            if (currentPourController != null)
            {
                currentPourController.ClearOverTarget();
                currentPourController = null;
            }
        }
    }
}
