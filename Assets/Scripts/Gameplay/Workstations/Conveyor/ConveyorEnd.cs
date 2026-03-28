using Gameplay.Coop;
using Gameplay.Workstations.RecipeTray;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.Conveyor
{
    public class ConveyorEnd : NetworkBehaviour
    {
        [Header("Tray Spawner")]
        [SerializeField] private TraySpawner traySpawner;

        [Header("Failure")]
        [Tooltip("Number of failed/incomplete trays allowed before the level fails. 0 = unlimited.")]
        [SerializeField] private int maxFailures = 3;

        private NetworkVariable<int> failureCount = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public int FailureCount => failureCount.Value;

        public void InitializeFrom(ConveyorEnd other)
        {
            traySpawner = other.traySpawner;
            maxFailures = other.maxFailures;
        }

        private bool CheckIsServer()
        {
            if (IsSpawned) return IsServer;
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsServer;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CheckIsServer()) return;
            HandleTrigger(other.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CheckIsServer()) return;
            HandleTrigger(other.gameObject);
        }

        private void HandleTrigger(GameObject other)
        {
            RecipeTray.RecipeTray tray = other.GetComponentInParent<RecipeTray.RecipeTray>();
            if (tray == null) return;

            EvaluateTray(tray);
        }

        private void EvaluateTray(RecipeTray.RecipeTray tray)
        {
            if (tray.IsComplete)
            {
                if (CoopGameManager.Instance != null)
                    CoopGameManager.Instance.ShowAlert("Order delivered!");
            }
            else
            {
                if (IsSpawned)
                    failureCount.Value++;

                int count = IsSpawned ? failureCount.Value : ++localFailureCount;

                if (CoopGameManager.Instance != null)
                {
                    if (maxFailures > 0)
                        CoopGameManager.Instance.ShowAlert($"Incomplete order! ({count}/{maxFailures})");
                    else
                        CoopGameManager.Instance.ShowAlert("Incomplete order!");

                    if (maxFailures > 0 && count >= maxFailures)
                    {
                        CoopGameManager.Instance.ShowAlert("Too many failed orders!");
                        CoopGameManager.Instance.FailLevelServer();
                    }
                }
            }

            DespawnTray(tray);
        }

        private int localFailureCount;

        private void DespawnTray(RecipeTray.RecipeTray tray)
        {
            if (traySpawner != null)
                traySpawner.NotifyTrayRemoved();

            NetworkObject netObj = tray.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);
            else
                Destroy(tray.gameObject);
        }
    }
}
