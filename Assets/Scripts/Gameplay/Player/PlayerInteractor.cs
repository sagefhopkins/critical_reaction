using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Gameplay.Coop;
using Gameplay.Workstations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.WSA;
using UX;
using UX.Options;

namespace Gameplay.Player
{
    public class PlayerInteractor : NetworkBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerCarry carry;
        [SerializeField] private float confirmHoldTime = 0.35f;

        private readonly List<StorageRack> racksInRange = new List<StorageRack>(8);
        private readonly List<Workstation> workstationsInRange = new List<Workstation>(8);
        private readonly List<DeliveryPoint> deliveryPointsInRange = new List<DeliveryPoint>(4);

        private StorageRack currentRack;
        private Workstation currentWorkstation;
        private DeliveryPoint currentDeliveryPoint;

        private StorageRackContextPrompt currentRackPrompt;
        private WorkstationContextPrompt currentWorkstationPrompt;
        private DeliveryPointContextPrompt currentDeliveryPrompt;
        private float confirmTimer = 0f;
        private bool hasConfirmed = false;

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

            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                return;

            PruneNulls();

            UpdateCurrentInteractable();

            if (InteractionMenus.Instance != null && InteractionMenus.Instance.AnyMenuOpen)
                return;

           if (Input.GetKey(KeyCode.E))
           {
                confirmTimer += Time.unscaledDeltaTime;

                if (!hasConfirmed && confirmTimer >= confirmHoldTime)
                {
                    hasConfirmed = true;
                    TryInteract();
                }
           } 
            else
            {
                confirmTimer = 0f;
                hasConfirmed = false;
            }
        }

        private void UpdateCurrentInteractable()
        {
            StorageRack nextRack = FindClosestRack();
            Workstation nextWorkstation = FindClosestWorkstation();
            DeliveryPoint nextDelivery = FindClosestDeliveryPoint();

            float rackDist = nextRack != null
                ? ((Vector2)nextRack.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float workstationDist = nextWorkstation != null
                ? ((Vector2)nextWorkstation.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float deliveryDist = nextDelivery != null
                ? ((Vector2)nextDelivery.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float minDist = Mathf.Min(rackDist, Mathf.Min(workstationDist, deliveryDist));

            if (minDist == float.MaxValue)
            {
                if (currentRack != null || currentWorkstation != null || currentDeliveryPoint != null)
                {
                    currentRack = null;
                    currentWorkstation = null;
                    currentDeliveryPoint = null;
                    ClearAllPrompts();
                }
                return;
            }

            if (minDist == rackDist && nextRack != null)
            {
                if (nextRack != currentRack || currentWorkstation != null || currentDeliveryPoint != null)
                {
                    currentRack = nextRack;
                    currentWorkstation = null;
                    currentDeliveryPoint = null;
                    SetRackPrompt(currentRack);
                    SetWorkstationPrompt(null);
                    SetDeliveryPrompt(null);
                }
            }
            else if (minDist == workstationDist && nextWorkstation != null)
            {
                if (nextWorkstation != currentWorkstation || currentRack != null || currentDeliveryPoint != null)
                {
                    currentWorkstation = nextWorkstation;
                    currentRack = null;
                    currentDeliveryPoint = null;
                    SetWorkstationPrompt(currentWorkstation);
                    SetRackPrompt(null);
                    SetDeliveryPrompt(null);
                }
            }
            else if (minDist == deliveryDist && nextDelivery != null)
            {
                if (nextDelivery != currentDeliveryPoint || currentRack != null || currentWorkstation != null)
                {
                    currentDeliveryPoint = nextDelivery;
                    currentRack = null;
                    currentWorkstation = null;
                    SetDeliveryPrompt(currentDeliveryPoint);
                    SetRackPrompt(null);
                    SetWorkstationPrompt(null);
                }
            }
        }

        private void TryInteract()
        {
            if (carry == null) return;

            if (currentDeliveryPoint != null)
            {
                currentDeliveryPoint.TryDeliver(carry);
                return;
            }

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

        private void SetDeliveryPrompt(DeliveryPoint delivery)
        {
            if (currentDeliveryPrompt != null)
                currentDeliveryPrompt.Hide();

            currentDeliveryPrompt = null;

            if (delivery == null)
                return;

            currentDeliveryPrompt = delivery.GetComponentInChildren<DeliveryPointContextPrompt>(true);
            if (currentDeliveryPrompt != null)
                currentDeliveryPrompt.Show();
        }

        private void ClearAllPrompts()
        {
            if (currentRackPrompt != null)
                currentRackPrompt.Hide();

            if (currentWorkstationPrompt != null)
                currentWorkstationPrompt.Hide();

            if (currentDeliveryPrompt != null)
                currentDeliveryPrompt.Hide();

            currentRackPrompt = null;
            currentWorkstationPrompt = null;
            currentDeliveryPrompt = null;
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

            DeliveryPoint delivery = other.GetComponentInParent<DeliveryPoint>();
            if (delivery != null && !deliveryPointsInRange.Contains(delivery))
                deliveryPointsInRange.Add(delivery);
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

            DeliveryPoint delivery = other.GetComponentInParent<DeliveryPoint>();
            if (delivery != null)
            {
                deliveryPointsInRange.Remove(delivery);

                if (delivery == currentDeliveryPoint)
                {
                    currentDeliveryPoint = null;
                    SetDeliveryPrompt(null);
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

        private DeliveryPoint FindClosestDeliveryPoint()
        {
            DeliveryPoint best = null;
            float bestD = float.MaxValue;
            Vector2 p = transform.position;

            for (int i = 0; i < deliveryPointsInRange.Count; i++)
            {
                DeliveryPoint d = deliveryPointsInRange[i];
                if (d == null) continue;

                float dist = ((Vector2)d.transform.position - p).sqrMagnitude;
                if (dist < bestD)
                {
                    bestD = dist;
                    best = d;
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

            for (int i = deliveryPointsInRange.Count - 1; i >= 0; i--)
            {
                if (deliveryPointsInRange[i] == null)
                    deliveryPointsInRange.RemoveAt(i);
            }
        }
    }
}
