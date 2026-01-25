using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public class LiquidMeasurementMenu : WorkstationMenuBase
    {
        [Header("Screen Roots")]
        [SerializeField] private GameObject benchScreenRoot;
        [SerializeField] private GameObject inventoryScreenRoot;

        [Header("Source Containers (UI elements in menu prefab)")]
        [SerializeField] private LiquidSourceSlot[] sourceSlots;

        [Header("Target Container (UI elements in menu prefab)")]
        [SerializeField] private LiquidTargetZone targetZone;
        [SerializeField] private Image outputFillImage;

        [Header("Pour Controller (UI element in menu prefab)")]
        [SerializeField] private ClickPourController pourController;
        [SerializeField] private Canvas menuCanvas;

        [Header("Display")]
        [SerializeField] private TMP_Text volumeDisplayText;
        [SerializeField] private TMP_Text phDisplayText;
        [SerializeField] private TMP_Text pourRateText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Bench Controls")]
        [SerializeField] private Button powerButton;
        [SerializeField] private Button unitButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inventoryButton;

        [Header("Inventory Controls")]
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Button depositButton;
        [SerializeField] private Button backToBenchButton;

        [Header("Output Slot")]
        [SerializeField] private Button outputSlotButton;
        [SerializeField] private Image outputSlotImage;

        private LiquidMeasurementController controller;
        private bool manualInventoryView;

        private void Awake()
        {
            // Wire up inventory slot buttons
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

            // Wire up bench control buttons
            if (powerButton != null)
                powerButton.onClick.AddListener(OnClickPower);

            if (unitButton != null)
                unitButton.onClick.AddListener(OnClickUnit);

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClickClear);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnClickConfirm);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnClickClose);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnClickInventory);

            if (backToBenchButton != null)
                backToBenchButton.onClick.AddListener(OnClickBackToBench);

            // Find canvas if not assigned
            if (menuCanvas == null)
                menuCanvas = GetComponentInParent<Canvas>();
        }

        protected override void OnOpened()
        {
            manualInventoryView = false;

            // Get controller from the workstation (discovered at runtime)
            controller = workstation.GetComponent<LiquidMeasurementController>();

            if (workstation != null)
            {
                workstation.OnOutputChanged += OnOutputChanged;
            }

            if (controller != null)
            {
                controller.OnBenchStateChanged += RefreshButtons;
                controller.OnVolumeChanged += RefreshDisplay;
                controller.OnPHChanged += RefreshDisplay;
                controller.OnOutputContentsChanged += RefreshOutputVisual;
                controller.OnSourcesChanged += RefreshSourceSlots;
            }

            // Initialize pour controller with runtime references
            if (pourController != null)
            {
                pourController.Initialize(controller, menuCanvas);
                pourController.OnPourRateChanged += OnPourRateChanged;
                pourController.OnStateChanged += OnPourStateChanged;
            }

            // Initialize target zone
            if (targetZone != null)
            {
                targetZone.Initialize(pourController);
            }

            // Initialize source slots with pour controller reference
            if (sourceSlots != null)
            {
                foreach (var slot in sourceSlots)
                {
                    if (slot != null)
                    {
                        slot.Initialize(pourController);
                    }
                }
            }

            // Configure source slots from controller's inventory data
            RefreshSourceSlots();

            UpdateActiveScreen();
            RefreshAll();
        }

        protected override void OnClosed()
        {
            if (benchScreenRoot != null)
                benchScreenRoot.SetActive(false);
            if (inventoryScreenRoot != null)
                inventoryScreenRoot.SetActive(false);

            if (workstation != null)
            {
                workstation.OnOutputChanged -= OnOutputChanged;
            }

            if (controller != null)
            {
                controller.OnBenchStateChanged -= RefreshButtons;
                controller.OnVolumeChanged -= RefreshDisplay;
                controller.OnPHChanged -= RefreshDisplay;
                controller.OnOutputContentsChanged -= RefreshOutputVisual;
                controller.OnSourcesChanged -= RefreshSourceSlots;
            }

            if (pourController != null)
            {
                pourController.OnPourRateChanged -= OnPourRateChanged;
                pourController.OnStateChanged -= OnPourStateChanged;
                pourController.Cleanup();
            }

            // Clear source slot configurations
            if (sourceSlots != null)
            {
                foreach (var slot in sourceSlots)
                {
                    if (slot != null)
                    {
                        slot.ClearConfiguration();
                    }
                }
            }

            controller = null;
        }

        protected override void OnWorkStateChanged()
        {
            RefreshAll();
        }

        protected override void OnInventoryChanged()
        {
            UpdateActiveScreen();
            RefreshSourceSlots();
            RefreshAll();
        }

        protected override void OnHeldItemChanged()
        {
            UpdateActiveScreen();
            RefreshAll();
        }

        private void RefreshSourceSlots()
        {
            if (controller == null || sourceSlots == null) return;

            // Clear all slots first
            foreach (var slot in sourceSlots)
            {
                if (slot != null)
                    slot.ClearConfiguration();
            }

            // Configure slots from controller's inventory
            int slotIndex = 0;
            foreach (var (inventorySlot, chemicalId, labItem) in controller.GetSourcesFromInventory())
            {
                if (slotIndex >= sourceSlots.Length) break;

                var sourceSlot = sourceSlots[slotIndex];
                if (sourceSlot != null && labItem != null)
                {
                    sourceSlot.Configure(labItem, controller.DefaultSourceVolume);
                }

                slotIndex++;
            }
        }

        private void RefreshOutputVisual()
        {
            if (controller == null) return;

            // Update fill image based on controller's output contents
            if (outputFillImage != null)
            {
                float totalVolume = 0f;
                foreach (var cv in controller.OutputContents)
                {
                    totalVolume += cv.Volume;
                }

                float fillPercent = controller.OutputCapacity > 0f
                    ? totalVolume / controller.OutputCapacity
                    : 0f;
                outputFillImage.fillAmount = fillPercent;

                // Blend colors based on contents
                if (totalVolume > 0f)
                {
                    Color blendedColor = CalculateBlendedColor();
                    outputFillImage.color = blendedColor;
                }
            }

            RefreshDisplay();
        }

        private Color CalculateBlendedColor()
        {
            if (controller == null) return Color.cyan;

            float totalVolume = 0f;
            float r = 0f, g = 0f, b = 0f, a = 0f;

            foreach (var cv in controller.OutputContents)
            {
                var data = controller.GetChemicalDataById(cv.ChemicalId);
                if (data != null)
                {
                    Color c = data.ParticleColor;
                    r += c.r * cv.Volume;
                    g += c.g * cv.Volume;
                    b += c.b * cv.Volume;
                    a += c.a * cv.Volume;
                    totalVolume += cv.Volume;
                }
            }

            if (totalVolume > 0f)
            {
                return new Color(r / totalVolume, g / totalVolume, b / totalVolume, a / totalVolume);
            }

            return Color.cyan;
        }

        private void UpdateActiveScreen()
        {
            bool showInventory = ShouldShowInventoryScreen();

            if (benchScreenRoot != null)
                benchScreenRoot.SetActive(!showInventory);
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

        private bool CanReturnToBench()
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

        private void OnPourRateChanged(float rate)
        {
            RefreshPourRate();
        }

        private void OnPourStateChanged(ClickPourController.PourState state)
        {
            RefreshPourRate();
            RefreshButtons();
        }

        #region Button Handlers

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
            if (controller == null) return;
            controller.TogglePowerServerRpc();
        }

        private void OnClickUnit()
        {
            if (controller == null) return;
            controller.CycleUnitServerRpc();
        }

        private void OnClickClear()
        {
            if (controller == null) return;
            controller.ClearOutputServerRpc();

            // Also reset source slots to full
            RefreshSourceSlots();
        }

        private void OnClickConfirm()
        {
            if (controller == null) return;

            var result = controller.CheckConcentration();
            if (result.IsCorrect)
            {
                controller.ConfirmAndPlaceOutputServerRpc();
                UpdateActiveScreen();
                RefreshAll();
            }
            else
            {
                ShowFeedback(result.Feedback);
            }
        }

        private void OnClickClose()
        {
            RequestClose();
        }

        private void OnClickInventory()
        {
            manualInventoryView = true;
            UpdateActiveScreen();
            RefreshAll();
        }

        private void OnClickBackToBench()
        {
            if (!CanReturnToBench())
                return;

            manualInventoryView = false;
            UpdateActiveScreen();
            RefreshAll();
        }

        #endregion

        #region Refresh Methods

        private void RefreshAll()
        {
            RefreshButtons();
            RefreshDisplay();
            RefreshPourRate();
            RefreshInventorySlots();
            RefreshOutputVisual();
        }

        private void RefreshButtons()
        {
            if (workstation == null) return;

            bool isHolding = localCarry != null && localCarry.IsHoldingLocal;
            bool hasEmptySlot = workstation.HasEmptySlotClient();
            bool isPowered = controller != null && controller.IsPoweredOn;
            bool isPouring = pourController != null && pourController.IsPouring;

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

            if (clearButton != null)
                clearButton.interactable = isPowered && !isPouring;

            if (confirmButton != null)
                confirmButton.interactable = isPowered && !isPouring;

            if (backToBenchButton != null)
            {
                bool canReturn = CanReturnToBench();
                backToBenchButton.gameObject.SetActive(canReturn && manualInventoryView);
                backToBenchButton.interactable = canReturn;
            }
        }

        private void RefreshDisplay()
        {
            if (controller == null) return;

            if (volumeDisplayText != null)
                volumeDisplayText.text = controller.GetVolumeDisplayString();

            if (phDisplayText != null)
                phDisplayText.text = controller.GetPHDisplayString();
        }

        private void RefreshPourRate()
        {
            if (pourRateText == null) return;

            if (pourController != null && (pourController.IsHolding || pourController.IsPouring))
            {
                pourRateText.gameObject.SetActive(true);
                pourRateText.text = $"Pour Rate: {pourController.CurrentPourRate:F1} mL/s";
            }
            else
            {
                pourRateText.gameObject.SetActive(false);
            }
        }

        private void RefreshInventorySlots()
        {
            if (workstation == null) return;

            bool isHolding = localCarry != null && localCarry.IsHoldingLocal;

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
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.gameObject.SetActive(true);

                CancelInvoke(nameof(HideFeedback));
                Invoke(nameof(HideFeedback), 3f);
            }
        }

        private void HideFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
