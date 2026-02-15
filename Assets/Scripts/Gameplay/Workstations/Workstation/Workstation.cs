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

        [Header("Recipes")]
        [SerializeField] private Recipe[] recipes;

        [Header("Initial Contents (size 5)")]
        [SerializeField] private LabItem[] initialContents = new LabItem[SlotCount];

        [Header("Completion Grace Period")]
        [Tooltip("Seconds after completion before output degrades to Failed. 0 = no timeout.")]
        [SerializeField] private float completionGracePeriod = 0f;

        private float completedAtTime = -1f;

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

        public NetworkList<ushort> OutputSlotIds { get; private set; }

        private Recipe matchedRecipe;

        public WorkstationType Type => workstationType;
        public WorkState CurrentWorkState => workState.Value;
        public float WorkProgress => workProgress.Value;
        public Recipe AssignedRecipe => matchedRecipe;
        public Recipe[] Recipes => recipes;

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
            OutputSlotIds = new NetworkList<ushort>(
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
            OutputSlotIds.OnListChanged += HandleOutputChanged;

            if (IsServer)
            {
                workState.Value = WorkState.Idle;
                workProgress.Value = 0f;
                matchedRecipe = null;
                completedAtTime = -1f;
                SlotItemIds.Clear();
                OutputSlotIds.Clear();
                EnsureSlotCountServer();
                EnsureOutputSlotCountServer();
                InitializeFromEditorIfEmptyServer();
            }
        }

        public override void OnNetworkDespawn()
        {
            workState.OnValueChanged -= HandleWorkStateChanged;
            workProgress.OnValueChanged -= HandleProgressChanged;
            SlotItemIds.OnListChanged -= HandleInventoryChanged;
            OutputSlotIds.OnListChanged -= HandleOutputChanged;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (completionGracePeriod <= 0f) return;
            if (workState.Value != WorkState.Completed) return;
            if (completedAtTime < 0f) return;

            if (Time.time - completedAtTime >= completionGracePeriod)
            {
                FailWorkServer();
                completedAtTime = -1f;
            }
        }

        private void HandleWorkStateChanged(WorkState prev, WorkState next)
        {
            if (IsServer)
            {
                if (next == WorkState.Completed)
                    completedAtTime = Time.time;
                else
                    completedAtTime = -1f;
            }

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

        private void HandleOutputChanged(NetworkListEvent<ushort> evt)
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

        public bool CanAcceptItemClient(ushort itemId)
        {
            if (!HasEmptySlotClient()) return false;
            if (recipes == null || recipes.Length == 0) return false;

            ushort[] currentIds = new ushort[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                currentIds[i] = GetSlotId(i);

            for (int r = 0; r < recipes.Length; r++)
            {
                if (recipes[r] != null && recipes[r].IsValidIngredient(itemId, currentIds, SlotCount))
                    return true;
            }

            return false;
        }

        public bool HasOccupiedSlotClient()
        {
            if (SlotItemIds == null) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (i < SlotItemIds.Count && SlotItemIds[i] != NoneId)
                    return true;
            }

            return false;
        }

        private Sprite GetSpriteById(ushort id)
        {
            if (id == NoneId) return null;

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    LabItem it = items[i];
                    if (it != null && it.Id == id)
                        return it.Sprite;
                }
            }

            if (recipes != null)
            {
                for (int r = 0; r < recipes.Length; r++)
                {
                    if (recipes[r] != null && recipes[r].OutputItem != null
                        && recipes[r].OutputItem.Id == id)
                        return recipes[r].OutputItem.Sprite;
                }
            }

            Debug.LogWarning($"GetSpriteById: Could not find item with id {id} in items or recipe outputs");
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

        #region Output Slots

        private void EnsureOutputSlotCountServer()
        {
            if (!IsServer) return;

            if (OutputSlotIds.Count == 0)
            {
                for (int i = 0; i < SlotCount; i++)
                    OutputSlotIds.Add(NoneId);
                return;
            }

            while (OutputSlotIds.Count < SlotCount)
                OutputSlotIds.Add(NoneId);

            while (OutputSlotIds.Count > SlotCount)
                OutputSlotIds.RemoveAt(OutputSlotIds.Count - 1);
        }

        public ushort GetOutputSlotId(int slotIndex)
        {
            if (OutputSlotIds == null) return NoneId;
            if (slotIndex < 0 || slotIndex >= SlotCount) return NoneId;
            if (OutputSlotIds.Count <= slotIndex) return NoneId;
            return OutputSlotIds[slotIndex];
        }

        public Sprite GetOutputSpriteForSlot(int slotIndex)
        {
            return GetSpriteById(GetOutputSlotId(slotIndex));
        }

        public bool HasAnyOutput()
        {
            if (OutputSlotIds == null) return false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (i < OutputSlotIds.Count && OutputSlotIds[i] != NoneId)
                    return true;
            }
            return false;
        }

        public bool HasEmptyOutputSlotServer()
        {
            if (OutputSlotIds == null) return false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (OutputSlotIds[i] == NoneId)
                    return true;
            }
            return false;
        }

        public void SetOutputServer(int slotIndex, ushort itemId)
        {
            if (!IsServer) return;
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            EnsureOutputSlotCountServer();
            OutputSlotIds[slotIndex] = itemId;
        }

        public int AddOutputServer(ushort itemId)
        {
            if (!IsServer) return -1;
            EnsureOutputSlotCountServer();
            for (int i = 0; i < SlotCount; i++)
            {
                if (OutputSlotIds[i] == NoneId)
                {
                    OutputSlotIds[i] = itemId;
                    return i;
                }
            }
            return -1;
        }

        public void ClearOutputServer()
        {
            if (!IsServer) return;
            for (int i = 0; i < SlotCount; i++)
                OutputSlotIds[i] = NoneId;
        }

        #endregion

        #region Recipe & Progress

        public bool CanStartWork()
        {
            if (workState.Value != WorkState.Idle)
                return false;

            if (recipes == null || recipes.Length == 0)
                return false;

            ushort[] ids = new ushort[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                ids[i] = GetSlotId(i);

            for (int r = 0; r < recipes.Length; r++)
            {
                if (recipes[r] != null && recipes[r].CanCraftWith(ids, SlotCount))
                {
                    matchedRecipe = recipes[r];
                    return true;
                }
            }

            return false;
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

            if (matchedRecipe != null && matchedRecipe.OutputItem != null)
            {
                ClearInventoryServer();
                int qty = Mathf.Max(1, matchedRecipe.OutputQuantity);
                for (int i = 0; i < qty && i < SlotCount; i++)
                    AddOutputServer(matchedRecipe.OutputItem.Id);
            }

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
            matchedRecipe = null;
            workState.Value = WorkState.Idle;
            workProgress.Value = 0f;
        }

        public void FullResetServer()
        {
            if (!IsServer) return;
            matchedRecipe = null;
            completedAtTime = -1f;
            workState.Value = WorkState.Idle;
            workProgress.Value = 0f;
            ClearInventoryServer();
            ClearOutputServer();
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
            if (workState.Value == WorkState.Working || workState.Value == WorkState.Completed) return;

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

            if (recipes != null && recipes.Length > 0)
            {
                ushort[] currentIds = new ushort[SlotCount];
                for (int i = 0; i < SlotCount; i++)
                    currentIds[i] = GetSlotId(i);

                bool valid = false;
                for (int r = 0; r < recipes.Length; r++)
                {
                    if (recipes[r] != null && recipes[r].IsValidIngredient(held, currentIds, SlotCount))
                    {
                        valid = true;
                        break;
                    }
                }

                if (!valid) return;
            }

            int empty = FindFirstEmptySlotServer();
            if (empty < 0)
                return;

            SlotItemIds[empty] = held;
            carry.ClearHeldItemServer();

            AutoStartIfReady();
        }

        public void AutoStartIfReady()
        {
            if (!IsServer) return;
            if (!CanStartWork()) return;

            SetWorkStateServer(WorkState.Working);
            SetProgressServer(0f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryTakeFirstOccupiedSlotServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workState.Value == WorkState.Working) return;

            EnsureSlotCountServer();

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotItemIds[i] != NoneId)
                {
                    ushort slotId = SlotItemIds[i];
                    SlotItemIds[i] = NoneId;
                    carry.SetHeldItemServer(slotId);

                    if (workState.Value == WorkState.Failed && !HasAnyItemServer())
                        ResetToIdleServer();

                    return;
                }
            }
        }

        private bool HasAnyItemServer()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotItemIds[i] != NoneId)
                    return true;
            }
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CollectOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workState.Value != WorkState.Completed) return;
            if (!HasAnyOutput()) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (OutputSlotIds[i] != NoneId)
                {
                    carry.SetHeldItemServer(OutputSlotIds[i]);
                    OutputSlotIds[i] = NoneId;

                    if (!HasAnyOutput())
                    {
                        ClearInventoryServer();
                        matchedRecipe = null;
                        ResetToIdleServer();
                    }

                    return;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryTakeOutputServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;

            if (carry.IsHoldingServer())
                return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (OutputSlotIds[i] != NoneId)
                {
                    carry.SetHeldItemServer(OutputSlotIds[i]);
                    OutputSlotIds[i] = NoneId;
                    return;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SwapOutputWithHeldServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workState.Value != WorkState.Completed) return;
            if (!HasAnyOutput()) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null) return;
            if (!carry.IsHoldingServer()) return;

            ushort heldId = carry.GetHeldItemIdServer();
            if (heldId == NoneId) return;

            ushort outputId = NoneId;
            for (int i = 0; i < SlotCount; i++)
            {
                if (OutputSlotIds[i] != NoneId)
                {
                    outputId = OutputSlotIds[i];
                    OutputSlotIds[i] = NoneId;
                    break;
                }
            }

            if (outputId == NoneId) return;

            carry.SetHeldItemServer(outputId);

            if (!HasAnyOutput())
            {
                ClearInventoryServer();
                ClearOutputServer();
                matchedRecipe = null;
                ResetToIdleServer();
            }

            int empty = FindFirstEmptySlotServer();
            if (empty >= 0)
            {
                SlotItemIds[empty] = heldId;
                AutoStartIfReady();
            }
        }

        public bool IsIngredientForAnyRecipe(ushort itemId)
        {
            if (recipes == null || recipes.Length == 0) return false;

            for (int r = 0; r < recipes.Length; r++)
            {
                if (recipes[r] == null || recipes[r].Ingredients == null) continue;
                var ingredients = recipes[r].Ingredients;
                for (int i = 0; i < ingredients.Length; i++)
                {
                    if (ingredients[i] != null && ingredients[i].Id == itemId)
                        return true;
                }
            }

            return false;
        }

        #endregion
    }
}
