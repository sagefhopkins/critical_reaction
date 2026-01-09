using System;
using Gameplay.Items;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class StorageRack : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        private struct SlotState : INetworkSerializable, IEquatable<SlotState>
        {
            public ushort ItemId;
        
            public SlotState(ushort itemId)
            {
                ItemId = itemId;
            }
        
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ItemId);
            }
        
            public bool Equals(SlotState other) => ItemId == other.ItemId;
        }

        private struct RackState : INetworkSerializable, IEquatable<RackState>
        {
            public SlotState S0;
            public SlotState S1;
            public SlotState S2;
            public SlotState S3;
            public SlotState S4;
            public SlotState S5;
            public SlotState S6;
            public SlotState S7;
            public SlotState S8;

            public SlotState Get(int index)
            {
                switch (index)
                {
                    case 0: return S0;
                    case 1: return S1;
                    case 2: return S2;
                    case 3: return S3;
                    case 4: return S4;
                    case 5: return S5;
                    case 6: return S6;
                    case 7: return S7;
                    case 8: return S8;
                    default: return new SlotState(NoneId);
                }
            }

            public void Set(int index, SlotState value)
            {
                switch (index)
                {
                    case 0: S0 = value; break;
                    case 1: S1 = value; break;
                    case 2: S2 = value; break;
                    case 3: S3 = value; break;
                    case 4: S4 = value; break;
                    case 5: S5 = value; break;
                    case 6: S6 = value; break;
                    case 7: S7 = value; break;
                    case 8: S8 = value; break;
                }
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref S0);
                serializer.SerializeValue(ref S1);
                serializer.SerializeValue(ref S2);
                serializer.SerializeValue(ref S3);
                serializer.SerializeValue(ref S4);
                serializer.SerializeValue(ref S5);
                serializer.SerializeValue(ref S6);
                serializer.SerializeValue(ref S7);
                serializer.SerializeValue(ref S8);
            }

            public bool Equals(RackState other)
            {
                return S0.Equals(other.S0) && 
                       S1.Equals(other.S1) && 
                       S2.Equals(other.S2) && 
                       S3.Equals(other.S3) && 
                       S4.Equals(other.S4) && 
                       S5.Equals(other.S5) && 
                       S6.Equals(other.S6) && 
                       S7.Equals(other.S7) && 
                       S8.Equals(other.S8);
            }
        }
    
    
        [Header("Item List")] [SerializeField] 
        private LabItem[] items;

        [Header("Rack visuals")] [SerializeField]
        private SpriteRenderer[] slotRenderers;

        [Header("Initial fill")] [SerializeField]
        private LabItem[] initialItems;

        private NetworkVariable<RackState> _rack = new NetworkVariable<RackState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private void Awake()
        {
            if (slotRenderers == null || slotRenderers.Length != 9)
                slotRenderers = new SpriteRenderer[9];
        }

        public override void OnNetworkSpawn()
        {
            _rack.OnValueChanged += OnRackChanged;

            if (IsServer)
            {
                RackState s = new RackState();

                for (int i = 0; i < 9; i++)
                {
                    ushort id = NoneId;
                    
                    if(initialItems != null && i < initialItems.Length && initialItems[i] != null)
                        id = initialItems[i].Id;
                    
                    s.Set(i, new SlotState(id));
                }

                _rack.Value = s;
            }

            ApplyVisuals(_rack.Value);
        }

        public override void OnNetworkDespawn()
        {
            _rack.OnValueChanged -= OnRackChanged;
        }

        private void OnRackChanged(RackState prev, RackState next)
        {
            ApplyVisuals(next);
        }

        private void ApplyVisuals(RackState s)
        {
            if (slotRenderers == null) return;

            for (int i = 0; i < 9; i++)
            {
                SpriteRenderer r = (i >= 0 && i < slotRenderers.Length) ? slotRenderers[i] : null;
                if (r == null) continue;

                ushort id = s.Get(i).ItemId;

                if (id == NoneId)
                {
                    r.sprite = null;
                    r.enabled = false;
                    continue;
                }

                Sprite spr = GetSpriteById(id);
                r.sprite = spr;
                r.enabled = spr != null;
            }
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

        [ServerRpc(RequireOwnership = false)]
        public void TryTakeFromSlotServerRpc(int slotIndex, ulong requestingClientId,
            ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (slotIndex < 0 || slotIndex >= 9) return;

            RackState s = _rack.Value;
            ushort id = s.Get(slotIndex).ItemId;
            if (id == NoneId) return;
            
            PlayerCarry carry = FindCarryForClient(requestingClientId);
            if (carry == null) return;
            if (carry.IsHoldingServer()) return;

            s.Set(slotIndex, new SlotState(id));
            _rack.Value = s;

            carry.SetHeldItemServer(id);

        }

        [ServerRpc(RequireOwnership = false)]
        public void TryPlaceIntoSlotServerRpc(int slotIndex, ulong requestingClientId,
            ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (slotIndex < 0 || slotIndex >= 9) return;
            
            
            RackState s = _rack.Value;
            if (s.Get(slotIndex).ItemId != NoneId) return;
            
            PlayerCarry carry = FindCarryForClient(requestingClientId);
            if (carry == null) return;

            ushort held = carry.GetHeldItemIdServer();
            if (held == NoneId) return;
            
            s.Set(slotIndex, new SlotState(held));
            _rack.Value = s;

            carry.ClearHeldItemServer();
        }

        private PlayerCarry FindCarryForClient(ulong requestingClientId)
        {
            PlayerCarry[] carries = FindObjectsByType<PlayerCarry>(FindObjectsSortMode.None);
            for (int i = 0; i < carries.Length; i++)
            {
                NetworkObject n = carries[i].NetworkObject;
                if (n != null && n.OwnerClientId == requestingClientId)
                    return carries[i];
            }

            return null;
        }
    }
}
