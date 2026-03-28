using System;
using System.Collections.Generic;
using Gameplay.Items;
using Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.Conveyor
{
    public enum ConveyorDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public struct ConveyorEntry : INetworkSerializable, IEquatable<ConveyorEntry>
    {
        public ushort ItemId;
        public float Position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ItemId);
            serializer.SerializeValue(ref Position);
        }

        public bool Equals(ConveyorEntry other) =>
            ItemId == other.ItemId && Mathf.Approximately(Position, other.Position);
    }

    public class ConveyorBelt : NetworkBehaviour
    {
        private const ushort NoneId = 0;

        [Header("Item List")]
        [SerializeField] private LabItem[] items;

        [Header("Conveyor Settings")]
        [SerializeField] private ConveyorDirection direction = ConveyorDirection.Right;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float itemSpacing = 0.5f;

        [Header("Turn")]
        [SerializeField] private bool isTurn;
        [SerializeField] private ConveyorDirection outDirection = ConveyorDirection.Up;

        public float Speed => speed;

        [Header("Bounds")]
        [Tooltip("Assign a 2D or 3D collider. If left empty, auto-detects from this GameObject.")]
        [SerializeField] private Component beltCollider;

        [Header("Output")]
        [Tooltip("Optional: conveyor or workstation to receive items that reach the end.")]
        [SerializeField] private ConveyorBelt outputConveyor;

        public ConveyorBelt OutputConveyor => outputConveyor;

        public void InitializeFrom(ConveyorBelt other)
        {
            items = other.items;
            direction = other.direction;
            speed = other.speed;
            itemSpacing = other.itemSpacing;
            isTurn = other.isTurn;
            outDirection = other.outDirection;
            outputConveyor = other.outputConveyor;
        }

        public NetworkList<ConveyorEntry> Entries { get; private set; }

        private readonly List<SpriteRenderer> rendererPool = new List<SpriteRenderer>();

        private Vector3 StartWorldPos => GetWorldPosAtNormalized(0f);
        private Vector3 EndWorldPos => GetWorldPosAtNormalized(1f);

        private float BeltLength
        {
            get
            {
                Bounds b = GetBeltBounds();
                if (isTurn)
                    return GetTurnLegLength(direction, b) + GetTurnLegLength(outDirection, b);
                return IsDirectionHorizontal(direction) ? b.size.x : b.size.y;
            }
        }

        private static bool IsDirectionHorizontal(ConveyorDirection dir) =>
            dir == ConveyorDirection.Left || dir == ConveyorDirection.Right;

        private void Awake()
        {
            Entries = new NetworkList<ConveyorEntry>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

            if (beltCollider == null)
            {
                Collider2D c2d = GetComponent<Collider2D>();
                if (c2d != null)
                    beltCollider = c2d;
                else
                    beltCollider = GetComponent<Collider>();
            }
        }

        public override void OnNetworkSpawn()
        {
            Entries.OnListChanged += OnEntriesChanged;
            RefreshVisuals();
        }

        public override void OnNetworkDespawn()
        {
            Entries.OnListChanged -= OnEntriesChanged;
        }

        private void Update()
        {
            if (IsServer)
                AdvanceItemsServer();

            UpdateVisualPositions();
        }

        private void AdvanceItemsServer()
        {
            if (!IsServer) return;

            float delta = (speed / BeltLength) * Time.deltaTime;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                ConveyorEntry entry = Entries[i];
                entry.Position += delta;

                if (entry.Position >= 1f)
                {
                    if (TryOutputItem(entry.ItemId))
                    {
                        Entries.RemoveAt(i);
                    }
                    else
                    {
                        entry.Position = 1f;
                        Entries[i] = entry;
                    }
                    continue;
                }

                if (i < Entries.Count - 1)
                {
                }

                Entries[i] = entry;
            }

            PreventOverlap();
        }

        private void PreventOverlap()
        {
            if (Entries.Count < 2) return;

            float spacingNormalized = itemSpacing / Mathf.Max(0.01f, BeltLength);

            for (int i = Entries.Count - 2; i >= 0; i--)
            {
                ConveyorEntry ahead = Entries[i + 1];
                ConveyorEntry behind = Entries[i];

                float minPos = ahead.Position - spacingNormalized;
                if (behind.Position > minPos)
                {
                    behind.Position = minPos;
                    Entries[i] = behind;
                }
            }
        }

        private bool TryOutputItem(ushort itemId)
        {
            if (outputConveyor != null)
                return outputConveyor.TryReceiveItem(itemId);

            return true;
        }

        public bool TryReceiveItem(ushort itemId)
        {
            if (!IsServer) return false;
            if (itemId == NoneId) return false;

            float spacingNormalized = itemSpacing / Mathf.Max(0.01f, BeltLength);
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Position < spacingNormalized)
                    return false;
            }

            Entries.Add(new ConveyorEntry { ItemId = itemId, Position = 0f });
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryPickUpItemServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (Entries.Count == 0) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null || carry.IsHoldingServer()) return;

            int bestIdx = -1;
            float bestPos = -1f;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Position > bestPos)
                {
                    bestPos = Entries[i].Position;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0) return;

            carry.SetHeldItemServer(Entries[bestIdx].ItemId);
            Entries.RemoveAt(bestIdx);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TryPlaceItemServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            PlayerCarry carry = GetCarryForClient(clientId);
            if (carry == null || !carry.IsHoldingServer()) return;

            ushort itemId = carry.GetHeldItemIdServer();
            if (TryReceiveItem(itemId))
                carry.ClearHeldItemServer();
        }

        #region Visuals

        private void OnEntriesChanged(NetworkListEvent<ConveyorEntry> _)
        {
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            EnsureRendererCount(Entries.Count);

            for (int i = 0; i < Entries.Count; i++)
            {
                SpriteRenderer sr = rendererPool[i];
                sr.sprite = GetSpriteById(Entries[i].ItemId);
                sr.enabled = sr.sprite != null;
                sr.transform.position = GetWorldPosAtNormalized(Entries[i].Position);
            }

            for (int i = Entries.Count; i < rendererPool.Count; i++)
            {
                rendererPool[i].enabled = false;
            }
        }

        private void UpdateVisualPositions()
        {
            for (int i = 0; i < Entries.Count && i < rendererPool.Count; i++)
            {
                rendererPool[i].transform.position = GetWorldPosAtNormalized(Entries[i].Position);
            }
        }

        private void EnsureRendererCount(int needed)
        {
            while (rendererPool.Count < needed)
            {
                GameObject go = new GameObject($"ConveyorItem_{rendererPool.Count}");
                go.transform.SetParent(transform);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.enabled = false;
                rendererPool.Add(sr);
            }
        }

        #endregion

        #region Helpers

        private Bounds GetBeltBounds()
        {
            if (beltCollider is Collider2D c2d)
                return c2d.bounds;
            if (beltCollider is Collider c3d)
                return c3d.bounds;

            return new Bounds(transform.position, Vector3.one);
        }

        public float GetBeltLength() => BeltLength;

        public Vector3 GetWorldPosition(float normalizedT) => GetWorldPosAtNormalized(normalizedT);

        private Vector3 GetWorldPosAtNormalized(float t)
        {
            Bounds b = GetBeltBounds();

            if (isTurn)
                return GetTurnPosition(t, b);

            return GetStraightPosition(t, b);
        }

        private Vector3 GetStraightPosition(float t, Bounds b)
        {
            Vector3 center = b.center;

            switch (direction)
            {
                case ConveyorDirection.Right:
                    return new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), center.y, center.z);
                case ConveyorDirection.Left:
                    return new Vector3(Mathf.Lerp(b.max.x, b.min.x, t), center.y, center.z);
                case ConveyorDirection.Up:
                    return new Vector3(center.x, Mathf.Lerp(b.min.y, b.max.y, t), center.z);
                case ConveyorDirection.Down:
                    return new Vector3(center.x, Mathf.Lerp(b.max.y, b.min.y, t), center.z);
                default:
                    return center;
            }
        }

        private Vector3 GetTurnPosition(float t, Bounds b)
        {
            Vector3 entry = GetEdgePoint(direction, true, b);
            Vector3 corner = b.center;
            Vector3 exit = GetEdgePoint(outDirection, false, b);

            float legIn = GetTurnLegLength(direction, b);
            float legOut = GetTurnLegLength(outDirection, b);
            float total = legIn + legOut;

            float dist = t * total;

            if (dist <= legIn)
            {
                float legT = dist / Mathf.Max(0.001f, legIn);
                return Vector3.Lerp(entry, corner, legT);
            }
            else
            {
                float legT = (dist - legIn) / Mathf.Max(0.001f, legOut);
                return Vector3.Lerp(corner, exit, legT);
            }
        }

        private Vector3 GetEdgePoint(ConveyorDirection dir, bool isEntry, Bounds b)
        {
            Vector3 c = b.center;

            switch (dir)
            {
                case ConveyorDirection.Right:
                    return isEntry ? new Vector3(b.min.x, c.y, c.z) : new Vector3(b.max.x, c.y, c.z);
                case ConveyorDirection.Left:
                    return isEntry ? new Vector3(b.max.x, c.y, c.z) : new Vector3(b.min.x, c.y, c.z);
                case ConveyorDirection.Up:
                    return isEntry ? new Vector3(c.x, b.min.y, c.z) : new Vector3(c.x, b.max.y, c.z);
                case ConveyorDirection.Down:
                    return isEntry ? new Vector3(c.x, b.max.y, c.z) : new Vector3(c.x, b.min.y, c.z);
                default:
                    return c;
            }
        }

        private static float GetTurnLegLength(ConveyorDirection dir, Bounds b)
        {
            return IsDirectionHorizontal(dir) ? b.size.x * 0.5f : b.size.y * 0.5f;
        }

        private Sprite GetSpriteById(ushort id)
        {
            if (id == NoneId || items == null) return null;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].Id == id)
                    return items[i].Sprite;
            }
            return null;
        }

        private PlayerCarry GetCarryForClient(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            if (client.PlayerObject == null) return null;
            return client.PlayerObject.GetComponent<PlayerCarry>();
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Bounds b = GetBeltBounds();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(b.center, b.size);

            Gizmos.color = Color.yellow;
            int segments = isTurn ? 10 : 4;
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments;
                float t1 = (i + 1) / (float)segments;
                Gizmos.DrawLine(GetWorldPosAtNormalized(t0), GetWorldPosAtNormalized(t1));
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetWorldPosAtNormalized(0f), 0.06f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(GetWorldPosAtNormalized(1f), 0.06f);
        }
#endif
    }
}
