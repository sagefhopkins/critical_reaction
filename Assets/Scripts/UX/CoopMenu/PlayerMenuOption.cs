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
        [SerializeField] private string emptySlotText = "No Player Selected";

        private static readonly Color32 ButtonIdleColor = new Color32(120, 64, 24, 255);

        private Color buttonSelectedColor = Color.yellow;

        private string slotPlayerName = string.Empty;

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

        public void SetSelected(bool selected, Color selectedColor)
        {
            bool changed = isSelected != selected || buttonSelectedColor != selectedColor;
            isSelected = selected;
            buttonSelectedColor = selectedColor;
            if (changed) ApplyVisuals();
        }

        public void SetSlotJoined(bool joined, string playerDisplayName = null)
        {
            bool nameChanged = playerDisplayName != null && slotPlayerName != playerDisplayName;
            if (playerDisplayName != null)
                slotPlayerName = playerDisplayName;

            if (isSlotJoined == joined && !nameChanged) return;
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
            slotPlayerName = string.Empty;
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
            playerButton.image.color = isSelected ? buttonSelectedColor : ButtonIdleColor;
        }

        private void ApplyIconVisuals()
        {
            if (playerIcon != null)
                playerIcon.gameObject.SetActive(false);

            if (playerName != null)
            {
                bool showName = isSlotJoined && !string.IsNullOrEmpty(slotPlayerName);
                playerName.text = showName ? slotPlayerName : emptySlotText;
                playerName.gameObject.SetActive(true);
            }
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
