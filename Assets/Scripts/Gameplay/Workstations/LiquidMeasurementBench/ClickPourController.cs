using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public class ClickPourController : MonoBehaviour
    {
        public enum PourState
        {
            Idle,
            Holding,
            Pouring
        }

        [Header("Held Container Visual")]
        [SerializeField] private RectTransform heldContainerVisual;
        [SerializeField] private Image heldContainerImage;
        [SerializeField] private Image heldLiquidFillImage;

        [Header("Pour Settings")]
        [SerializeField] private float defaultPourRate = 10f;
        [SerializeField] private float minPourRate = 2f;
        [SerializeField] private float maxPourRate = 30f;
        [SerializeField] private float scrollSensitivity = 2f;

        [Header("Visual Settings")]
        [SerializeField] private Vector2 cursorOffset = new Vector2(30f, -30f);

        private Canvas canvas;
        private PourState currentState = PourState.Idle;
        private float currentPourRate;

        private LiquidSourceSlot heldSourceSlot;
        private ushort heldChemicalId;
        private float heldSourceVolume;
        private float heldMaxVolume;
        private Color heldLiquidColor;

        private LiquidTargetZone currentTarget;
        private bool isOverTarget;

        // Reference to controller for ServerRpc calls
        private LiquidMeasurementController controller;

        public event Action<PourState> OnStateChanged;
        public event Action<float> OnPourRateChanged;
        public event Action<float> OnVolumeChanged;

        public PourState CurrentState => currentState;
        public float CurrentPourRate => currentPourRate;
        public bool IsHolding => currentState == PourState.Holding;
        public bool IsPouring => currentState == PourState.Pouring;

        private void Awake()
        {
            currentPourRate = defaultPourRate;

            if (heldContainerVisual != null)
            {
                heldContainerVisual.gameObject.SetActive(false);
            }
        }

        public void Initialize(LiquidMeasurementController ctrl, Canvas parentCanvas)
        {
            controller = ctrl;
            canvas = parentCanvas;
        }

        public void Cleanup()
        {
            CancelHold();
            controller = null;
            canvas = null;
        }

        private void Update()
        {
            if (currentState == PourState.Holding)
            {
                FollowMouse();
                HandleScrollInput();
                HandleClickInput();
            }
            else if (currentState == PourState.Pouring)
            {
                FollowMouse();
                HandleScrollInput();
                HandleClickInput();
                ContinuePouring();
            }
        }

        private void FollowMouse()
        {
            if (heldContainerVisual == null || canvas == null) return;

            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out mousePos
            );

            heldContainerVisual.anchoredPosition = mousePos + cursorOffset;
        }

        private void HandleScrollInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentPourRate += scroll * scrollSensitivity * 10f;
                currentPourRate = Mathf.Clamp(currentPourRate, minPourRate, maxPourRate);
                OnPourRateChanged?.Invoke(currentPourRate);
            }
        }

        private void HandleClickInput()
        {
            if (Input.GetMouseButtonDown(1))
            {
                CancelHold();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI()) return;

                if (currentState == PourState.Holding)
                {
                    if (isOverTarget && currentTarget != null)
                    {
                        StartPouring();
                    }
                    else
                    {
                        CancelHold();
                    }
                }
                else if (currentState == PourState.Pouring)
                {
                    StopPouring();
                }
            }
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void StartPouring()
        {
            if (currentTarget == null) return;

            SetState(PourState.Pouring);
        }

        private void StopPouring()
        {
            SetState(PourState.Holding);
        }

        private void ContinuePouring()
        {
            if (currentTarget == null || heldSourceSlot == null || controller == null)
            {
                StopPouring();
                return;
            }

            if (heldSourceVolume <= 0f)
            {
                ReturnSource();
                return;
            }

            float amountToPour = currentPourRate * Time.deltaTime;
            amountToPour = Mathf.Min(amountToPour, heldSourceVolume);

            if (amountToPour > 0f)
            {
                // Call ServerRpc to pour - the controller handles capacity checks
                controller.PourToOutputServerRpc(heldChemicalId, amountToPour);

                heldSourceVolume -= amountToPour;
                UpdateHeldVisual();
                OnVolumeChanged?.Invoke(heldSourceVolume);
            }

            if (heldSourceVolume <= 0f)
            {
                ReturnSource();
            }
        }

        public void PickUpSource(LiquidSourceSlot sourceSlot, ushort chemicalId, float volume, float maxVolume, Color liquidColor)
        {
            if (currentState != PourState.Idle) return;
            if (volume <= 0f) return;

            heldSourceSlot = sourceSlot;
            heldChemicalId = chemicalId;
            heldSourceVolume = volume;
            heldMaxVolume = maxVolume;
            heldLiquidColor = liquidColor;
            currentPourRate = defaultPourRate;

            if (heldContainerVisual != null)
            {
                heldContainerVisual.gameObject.SetActive(true);
            }

            UpdateHeldVisual();
            SetState(PourState.Holding);
        }

        private void UpdateHeldVisual()
        {
            if (heldLiquidFillImage != null)
            {
                float fillPercent = heldMaxVolume > 0f ? heldSourceVolume / heldMaxVolume : 0f;
                heldLiquidFillImage.fillAmount = fillPercent;
                heldLiquidFillImage.color = heldLiquidColor;
            }
        }

        public void CancelHold()
        {
            ReturnSource();
        }

        private void ReturnSource()
        {
            if (heldSourceSlot != null)
            {
                heldSourceSlot.ReturnVolume(heldSourceVolume);
            }

            heldSourceSlot = null;
            heldChemicalId = 0;
            heldSourceVolume = 0f;
            heldMaxVolume = 0f;

            if (heldContainerVisual != null)
            {
                heldContainerVisual.gameObject.SetActive(false);
            }

            SetState(PourState.Idle);
        }

        public void SetOverTarget(LiquidTargetZone target)
        {
            currentTarget = target;
            isOverTarget = target != null;
        }

        public void ClearTarget()
        {
            if (currentState == PourState.Pouring)
            {
                StopPouring();
            }

            currentTarget = null;
            isOverTarget = false;
        }

        private void SetState(PourState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(currentState);
            }
        }

        public void SetPourRate(float rate)
        {
            currentPourRate = Mathf.Clamp(rate, minPourRate, maxPourRate);
            OnPourRateChanged?.Invoke(currentPourRate);
        }

        public void ResetPourRate()
        {
            currentPourRate = defaultPourRate;
            OnPourRateChanged?.Invoke(currentPourRate);
        }
    }
}
