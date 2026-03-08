using UnityEngine;

namespace Gameplay.Items
{
    [System.Serializable]
    public class ReactionStep
    {
        public string StepName;

        public float WindowStart = 0f;
        public float WindowEnd = 0f;

        public float StepDuration = 5f;

        public LabItem FullYieldItem;
        public LabItem DownGradedItem;
    }
}

