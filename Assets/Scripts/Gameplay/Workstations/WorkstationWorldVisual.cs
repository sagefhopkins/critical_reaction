using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class WorkstationWorldVisual : NetworkBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Workstation workstation;

        [Header("Slot Renderers (size 5)")]
        [SerializeField] private SpriteRenderer[] slotRenderers;

        [Header("State Indicators")]
        [SerializeField] private SpriteRenderer workingIndicator;
        [SerializeField] private SpriteRenderer completedIndicator;
        [SerializeField] private SpriteRenderer failedIndicator;

        [Header("Output Display")]
        [SerializeField] private SpriteRenderer outputRenderer;

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        public override void OnNetworkSpawn()
        {
            if (workstation != null)
            {
                if (workstation.SlotItemIds != null)
                    workstation.SlotItemIds.OnListChanged += OnSlotsChanged;

                workstation.OnWorkStateChanged += OnWorkStateChanged;
            }

            RefreshAll();
        }

        public override void OnNetworkDespawn()
        {
            if (workstation != null)
            {
                if (workstation.SlotItemIds != null)
                    workstation.SlotItemIds.OnListChanged -= OnSlotsChanged;

                workstation.OnWorkStateChanged -= OnWorkStateChanged;
            }
        }

        private void OnSlotsChanged(NetworkListEvent<ushort> _)
        {
            RefreshSlots();
        }

        private void OnWorkStateChanged()
        {
            RefreshStateIndicators();
            RefreshOutput();
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshStateIndicators();
            RefreshOutput();
        }

        private void RefreshSlots()
        {
            if (workstation == null) return;

            for (int i = 0; i < Workstation.SlotCount; i++)
            {
                SpriteRenderer sr = (slotRenderers != null && i < slotRenderers.Length) ? slotRenderers[i] : null;
                if (sr == null) continue;

                Sprite spr = workstation.GetSpriteForSlot(i);
                sr.sprite = spr;
                sr.enabled = spr != null;
            }
        }

        private void RefreshStateIndicators()
        {
            if (workstation == null) return;

            WorkState state = workstation.CurrentWorkState;

            if (workingIndicator != null)
                workingIndicator.enabled = state == WorkState.Working;

            if (completedIndicator != null)
                completedIndicator.enabled = state == WorkState.Completed;

            if (failedIndicator != null)
                failedIndicator.enabled = state == WorkState.Failed;
        }

        private void RefreshOutput()
        {
            if (outputRenderer == null || workstation == null) return;

            if (workstation.CurrentWorkState == WorkState.Completed && workstation.AssignedRecipe != null)
            {
                var outputItem = workstation.AssignedRecipe.OutputItem;
                if (outputItem != null)
                {
                    outputRenderer.sprite = outputItem.Sprite;
                    outputRenderer.enabled = true;
                    return;
                }
            }

            outputRenderer.sprite = null;
            outputRenderer.enabled = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();

            if (Application.isPlaying) return;

            RefreshSlots();
        }
#endif
    }
}
