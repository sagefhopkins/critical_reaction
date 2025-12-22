using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UX.CoopMenu
{
    public class PlayerMenuOption : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image playerIcon;
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private Button playerButton;

        [Header("State")]
        [SerializeField] private bool isSlotJoined;
        [SerializeField] private bool isSelected;

        private static readonly Color32 ButtonIdleColor = new Color32(120, 64, 24, 255);
        private static readonly Color ButtonSelectedColor = Color.yellow;

        private static readonly Color IconJoinedColor = Color.yellow;

        private TMP_Text buttonLabel;

        private void Awake()
        {
            if (playerButton != null)
                buttonLabel = playerButton.GetComponentInChildren<TMP_Text>(true);

            ApplyVisuals();
        }

        private void OnEnable()
        {
            ApplyVisuals();
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;
            ApplyVisuals();
        }

        public void SetSlotJoined(bool joined)
        {
            if (isSlotJoined == joined) return;
            isSlotJoined = joined;
            ApplyVisuals();
        }

        public void Pressed()
        {
            if (isSlotJoined) return;

            isSlotJoined = true;
            ApplyVisuals();
        }

        public void ResetState()
        {
            isSlotJoined = false;
            isSelected = false;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            ApplyButtonVisuals();
            ApplyIconVisuals();
            ApplyButtonText();
        }

        private void ApplyButtonVisuals()
        {
            if (playerButton == null) return;
            playerButton.image.color = isSelected ? ButtonSelectedColor : ButtonIdleColor;
        }

        private void ApplyIconVisuals()
        {
            if (playerIcon == null) return;

            playerIcon.gameObject.SetActive(isSlotJoined);
            if (isSlotJoined)
                playerIcon.color = IconJoinedColor;
        }

        private void ApplyButtonText()
        {
            if (buttonLabel == null) return;
            buttonLabel.text = isSlotJoined ? "Ready" : "Join";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyVisuals();
        }
#endif
    }
}
