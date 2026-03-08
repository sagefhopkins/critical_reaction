using UnityEngine;
using Gameplay.Items;

namespace Gameplay.Coop.Reactions
{
    public class ReactionStepExecutor : MonoBehaviour
    {
        private float startTime;
        private bool active;
        private ReactionStep currentStep;

        public void StartStep(ReactionStep step)
        {
            if (active)
                return;
            currentStep = step;
            startTime = Time.time;
            active = true;
        }
        public void CompleteStep()
        {
            if (!active || currentStep == null)
                return;

            float elapsed = Time.time - startTime;

            LabItem result =
                elapsed >= currentStep.WindowStart &&
                elapsed <= currentStep.WindowEnd
                    ? currentStep.FullYieldItem
                    : currentStep.DownGradedItem;

            active = false;
            currentStep = null;
        }
    }
}


