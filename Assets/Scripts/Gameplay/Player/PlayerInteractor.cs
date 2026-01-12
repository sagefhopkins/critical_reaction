using System.Collections.Generic;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;
using UX;
using UX.Options;

namespace Gameplay.Player
{
    public class PlayerInteractor : NetworkBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerCarry carry;

        private readonly List<StorageRack> racksInRange = new List<StorageRack>(8);
        private readonly List<Workstation> workstationsInRange = new List<Workstation>(8);

        private StorageRack currentRack;
        private Workstation currentWorkstation;

        private StorageRackContextPrompt currentRackPrompt;
        private WorkstationContextPrompt currentWorkstationPrompt;

        private void Awake()
        {
            if (carry == null)
                carry = GetComponent<PlayerCarry>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                enabled = false;
        }

        private void OnDisable()
        {
            ClearAllPrompts();
        }

        private void Update()
        {
            if (!IsOwner) return;

            PruneNulls();

            UpdateCurrentInteractable();

            if (InteractionMenus.Instance != null && InteractionMenus.Instance.AnyMenuOpen)
                return;

            bool interact = InputSettings.Instance != null ? InputSettings.Instance.IsInteractPressed() : Input.GetKeyDown(KeyCode.E);
            if (interact)
            {
                TryInteract();
            }
        }

        private void UpdateCurrentInteractable()
        {
            StorageRack nextRack = FindClosestRack();
            Workstation nextWorkstation = FindClosestWorkstation();

            float rackDist = nextRack != null
                ? ((Vector2)nextRack.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float workstationDist = nextWorkstation != null
                ? ((Vector2)nextWorkstation.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            if (rackDist <= workstationDist && nextRack != null)
            {
                if (nextRack != currentRack || currentWorkstation != null)
                {
                    currentRack = nextRack;
                    currentWorkstation = null;
                    SetRackPrompt(currentRack);
                    SetWorkstationPrompt(null);
                }
            }
            else if (nextWorkstation != null)
            {
                if (nextWorkstation != currentWorkstation || currentRack != null)
                {
                    currentWorkstation = nextWorkstation;
                    currentRack = null;
                    SetWorkstationPrompt(currentWorkstation);
                    SetRackPrompt(null);
                }
            }
            else
            {
                if (currentRack != null || currentWorkstation != null)
                {
                    currentRack = null;
                    currentWorkstation = null;
                    ClearAllPrompts();
                }
            }
        }

        private void TryInteract()
        {
            if (carry == null) return;
            if (InteractionMenus.Instance == null) return;

            if (currentRack != null)
            {
                InteractionMenus.Instance.OpenStorageRack(currentRack, carry);
                if (currentRackPrompt != null)
                    currentRackPrompt.Hide();
            }
            else if (currentWorkstation != null)
            {
                InteractionMenus.Instance.OpenWorkstation(currentWorkstation, carry);
                if (currentWorkstationPrompt != null)
                    currentWorkstationPrompt.Hide();
            }
        }

        private void SetRackPrompt(StorageRack rack)
        {
            if (currentRackPrompt != null)
                currentRackPrompt.Hide();

            currentRackPrompt = null;

            if (rack == null)
                return;

            currentRackPrompt = rack.GetComponentInChildren<StorageRackContextPrompt>(true);
            if (currentRackPrompt != null)
                currentRackPrompt.Show();
        }

        private void SetWorkstationPrompt(Workstation workstation)
        {
            if (currentWorkstationPrompt != null)
                currentWorkstationPrompt.Hide();

            currentWorkstationPrompt = null;

            if (workstation == null)
                return;

            currentWorkstationPrompt = workstation.GetComponentInChildren<WorkstationContextPrompt>(true);
            if (currentWorkstationPrompt != null)
                currentWorkstationPrompt.Show();
        }

        private void ClearAllPrompts()
        {
            if (currentRackPrompt != null)
                currentRackPrompt.Hide();

            if (currentWorkstationPrompt != null)
                currentWorkstationPrompt.Hide();

            currentRackPrompt = null;
            currentWorkstationPrompt = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOwner) return;

            StorageRack rack = other.GetComponentInParent<StorageRack>();
            if (rack != null && !racksInRange.Contains(rack))
                racksInRange.Add(rack);

            Workstation workstation = other.GetComponentInParent<Workstation>();
            if (workstation != null && !workstationsInRange.Contains(workstation))
                workstationsInRange.Add(workstation);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsOwner) return;

            StorageRack rack = other.GetComponentInParent<StorageRack>();
            if (rack != null)
            {
                racksInRange.Remove(rack);

                if (rack == currentRack)
                {
                    currentRack = null;
                    SetRackPrompt(null);
                }
            }

            Workstation workstation = other.GetComponentInParent<Workstation>();
            if (workstation != null)
            {
                workstationsInRange.Remove(workstation);

                if (workstation == currentWorkstation)
                {
                    currentWorkstation = null;
                    SetWorkstationPrompt(null);
                }
            }
        }

        private StorageRack FindClosestRack()
        {
            StorageRack best = null;
            float bestD = float.MaxValue;
            Vector2 p = transform.position;

            for (int i = 0; i < racksInRange.Count; i++)
            {
                StorageRack r = racksInRange[i];
                if (r == null) continue;

                float d = ((Vector2)r.transform.position - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = r;
                }
            }

            return best;
        }

        private Workstation FindClosestWorkstation()
        {
            Workstation best = null;
            float bestD = float.MaxValue;
            Vector2 p = transform.position;

            for (int i = 0; i < workstationsInRange.Count; i++)
            {
                Workstation w = workstationsInRange[i];
                if (w == null) continue;

                float d = ((Vector2)w.transform.position - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = w;
                }
            }

            return best;
        }

        private void PruneNulls()
        {
            for (int i = racksInRange.Count - 1; i >= 0; i--)
            {
                if (racksInRange[i] == null)
                    racksInRange.RemoveAt(i);
            }

            for (int i = workstationsInRange.Count - 1; i >= 0; i--)
            {
                if (workstationsInRange[i] == null)
                    workstationsInRange.RemoveAt(i);
            }
        }
    }
}
