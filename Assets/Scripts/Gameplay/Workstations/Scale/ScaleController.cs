using System;
using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.Scale
{
    public class ScaleController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Workstation workstation;
        [SerializeField] private Beaker sourceBeaker;
        [SerializeField] private Beaker measurementBeaker;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Scale Properties")]
        [SerializeField] private float emptyBeakerMass = 50f;
        [SerializeField] private float scaleAccuracy = 0.01f;
        [SerializeField] private float noiseAmount = 0.005f;
        [SerializeField] private float updateInterval = 0.1f;

        private NetworkVariable<bool> isPoweredOn = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<WeightUnit> currentUnit = new NetworkVariable<WeightUnit>(
            WeightUnit.Grams,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> tareOffset = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> displayedWeight = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> finalMeasuredMass = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<ushort> sourceChemicalId = new NetworkVariable<ushort>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float lastUpdateTime;
        private int sourceSlotIndex = -1;

        public event Action OnScaleStateChanged;
        public event Action OnWeightChanged;
        public event Action OnSourceChemicalChanged;

        public bool IsPoweredOn => isPoweredOn.Value;
        public WeightUnit CurrentUnit => currentUnit.Value;
        public float DisplayedWeight => displayedWeight.Value;
        public float TargetMass => GetTargetMassFromRecipe();
        public float Tolerance => GetToleranceFromRecipe();
        public WeightUnit RequiredUnit => GetRequiredUnitFromRecipe();
        public Beaker MeasurementBeaker => measurementBeaker;
        public Beaker SourceBeaker => sourceBeaker;
        public float FinalMeasuredMass => finalMeasuredMass.Value;
        public ushort SourceChemicalId => sourceChemicalId.Value;

        private float GetTargetMassFromRecipe()
        {
            if (workstation != null && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe.TargetMass;
            return 10f;
        }

        private float GetToleranceFromRecipe()
        {
            if (workstation != null && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe.MassTolerance;
            return 0.5f;
        }

        private WeightUnit GetRequiredUnitFromRecipe()
        {
            if (workstation != null && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe.RequiredWeightUnit;
            return WeightUnit.Grams;
        }

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        public override void OnNetworkSpawn()
        {
            isPoweredOn.OnValueChanged += HandlePowerChanged;
            currentUnit.OnValueChanged += HandleUnitChanged;
            tareOffset.OnValueChanged += HandleTareChanged;
            displayedWeight.OnValueChanged += HandleWeightChanged;
            sourceChemicalId.OnValueChanged += HandleSourceChemicalChanged;

            if (workstation != null)
            {
                workstation.OnInventoryChanged += HandleInventoryChanged;
            }

            if (IsServer)
            {
                UpdateSourceChemicalFromInventory();
            }
        }

        public override void OnNetworkDespawn()
        {
            isPoweredOn.OnValueChanged -= HandlePowerChanged;
            currentUnit.OnValueChanged -= HandleUnitChanged;
            tareOffset.OnValueChanged -= HandleTareChanged;
            displayedWeight.OnValueChanged -= HandleWeightChanged;
            sourceChemicalId.OnValueChanged -= HandleSourceChemicalChanged;

            if (workstation != null)
            {
                workstation.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandlePowerChanged(bool prev, bool next) => OnScaleStateChanged?.Invoke();
        private void HandleUnitChanged(WeightUnit prev, WeightUnit next) => OnScaleStateChanged?.Invoke();
        private void HandleTareChanged(float prev, float next) => OnScaleStateChanged?.Invoke();
        private void HandleWeightChanged(float prev, float next) => OnWeightChanged?.Invoke();
        private void HandleSourceChemicalChanged(ushort prev, ushort next) => OnSourceChemicalChanged?.Invoke();

        private void HandleInventoryChanged()
        {
            if (IsServer)
            {
                UpdateSourceChemicalFromInventory();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateWeight();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateWeight()
        {
            if (!IsServer) return;

            if (!isPoweredOn.Value)
            {
                displayedWeight.Value = 0f;
                return;
            }

            float rawMass = CalculateRawMass();
            float adjustedMass = rawMass - tareOffset.Value;

            adjustedMass += UnityEngine.Random.Range(-noiseAmount, noiseAmount);
            adjustedMass = Mathf.Round(adjustedMass / scaleAccuracy) * scaleAccuracy;

            displayedWeight.Value = adjustedMass;
        }

        private float CalculateRawMass()
        {
            float beakerContentMass = measurementBeaker != null ? measurementBeaker.GetTotalMass() : 0f;
            return emptyBeakerMass + beakerContentMass;
        }

        public string GetDisplayString()
        {
            if (!isPoweredOn.Value)
                return "----";

            float displayValue = ConvertFromGrams(displayedWeight.Value, currentUnit.Value);
            string unitLabel = GetUnitLabel(currentUnit.Value);
            return $"{displayValue:F2} {unitLabel}";
        }

        public static float ConvertFromGrams(float grams, WeightUnit unit)
        {
            return unit switch
            {
                WeightUnit.Grams => grams,
                WeightUnit.Milligrams => grams * 1000f,
                WeightUnit.Kilograms => grams / 1000f,
                WeightUnit.Ounces => grams * 0.035274f,
                _ => grams
            };
        }

        public static string GetUnitLabel(WeightUnit unit)
        {
            return unit switch
            {
                WeightUnit.Grams => "g",
                WeightUnit.Milligrams => "mg",
                WeightUnit.Kilograms => "kg",
                WeightUnit.Ounces => "oz",
                _ => "g"
            };
        }

        public LabItem GetLabItemById(ushort id)
        {
            if (id == 0) return null;

            if (items != null && items.Length > 0)
            {
                foreach (var item in items)
                {
                    if (item != null && item.Id == id)
                        return item;
                }
            }

            if (workstation != null)
            {
                LabItem workstationItem = workstation.GetLabItemById(id);
                if (workstationItem != null)
                    return workstationItem;
            }

            return null;
        }

        public LabItem GetSourceChemicalItem()
        {
            return GetLabItemById(sourceChemicalId.Value);
        }

        #region Inventory Integration

        private void UpdateSourceChemicalFromInventory()
        {
            if (!IsServer || workstation == null) return;

            ushort foundChemicalId = 0;
            sourceSlotIndex = -1;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                ushort slotId = workstation.GetSlotId(i);

                if (slotId == 0)
                    continue;

                LabItem item = GetLabItemById(slotId);

                if (item == null)
                    continue;

                if (!item.IsChemical)
                    continue;

                if (!IsValidIngredientForRecipe(item))
                    continue;

                foundChemicalId = slotId;
                sourceSlotIndex = i;
                break;
            }

            if (sourceChemicalId.Value != foundChemicalId)
            {
                sourceChemicalId.Value = foundChemicalId;
                ConfigureSourceBeakerFromChemical();
            }
        }

        private bool IsValidIngredientForRecipe(LabItem item)
        {
            if (workstation == null || workstation.AssignedRecipe == null)
                return true;

            var ingredients = workstation.AssignedRecipe.Ingredients;
            if (ingredients == null || ingredients.Length == 0)
                return true;

            foreach (var ingredient in ingredients)
            {
                if (ingredient != null && ingredient.Id == item.Id)
                    return true;
            }

            return false;
        }

        private void ConfigureSourceBeakerFromChemical()
        {
            if (sourceBeaker == null) return;

            LabItem chemicalItem = GetLabItemById(sourceChemicalId.Value);

            if (chemicalItem != null && chemicalItem.IsChemical)
            {
                sourceBeaker.ConfigureFromLabItem(chemicalItem, chemicalItem.TotalParticleCount);
            }
            else
            {
                sourceBeaker.ClearConfiguration();
            }
        }

        public void RefreshSourceBeakerConfiguration()
        {
            if (IsServer)
            {
                UpdateSourceChemicalFromInventory();
            }

            LabItem chemicalItem = GetLabItemById(sourceChemicalId.Value);

            if (chemicalItem != null && chemicalItem.IsChemical && sourceBeaker != null)
            {
                sourceBeaker.ConfigureFromLabItem(chemicalItem, chemicalItem.TotalParticleCount);
            }
        }

        #endregion

        #region Measurement

        public MeasurementResult CheckMeasurement()
        {
            if (!isPoweredOn.Value)
            {
                return new MeasurementResult(false, "The scale is not turned on!");
            }

            if (sourceChemicalId.Value == 0)
            {
                return new MeasurementResult(false, "No chemical source in workstation!");
            }

            bool properlyTared = Mathf.Abs(tareOffset.Value - emptyBeakerMass) < 1f;
            if (!properlyTared)
            {
                return new MeasurementResult(false, "Did you remember to tare the scale with the empty beaker?");
            }

            WeightUnit reqUnit = RequiredUnit;
            if (currentUnit.Value != reqUnit)
            {
                return new MeasurementResult(false, $"Please measure in {GetUnitLabel(reqUnit)}");
            }

            float actualMass = measurementBeaker != null ? measurementBeaker.GetTotalMass() : 0f;
            float target = TargetMass;
            float tol = Tolerance;
            float difference = Mathf.Abs(actualMass - target);

            if (difference <= tol)
            {
                float accuracy = 1f - (difference / tol);
                return new MeasurementResult(true, "Perfect measurement!", accuracy);
            }
            else if (actualMass < target)
            {
                return new MeasurementResult(false, $"Not enough! You need {target - actualMass:F2}g more.");
            }
            else
            {
                return new MeasurementResult(false, $"Too much! You have {actualMass - target:F2}g extra.");
            }
        }

        #endregion

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void TogglePowerServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            isPoweredOn.Value = !isPoweredOn.Value;

            if (!isPoweredOn.Value)
            {
                tareOffset.Value = 0f;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TareServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (!isPoweredOn.Value) return;

            tareOffset.Value = CalculateRawMass();
        }

        [ServerRpc(RequireOwnership = false)]
        public void CycleUnitServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (!isPoweredOn.Value) return;

            int unitCount = Enum.GetValues(typeof(WeightUnit)).Length;
            currentUnit.Value = (WeightUnit)(((int)currentUnit.Value + 1) % unitCount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ConfirmAndPlaceOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workstation == null) return;

            MeasurementResult result = CheckMeasurement();

            if (result.IsCorrect)
            {
                float measuredMass = measurementBeaker != null ? measurementBeaker.GetTotalMass() : 0f;
                finalMeasuredMass.Value = measuredMass;

                ConsumeSourceChemical();

                LabItem outputItem = workstation.AssignedRecipe?.OutputItem;
                if (outputItem != null)
                {
                    PlaceOutputInOutputSlot(outputItem.Id);
                }

                ResetScale();
            }
        }

        #endregion

        #region Server Helpers

        private void ConsumeSourceChemical()
        {
            if (!IsServer) return;
            if (workstation == null || sourceSlotIndex < 0) return;

            workstation.SlotItemIds[sourceSlotIndex] = 0;

            sourceChemicalId.Value = 0;
            sourceSlotIndex = -1;

            if (sourceBeaker != null)
            {
                sourceBeaker.ClearConfiguration();
            }
        }

        private void PlaceOutputInOutputSlot(ushort outputItemId)
        {
            if (!IsServer) return;
            if (workstation == null) return;

            workstation.SetOutputServer(outputItemId);
        }

        private void ResetScale()
        {
            if (!IsServer) return;

            isPoweredOn.Value = false;
            tareOffset.Value = 0f;
            currentUnit.Value = WeightUnit.Grams;
            finalMeasuredMass.Value = 0f;

            if (measurementBeaker != null)
            {
                measurementBeaker.SetParticleCount(0);
            }
        }

        #endregion
    }
}
