using System;
using System.Collections.Generic;
using Gameplay.Items;
using Gameplay.Workstations.GraduatedCylinder;
using Gameplay.Workstations.Scale;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public class LiquidMeasurementController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Workstation workstation;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Bench Properties")]
        [SerializeField] private float outputCapacity = 500f;
        [SerializeField] private float measurementAccuracy = 0.1f;
        [SerializeField] private float noiseAmount = 0.05f;
        [SerializeField] private float updateInterval = 0.1f;

        [Header("Source Volumes")]
        [SerializeField] private float defaultSourceVolume = 100f;

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

        private NetworkVariable<float> displayedPH = new NetworkVariable<float>(
            7f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkList<ChemicalVolume> outputContents;

        private float lastUpdateTime;

        public event Action OnBenchStateChanged;
        public event Action OnVolumeChanged;
        public event Action OnPHChanged;
        public event Action OnOutputContentsChanged;
        public event Action OnSourcesChanged;

        public bool IsPoweredOn => isPoweredOn.Value;
        public VolumeUnit CurrentUnit => currentUnit.Value;
        public float DisplayedVolume => displayedVolume.Value;
        public float DisplayedPH => displayedPH.Value;
        public float OutputCapacity => outputCapacity;
        public float DefaultSourceVolume => defaultSourceVolume;
        public IReadOnlyList<ChemicalVolume> OutputContents => outputContents as IReadOnlyList<ChemicalVolume>;

        private void Awake()
        {
            outputContents = new NetworkList<ChemicalVolume>();

            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        public override void OnNetworkSpawn()
        {
            isPoweredOn.OnValueChanged += HandlePowerChanged;
            currentUnit.OnValueChanged += HandleUnitChanged;
            displayedVolume.OnValueChanged += HandleVolumeChanged;
            displayedPH.OnValueChanged += HandlePHChanged;
            outputContents.OnListChanged += HandleOutputContentsChanged;

            if (workstation != null)
            {
                workstation.OnInventoryChanged += HandleInventoryChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            isPoweredOn.OnValueChanged -= HandlePowerChanged;
            currentUnit.OnValueChanged -= HandleUnitChanged;
            displayedVolume.OnValueChanged -= HandleVolumeChanged;
            displayedPH.OnValueChanged -= HandlePHChanged;
            outputContents.OnListChanged -= HandleOutputContentsChanged;

            if (workstation != null)
            {
                workstation.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandlePowerChanged(bool prev, bool next) => OnBenchStateChanged?.Invoke();
        private void HandleUnitChanged(VolumeUnit prev, VolumeUnit next) => OnBenchStateChanged?.Invoke();
        private void HandleVolumeChanged(float prev, float next) => OnVolumeChanged?.Invoke();
        private void HandlePHChanged(float prev, float next) => OnPHChanged?.Invoke();
        private void HandleOutputContentsChanged(NetworkListEvent<ChemicalVolume> changeEvent) => OnOutputContentsChanged?.Invoke();

        private void HandleInventoryChanged()
        {
            OnSourcesChanged?.Invoke();
        }

        private void Update()
        {
            if (!IsServer) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDisplayValues();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateDisplayValues()
        {
            if (!IsServer) return;

            if (!isPoweredOn.Value)
            {
                displayedVolume.Value = 0f;
                displayedPH.Value = 7f;
                return;
            }

            float totalVolume = GetTotalOutputVolume();

            totalVolume += UnityEngine.Random.Range(-noiseAmount, noiseAmount);
            totalVolume = Mathf.Round(totalVolume / measurementAccuracy) * measurementAccuracy;
            totalVolume = Mathf.Max(0f, totalVolume);

            displayedVolume.Value = totalVolume;

            float ph = CalculatePH();
            displayedPH.Value = Mathf.Round(ph * 10f) / 10f;
        }

        private float GetTotalOutputVolume()
        {
            float total = 0f;
            foreach (var cv in outputContents)
            {
                total += cv.Volume;
            }
            return total;
        }

        private float CalculatePH()
        {
            float totalWeightedPH = 0f;
            float totalVolume = 0f;

            foreach (var cv in outputContents)
            {
                var data = GetChemicalDataById(cv.ChemicalId);
                if (data != null)
                {
                    totalWeightedPH += data.PHValue * cv.Volume;
                    totalVolume += cv.Volume;
                }
            }

            return totalVolume > 0f ? totalWeightedPH / totalVolume : 7f;
        }

        #region Source Management

        public IEnumerable<(int slotIndex, ushort chemicalId, LabItem item)> GetSourcesFromInventory()
        {
            if (workstation == null) yield break;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                ushort slotId = workstation.GetSlotId(i);
                if (slotId == 0) continue;

                LabItem item = GetLabItemById(slotId);
                if (item == null || !item.IsChemical) continue;

                yield return (i, slotId, item);
            }
        }

        #endregion

        #region Display Helpers

        public string GetVolumeDisplayString()
        {
            if (!isPoweredOn.Value)
                return "----";

            float displayValue = ConvertFromMilliliters(displayedVolume.Value, currentUnit.Value);
            string unitLabel = GetUnitLabel(currentUnit.Value);
            return $"{displayValue:F1} {unitLabel}";
        }

        public string GetPHDisplayString()
        {
            if (!isPoweredOn.Value)
                return "----";

            return $"pH {displayedPH.Value:F1}";
        }

        public static float ConvertFromMilliliters(float mL, VolumeUnit unit)
        {
            return unit switch
            {
                VolumeUnit.Milliliters => mL,
                VolumeUnit.Liters => mL / 1000f,
                VolumeUnit.Microliters => mL * 1000f,
                _ => mL
            };
        }

        public static string GetUnitLabel(VolumeUnit unit)
        {
            return unit switch
            {
                VolumeUnit.Milliliters => "mL",
                VolumeUnit.Liters => "L",
                VolumeUnit.Microliters => "uL",
                _ => "mL"
            };
        }

        #endregion

        #region Item Lookups

        public LabItem GetLabItemById(ushort id)
        {
            if (id == 0) return null;

            if (items != null)
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

        public ChemicalParticleData GetChemicalDataById(ushort id)
        {
            LabItem item = GetLabItemById(id);
            return item?.ChemicalParticleData;
        }

        #endregion

        #region Concentration & Recipe Validation

        public float GetConcentration(ushort chemicalId)
        {
            float chemicalVolume = 0f;
            float totalVolume = 0f;

            foreach (var cv in outputContents)
            {
                totalVolume += cv.Volume;
                if (cv.ChemicalId == chemicalId)
                {
                    chemicalVolume += cv.Volume;
                }
            }

            return totalVolume > 0f ? chemicalVolume / totalVolume : 0f;
        }

        public float GetOutputVolume(ushort chemicalId)
        {
            foreach (var cv in outputContents)
            {
                if (cv.ChemicalId == chemicalId)
                {
                    return cv.Volume;
                }
            }
            return 0f;
        }

        public MeasurementResult CheckConcentration()
        {
            if (!isPoweredOn.Value)
            {
                return new MeasurementResult(false, "The bench is not turned on!");
            }

            if (outputContents.Count == 0)
            {
                return new MeasurementResult(false, "No liquids in the measurement container!");
            }

            if (workstation == null || workstation.AssignedRecipe == null)
            {
                return new MeasurementResult(false, "No recipe assigned!");
            }

            var recipe = workstation.AssignedRecipe;

            if (recipe.ConcentrationRequirements != null)
            {
                foreach (var req in recipe.ConcentrationRequirements)
                {
                    if (req.Chemical == null) continue;

                    float actualConc = GetConcentration(req.Chemical.Id);
                    float diff = Mathf.Abs(actualConc - req.TargetConcentration);

                    if (diff > req.Tolerance)
                    {
                        string percentActual = (actualConc * 100f).ToString("F1");
                        string percentTarget = (req.TargetConcentration * 100f).ToString("F1");
                        return new MeasurementResult(false,
                            $"{req.Chemical.DisplayName}: {percentActual}% (need {percentTarget}%)");
                    }
                }
            }

            if (recipe.RequiresPHCheck)
            {
                float currentPH = CalculatePH();
                float phDiff = Mathf.Abs(currentPH - recipe.PHTarget);

                if (phDiff > recipe.PHTolerance)
                {
                    return new MeasurementResult(false,
                        $"pH is {currentPH:F1} (need {recipe.PHTarget:F1} +/- {recipe.PHTolerance:F1})");
                }
            }

            return new MeasurementResult(true, "Perfect mixture!", 1f);
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
        public void PourToOutputServerRpc(ushort chemicalId, float volume, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (chemicalId == 0 || volume <= 0f) return;

            float totalVolume = GetTotalOutputVolume();
            float remaining = outputCapacity - totalVolume;
            float actualVolume = Mathf.Min(volume, remaining);

            if (actualVolume <= 0f) return;

            int existingIndex = -1;
            for (int i = 0; i < outputContents.Count; i++)
            {
                if (outputContents[i].ChemicalId == chemicalId)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                var existing = outputContents[existingIndex];
                outputContents[existingIndex] = new ChemicalVolume(chemicalId, existing.Volume + actualVolume);
            }
            else
            {
                outputContents.Add(new ChemicalVolume(chemicalId, actualVolume));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            outputContents.Clear();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ConfirmAndPlaceOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workstation == null) return;

            MeasurementResult result = CheckConcentration();

            if (result.IsCorrect)
            {
                ConsumeSourceChemicals();

                LabItem outputItem = workstation.AssignedRecipe?.OutputItem;
                if (outputItem != null)
                {
                    workstation.SetOutputServer(outputItem.Id);
                }

                ResetBench();
            }
        }

        #endregion

        #region Server Helpers

        private void ConsumeSourceChemicals()
        {
            if (!IsServer) return;
            if (workstation == null) return;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                ushort slotId = workstation.GetSlotId(i);
                if (slotId == 0) continue;

                LabItem item = GetLabItemById(slotId);
                if (item == null || !item.IsChemical) continue;

                workstation.SlotItemIds[i] = 0;
            }
        }

        private void ResetBench()
        {
            if (!IsServer) return;

            isPoweredOn.Value = false;
            currentUnit.Value = VolumeUnit.Milliliters;
            outputContents.Clear();
        }

        #endregion
    }
}
