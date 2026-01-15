using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Workstations.Scale
{
    public class BeakerTriggerZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Beaker beaker;
        [SerializeField] private bool isSourceBeaker = false;

        private ScoopController currentScoop;

        public Beaker Beaker => beaker;
        public bool IsSourceBeaker => isSourceBeaker;

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
            Debug.Log($"BeakerTriggerZone: OnPointerEnter on {gameObject.name}, isSource: {isSourceBeaker}, pointerDrag: {eventData.pointerDrag?.name}");
            if (eventData.pointerDrag == null) return;

            ScoopController scoop = eventData.pointerDrag.GetComponent<ScoopController>();
            if (scoop != null)
            {
                currentScoop = scoop;
                scoop.SetOverBeaker(beaker);
                Debug.Log($"BeakerTriggerZone: Scoop entered {gameObject.name}, set over beaker");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"BeakerTriggerZone: OnPointerExit on {gameObject.name}");
            if (currentScoop != null)
            {
                currentScoop.ClearOverBeaker(beaker);
                currentScoop = null;
            }
        }

        private void Update()
        {
            if (currentScoop != null && beaker != null)
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
