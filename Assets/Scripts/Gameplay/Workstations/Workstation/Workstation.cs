using System;
using Gameplay.Items;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class Workstation : NetworkBehaviour
    {
        private const ushort NoneId = 0;
        public const int SlotCount = 5;

        [Header("Workstation Type")]
        [SerializeField] private WorkstationType workstationType;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Recipe")]
        [SerializeField] private Recipe assignedRecipe;

        [Header("Initial Contents (size 5)")]
        [SerializeField] private LabItem[] initialContents = new LabItem[SlotCount];

        public NetworkList<ushort> SlotItemIds { get; private set; }

        private NetworkVariable<WorkState> workState = new NetworkVariable<WorkState>(
            WorkState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> workProgress = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<ushort> outputSlotId = new NetworkVariable<ushort>(
            NoneId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public WorkstationType Type => workstationType;
        public WorkState CurrentWorkState => workState.Value;
        public float WorkProgress => workProgress.Value;
        public Recipe AssignedRecipe => assignedRecipe;
        public ushort OutputSlotId => outputSlotId.Value;
        public bool HasOutput => outputSlotId.Value != NoneId;

        public event Action OnWorkStateChanged;
        public event Action OnProgressChanged;
        public event Action OnInventoryChanged;
        public event Action OnOutputChanged;

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
            workState.OnValueChanged += HandleWorkStateChanged;
            workProgress.OnValueChanged += HandleProgressChanged;
            SlotItemIds.OnListChanged += HandleInventoryChanged;
            outputSlotId.OnValueChanged += HandleOutputChanged;

            if (IsServer)
            {
                EnsureSlotCountServer();
                InitializeFromEditorIfEmptyServer();
            }
        }

        public override void OnNetworkDespawn()
        {
            workState.OnValueChanged -= HandleWorkStateChanged;
            workProgress.OnValueChanged -= HandleProgressChanged;
            SlotItemIds.OnListChanged -= HandleInventoryChanged;
            outputSlotId.OnValueChanged -= HandleOutputChanged;
        }

        private void HandleWorkStateChanged(WorkState prev, WorkState next)
        {
            OnWorkStateChanged?.Invoke();
        }

        private void HandleProgressChanged(float prev, float next)
        {
            OnProgressChanged?.Invoke();
        }

        private void HandleInventoryChanged(NetworkListEvent<ushort> evt)
        {
            OnInventoryChanged?.Invoke();
        }

        private void HandleOutputChanged(ushort prev, ushort next)
        {
            OnOutputChanged?.Invoke();
        }

        #region Inventory

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
            if (items == null)
            {
                Debug.LogWarning($"GetSpriteById: items array is null, cannot find sprite for id {id}");
                return null;
            }

            for (int i = 0; i < items.Length; i++)
            {
                LabItem it = items[i];
                if (it != null && it.Id == id)
                    return it.Sprite;
            }

            Debug.LogWarning($"GetSpriteById: Could not find item with id {id} in items array (length={items.Length})");
            return null;
        }

        public LabItem GetLabItemById(ushort id)
        {
            if (id == NoneId) return null;
            if (items == null) return null;

            for (int i = 0; i < items.Length; i++)
            {
                LabItem it = items[i];
                if (it != null && it.Id == id)
                    return it;
            }

            return null;
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

        #endregion

        #region Output Slot

        public Sprite GetOutputSprite()
        {
            return GetSpriteById(outputSlotId.Value);
        }

        public void SetOutputServer(ushort itemId)
        {
            if (!IsServer) return;
            outputSlotId.Value = itemId;
        }

        public void ClearOutputServer()
        {
            if (!IsServer) return;
            outputSlotId.Value = NoneId;
        }

        #endregion

        #region Recipe & Progress

        public bool CanStartWork()
        {
            if (workState.Value != WorkState.Idle)
                return false;

            if (assignedRecipe == null)
                return false;

            ushort[] ids = new ushort[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                ids[i] = GetSlotId(i);

            return assignedRecipe.CanCraftWith(ids, SlotCount);
        }

        public float GetProgressNormalized()
        {
            return workProgress.Value;
        }

        public int GetProgressPercent()
        {
            return Mathf.RoundToInt(workProgress.Value * 100f);
        }

        public string GetProgressText()
        {
            return $"{GetProgressPercent()}%";
        }

        public string GetStateText()
        {
            return workState.Value switch
            {
                WorkState.Idle => "Idle",
                WorkState.Working => "Working...",
                WorkState.Completed => "Complete!",
                WorkState.Failed => "Failed",
                _ => "Unknown"
            };
        }

        #endregion

        #region Server State Control (called by individual workstation scripts)

        public void SetWorkStateServer(WorkState state)
        {
            if (!IsServer) return;
            workState.Value = state;
        }

        public void SetProgressServer(float progress)
        {
            if (!IsServer) return;
            workProgress.Value = Mathf.Clamp01(progress);
        }

        public void AddProgressServer(float delta)
        {
            if (!IsServer) return;
            workProgress.Value = Mathf.Clamp01(workProgress.Value + delta);
        }

        public void ClearInventoryServer()
        {
            if (!IsServer) return;
            for (int i = 0; i < SlotCount; i++)
                SlotItemIds[i] = NoneId;
        }

        public void CompleteWorkServer()
        {
            if (!IsServer) return;
            workState.Value = WorkState.Completed;
            workProgress.Value = 1f;
        }

        public void FailWorkServer()
        {
            if (!IsServer) return;
            workState.Value = WorkState.Failed;
        }

        public void ResetToIdleServer()
        {
            if (!IsServer) return;
            workState.Value = WorkState.Idle;
            workProgress.Value = 0f;
        }

        #endregion

        #region Player Interaction

        private PlayerCarry GetCarryForClient(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            if (client.PlayerObject == null) return null;
            return client.PlayerObject.GetComponent<PlayerCarry>();
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryTakeFromSlotServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workState.Value == WorkState.Working) return;

            EnsureSlotCountServer();

            if (slotIndex < 0 || slotIndex >= SlotCount) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            ushort slotId = SlotItemIds[slotIndex];
            if (slotId == NoneId)
                return;

            SlotItemIds[slotIndex] = NoneId;
            carry.SetHeldItemServer(slotId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryDepositHeldServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workState.Value == WorkState.Working) return;

            EnsureSlotCountServer();

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null)
                return;

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

        [ServerRpc(RequireOwnership = false)]
        public void CollectOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workState.Value != WorkState.Completed) return;
            if (assignedRecipe == null || assignedRecipe.OutputItem == null) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            carry.SetHeldItemServer(assignedRecipe.OutputItem.Id);

            ClearInventoryServer();
            ResetToIdleServer();
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryTakeOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (outputSlotId.Value == NoneId) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            carry.SetHeldItemServer(outputSlotId.Value);
            outputSlotId.Value = NoneId;
        }

        #endregion
    }
}
