using System.Collections.Generic;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;
using UX;

namespace Gameplay.Player
{
    public class PlayerInteractor : NetworkBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Dependencies")]
        [SerializeField] private PlayerCarry carry;

        private readonly List<StorageRack> racksInRange = new List<StorageRack>(8);
        private StorageRack currentRack;
        private StorageRackContextPrompt currentPrompt;

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
            SetPrompt(null);
        }

        private void Update()
        {
            if (!IsOwner) return;

            PruneNulls();

            StorageRack nextRack = FindClosestRack();
            if (nextRack != currentRack)
            {
                currentRack = nextRack;
                SetPrompt(currentRack);
            }

            if (InteractionMenus.Instance != null && InteractionMenus.Instance.AnyMenuOpen)
                return;

            if (Input.GetKeyDown(interactKey))
            {
                if (currentRack != null && carry != null && InteractionMenus.Instance != null)
                {
                    InteractionMenus.Instance.OpenStorageRack(currentRack, carry);
                    if (currentPrompt != null)
                        currentPrompt.Hide();
                }
            }
        }

        private void SetPrompt(StorageRack rack)
        {
            if (currentPrompt != null)
                currentPrompt.Hide();

            currentPrompt = null;

            if (rack == null)
                return;

            currentPrompt = rack.GetComponentInChildren<StorageRackContextPrompt>(true);
            if (currentPrompt != null)
                currentPrompt.Show();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOwner) return;

            StorageRack rack = other.GetComponentInParent<StorageRack>();
            if (rack == null) return;

            if (!racksInRange.Contains(rack))
                racksInRange.Add(rack);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsOwner) return;

            StorageRack rack = other.GetComponentInParent<StorageRack>();
            if (rack == null) return;

            racksInRange.Remove(rack);

            if (rack == currentRack)
            {
                currentRack = null;
                SetPrompt(null);
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

        private void PruneNulls()
        {
            for (int i = racksInRange.Count - 1; i >= 0; i--)
            {
                if (racksInRange[i] == null)
                    racksInRange.RemoveAt(i);
            }
        }
    }
}
