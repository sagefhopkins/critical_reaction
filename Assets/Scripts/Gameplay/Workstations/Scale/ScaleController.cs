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
         * is commented out. Scale now auto-processes like other stations.
         * Restore for post-beta interactive measurement gameplay.
         * ────────────────────────────────────────────────────────────── */

        // [SerializeField] private Beaker sourceBeaker;
        // [SerializeField] private Beaker measurementBeaker;
        // [SerializeField] private LabItem[] items;
        // [SerializeField] private float emptyBeakerMass = 50f;
        // [SerializeField] private float scaleAccuracy = 0.01f;
        // [SerializeField] private float noiseAmount = 0.005f;
        // [SerializeField] private float updateInterval = 0.1f;

        // private NetworkVariable<bool> isPoweredOn = new(...);
        // private NetworkVariable<WeightUnit> currentUnit = new(...);
        // private NetworkVariable<float> tareOffset = new(...);
        // private NetworkVariable<float> displayedWeight = new(...);
        // private NetworkVariable<float> finalMeasuredMass = new(...);
        // private NetworkVariable<ushort> sourceChemicalId = new(...);
        // private NetworkVariable<int> remainingSourceParticles = new(...);

        // [ServerRpc] TogglePowerServerRpc, TareServerRpc, CycleUnitServerRpc
        // [ServerRpc] ConfirmAndPlaceOutputServerRpc
        // CheckMeasurement(), UpdateWeight(), CalculateRawMass()
        // ConsumeSourceChemical(), PlaceOutputInOutputSlot(), ResetScale()
        // UpdateSourceChemicalFromInventory(), ConfigureSourceBeakerFromChemical()
    }
}
