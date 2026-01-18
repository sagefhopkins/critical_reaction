using System;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Coop
{
    [CreateAssetMenu(menuName = "Coop/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Serializable]
        public struct DeliveryTarget
        {
            public LabItem item;
            public int quantity;
        }

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

        [Header("Time")]
        [SerializeField] private float timeLimit = 300f;

        [Header("Objectives")]
        [SerializeField] private DeliveryTarget[] deliveryTargets;

        [Header("Scoring")]
        [SerializeField] private StarThreshold starThresholds;

        public int LevelId => levelId;
        public string LevelName => levelName;
        public float TimeLimit => timeLimit;
        public DeliveryTarget[] DeliveryTargets => deliveryTargets;
        public StarThreshold StarThresholds => starThresholds;

        public int TotalTargetCount
        {
            get
            {
                int total = 0;
                if (deliveryTargets != null)
                {
                    foreach (var t in deliveryTargets)
                        total += t.quantity;
                }
                return total;
            }
        }

        public string PrimaryTargetName
        {
            get
            {
                if (deliveryTargets == null || deliveryTargets.Length == 0)
                    return "Unknown";
                if (deliveryTargets[0].item == null)
                    return "Unknown";
                return deliveryTargets[0].item.DisplayName;
            }
        }
    }
}
