using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations.Scale
{
    public class ScaleMenu : WorkstationMenuBase
    {
        [Header("Screen Roots")]
        [SerializeField] private GameObject scaleScreenRoot;
        [SerializeField] private GameObject inventoryScreenRoot;

        [Header("Output Slot")]
        [SerializeField] private Button outputSlotButton;
        [SerializeField] private Image outputSlotImage;

        [Header("Inventory Slots")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Button depositButton;

        [Header("Scale References")]
        [SerializeField] private ScoopController scoop;
        [SerializeField] private BeakerTriggerZone sourceZone;
        [SerializeField] private BeakerTriggerZone targetZone;

        [Header("Scale Controls")]
        [SerializeField] private Button powerButton;
        [SerializeField] private Button tareButton;
        [SerializeField] private Button unitButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inventoryButton;

        [Header("Inventory Controls")]
        [SerializeField] private Button backToScaleButton;

        [Header("Display")]
        [SerializeField] private TMP_Text weightDisplayText;

        private ScaleController scaleController;
        private Beaker sourceBeaker;
        private Beaker measurementBeaker;
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

            if (tareButton != null)
                tareButton.onClick.AddListener(OnClickTare);

            if (unitButton != null)
                unitButton.onClick.AddListener(OnClickUnit);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnClickClose);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnClickInventory);

            if (backToScaleButton != null)
                backToScaleButton.onClick.AddListener(OnClickBackToScale);
        }

        protected override void OnOpened()
        {
            manualInventoryView = false;
            scaleController = workstation.GetComponent<ScaleController>();

            if (workstation != null)
            {
                workstation.OnOutputChanged += OnOutputChanged;
            }

            if (scaleController != null)
            {
                sourceBeaker = scaleController.SourceBeaker;
                measurementBeaker = scaleController.MeasurementBeaker;

                scaleController.OnScaleStateChanged += RefreshButtons;
                scaleController.OnWeightChanged += RefreshWeight;
                scaleController.OnSourceChemicalChanged += OnSourceChemicalChanged;

                if (measurementBeaker != null)
                {
                    measurementBeaker.OnParticleCountChanged += RefreshWeight;
                }

                if (sourceBeaker != null)
                {
                    sourceBeaker.OnParticleCountChanged += RefreshAll;
                    sourceBeaker.OnParticleDataChanged += RefreshAll;
                }

                scaleController.RefreshSourceBeakerConfiguration();
            }

            if (scoop != null && sourceBeaker != null)
            {
                scoop.SetSourceBeaker(sourceBeaker);
            }

            if (sourceZone != null && sourceBeaker != null)
            {
                sourceZone.gameObject.SetActive(sourceBeaker.IsConfigured);
            }

            UpdateActiveScreen();
            RefreshAll();
        }

        protected override void OnClosed()
        {
            if (scaleScreenRoot != null)
                scaleScreenRoot.SetActive(false);
            if (inventoryScreenRoot != null)
                inventoryScreenRoot.SetActive(false);

            if (workstation != null)
            {
                workstation.OnOutputChanged -= OnOutputChanged;
            }

            if (scaleController != null)
            {
                scaleController.OnScaleStateChanged -= RefreshButtons;
                scaleController.OnWeightChanged -= RefreshWeight;
                scaleController.OnSourceChemicalChanged -= OnSourceChemicalChanged;

                if (measurementBeaker != null)
                {
                    measurementBeaker.OnParticleCountChanged -= RefreshWeight;
                }

                if (sourceBeaker != null)
                {
                    sourceBeaker.OnParticleCountChanged -= RefreshAll;
                    sourceBeaker.OnParticleDataChanged -= RefreshAll;
                }
            }

            if (scoop != null && scoop.gameObject.activeInHierarchy)
            {
                scoop.ClearAllHeldParticles();
                scoop.ResetToOriginalPosition();
            }

            scaleController = null;
            sourceBeaker = null;
            measurementBeaker = null;
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

            if (scaleScreenRoot != null)
                scaleScreenRoot.SetActive(!showInventory);
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

        private bool CanReturnToScale()
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
            if (scaleController != null)
            {
                scaleController.RefreshSourceBeakerConfiguration();
            }

            if (sourceZone != null && sourceBeaker != null)
            {
                sourceZone.gameObject.SetActive(sourceBeaker.IsConfigured);
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
            if (scaleController == null) return;
            scaleController.TogglePowerServerRpc();
        }

        private void OnClickTare()
        {
            if (scaleController == null) return;
            scaleController.TareServerRpc();
        }

        private void OnClickUnit()
        {
            if (scaleController == null) return;
            scaleController.CycleUnitServerRpc();
        }

        private void OnClickClose()
        {
            if (scaleController != null)
            {
                MeasurementResult result = scaleController.CheckMeasurement();
                if (result.IsCorrect)
                {
                    scaleController.ConfirmAndPlaceOutputServerRpc();
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

        private void OnClickBackToScale()
        {
            if (!CanReturnToScale())
                return;

            manualInventoryView = false;
            UpdateActiveScreen();
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshButtons();
            RefreshWeight();
        }

        private void RefreshButtons()
        {
            if (workstation == null)
                return;

            bool isHolding = localCarry != null && localCarry.IsHoldingLocal;
            bool hasEmptySlot = workstation.HasEmptySlotClient();
            bool isPowered = scaleController != null && scaleController.IsPoweredOn;

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

            if (tareButton != null)
                tareButton.interactable = isPowered;

            if (unitButton != null)
                unitButton.interactable = isPowered;

            if (backToScaleButton != null)
            {
                bool canReturn = CanReturnToScale();
                backToScaleButton.gameObject.SetActive(canReturn && manualInventoryView);
                backToScaleButton.interactable = canReturn;
            }
        }

        private void RefreshWeight()
        {
            if (scaleController == null) return;

            if (weightDisplayText != null)
                weightDisplayText.text = scaleController.GetDisplayString();
        }
    }
}
