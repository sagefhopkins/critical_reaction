using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations
{
    public class BurnerMenu : WorkstationMenuBase
    {
        [Header("Slots")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotImages;

        [Header("Actions")]
        [SerializeField] private Button depositButton;
        [SerializeField] private Button startWorkButton;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button closeButton;

        [Header("Progress")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressText;

        [Header("Status")]
        [SerializeField] private Text stateText;

        private BurnerController burnerController;

        private void Awake()
        {
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

            if (startWorkButton != null)
                startWorkButton.onClick.AddListener(OnClickStartWork);

            if (collectButton != null)
                collectButton.onClick.AddListener(OnClickCollect);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnClickClose);
        }

        protected override void OnOpened()
        {
            burnerController = workstation.GetComponent<BurnerController>();
            RefreshAll();
        }

        protected override void OnClosed()
        {
            burnerController = null;
        }

        protected override void OnWorkStateChanged()
        {
            RefreshAll();
        }

        protected override void OnProgressChanged()
        {
            RefreshProgress();
        }

        protected override void OnInventoryChanged()
        {
            RefreshAll();
        }

        protected override void OnHeldItemChanged()
        {
            RefreshAll();
        }

        private void OnClickSlot(int slotIndex)
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;
            if (workstation.CurrentWorkState == WorkState.Working) return;

            if (localCarry.IsHoldingLocal)
                return;

            workstation.TryTakeFromSlotServerRpc(slotIndex);
        }

        private void OnClickDeposit()
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;
            if (workstation.CurrentWorkState == WorkState.Working) return;

            if (!localCarry.IsHoldingLocal)
                return;

            workstation.TryDepositHeldServerRpc();
        }

        private void OnClickStartWork()
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;
            if (burnerController == null) return;

            burnerController.TryStartWorkServerRpc();
        }

        private void OnClickCollect()
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;

            workstation.CollectOutputServerRpc();
        }

        private void OnClickClose()
        {
            RequestClose();
        }

        private void RefreshAll()
        {
            if (workstation == null || localCarry == null) return;

            bool holding = localCarry.IsHoldingLocal;
            WorkState state = workstation.CurrentWorkState;
            bool isWorking = state == WorkState.Working;
            bool isCompleted = state == WorkState.Completed;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                ushort id = workstation.GetSlotId(i);
                Sprite spr = workstation.GetSpriteForSlot(i);

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    slotImages[i].sprite = spr;
                    slotImages[i].enabled = spr != null;
                }

                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                {
                    slotButtons[i].interactable = !holding && !isWorking && id != 0;
                }
            }

            if (depositButton != null)
                depositButton.interactable = holding && !isWorking && workstation.HasEmptySlotClient();

            if (startWorkButton != null)
            {
                startWorkButton.interactable = state == WorkState.Idle && workstation.CanStartWork();
                startWorkButton.gameObject.SetActive(state == WorkState.Idle);
            }

            if (collectButton != null)
            {
                collectButton.interactable = isCompleted && !holding;
                collectButton.gameObject.SetActive(isCompleted);
            }

            RefreshProgress();
            RefreshState();
        }

        private void RefreshProgress()
        {
            if (workstation == null) return;

            float progress = workstation.GetProgressNormalized();

            if (progressSlider != null)
                progressSlider.value = progress;

            if (progressText != null)
                progressText.text = workstation.GetProgressText();
        }

        private void RefreshState()
        {
            if (stateText == null || workstation == null) return;
            stateText.text = workstation.GetStateText();
        }
    }
}
