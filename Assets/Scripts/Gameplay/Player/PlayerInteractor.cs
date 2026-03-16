using System.Collections.Generic;
using Gameplay.Coop;
using Gameplay.Hazards;
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
        [SerializeField] private float confirmHoldTime = 0.35f;
        [SerializeField] private InteractHoldIndicator holdIndicator;

        private PlayerController playerController;

        private readonly List<StorageRack> racksInRange = new List<StorageRack>(8);
        private readonly List<Workstation> workstationsInRange = new List<Workstation>(8);
        private readonly List<DeliveryPoint> deliveryPointsInRange = new List<DeliveryPoint>(4);
        private readonly List<SpillZone> spillsInRange = new List<SpillZone>(4);
        private readonly List<MopBucket> bucketsInRange = new List<MopBucket>(4);

        private StorageRack currentRack;
        private Workstation currentWorkstation;
        private DeliveryPoint currentDeliveryPoint;
        private SpillZone currentSpillZone;
        private MopBucket currentMopBucket;

        private StorageRackContextPrompt currentRackPrompt;
        private WorkstationContextPrompt currentWorkstationPrompt;
        private DeliveryPointContextPrompt currentDeliveryPrompt;
        private SpillZoneContextPrompt currentSpillPrompt;
        private MopBucketContextPrompt currentBucketPrompt;

        private float confirmTimer = 0f;
        private bool hasConfirmed = false;

        private void Awake()
        {
            if (carry == null)
                carry = GetComponent<PlayerCarry>();
            if (holdIndicator == null)
                holdIndicator = GetComponentInChildren<InteractHoldIndicator>(true);

            playerController = GetComponent<PlayerController>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                enabled = false;
        }

        private void OnDisable()
        {
            ClearAllPrompts();
            HideHoldIndicator();
        }

        private float GetEffectiveHoldTime()
        {
            if (currentSpillZone != null && playerController != null && playerController.CanMop)
                return currentSpillZone.MopDuration;
            return confirmHoldTime;
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

            bool interact = InputSettings.Instance != null
                ? InputSettings.Instance.IsInteractHeld()
                : Input.GetKey(KeyCode.E);

            float holdTime = GetEffectiveHoldTime();

           if (interact)
           {
                confirmTimer += Time.unscaledDeltaTime;

                if (!hasConfirmed)
                {
                    UpdateHoldIndicator(holdTime);

                    if (confirmTimer >= holdTime)
                    {
                        hasConfirmed = true;
                        HideHoldIndicator();
                        TryInteract();
                    }
                }
           }
            else
            {
                if (confirmTimer > 0f)
                    HideHoldIndicator();

                confirmTimer = 0f;
                hasConfirmed = false;
            }
        }

        private void UpdateCurrentInteractable()
        {
            StorageRack nextRack = FindClosestRack();
            Workstation nextWorkstation = FindClosestWorkstation();
            DeliveryPoint nextDelivery = FindClosestDeliveryPoint();
            SpillZone nextSpill = FindClosestSpillZone();
            MopBucket nextBucket = FindClosestMopBucket();

            float rackDist = nextRack != null
                ? ((Vector2)nextRack.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float workstationDist = nextWorkstation != null
                ? ((Vector2)nextWorkstation.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float deliveryDist = nextDelivery != null
                ? ((Vector2)nextDelivery.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float spillDist = nextSpill != null
                ? ((Vector2)nextSpill.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float bucketDist = nextBucket != null
                ? ((Vector2)nextBucket.transform.position - (Vector2)transform.position).sqrMagnitude
                : float.MaxValue;

            float minDist = Mathf.Min(rackDist,
                Mathf.Min(workstationDist,
                    Mathf.Min(deliveryDist,
                        Mathf.Min(spillDist, bucketDist))));

            if (minDist == float.MaxValue)
            {
                if (currentRack != null || currentWorkstation != null || currentDeliveryPoint != null
                    || currentSpillZone != null || currentMopBucket != null)
                {
                    ClearCurrentInteractable();
                    ClearAllPrompts();
                }
                return;
            }

            if (minDist == spillDist && nextSpill != null)
            {
                if (nextSpill != currentSpillZone || HasOtherInteractable(InteractableKind.Spill))
                {
                    ClearCurrentInteractable();
                    currentSpillZone = nextSpill;
                    SetSpillPrompt(currentSpillZone);
                    ClearPromptsExcept(InteractableKind.Spill);
                }
            }
            else if (minDist == bucketDist && nextBucket != null)
            {
                if (nextBucket != currentMopBucket || HasOtherInteractable(InteractableKind.Bucket))
                {
                    ClearCurrentInteractable();
                    currentMopBucket = nextBucket;
                    SetBucketPrompt(currentMopBucket);
                    ClearPromptsExcept(InteractableKind.Bucket);
                }
            }
            else if (minDist == rackDist && nextRack != null)
            {
                if (nextRack != currentRack || HasOtherInteractable(InteractableKind.Rack))
                {
                    ClearCurrentInteractable();
                    currentRack = nextRack;
                    SetRackPrompt(currentRack);
                    ClearPromptsExcept(InteractableKind.Rack);
                }
            }
            else if (minDist == workstationDist && nextWorkstation != null)
            {
                if (nextWorkstation != currentWorkstation || HasOtherInteractable(InteractableKind.Workstation))
                {
                    ClearCurrentInteractable();
                    currentWorkstation = nextWorkstation;
                    SetWorkstationPrompt(currentWorkstation);
                    ClearPromptsExcept(InteractableKind.Workstation);
                }
            }
            else if (minDist == deliveryDist && nextDelivery != null)
            {
                if (nextDelivery != currentDeliveryPoint || HasOtherInteractable(InteractableKind.Delivery))
                {
                    ClearCurrentInteractable();
                    currentDeliveryPoint = nextDelivery;
                    SetDeliveryPrompt(currentDeliveryPoint);
                    ClearPromptsExcept(InteractableKind.Delivery);
                }
            }
        }

        private enum InteractableKind { Rack, Workstation, Delivery, Spill, Bucket }

        private bool HasOtherInteractable(InteractableKind current)
        {
            if (current != InteractableKind.Rack && currentRack != null) return true;
            if (current != InteractableKind.Workstation && currentWorkstation != null) return true;
            if (current != InteractableKind.Delivery && currentDeliveryPoint != null) return true;
            if (current != InteractableKind.Spill && currentSpillZone != null) return true;
            if (current != InteractableKind.Bucket && currentMopBucket != null) return true;
            return false;
        }

        private void ClearCurrentInteractable()
        {
            currentRack = null;
            currentWorkstation = null;
            currentDeliveryPoint = null;
            currentSpillZone = null;
            currentMopBucket = null;
        }

        private void ClearPromptsExcept(InteractableKind keep)
        {
            if (keep != InteractableKind.Rack) SetRackPrompt(null);
            if (keep != InteractableKind.Workstation) SetWorkstationPrompt(null);
            if (keep != InteractableKind.Delivery) SetDeliveryPrompt(null);
            if (keep != InteractableKind.Spill) SetSpillPrompt(null);
            if (keep != InteractableKind.Bucket) SetBucketPrompt(null);
        }

        private void TryInteract()
        {
            if (carry == null) return;

            if (currentSpillZone != null && playerController != null && playerController.CanMop)
            {
                currentSpillZone.CleanSpillServerRpc();
                return;
            }

            if (currentMopBucket != null)
            {
                TryInteractWithMopBucket();
                return;
            }

            if (playerController != null && playerController.HasMop) return;

            if (currentDeliveryPoint != null)
            {
                currentDeliveryPoint.TryDeliver(carry);
                return;
            }

            if (currentWorkstation != null)
            {
                TryInteractWithWorkstation();
                return;
            }

            if (InteractionMenus.Instance == null) return;

            if (currentRack != null)
            {
                InteractionMenus.Instance.OpenStorageRack(currentRack, carry);
                if (currentRackPrompt != null)
                    currentRackPrompt.Hide();
            }
        }

        private void TryInteractWithWorkstation()
        {
            if (currentWorkstation == null || carry == null) return;

            if (currentWorkstation.IsClaimedByOther(NetworkManager.Singleton.LocalClientId))
                return;

            bool isHolding = carry.IsHoldingLocal;
            WorkState state = currentWorkstation.CurrentWorkState;

            if (isHolding && state == WorkState.Idle
                && currentWorkstation.CanAcceptItemClient(carry.HeldItemIdLocal))
            {
                currentWorkstation.TryDepositHeldServerRpc();
            }
            else if (isHolding && state == WorkState.Completed
                     && currentWorkstation.HasAnyOutput()
                     && currentWorkstation.IsIngredientForAnyRecipe(carry.HeldItemIdLocal))
            {
                currentWorkstation.SwapOutputWithHeldServerRpc();
            }
            else if (!isHolding && state == WorkState.Completed)
            {
                currentWorkstation.CollectOutputServerRpc();
            }
            else if (!isHolding && (state == WorkState.Idle || state == WorkState.Failed)
                     && currentWorkstation.HasOccupiedSlotClient())
            {
                currentWorkstation.TryTakeFirstOccupiedSlotServerRpc();
            }
        }

        private void TryInteractWithMopBucket()
        {
            if (currentMopBucket == null || playerController == null) return;

            bool holdingItem = carry != null && carry.IsHoldingLocal;

            if (currentMopBucket.IsMopAvailable && !playerController.HasMop && !holdingItem)
                currentMopBucket.TakeMopServerRpc();
            else if (!currentMopBucket.IsMopAvailable && playerController.HasMop)
                currentMopBucket.ReturnMopServerRpc();
        }

        #region Prompts

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

        private void SetSpillPrompt(SpillZone spill)
        {
            if (currentSpillPrompt != null)
                currentSpillPrompt.Hide();

            currentSpillPrompt = null;

            if (spill == null)
                return;

            currentSpillPrompt = spill.GetComponentInChildren<SpillZoneContextPrompt>(true);
            if (currentSpillPrompt != null)
                currentSpillPrompt.Show();
        }

        private void SetBucketPrompt(MopBucket bucket)
        {
            if (currentBucketPrompt != null)
                currentBucketPrompt.Hide();

            currentBucketPrompt = null;

            if (bucket == null)
                return;

            currentBucketPrompt = bucket.GetComponentInChildren<MopBucketContextPrompt>(true);
            if (currentBucketPrompt != null)
            {
                if (bucket.IsMopAvailable && playerController != null && !playerController.HasMop)
                    currentBucketPrompt.SetMessage("E - Take Mop");
                else if (!bucket.IsMopAvailable && playerController != null && playerController.HasMop)
                    currentBucketPrompt.SetMessage("E - Return Mop");

                currentBucketPrompt.Show();
            }
        }

        private void ClearAllPrompts()
        {
            if (currentRackPrompt != null) currentRackPrompt.Hide();
            if (currentWorkstationPrompt != null) currentWorkstationPrompt.Hide();
            if (currentDeliveryPrompt != null) currentDeliveryPrompt.Hide();
            if (currentSpillPrompt != null) currentSpillPrompt.Hide();
            if (currentBucketPrompt != null) currentBucketPrompt.Hide();

            currentRackPrompt = null;
            currentWorkstationPrompt = null;
            currentDeliveryPrompt = null;
            currentSpillPrompt = null;
            currentBucketPrompt = null;
        }

        #endregion

        #region Hold Indicator

        private void UpdateHoldIndicator(float holdTime)
        {
            if (holdIndicator == null) return;

            Transform target = null;
            if (currentSpillZone != null)
                target = currentSpillZone.transform;
            else if (currentMopBucket != null)
                target = currentMopBucket.transform;
            else if (currentWorkstation != null)
                target = currentWorkstation.transform;
            else if (currentRack != null)
                target = currentRack.transform;
            else if (currentDeliveryPoint != null)
                target = currentDeliveryPoint.transform;

            if (target != null && holdTime > 0f)
                holdIndicator.Show(target, confirmTimer / holdTime);
            else
                holdIndicator.Hide();
        }

        private void HideHoldIndicator()
        {
            if (holdIndicator != null)
                holdIndicator.Hide();
        }

        #endregion

        #region Trigger Detection

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

            SpillZone spill = other.GetComponentInParent<SpillZone>();
            if (spill != null && !spillsInRange.Contains(spill))
                spillsInRange.Add(spill);

            MopBucket bucket = other.GetComponentInParent<MopBucket>();
            if (bucket != null && !bucketsInRange.Contains(bucket))
                bucketsInRange.Add(bucket);
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

            SpillZone spill = other.GetComponentInParent<SpillZone>();
            if (spill != null)
            {
                spillsInRange.Remove(spill);
                if (spill == currentSpillZone)
                {
                    currentSpillZone = null;
                    SetSpillPrompt(null);
                }
            }

            MopBucket bucket = other.GetComponentInParent<MopBucket>();
            if (bucket != null)
            {
                bucketsInRange.Remove(bucket);
                if (bucket == currentMopBucket)
                {
                    currentMopBucket = null;
                    SetBucketPrompt(null);
                }
            }
        }

        #endregion

        #region Find Closest

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

        private SpillZone FindClosestSpillZone()
        {
            if (playerController == null || !playerController.CanMop)
                return null;

            SpillZone best = null;
            float bestD = float.MaxValue;
            Vector2 p = transform.position;

            for (int i = 0; i < spillsInRange.Count; i++)
            {
                SpillZone s = spillsInRange[i];
                if (s == null) continue;

                float d = ((Vector2)s.transform.position - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }

            return best;
        }

        private MopBucket FindClosestMopBucket()
        {
            MopBucket best = null;
            float bestD = float.MaxValue;
            Vector2 p = transform.position;

            for (int i = 0; i < bucketsInRange.Count; i++)
            {
                MopBucket b = bucketsInRange[i];
                if (b == null) continue;

                bool canInteract = false;
                if (b.IsMopAvailable && playerController != null && !playerController.HasMop
                    && (carry == null || !carry.IsHoldingLocal))
                    canInteract = true;
                else if (!b.IsMopAvailable && playerController != null && playerController.HasMop)
                    canInteract = true;

                if (!canInteract) continue;

                float d = ((Vector2)b.transform.position - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = b;
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

            for (int i = spillsInRange.Count - 1; i >= 0; i--)
            {
                if (spillsInRange[i] == null)
                    spillsInRange.RemoveAt(i);
            }

            for (int i = bucketsInRange.Count - 1; i >= 0; i--)
            {
                if (bucketsInRange[i] == null)
                    bucketsInRange.RemoveAt(i);
            }
        }

        #endregion
    }
}
