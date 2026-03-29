using System.Collections;
using Gameplay.Items;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.SupplyHatch
{
    public class SupplyHatch : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        private enum HatchState : byte
        {
            Closed,
            Opening,
            Dispensing,
            Closing,
            Ready
        }

        [Header("Item")]
        [SerializeField] private LabItem dispenseItem;
        [SerializeField] private bool infiniteSupply = true;

        [Header("Timing")]
        [SerializeField] private float dispenseInterval = 5f;
        [SerializeField] private float frameTime = 0.1f;
        [SerializeField] private float itemRiseDuration = 0.4f;

        [Header("Hatch Sprites (3 frames: closed -> open)")]
        [SerializeField] private Sprite[] hatchFrames = new Sprite[3];

        public void InitializeFrom(SupplyHatch other)
        {
            dispenseItem = other.dispenseItem;
            infiniteSupply = other.infiniteSupply;
            dispenseInterval = other.dispenseInterval;
            frameTime = other.frameTime;
            itemRiseDuration = other.itemRiseDuration;
            hatchFrames = other.hatchFrames;

            if (IsServer)
            {
                dispenseTimer = 0f;
                if (activeSequence != null)
                {
                    StopCoroutine(activeSequence);
                    activeSequence = null;
                }
                hatchState.Value = (byte)HatchState.Closed;
            }
        }

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer hatchRenderer;
        [SerializeField] private SpriteRenderer itemRenderer;

        private NetworkVariable<byte> hatchState = new NetworkVariable<byte>(
            (byte)HatchState.Closed,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<byte> animFrame = new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> itemScale = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<ushort> readyItemId = new NetworkVariable<ushort>(
            NoneId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float dispenseTimer;
        private Coroutine activeSequence;

        public bool HasItemReady => (HatchState)hatchState.Value == HatchState.Ready
                                    && readyItemId.Value != NoneId;

        public override void OnNetworkSpawn()
        {
            hatchState.OnValueChanged += OnStateChanged;
            animFrame.OnValueChanged += OnFrameChanged;
            itemScale.OnValueChanged += OnItemScaleChanged;
            readyItemId.OnValueChanged += OnReadyItemChanged;

            RefreshHatchVisual();
            RefreshItemVisual();

            if (IsServer)
                dispenseTimer = dispenseInterval;
        }

        public override void OnNetworkDespawn()
        {
            hatchState.OnValueChanged -= OnStateChanged;
            animFrame.OnValueChanged -= OnFrameChanged;
            itemScale.OnValueChanged -= OnItemScaleChanged;
            readyItemId.OnValueChanged -= OnReadyItemChanged;
        }

        private void Update()
        {
            if (IsServer)
                ServerUpdate();

            if (itemRenderer != null)
            {
                float s = itemScale.Value;
                itemRenderer.transform.localScale = new Vector3(s, s, 1f);
                itemRenderer.enabled = s > 0.01f;
            }
        }

        private void ServerUpdate()
        {
            if (activeSequence != null) return;
            if ((HatchState)hatchState.Value != HatchState.Closed) return;

            dispenseTimer -= Time.deltaTime;
            if (dispenseTimer <= 0f)
            {
                dispenseTimer = dispenseInterval;

                if (dispenseItem == null)
                {
                    return;
                }

                activeSequence = StartCoroutine(DispenseSequence());
            }
        }

        private IEnumerator DispenseSequence()
        {
            if (dispenseItem == null)
            {
                activeSequence = null;
                yield break;
            }

            float ft = Mathf.Max(0.05f, frameTime);

            hatchState.Value = (byte)HatchState.Opening;
            if (hatchFrames != null && hatchFrames.Length > 0)
            {
                for (int i = 0; i < hatchFrames.Length; i++)
                {
                    animFrame.Value = (byte)i;
                    yield return new WaitForSeconds(ft);
                }
            }
            else
            {
                yield return new WaitForSeconds(ft);
            }

            hatchState.Value = (byte)HatchState.Dispensing;
            itemScale.Value = 0f;
            readyItemId.Value = dispenseItem.Id;

            float riseDuration = Mathf.Max(0.1f, itemRiseDuration);
            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                itemScale.Value = t * t * (3f - 2f * t);
                yield return null;
            }

            itemScale.Value = 1f;

            hatchState.Value = (byte)HatchState.Closing;
            if (hatchFrames != null && hatchFrames.Length > 0)
            {
                for (int i = hatchFrames.Length - 1; i >= 0; i--)
                {
                    animFrame.Value = (byte)i;
                    yield return new WaitForSeconds(ft);
                }
            }
            else
            {
                yield return new WaitForSeconds(ft);
            }

            hatchState.Value = (byte)HatchState.Ready;
            activeSequence = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryPickUpServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if ((HatchState)hatchState.Value != HatchState.Ready) return;
            if (readyItemId.Value == NoneId) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null || carry.IsHoldingServer()) return;

            carry.SetHeldItemServer(readyItemId.Value);

            readyItemId.Value = NoneId;
            itemScale.Value = 0f;

            hatchState.Value = (byte)HatchState.Closed;
            dispenseTimer = infiniteSupply ? dispenseInterval : float.MaxValue;
        }

        #region Visual Callbacks

        private void OnStateChanged(byte prev, byte next)
        {
            RefreshHatchVisual();
        }

        private void OnFrameChanged(byte prev, byte next)
        {
            RefreshHatchVisual();
        }

        private void OnItemScaleChanged(float prev, float next)
        {
            RefreshItemVisual();
        }

        private void OnReadyItemChanged(ushort prev, ushort next)
        {
            RefreshItemVisual();
        }

        private void RefreshHatchVisual()
        {
            if (hatchRenderer == null) return;
            if (hatchFrames == null || hatchFrames.Length == 0) return;

            int frame = Mathf.Clamp(animFrame.Value, 0, hatchFrames.Length - 1);
            hatchRenderer.sprite = hatchFrames[frame];
        }

        private void RefreshItemVisual()
        {
            if (itemRenderer == null) return;

            ushort id = readyItemId.Value;
            if (id != NoneId && dispenseItem != null && dispenseItem.Id == id)
                itemRenderer.sprite = dispenseItem.Sprite;
            else
                itemRenderer.sprite = null;

            float s = itemScale.Value;
            itemRenderer.transform.localScale = new Vector3(s, s, 1f);
            itemRenderer.enabled = s > 0.01f && itemRenderer.sprite != null;
        }



        #endregion

        private PlayerCarry GetCarryForClient(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            if (client.PlayerObject == null) return null;
            return client.PlayerObject.GetComponent<PlayerCarry>();
        }
    }
}
