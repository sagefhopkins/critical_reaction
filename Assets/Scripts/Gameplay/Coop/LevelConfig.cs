using System;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Coop
{
    [CreateAssetMenu(menuName = "Coop/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Serializable]
        public struct StarThreshold
        {
            [Tooltip("Minimum deliveries for 1 star (usually same as target)")]
            public int oneStar;
            [Tooltip("Time remaining (seconds) for 2 stars")]
            public float twoStarTimeRemaining;
            [Tooltip("Time remaining (seconds) for 3 stars")]
            public float threeStarTimeRemaining;
        }

        [Header("Level Info")]
        [SerializeField] private int levelId;
        [SerializeField] private string levelName;
        [SerializeField] private GameObject layoutPrefab;

        [Header("Time")]
        [SerializeField] private float timeLimit = 300f;

        [Header("Orders")]
        [SerializeField] private OrderDefinition[] orders;

        [Header("Scoring")]
        [SerializeField] private StarThreshold starThresholds;

        public int LevelId => levelId;
        public string LevelName => levelName;
        public GameObject LayoutPrefab => layoutPrefab;
        public float TimeLimit => timeLimit;
        public StarThreshold StarThresholds => starThresholds;
        public OrderDefinition[] Orders => orders;

        public int TotalTargetCount
        {
            get
            {
                int total = 0;
                if (orders != null)
                {
                    foreach (var o in orders)
                        total += o.requiredQuantity;
                }
                return total;
            }
        }

        public string PrimaryTargetName
        {
            get
            {
                if (orders == null || orders.Length == 0)
                    return "Unknown";
                if (orders[0].requiredProduct == null)
                    return "Unknown";
                return orders[0].requiredProduct.DisplayName;
            }
        }
    }
}
