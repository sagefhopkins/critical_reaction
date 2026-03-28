using System;
using Gameplay.Hazards;
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

        [Header("Overheat")]
        [SerializeField] private float overheatTime = 10f;
        [SerializeField] private float spillSpawnRadius = 1.5f;
        [SerializeField] private GameObject spillZonePrefab;
        [SerializeField] private Color32 spillColor = new Color32(255, 120, 50, 200);

        private NetworkVariable<ulong> workingClientId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float completedAtTime = -1f;

        public event Action OnBurnerStateChanged;

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                workingClientId.Value = ulong.MaxValue;

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
            if (IsServer)
            {
                if (workstation.CurrentWorkState == WorkState.Completed)
                    completedAtTime = Time.time;
                else
                    completedAtTime = -1f;
            }

            OnBurnerStateChanged?.Invoke();
        }

        private void Update()
        {
            if (!IsServer) return;
            if (workstation == null) return;

            if (workstation.CurrentWorkState == WorkState.Working)
            {
                float delta = Time.deltaTime / workDuration;
                workstation.AddProgressServer(delta);

                if (workstation.WorkProgress >= 1f)
                {
                    workstation.CompleteWorkServer();
                }
            }
            else if (workstation.CurrentWorkState == WorkState.Completed && overheatTime > 0f)
            {
                if (completedAtTime > 0f)
                {
                    float elapsed = Time.time - completedAtTime;
                    workstation.SetDangerProgressServer(Mathf.Clamp01(elapsed / overheatTime));

                    if (elapsed >= overheatTime)
                    {
                        SpawnSpillZone();
                        workstation.FailWorkServer();
                        completedAtTime = -1f;
                    }
                }
            }
        }

        private void SpawnSpillZone()
        {
            if (spillZonePrefab == null) return;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * spillSpawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

            GameObject spill = Instantiate(spillZonePrefab, spawnPos, Quaternion.identity);
            spill.GetComponent<NetworkObject>()?.Spawn();
            spill.GetComponent<SpillZone>()?.SetColorServer(spillColor);
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
