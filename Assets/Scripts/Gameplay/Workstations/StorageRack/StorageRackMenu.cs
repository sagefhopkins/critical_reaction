using Gameplay.Player;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UX
{
    public class StorageRackMenu : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Slots")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotImages;

        [Header("Actions")]
        [SerializeField] private Button depositButton;
        [SerializeField] private Button closeButton;

        private StorageRack rack;
        private PlayerCarry carry;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (slotButtons != null)
            {
                for (int i = 0; i < slotButtons.Length; i++)
                {
                    int idx = i;
                    if (slotButtons[i] != null)
                        slotButtons[i].onClick.AddListener(() => OnClickSlot(idx));
                }
            }

            if (depositButton != null)
                depositButton.onClick.AddListener(OnClickDeposit);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnClickClose);

            Hide();
        }

        public void Open(StorageRack targetRack, PlayerCarry localCarry)
        {
            Unsubscribe();

            rack = targetRack;
            carry = localCarry;

            if (rack == null || carry == null)
            {
                Hide();
                return;
            }

            if (rack.SlotItemIds != null)
                rack.SlotItemIds.OnListChanged += OnRackSlotsChanged;

            carry.HeldItemChanged += OnHeldChanged;

            Show();
            RefreshAll();
        }

        public void Close()
        {
            Unsubscribe();
            Hide();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (rack != null && rack.SlotItemIds != null)
                rack.SlotItemIds.OnListChanged -= OnRackSlotsChanged;

            if (carry != null)
                carry.HeldItemChanged -= OnHeldChanged;

            rack = null;
            carry = null;
        }

        private void OnRackSlotsChanged(NetworkListEvent<ushort> _)
        {
            RefreshAll();
        }

        private void OnHeldChanged()
        {
            RefreshAll();
        }

        private void OnClickSlot(int slotIndex)
        {
            if (rack == null || carry == null) return;
            if (!carry.IsOwner) return;
            
            if (carry.IsHoldingLocal)
                return;

            carry.TryPickupFromRack(rack, slotIndex);
        }

        private void OnClickDeposit()
        {
            if (rack == null || carry == null) return;
            if (!carry.IsOwner) return;

            if (!carry.IsHoldingLocal)
                return;

            carry.TryDepositToRack(rack);
        }

        private void OnClickClose()
        {
            if (InteractionMenus.Instance != null)
                InteractionMenus.Instance.CloseAll();
            else
                Close();
        }

        private void RefreshAll()
        {
            if (rack == null || carry == null) return;

            bool holding = carry.IsHoldingLocal;

            for (int i = 0; i < StorageRack.SlotCount; i++)
            {
                ushort id = rack.GetSlotId(i);
                Sprite spr = rack.GetSpriteForSlot(i);

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    slotImages[i].sprite = spr;
                    slotImages[i].enabled = spr != null;
                }

                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                {
                    slotButtons[i].interactable = !holding && id != 0;
                }
            }

            if (depositButton != null)
                depositButton.interactable = holding && rack.HasEmptySlotClient();
        }

        private void Show()
        {
            if (root != null)
                root.SetActive(true);
        }

        private void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}
