using Gameplay.Coop;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public class DroppedItem : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer visual;

        [Header("Pickup")]
        [Tooltip("Seconds after spawn during which this item cannot be picked up. Prevents immediate re-pickup by the dropper.")]
        [SerializeField] private float pickupDelay = 0.5f;

        private NetworkVariable<ushort> itemId = new NetworkVariable<ushort>(
            NoneId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float pickupEnableTime;

        public ushort ItemId => itemId.Value;

        private void Awake()
        {
            if (visual == null)
                visual = GetComponentInChildren<SpriteRenderer>(true);
        }

        public void SetItemServer(ushort id)
        {
            if (!IsServer) return;
            itemId.Value = id;
        }

        public override void OnNetworkSpawn()
        {
            itemId.OnValueChanged += OnItemChanged;
            pickupEnableTime = Time.time + pickupDelay;
            RefreshVisual();
        }

        public override void OnNetworkDespawn()
        {
            itemId.OnValueChanged -= OnItemChanged;
        }

        private void OnItemChanged(ushort prev, ushort next) => RefreshVisual();

        private void Update()
        {
            if (visual != null && visual.sprite == null && itemId.Value != NoneId)
                RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (visual == null) return;

            Sprite spr = GetSpriteById(itemId.Value);
            visual.sprite = spr;
            visual.enabled = spr != null;
        }

        private Sprite GetSpriteById(ushort id)
        {
            if (id == NoneId) return null;
            if (CoopGameManager.Instance == null) return null;

            var config = CoopGameManager.Instance.CurrentLevelConfig;
            if (config == null || config.AvailableProducts == null) return null;

            var pool = config.AvailableProducts;
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].Id == id)
                    return pool[i].Sprite;

            return null;
        }

        private void OnTriggerEnter2D(Collider2D other) => HandleTrigger(other.gameObject);
        private void OnTriggerStay2D(Collider2D other) => HandleTrigger(other.gameObject);
        private void OnTriggerEnter(Collider other) => HandleTrigger(other.gameObject);
        private void OnTriggerStay(Collider other) => HandleTrigger(other.gameObject);

        private void HandleTrigger(GameObject other)
        {
            if (!IsServer) return;
            if (Time.time < pickupEnableTime) return;
            if (itemId.Value == NoneId) return;

            PlayerCarry carry = other.GetComponentInParent<PlayerCarry>();
            if (carry == null) return;
            if (carry.IsHoldingServer()) return;

            carry.SetHeldItemServer(itemId.Value);

            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
            else
                Destroy(gameObject);
        }
    }
}
