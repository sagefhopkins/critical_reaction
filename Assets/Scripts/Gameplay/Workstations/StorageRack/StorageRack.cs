using Gameplay.Items;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class StorageRack : NetworkBehaviour
    {
        private const ushort NoneId = 0;
        public const int SlotCount = 9;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Initial Contents (3x3, size 9)")]
        [SerializeField] private LabItem[] initialContents = new LabItem[SlotCount];

        [Header("Behaviour")]
        [SerializeField] private bool infiniteSupply;

        public NetworkList<ushort> SlotItemIds { get; private set; }

        private void Awake()
        {
            SlotItemIds = new NetworkList<ushort>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EnsureSlotCountServer();
                InitializeFromEditorIfEmptyServer();
            }
        }

        private void EnsureSlotCountServer()
        {
            if (!IsServer) return;

            if (SlotItemIds.Count == 0)
            {
                for (int i = 0; i < SlotCount; i++)
                    SlotItemIds.Add(NoneId);
                return;
            }

            while (SlotItemIds.Count < SlotCount)
                SlotItemIds.Add(NoneId);

            while (SlotItemIds.Count > SlotCount)
                SlotItemIds.RemoveAt(SlotItemIds.Count - 1);
        }

        private void InitializeFromEditorIfEmptyServer()
        {
            if (!IsServer) return;

            bool any = false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotItemIds[i] != NoneId)
                {
                    any = true;
                    break;
                }
            }

            if (any) return;

            for (int i = 0; i < SlotCount; i++)
            {
                LabItem it = (initialContents != null && i < initialContents.Length) ? initialContents[i] : null;
                SlotItemIds[i] = it != null ? it.Id : NoneId;
            }
        }

        public ushort GetSlotId(int slotIndex)
        {
            if (SlotItemIds == null) return NoneId;
            if (slotIndex < 0 || slotIndex >= SlotCount) return NoneId;
            if (SlotItemIds.Count <= slotIndex) return NoneId;

            return SlotItemIds[slotIndex];
        }

        public Sprite GetSpriteForSlot(int slotIndex)
        {
            return GetSpriteById(GetSlotId(slotIndex));
        }

        public Sprite GetSpriteForItemId(ushort id)
        {
            return GetSpriteById(id);
        }

        public bool HasEmptySlotClient()
        {
            if (SlotItemIds == null) return false;
            if (SlotItemIds.Count < SlotCount) return true;

            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotItemIds[i] == NoneId)
                    return true;
            }

            return false;
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

        private PlayerCarry GetCarryForClient(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            if (client.PlayerObject == null) return null;
            return client.PlayerObject.GetComponent<PlayerCarry>();
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryTakeFromSlotServerRpc(int slotIndex, ulong requestingClientId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            EnsureSlotCountServer();

            if (slotIndex < 0 || slotIndex >= SlotCount) return;

            if (rpcParams.Receive.SenderClientId != requestingClientId)
                requestingClientId = rpcParams.Receive.SenderClientId;

            PlayerCarry carry = GetCarryForClient(requestingClientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            ushort slotId = SlotItemIds[slotIndex];
            if (slotId == NoneId)
                return;

            if (!infiniteSupply)
                SlotItemIds[slotIndex] = NoneId;
            carry.SetHeldItemServer(slotId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryDepositHeldServerRpc(ulong requestingClientId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (infiniteSupply) return;
            EnsureSlotCountServer();

            if (rpcParams.Receive.SenderClientId != requestingClientId)
                requestingClientId = rpcParams.Receive.SenderClientId;

            PlayerCarry carry = GetCarryForClient(requestingClientId);
            if (carry == null) return;

            if (!carry.IsHoldingServer())
                return;

            ushort held = carry.GetHeldItemIdServer();
            if (held == NoneId)
                return;

            int empty = FindFirstEmptySlotServer();
            if (empty < 0)
                return;

            SlotItemIds[empty] = held;
            carry.ClearHeldItemServer();
        }

        private int FindFirstEmptySlotServer()
        {
            if (!IsServer) return -1;

            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotItemIds[i] == NoneId)
                    return i;
            }

            return -1;
        }
    }
}
