using System;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Coop
{
    [Serializable]
    public struct OrderDefinition
    {
        public LabItem requiredProduct;

        [Min(1)]
        public int requiredQuantity;

        [Min(0f)]
        [Tooltip("Per-order time limit in seconds. 0 = use level timer.")]
        public float timeLimit;

        public OrderData ToOrderData()
        {
            return new OrderData
            {
                RequiredProductId = requiredProduct != null ? requiredProduct.Id : (ushort)0,
                ProductName = requiredProduct != null ? requiredProduct.DisplayName : "Unknown",
                RequiredQuantity = requiredQuantity,
                TimeLimit = timeLimit,
                DeliveredCount = 0,
                ElapsedTime = 0f
            };
        }
    }
}
