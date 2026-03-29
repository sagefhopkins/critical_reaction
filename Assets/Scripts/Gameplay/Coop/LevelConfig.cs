using System;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Coop
{
    [CreateAssetMenu(menuName = "Critical Reaction/Level Config", fileName = "LevelConfig")]
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

        [Header("Dynamic Orders")]
        [Tooltip("Pool of items that can be randomly requested on recipe trays.")]
        [SerializeField] private LabItem[] availableProducts;
        [Tooltip("Min items per tray order (1 or 2).")]
        [SerializeField] [Range(1, 2)] private int minItemsPerOrder = 1;
        [Tooltip("Max items per tray order (1 or 2).")]
        [SerializeField] [Range(1, 2)] private int maxItemsPerOrder = 2;

        [Header("Music")]
        [SerializeField] private AudioClip music;

        [Header("Scoring")]
        [SerializeField] private StarThreshold starThresholds;

        public int LevelId => levelId;
        public string LevelName => levelName;
        public GameObject LayoutPrefab => layoutPrefab;
        public float TimeLimit => timeLimit;
        public AudioClip Music => music;
        public StarThreshold StarThresholds => starThresholds;
        public LabItem[] AvailableProducts => availableProducts;
        public int MinItemsPerOrder => minItemsPerOrder;
        public int MaxItemsPerOrder => maxItemsPerOrder;

    }
}
