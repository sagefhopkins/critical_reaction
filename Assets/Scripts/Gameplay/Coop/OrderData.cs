using System;
using Unity.Collections;
using Unity.Netcode;

namespace Gameplay.Coop
{
    public struct OrderData : INetworkSerializable, IEquatable<OrderData>
    {
        public ushort RequiredProductId;
        public FixedString64Bytes ProductName;
        public int RequiredQuantity;
        public float TimeLimit;
        public int DeliveredCount;
        public float ElapsedTime;

        public bool IsComplete => DeliveredCount >= RequiredQuantity;
        public bool IsExpired => TimeLimit > 0 && ElapsedTime >= TimeLimit;
        public float RemainingTime => TimeLimit > 0 ? Math.Max(0f, TimeLimit - ElapsedTime) : 0f;
        public int RemainingQuantity => Math.Max(0, RequiredQuantity - DeliveredCount);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RequiredProductId);
            serializer.SerializeValue(ref ProductName);
            serializer.SerializeValue(ref RequiredQuantity);
            serializer.SerializeValue(ref TimeLimit);
            serializer.SerializeValue(ref DeliveredCount);
            serializer.SerializeValue(ref ElapsedTime);
        }

        public bool Equals(OrderData other)
        {
            return RequiredProductId == other.RequiredProductId
                && RequiredQuantity == other.RequiredQuantity
                && TimeLimit == other.TimeLimit
                && DeliveredCount == other.DeliveredCount
                && ElapsedTime == other.ElapsedTime;
        }

        public override bool Equals(object obj) => obj is OrderData other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(RequiredProductId, RequiredQuantity, TimeLimit, DeliveredCount, ElapsedTime);
        }

        public override string ToString()
        {
            return $"Order({ProductName} {DeliveredCount}/{RequiredQuantity} Time:{ElapsedTime:F1}/{TimeLimit:F1})";
        }
    }
}
