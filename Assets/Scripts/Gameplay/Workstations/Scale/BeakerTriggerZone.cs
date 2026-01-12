using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Workstations.Scale
{
    public class BeakerTriggerZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Beaker beaker;
        [SerializeField] private bool isSourceBeaker = false;

        private ScoopController currentScoop;

        public Beaker Beaker => beaker;
        public bool IsSourceBeaker => isSourceBeaker;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            ScoopController scoop = eventData.pointerDrag.GetComponent<ScoopController>();
            if (scoop != null)
            {
                currentScoop = scoop;
                scoop.SetOverBeaker(beaker);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (currentScoop != null)
            {
                currentScoop.ClearOverBeaker(beaker);
                currentScoop = null;
            }
        }

        private void Update()
        {
            if (isSourceBeaker && currentScoop != null && beaker != null)
            {
                currentScoop.TryPickupParticles(beaker);
            }
        }

        private void OnDisable()
        {
            if (currentScoop != null)
            {
                currentScoop.ClearOverBeaker(beaker);
                currentScoop = null;
            }
        }
    }
}
