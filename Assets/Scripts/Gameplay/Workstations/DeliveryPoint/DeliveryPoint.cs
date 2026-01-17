using Gameplay.Coop;
using Gameplay.Items;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class DeliveryPoint : NetworkBehaviour
    {
        [Header("Delivery Settings")]
        [SerializeField] private LabItem[] acceptedItems;
        [SerializeField] private bool acceptAnyItem = true;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer indicatorRenderer;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color acceptColor = Color.green;

        [Header("Item List Reference")]
        [SerializeField] private LabItem[] allItems;

        public bool CanAcceptItem(ushort itemId)
        {
            if (acceptAnyItem) return true;
            if (acceptedItems == null || acceptedItems.Length == 0) return false;

            for (int i = 0; i < acceptedItems.Length; i++)
            {
                if (acceptedItems[i] != null && acceptedItems[i].Id == itemId)
                    return true;
            }

            return false;
        }

        public void TryDeliver(PlayerCarry carry)
        {
            if (carry == null) return;
            if (!carry.IsOwner) return;

            ushort heldId = carry.HeldItemIdLocal;
            if (heldId == 0) return;

            if (!CanAcceptItem(heldId)) return;

            TryDeliverServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void TryDeliverServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            PlayerCarry carry = FindCarryForClient(clientId);
            if (carry == null) return;

            if (!carry.IsHoldingServer()) return;

            ushort itemId = carry.GetHeldItemIdServer();
            if (!CanAcceptItem(itemId)) return;

            string itemName = GetItemName(itemId);
            carry.ClearHeldItemServer();

            if (CoopGameManager.Instance != null)
            {
                CoopGameManager.Instance.RegisterDelivery(1);

                if (!string.IsNullOrEmpty(itemName))
                {
                    CoopGameManager.Instance.ShowAlert($"Delivered {itemName}!");
                }
            }
        }

        private PlayerCarry FindCarryForClient(ulong clientId)
        {
            PlayerCarry[] allCarries = FindObjectsByType<PlayerCarry>(FindObjectsSortMode.None);

            for (int i = 0; i < allCarries.Length; i++)
            {
                PlayerCarry c = allCarries[i];
                if (c != null && c.OwnerClientId == clientId)
                    return c;
            }

            return null;
        }

        private string GetItemName(ushort id)
        {
            if (allItems == null) return null;

            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i] != null && allItems[i].Id == id)
                    return allItems[i].DisplayName;
            }

            return null;
        }

        public void SetIndicatorColor(bool canAccept)
        {
            if (indicatorRenderer == null) return;
            indicatorRenderer.color = canAccept ? acceptColor : idleColor;
        }
    }
}
