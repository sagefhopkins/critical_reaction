using Gameplay.Items;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerCarry : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        [Header("Item List")] [SerializeField] 
        private LabItem[] items;
    
        [Header("Held Visuals")] [SerializeField] 
        private SpriteRenderer heldRenderer;
    
        private NetworkVariable<ushort> heldItemId = new NetworkVariable<ushort>(
            NoneId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private void Awake()
        {
            if (heldRenderer == null)
                heldRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        public override void OnNetworkSpawn()
        {
            heldItemId.OnValueChanged += OnHeldChanged;
            ApplyHeldVisual(heldItemId.Value);
        }

        public override void OnNetworkDespawn()
        {
            heldItemId.OnValueChanged -= OnHeldChanged;
        }

        private void OnHeldChanged(ushort prev, ushort next)
        {
            ApplyHeldVisual(next);
        }

        private void ApplyHeldVisual(ushort id)
        {
            if (heldRenderer == null) return;
            if (id == NoneId)
            {
                heldRenderer.sprite = null;
                heldRenderer.enabled = false;
                return;
            }

            Sprite spr = GetSpriteById(id);
            heldRenderer.sprite = spr;
            heldRenderer.enabled = spr != null;
        }

        private Sprite GetSpriteById(ushort id)
        {
            if (id == NoneId) return null;
            if (items == null) return null;


            for (int i = 0; i < items.Length; i++)
            {
                LabItem it = items[i];
                if (it != null && it.Id == id)
                    return it.Sprite;
            }

            return null;
        }

        public void TryPickupFromRack(StorageRack rack, int slotIndex)
        {
            if (!IsOwner) return;
            if (rack == null) return;
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.IsListening) return;
        
            rack.TryTakeFromSlotServerRpc(slotIndex, NetworkManager.Singleton.LocalClientId);
        }

        public void TryPlaceIntoRack(StorageRack rack, int slotIndex)
        {
            if (!IsOwner) return;
            if (rack == null) return;
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.IsListening) return;
        
            rack.TryPlaceIntoSlotServerRpc(slotIndex, NetworkManager.Singleton.LocalClientId);
        }

        public bool IsHoldingServer()
        {
            if (!IsServer) return false;
            return heldItemId.Value != NoneId;
        }

        public ushort GetHeldItemIdServer()
        {
            if (!IsServer) return NoneId;
            return heldItemId.Value;
        }
    
        public void SetHeldItemServer(ushort id)
        {
            if (!IsServer) return;
            heldItemId.Value = id;
        }

        public void ClearHeldItemServer()
        {
            if (!IsServer) return;
            heldItemId.Value = NoneId;
        }
    }
}
