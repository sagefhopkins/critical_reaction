using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Hazards
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MopBucket : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer mopVisual;

        private NetworkVariable<bool> mopAvailable = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool IsMopAvailable => mopAvailable.Value;

        public override void OnNetworkSpawn()
        {
            mopAvailable.OnValueChanged += OnMopAvailableChanged;
            UpdateMopVisual(mopAvailable.Value);
        }

        public override void OnNetworkDespawn()
        {
            mopAvailable.OnValueChanged -= OnMopAvailableChanged;
        }

        private void OnMopAvailableChanged(bool prev, bool next)
        {
            UpdateMopVisual(next);
        }

        private void UpdateMopVisual(bool available)
        {
            if (mopVisual != null)
                mopVisual.enabled = available;
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            var pc = col.gameObject.GetComponent<PlayerController>();
            if (pc != null && pc.IsOwner)
                pc.EnterPush();
        }

        private void OnCollisionExit2D(Collision2D col)
        {
            var pc = col.gameObject.GetComponent<PlayerController>();
            if (pc != null && pc.IsOwner)
                pc.ExitPush();
        }

        [ServerRpc(RequireOwnership = false)]
        public void TakeMopServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!mopAvailable.Value) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
            if (client.PlayerObject == null) return;

            var carry = client.PlayerObject.GetComponent<PlayerCarry>();
            if (carry != null && carry.IsHoldingServer()) return;

            var pc = client.PlayerObject.GetComponent<PlayerController>();
            if (pc == null || pc.HasMop) return;

            mopAvailable.Value = false;
            pc.SetHasMopServer(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReturnMopServerRpc(ServerRpcParams rpcParams = default)
        {
            if (mopAvailable.Value) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
            if (client.PlayerObject == null) return;

            var pc = client.PlayerObject.GetComponent<PlayerController>();
            if (pc == null || !pc.HasMop) return;

            mopAvailable.Value = true;
            pc.SetHasMopServer(false);
        }
    }
}
