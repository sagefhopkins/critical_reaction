using System;
using Unity.Netcode;
using UnityEngine;

namespace UX.CoopMenu
{
    public struct LobbySlot : INetworkSerializable, IEquatable<LobbySlot>
    {
        public ulong OwnerClientId;
        public bool IsOccupied => OwnerClientId != ulong.MaxValue;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref OwnerClientId);
        }
        
        public bool Equals(LobbySlot other) => OwnerClientId == other.OwnerClientId;
    }

    public class LobbyManager : NetworkBehaviour
    {
        [SerializeField] private int slotCount = 4;
        public NetworkList<LobbySlot> Slots { get; private set; }

        private void Awake()
        {
            Slots = new NetworkList<LobbySlot>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (Slots.Count == 0)
                {
                    for(int i = 0; i < slotCount; i++)
                        Slots.Add(new LobbySlot { OwnerClientId = ulong.MaxValue });
                }

                NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            ReleaseSlotInternal(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestClaimSlotServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (Slots.Count == 0) return;
            
            slotIndex = Mathf.Clamp(slotIndex, 0, Slots.Count - 1);
            if (Slots[slotIndex].OwnerClientId != ulong.MaxValue) return;
            
            LobbySlot slot = Slots[slotIndex];
            slot.OwnerClientId = clientId;
            Slots[slotIndex] = slot;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestLeaveSlotServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            ReleaseSlotInternal(clientId);
        }

        private void ReleaseSlotInternal(ulong clientId)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].OwnerClientId == clientId) continue;
                
                LobbySlot slot = Slots[i];
                slot.OwnerClientId = ulong.MaxValue;
                Slots[i] = slot;
                return;
            }
        }

        public int GetLocalClientSlotIndex()
        {
            if (NetworkManager.Singleton == null) return -1;
            
            ulong localId = NetworkManager.Singleton.LocalClientId;

            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].OwnerClientId == localId)
                    return i;
            }

            return -1;
        }
    }
}
