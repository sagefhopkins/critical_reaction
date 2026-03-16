using System;
using Gameplay.Items;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerCarry : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Held Visuals")]
        [SerializeField] private SpriteRenderer heldRenderer;
        [SerializeField] private Sprite mopSprite;

        private NetworkVariable<ushort> heldItemId = new NetworkVariable<ushort>(
            NoneId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action HeldItemChanged;

        public bool IsHoldingLocal => heldItemId.Value != NoneId;
        public ushort HeldItemIdLocal => heldItemId.Value;

        private void Awake()
        {
            if (heldRenderer == null)
                heldRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private PlayerController playerController;

        public override void OnNetworkSpawn()
        {
            playerController = GetComponent<PlayerController>();

            heldItemId.OnValueChanged += OnHeldChanged;

            if (playerController != null)
                playerController.OnHasMopChanged += OnMopChanged;

            RefreshVisual();
        }

        public override void OnNetworkDespawn()
        {
            heldItemId.OnValueChanged -= OnHeldChanged;

            if (playerController != null)
                playerController.OnHasMopChanged -= OnMopChanged;
        }

        private void OnHeldChanged(ushort prev, ushort next)
        {
            RefreshVisual();
            HeldItemChanged?.Invoke();
        }

        private void OnMopChanged(bool hasMop)
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (heldRenderer == null) return;

            if (playerController != null && playerController.HasMop)
            {
                heldRenderer.sprite = mopSprite;
                heldRenderer.enabled = mopSprite != null;
                return;
            }

            if (heldItemId.Value == NoneId)
            {
                heldRenderer.sprite = null;
                heldRenderer.enabled = false;
                return;
            }

            Sprite spr = GetSpriteById(heldItemId.Value);
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

        public void TryDepositToRack(StorageRack rack)
        {
            if (!IsOwner) return;
            if (rack == null) return;
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.IsListening) return;

            rack.TryDepositHeldServerRpc(NetworkManager.Singleton.LocalClientId);
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
