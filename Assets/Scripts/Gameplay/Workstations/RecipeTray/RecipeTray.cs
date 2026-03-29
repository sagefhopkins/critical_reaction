using Gameplay.Coop;
using Gameplay.Items;
using Gameplay.Player;
using Gameplay.Workstations.Conveyor;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.RecipeTray
{
    public class RecipeTray : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Tray Visual")]
        [SerializeField] private SpriteRenderer trayRenderer;

        [Header("Status Bubbles")]
        [SerializeField] private GameObject statusRootCenter;
        [SerializeField] private SpriteRenderer itemIconCenter;
        [SerializeField] private GameObject statusRootLeft;
        [SerializeField] private SpriteRenderer itemIconLeft;
        [SerializeField] private GameObject statusRootRight;
        [SerializeField] private SpriteRenderer itemIconRight;

        [Header("Tray Slots")]
        [SerializeField] private SpriteRenderer slotRenderer1;
        [SerializeField] private SpriteRenderer slotRenderer2;

        [Header("Conveyor")]
        [SerializeField] private ConveyorBelt conveyor;

        private NetworkVariable<ushort> requiredItem1 = new NetworkVariable<ushort>(
            NoneId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<ushort> requiredItem2 = new NetworkVariable<ushort>(
            NoneId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> slot1Filled = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> slot2Filled = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<float> beltPosition = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> isComplete = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsComplete => isComplete.Value;
        public float BeltPosition => beltPosition.Value;
        public int RequiredCount => requiredItem2.Value != NoneId ? 2 : 1;
        public ConveyorBelt Conveyor => conveyor;

        public bool CanAcceptItem(ushort itemId)
        {
            if (itemId == NoneId) return false;
            if (isComplete.Value) return false;

            if (!slot1Filled.Value && requiredItem1.Value == itemId)
                return true;
            if (!slot2Filled.Value && requiredItem2.Value != NoneId && requiredItem2.Value == itemId)
                return true;

            return false;
        }

        public override void OnNetworkSpawn()
        {
            requiredItem1.OnValueChanged += OnOrderChanged;
            requiredItem2.OnValueChanged += OnOrderChanged;
            slot1Filled.OnValueChanged += OnSlotChanged;
            slot2Filled.OnValueChanged += OnSlotChanged;
            isComplete.OnValueChanged += OnCompleteChanged;

            RefreshVisuals();
        }

        public override void OnNetworkDespawn()
        {
            requiredItem1.OnValueChanged -= OnOrderChanged;
            requiredItem2.OnValueChanged -= OnOrderChanged;
            slot1Filled.OnValueChanged -= OnSlotChanged;
            slot2Filled.OnValueChanged -= OnSlotChanged;
            isComplete.OnValueChanged -= OnCompleteChanged;
        }

        private void Update()
        {
            if (IsServer && conveyor != null && !isComplete.Value)
                AdvanceOnBelt();

            UpdateWorldPosition();
        }

        public void SetOrderServer(ushort item1, ushort item2, ConveyorBelt belt, LabItem[] itemList, float startPosition = 0f)
        {
            if (!IsServer) return;

            if (itemList != null)
                items = itemList;

            requiredItem1.Value = item1;
            requiredItem2.Value = item2;
            slot1Filled.Value = false;
            slot2Filled.Value = false;
            isComplete.Value = false;
            conveyor = belt;
            beltPosition.Value = startPosition;
        }

        public void SetConveyor(ConveyorBelt belt)
        {
            conveyor = belt;
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryDepositServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (isComplete.Value) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null || !carry.IsHoldingServer()) return;

            ushort heldId = carry.GetHeldItemIdServer();

            if (!slot1Filled.Value && requiredItem1.Value == heldId)
            {
                slot1Filled.Value = true;
                carry.ClearHeldItemServer();
                CheckComplete(clientId);
                return;
            }

            if (!slot2Filled.Value && requiredItem2.Value != NoneId && requiredItem2.Value == heldId)
            {
                slot2Filled.Value = true;
                carry.ClearHeldItemServer();
                CheckComplete(clientId);
                return;
            }
        }

        private void CheckComplete(ulong clientId)
        {
            bool done = slot1Filled.Value;
            if (requiredItem2.Value != NoneId)
                done = done && slot2Filled.Value;

            if (!done) return;

            isComplete.Value = true;

            if (CoopGameManager.Instance != null)
            {
                CoopGameManager.Instance.RegisterDelivery(requiredItem1.Value, 1);
                CoopGameManager.Instance.RecordDelivery(clientId);

                if (requiredItem2.Value != NoneId)
                    CoopGameManager.Instance.RegisterDelivery(requiredItem2.Value, 1);
            }
        }

        #region Belt Movement

        private void AdvanceOnBelt()
        {
            if (conveyor == null) return;

            float beltLength = GetBeltLength();
            if (beltLength <= 0f) return;

            float speed = GetConveyorSpeed();
            float delta = (speed / beltLength) * Time.deltaTime;
            float newPos = beltPosition.Value + delta;

            if (newPos >= 1f)
            {
                ConveyorBelt next = conveyor.OutputConveyor;
                if (next != null)
                {
                    float overflow = newPos - 1f;
                    float nextLength = next.GetBeltLength();
                    float nextSpeed = next.Speed;
                    float overflowNormalized = nextLength > 0f ? (overflow * beltLength) / nextLength : 0f;

                    conveyor = next;
                    beltPosition.Value = Mathf.Clamp01(overflowNormalized);
                    return;
                }

                newPos = 1f;
            }

            beltPosition.Value = newPos;
        }

        private void UpdateWorldPosition()
        {
            if (conveyor == null) return;
            Vector3 pos = conveyor.GetWorldPosition(beltPosition.Value);
            pos.z = transform.position.z;
            transform.position = pos;
        }

        private float GetBeltLength()
        {
            if (conveyor == null) return 1f;
            return conveyor.GetBeltLength();
        }

        private float GetConveyorSpeed()
        {
            if (conveyor == null) return 0f;
            return conveyor.Speed;
        }

        #endregion

        #region Visuals

        private void OnOrderChanged(ushort prev, ushort next) => RefreshVisuals();
        private void OnSlotChanged(bool prev, bool next) => RefreshVisuals();
        private void OnCompleteChanged(bool prev, bool next) => RefreshVisuals();

        private void RefreshVisuals()
        {
            bool hasItem1 = requiredItem1.Value != NoneId;
            bool hasItem2 = requiredItem2.Value != NoneId;
            bool singleItem = hasItem1 && !hasItem2;

            if (statusRootCenter != null)
                statusRootCenter.SetActive(singleItem);

            if (singleItem && itemIconCenter != null)
                itemIconCenter.sprite = GetSpriteById(requiredItem1.Value);

            if (statusRootLeft != null)
                statusRootLeft.SetActive(hasItem1 && hasItem2);

            if (statusRootRight != null)
                statusRootRight.SetActive(hasItem2);

            if (hasItem1 && hasItem2)
            {
                if (itemIconLeft != null)
                    itemIconLeft.sprite = GetSpriteById(requiredItem1.Value);

                if (itemIconRight != null)
                    itemIconRight.sprite = GetSpriteById(requiredItem2.Value);
            }

            if (slotRenderer1 != null)
            {
                Sprite spr = slot1Filled.Value ? GetSpriteById(requiredItem1.Value) : null;
                slotRenderer1.sprite = spr;
                slotRenderer1.enabled = spr != null;
            }

            if (slotRenderer2 != null)
            {
                Sprite spr = slot2Filled.Value ? GetSpriteById(requiredItem2.Value) : null;
                slotRenderer2.sprite = spr;
                slotRenderer2.enabled = spr != null;
            }
        }

        #endregion

        #region Helpers

        private Sprite GetSpriteById(ushort id)
        {
            if (id == NoneId) return null;

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null && items[i].Id == id)
                        return items[i].Sprite;
                }
            }

            if (Gameplay.Coop.CoopGameManager.Instance != null)
            {
                var config = Gameplay.Coop.CoopGameManager.Instance.CurrentLevelConfig;
                if (config != null && config.AvailableProducts != null)
                {
                    var pool = config.AvailableProducts;
                    for (int i = 0; i < pool.Length; i++)
                    {
                        if (pool[i] != null && pool[i].Id == id)
                            return pool[i].Sprite;
                    }
                }
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

        #endregion
    }
}
