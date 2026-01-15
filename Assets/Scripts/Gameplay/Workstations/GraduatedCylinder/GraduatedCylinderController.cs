using System;
using Gameplay.Items;
using Gameplay.Workstations.Scale;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.GraduatedCylinder
{
    public class GraduatedCylinderController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Workstation workstation;
        [SerializeField] private FluidContainer sourceContainer;
        [SerializeField] private FluidContainer measurementCylinder;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Cylinder Properties")]
        [SerializeField] private float cylinderCapacity = 100f;
        [SerializeField] private float measurementAccuracy = 0.1f;
        [SerializeField] private float noiseAmount = 0.05f;
        [SerializeField] private float updateInterval = 0.1f;

        private NetworkVariable<bool> isPoweredOn = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<VolumeUnit> currentUnit = new NetworkVariable<VolumeUnit>(
            VolumeUnit.Milliliters,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> displayedVolume = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> finalMeasuredVolume = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<ushort> sourceChemicalId = new NetworkVariable<ushort>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> remainingSourceVolume = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float lastUpdateTime;
        private int sourceSlotIndex = -1;

        public event Action OnCylinderStateChanged;
        public event Action OnVolumeChanged;
        public event Action OnSourceChemicalChanged;

        public bool IsPoweredOn => isPoweredOn.Value;
        public VolumeUnit CurrentUnit => currentUnit.Value;
        public float DisplayedVolume => displayedVolume.Value;
        public float TargetVolume => GetTargetVolumeFromRecipe();
        public float Tolerance => GetToleranceFromRecipe();
        public VolumeUnit RequiredUnit => GetRequiredUnitFromRecipe();
        public FluidContainer MeasurementCylinder => measurementCylinder;
        public FluidContainer SourceContainer => sourceContainer;
        public float FinalMeasuredVolume => finalMeasuredVolume.Value;
        public ushort SourceChemicalId => sourceChemicalId.Value;
        public float RemainingSourceVolume => remainingSourceVolume.Value;

        private float GetTargetVolumeFromRecipe()
        {
            if (workstation != null && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe.TargetVolume;
            return 50f;
        }

        private float GetToleranceFromRecipe()
        {
            if (workstation != null && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe.VolumeTolerance;
            return 1f;
        }

        private VolumeUnit GetRequiredUnitFromRecipe()
        {
            if (workstation != null && workstation.AssignedRecipe != null)
                return workstation.AssignedRecipe.RequiredVolumeUnit;
            return VolumeUnit.Milliliters;
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
            displayedVolume.OnValueChanged += HandleVolumeChanged;
            sourceChemicalId.OnValueChanged += HandleSourceChemicalChanged;

            if (workstation != null)
            {
                workstation.OnInventoryChanged += HandleInventoryChanged;
            }

            if (sourceContainer != null)
            {
                sourceContainer.OnVolumeChanged += HandleSourceVolumeChanged;
            }

            if (measurementCylinder != null)
            {
                measurementCylinder.OnVolumeChanged += HandleMeasurementVolumeChanged;
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
            displayedVolume.OnValueChanged -= HandleVolumeChanged;
            sourceChemicalId.OnValueChanged -= HandleSourceChemicalChanged;

            if (workstation != null)
            {
                workstation.OnInventoryChanged -= HandleInventoryChanged;
            }

            if (sourceContainer != null)
            {
                sourceContainer.OnVolumeChanged -= HandleSourceVolumeChanged;
            }

            if (measurementCylinder != null)
            {
                measurementCylinder.OnVolumeChanged -= HandleMeasurementVolumeChanged;
            }
        }

        private void HandlePowerChanged(bool prev, bool next) => OnCylinderStateChanged?.Invoke();
        private void HandleUnitChanged(VolumeUnit prev, VolumeUnit next) => OnCylinderStateChanged?.Invoke();
        private void HandleVolumeChanged(float prev, float next) => OnVolumeChanged?.Invoke();
        private void HandleSourceChemicalChanged(ushort prev, ushort next) => OnSourceChemicalChanged?.Invoke();

        private void HandleSourceVolumeChanged()
        {
            if (!IsServer) return;
            if (sourceContainer != null)
            {
                remainingSourceVolume.Value = sourceContainer.CurrentVolume;
            }
        }

        private void HandleMeasurementVolumeChanged()
        {
            OnVolumeChanged?.Invoke();
        }

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
                UpdateVolume();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateVolume()
        {
            if (!IsServer) return;

            if (!isPoweredOn.Value)
            {
                displayedVolume.Value = 0f;
                return;
            }

            float rawVolume = measurementCylinder != null ? measurementCylinder.CurrentVolume : 0f;

            rawVolume += UnityEngine.Random.Range(-noiseAmount, noiseAmount);
            rawVolume = Mathf.Round(rawVolume / measurementAccuracy) * measurementAccuracy;
            rawVolume = Mathf.Max(0f, rawVolume);

            displayedVolume.Value = rawVolume;
        }

        public string GetDisplayString()
        {
            if (!isPoweredOn.Value)
                return "----";

            float displayValue = ConvertFromMilliliters(displayedVolume.Value, currentUnit.Value);
            string unitLabel = GetUnitLabel(currentUnit.Value);
            return $"{displayValue:F2} {unitLabel}";
        }

        public static float ConvertFromMilliliters(float milliliters, VolumeUnit unit)
        {
            return unit switch
            {
                VolumeUnit.Milliliters => milliliters,
                VolumeUnit.Liters => milliliters / 1000f,
                VolumeUnit.Microliters => milliliters * 1000f,
                _ => milliliters
            };
        }

        public static string GetUnitLabel(VolumeUnit unit)
        {
            return unit switch
            {
                VolumeUnit.Milliliters => "mL",
                VolumeUnit.Liters => "L",
                VolumeUnit.Microliters => "μL",
                _ => "mL"
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
                ConfigureSourceContainerFromChemical();
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

        private void ConfigureSourceContainerFromChemical()
        {
            if (sourceContainer == null) return;

            LabItem chemicalItem = GetLabItemById(sourceChemicalId.Value);

            if (chemicalItem != null && chemicalItem.IsChemical)
            {
                float initialVolume = remainingSourceVolume.Value > 0
                    ? remainingSourceVolume.Value
                    : chemicalItem.TotalParticleCount;

                if (IsServer)
                {
                    remainingSourceVolume.Value = initialVolume;
                }

                sourceContainer.ConfigureFromLabItem(chemicalItem, initialVolume);
            }
            else
            {
                if (IsServer)
                {
                    remainingSourceVolume.Value = 0f;
                }
                sourceContainer.ClearConfiguration();
            }
        }

        public void RefreshSourceContainerConfiguration()
        {
            if (IsServer)
            {
                UpdateSourceChemicalFromInventory();
            }

            LabItem chemicalItem = GetLabItemById(sourceChemicalId.Value);

            if (chemicalItem != null && chemicalItem.IsChemical && sourceContainer != null)
            {
                float volume = remainingSourceVolume.Value > 0
                    ? remainingSourceVolume.Value
                    : chemicalItem.TotalParticleCount;
                sourceContainer.ConfigureFromLabItem(chemicalItem, volume);
            }
        }

        #endregion

        #region Measurement

        public MeasurementResult CheckMeasurement()
        {
            if (!isPoweredOn.Value)
            {
                return new MeasurementResult(false, "The graduated cylinder is not turned on!");
            }

            if (sourceChemicalId.Value == 0)
            {
                return new MeasurementResult(false, "No chemical source in workstation!");
            }

            VolumeUnit reqUnit = RequiredUnit;
            if (currentUnit.Value != reqUnit)
            {
                return new MeasurementResult(false, $"Please measure in {GetUnitLabel(reqUnit)}");
            }

            float actualVolume = measurementCylinder != null ? measurementCylinder.CurrentVolume : 0f;
            float target = TargetVolume;
            float tol = Tolerance;
            float difference = Mathf.Abs(actualVolume - target);

            if (difference <= tol)
            {
                float accuracy = 1f - (difference / tol);
                return new MeasurementResult(true, "Perfect measurement!", accuracy);
            }
            else if (actualVolume < target)
            {
                return new MeasurementResult(false, $"Not enough! You need {target - actualVolume:F2}mL more.");
            }
            else
            {
                return new MeasurementResult(false, $"Too much! You have {actualVolume - target:F2}mL extra.");
            }
        }

        #endregion

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void TogglePowerServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            isPoweredOn.Value = !isPoweredOn.Value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CycleUnitServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (!isPoweredOn.Value) return;

            int unitCount = Enum.GetValues(typeof(VolumeUnit)).Length;
            currentUnit.Value = (VolumeUnit)(((int)currentUnit.Value + 1) % unitCount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ConfirmAndPlaceOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workstation == null) return;

            MeasurementResult result = CheckMeasurement();

            if (result.IsCorrect)
            {
                float measuredVolume = measurementCylinder != null ? measurementCylinder.CurrentVolume : 0f;
                finalMeasuredVolume.Value = measuredVolume;

                ConsumeSourceChemical();

                LabItem outputItem = workstation.AssignedRecipe?.OutputItem;
                if (outputItem != null)
                {
                    PlaceOutputInOutputSlot(outputItem.Id);
                }

                ResetCylinder();
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
            remainingSourceVolume.Value = 0f;

            if (sourceContainer != null)
            {
                sourceContainer.ClearConfiguration();
            }
        }

        private void PlaceOutputInOutputSlot(ushort outputItemId)
        {
            if (!IsServer) return;
            if (workstation == null) return;

            workstation.SetOutputServer(outputItemId);
        }

        private void ResetCylinder()
        {
            if (!IsServer) return;

            isPoweredOn.Value = false;
            currentUnit.Value = VolumeUnit.Milliliters;
            finalMeasuredVolume.Value = 0f;
            remainingSourceVolume.Value = 0f;

            if (measurementCylinder != null)
            {
                measurementCylinder.SetVolume(0f);
            }
        }

        #endregion
    }
}
