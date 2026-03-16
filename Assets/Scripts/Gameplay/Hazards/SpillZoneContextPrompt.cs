using UnityEngine;

namespace Gameplay.Hazards
{
    public class SpillZoneContextPrompt : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private UX.WorldContextPopup popup;

        [Header("Text")]
        [SerializeField] private string message = "Hold E - Mop Spill";

        private void Awake()
        {
            if (popup == null)
                popup = GetComponentInChildren<UX.WorldContextPopup>(true);

            Hide();
        }

        public void Show()
        {
            if (popup == null) return;
            popup.Show(message);
        }

        public void Hide()
        {
            if (popup == null) return;
            popup.Hide();
        }

        public void SetMessage(string text)
        {
            message = text;
        }
    }
}
