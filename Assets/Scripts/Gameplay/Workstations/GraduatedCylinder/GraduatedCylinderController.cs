using System;
using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.GraduatedCylinder
{
    public class GraduatedCylinderController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Workstation workstation;

        [Header("Settings")]
        [SerializeField] private float workDuration = 5f;

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        private void Update()
        {
            if (!IsServer) return;
            if (workstation == null) return;
            if (workstation.CurrentWorkState != WorkState.Working) return;

            float duration = workstation.AssignedRecipe != null
                ? workstation.AssignedRecipe.WorkDuration
                : workDuration;

            float delta = Time.deltaTime / duration;
            workstation.AddProgressServer(delta);

            if (workstation.WorkProgress >= 1f)
            {
                workstation.CompleteWorkServer();
            }
        }

        /* ──────────────────────────────────────────────────────────────
         * BETA SIMPLIFICATION: All interactive measurement logic below
         * is commented out. Graduated cylinder now auto-processes like
         * other stations. Restore for post-beta interactive gameplay.
         * ────────────────────────────────────────────────────────────── */

        // [SerializeField] private FluidContainer sourceContainer;
        // [SerializeField] private FluidContainer measurementCylinder;
        // [SerializeField] private LabItem[] items;
        // [SerializeField] private float cylinderCapacity = 100f;
        // [SerializeField] private float measurementAccuracy = 0.1f;
        // [SerializeField] private float noiseAmount = 0.05f;
        // [SerializeField] private float updateInterval = 0.1f;

        // private NetworkVariable<bool> isPoweredOn = new(...);
        // private NetworkVariable<VolumeUnit> currentUnit = new(...);
        // private NetworkVariable<float> displayedVolume = new(...);
        // private NetworkVariable<float> finalMeasuredVolume = new(...);
        // private NetworkVariable<ushort> sourceChemicalId = new(...);
        // private NetworkVariable<float> remainingSourceVolume = new(...);

        // [ServerRpc] TogglePowerServerRpc, CycleUnitServerRpc
        // [ServerRpc] ConfirmAndPlaceOutputServerRpc
        // CheckMeasurement(), UpdateVolume()
        // ConsumeSourceChemical(), PlaceOutputInOutputSlot(), ResetCylinder()
        // UpdateSourceChemicalFromInventory(), ConfigureSourceContainerFromChemical()
    }
}
