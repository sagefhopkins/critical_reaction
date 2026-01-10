using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class StorageRackWorldVisual : NetworkBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private StorageRack rack;

        [Header("Slot Renderers (size 9, row-major)")]
        [SerializeField] private SpriteRenderer[] slotRenderers;

        private void Awake()
        {
            if (rack == null)
                rack = GetComponent<StorageRack>();
        }

        public override void OnNetworkSpawn()
        {
            if (rack != null && rack.SlotItemIds != null)
                rack.SlotItemIds.OnListChanged += OnSlotsChanged;

            RefreshAll();
        }

        public override void OnNetworkDespawn()
        {
            if (rack != null && rack.SlotItemIds != null)
                rack.SlotItemIds.OnListChanged -= OnSlotsChanged;
        }

        private void OnSlotsChanged(NetworkListEvent<ushort> _)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (rack == null) return;

            for (int i = 0; i < StorageRack.SlotCount; i++)
            {
                SpriteRenderer sr = (slotRenderers != null && i < slotRenderers.Length) ? slotRenderers[i] : null;
                if (sr == null) continue;

                Sprite spr = rack.GetSpriteForSlot(i);
                sr.sprite = spr;
                sr.enabled = spr != null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rack == null)
                rack = GetComponent<StorageRack>();

            if (Application.isPlaying) return;

            RefreshAll();
        }
#endif
    }
}