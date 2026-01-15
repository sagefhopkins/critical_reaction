using Gameplay.Workstations.Scale;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations.GraduatedCylinder
{
    public class GraduatedCylinderMenu : WorkstationMenuBase
    {
        [Header("Screen Roots")]
        [SerializeField] private GameObject cylinderScreenRoot;
        [SerializeField] private GameObject inventoryScreenRoot;

        [Header("Output Slot")]
        [SerializeField] private Button outputSlotButton;
        [SerializeField] private Image outputSlotImage;

        [Header("Inventory Slots")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Button depositButton;

        [Header("Cylinder References")]
        [SerializeField] private PourController pourController;
        [SerializeField] private FluidContainer sourceContainer;
        [SerializeField] private FluidContainer measurementCylinder;

        [Header("Cylinder Controls")]
        [SerializeField] private Button powerButton;
        [SerializeField] private Button unitButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inventoryButton;

        [Header("Inventory Controls")]
        [SerializeField] private Button backToCylinderButton;

        [Header("Display")]
        [SerializeField] private TMP_Text volumeDisplayText;

        private GraduatedCylinderController cylinderController;
        private bool manualInventoryView;

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

            if (outputSlotButton != null)
                outputSlotButton.onClick.AddListener(OnClickOutputSlot);

            if (powerButton != null)
                powerButton.onClick.AddListener(OnClickPower);

            if (unitButton != null)
                unitButton.onClick.AddListener(OnClickUnit);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnClickClose);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnClickInventory);

            if (backToCylinderButton != null)
                backToCylinderButton.onClick.AddListener(OnClickBackToCylinder);
        }

        protected override void OnOpened()
        {
            manualInventoryView = false;
            cylinderController = workstation.GetComponent<GraduatedCylinderController>();

            if (workstation != null)
            {
                workstation.OnOutputChanged += OnOutputChanged;
            }

            if (cylinderController != null)
            {
                sourceContainer = cylinderController.SourceContainer;
                measurementCylinder = cylinderController.MeasurementCylinder;

                cylinderController.OnCylinderStateChanged += RefreshButtons;
                cylinderController.OnVolumeChanged += RefreshVolume;
                cylinderController.OnSourceChemicalChanged += OnSourceChemicalChanged;

                if (measurementCylinder != null)
                {
                    measurementCylinder.OnVolumeChanged += RefreshVolume;
                }

                if (sourceContainer != null)
                {
                    sourceContainer.OnVolumeChanged += RefreshAll;
                }

                cylinderController.RefreshSourceContainerConfiguration();
            }

            if (pourController != null && sourceContainer != null)
            {
                pourController.SetSourceContainer(sourceContainer);
            }

            UpdateActiveScreen();
            RefreshAll();
        }

        protected override void OnClosed()
        {
            if (cylinderScreenRoot != null)
                cylinderScreenRoot.SetActive(false);
            if (inventoryScreenRoot != null)
                inventoryScreenRoot.SetActive(false);

            if (workstation != null)
            {
                workstation.OnOutputChanged -= OnOutputChanged;
            }

            if (cylinderController != null)
            {
                cylinderController.OnCylinderStateChanged -= RefreshButtons;
                cylinderController.OnVolumeChanged -= RefreshVolume;
                cylinderController.OnSourceChemicalChanged -= OnSourceChemicalChanged;

                if (measurementCylinder != null)
                {
                    measurementCylinder.OnVolumeChanged -= RefreshVolume;
                }

                if (sourceContainer != null)
                {
                    sourceContainer.OnVolumeChanged -= RefreshAll;
                }
            }

            if (pourController != null)
            {
                pourController.ResetToOriginalPosition();
            }

            cylinderController = null;
            sourceContainer = null;
            measurementCylinder = null;
        }

        protected override void OnWorkStateChanged()
        {
            RefreshAll();
        }

        protected override void OnInventoryChanged()
        {
            UpdateActiveScreen();
            RefreshAll();
        }

        protected override void OnHeldItemChanged()
        {
            UpdateActiveScreen();
            RefreshAll();
        }

        private void UpdateActiveScreen()
        {
            bool showInventory = ShouldShowInventoryScreen();

            if (cylinderScreenRoot != null)
                cylinderScreenRoot.SetActive(!showInventory);
            if (inventoryScreenRoot != null)
                inventoryScreenRoot.SetActive(showInventory);
        }

        private bool ShouldShowInventoryScreen()
        {
            bool isHolding = localCarry != null && localCarry.IsHoldingLocal;
            bool hasOutput = workstation != null && workstation.HasOutput;
            bool recipeReqsMet = AreRecipeRequirementsMet();

            if (isHolding)
                return true;

            if (hasOutput)
                return true;

            if (!recipeReqsMet)
                return true;

            if (manualInventoryView)
                return true;

            return false;
        }

        private bool CanReturnToCylinder()
        {
            bool isHolding = localCarry != null && localCarry.IsHoldingLocal;
            bool hasOutput = workstation != null && workstation.HasOutput;
            bool recipeReqsMet = AreRecipeRequirementsMet();

            return !isHolding && !hasOutput && recipeReqsMet;
        }

        private bool AreRecipeRequirementsMet()
        {
            if (workstation == null || workstation.AssignedRecipe == null)
                return false;

            var recipe = workstation.AssignedRecipe;
            if (recipe.Ingredients == null || recipe.Ingredients.Length == 0)
                return false;

            ushort[] slotIds = new ushort[Workstation.SlotCount];
            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                slotIds[i] = workstation.GetSlotId(i);
            }

            return recipe.CanCraftWith(slotIds, Workstation.SlotCount);
        }

        private void OnOutputChanged()
        {
            UpdateActiveScreen();
            RefreshAll();
        }

        private void OnSourceChemicalChanged()
        {
            if (cylinderController != null)
            {
                cylinderController.RefreshSourceContainerConfiguration();
            }

            UpdateActiveScreen();
            RefreshAll();
        }

        private void OnClickSlot(int slotIndex)
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;
            if (localCarry.IsHoldingLocal) return;

            workstation.TryTakeFromSlotServerRpc(slotIndex);
        }

        private void OnClickDeposit()
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;
            if (!localCarry.IsHoldingLocal) return;

            workstation.TryDepositHeldServerRpc();
        }

        private void OnClickOutputSlot()
        {
            if (workstation == null || localCarry == null) return;
            if (!localCarry.IsOwner) return;
            if (localCarry.IsHoldingLocal) return;
            if (!workstation.HasOutput) return;

            workstation.TryTakeOutputServerRpc();
        }

        private void OnClickPower()
        {
            if (cylinderController == null) return;
            cylinderController.TogglePowerServerRpc();
        }

        private void OnClickUnit()
        {
            if (cylinderController == null) return;
            cylinderController.CycleUnitServerRpc();
        }

        private void OnClickClose()
        {
            if (cylinderController != null)
            {
                MeasurementResult result = cylinderController.CheckMeasurement();
                if (result.IsCorrect)
                {
                    cylinderController.ConfirmAndPlaceOutputServerRpc();
                    UpdateActiveScreen();
                    RefreshAll();
                    return;
                }
            }

            RequestClose();
        }

        private void OnClickInventory()
        {
            manualInventoryView = true;
            UpdateActiveScreen();
            RefreshAll();
        }

        private void OnClickBackToCylinder()
        {
            if (!CanReturnToCylinder())
                return;

            manualInventoryView = false;
            UpdateActiveScreen();
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshButtons();
            RefreshVolume();
        }

        private void RefreshButtons()
        {
            if (workstation == null)
                return;

            bool isHolding = localCarry != null && localCarry.IsHoldingLocal;
            bool hasEmptySlot = workstation.HasEmptySlotClient();
            bool isPowered = cylinderController != null && cylinderController.IsPoweredOn;

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
                    slotButtons[i].interactable = !isHolding && id != 0;
                }
            }

            if (depositButton != null)
                depositButton.interactable = isHolding && hasEmptySlot;

            bool hasOutput = workstation.HasOutput;
            if (outputSlotImage != null)
            {
                Sprite outputSprite = workstation.GetOutputSprite();
                outputSlotImage.sprite = outputSprite;
                outputSlotImage.enabled = outputSprite != null;
            }
            if (outputSlotButton != null)
            {
                outputSlotButton.interactable = !isHolding && hasOutput;
                outputSlotButton.gameObject.SetActive(hasOutput);
            }

            if (unitButton != null)
                unitButton.interactable = isPowered;

            if (backToCylinderButton != null)
            {
                bool canReturn = CanReturnToCylinder();
                backToCylinderButton.gameObject.SetActive(canReturn && manualInventoryView);
                backToCylinderButton.interactable = canReturn;
            }
        }

        private void RefreshVolume()
        {
            if (cylinderController == null) return;

            if (volumeDisplayText != null)
                volumeDisplayText.text = cylinderController.GetDisplayString();
        }
    }
}
