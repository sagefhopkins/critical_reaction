using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public class LiquidTargetZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual Feedback")]
        [SerializeField] private GameObject highlightObject;

        // Set at runtime by the menu
        private ClickPourController pourController;

        public void Initialize(ClickPourController controller)
        {
            pourController = controller;
        }

        private void Awake()
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pourController == null) return;

            if (pourController.IsHolding || pourController.IsPouring)
            {
                pourController.SetOverTarget(this);
                ShowHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (pourController == null) return;

            pourController.ClearTarget();
            ShowHighlight(false);
        }

        private void ShowHighlight(bool show)
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(show);
            }
        }

        private void OnDisable()
        {
            ShowHighlight(false);
            if (pourController != null)
            {
                pourController.ClearTarget();
            }
        }
    }
}
