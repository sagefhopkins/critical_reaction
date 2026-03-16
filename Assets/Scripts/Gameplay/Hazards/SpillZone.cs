using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Hazards
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class SpillZone : NetworkBehaviour
    {
        [SerializeField] private float lifetime = 30f;
        [SerializeField] private GameObject spillZonePrefab;
        [SerializeField] private float throwDistance = 2f;
        [SerializeField] private float mopDuration = 5f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private NetworkVariable<Color32> spillColor = new NetworkVariable<Color32>(
            new Color32(255, 255, 255, 255),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float spawnTime;
        private bool localPlayerInZone;
        private PlayerController localPlayer;

        public float MopDuration => mopDuration;

        public override void OnNetworkSpawn()
        {
            spawnTime = Time.time;
            ApplyColor(spillColor.Value);
            spillColor.OnValueChanged += OnColorChanged;
        }

        public override void OnNetworkDespawn()
        {
            spillColor.OnValueChanged -= OnColorChanged;

            if (localPlayer != null)
            {
                localPlayer.ExitSlick();
                localPlayer = null;
                localPlayerInZone = false;
            }
        }

        private void OnColorChanged(Color32 prev, Color32 next)
        {
            ApplyColor(next);
        }

        private void ApplyColor(Color32 color)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }

        public void SetColorServer(Color32 color)
        {
            if (!IsServer) return;
            spillColor.Value = color;
        }

        private void Update()
        {
            if (IsServer && Time.time - spawnTime >= lifetime)
            {
                NetworkObject.Despawn(true);
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc == null || !pc.IsOwner) return;

            pc.EnterSlick();
            localPlayerInZone = true;
            localPlayer = pc;

            if (pc.HasMop) return;

            var carry = other.GetComponent<PlayerCarry>();
            if (carry != null && carry.IsHoldingLocal)
            {
                Vector2 velocity = other.attachedRigidbody != null
                    ? other.attachedRigidbody.linearVelocity
                    : Vector2.zero;

                DropItemServerRpc(pc.OwnerClientId, velocity);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.IsOwner)
            {
                pc.ExitSlick();
                localPlayerInZone = false;
                localPlayer = null;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CleanSpillServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
                && client.PlayerObject != null)
            {
                var pc = client.PlayerObject.GetComponent<PlayerController>();
                if (pc != null)
                    pc.SetMopDirtyServer();
            }

            NetworkObject.Despawn(true);
        }

        [ServerRpc(RequireOwnership = false)]
        private void DropItemServerRpc(ulong clientId, Vector2 throwDirection)
        {
            if (spillZonePrefab == null) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
            if (client.PlayerObject == null) return;

            var carry = client.PlayerObject.GetComponent<PlayerCarry>();
            if (carry == null || !carry.IsHoldingServer()) return;

            carry.ClearHeldItemServer();

            Vector2 dir = throwDirection.sqrMagnitude > 0.001f
                ? throwDirection.normalized
                : Vector2.down;

            Vector3 playerPos = client.PlayerObject.transform.position;
            Vector3 spawnPos = playerPos + new Vector3(dir.x, dir.y, 0f) * throwDistance;

            GameObject spill = Instantiate(spillZonePrefab, spawnPos, Quaternion.identity);
            var no = spill.GetComponent<NetworkObject>();
            no?.Spawn();

            var childSpillZone = spill.GetComponent<SpillZone>();
            childSpillZone?.SetColorServer(spillColor.Value);
        }
    }
}
