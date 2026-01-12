using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class BurnerController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Workstation workstation;

        [Header("Settings")]
        [SerializeField] private float workDuration = 5f;

        private NetworkVariable<ulong> workingClientId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action OnBurnerStateChanged;

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        public override void OnNetworkSpawn()
        {
            if (workstation != null)
                workstation.OnWorkStateChanged += HandleWorkStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (workstation != null)
                workstation.OnWorkStateChanged -= HandleWorkStateChanged;
        }

        private void HandleWorkStateChanged()
        {
            OnBurnerStateChanged?.Invoke();
        }

        private void Update()
        {
            if (!IsServer) return;
            if (workstation == null) return;
            if (workstation.CurrentWorkState != WorkState.Working) return;

            float delta = Time.deltaTime / workDuration;
            workstation.AddProgressServer(delta);

            if (workstation.WorkProgress >= 1f)
            {
                workstation.CompleteWorkServer();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryStartWorkServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workstation == null) return;

            if (!workstation.CanStartWork())
                return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            workingClientId.Value = clientId;

            workstation.SetWorkStateServer(WorkState.Working);
            workstation.SetProgressServer(0f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryCancelWorkServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (workstation == null) return;
            if (workstation.CurrentWorkState != WorkState.Working) return;

            workstation.ResetToIdleServer();
            workingClientId.Value = ulong.MaxValue;
        }
    }
}
