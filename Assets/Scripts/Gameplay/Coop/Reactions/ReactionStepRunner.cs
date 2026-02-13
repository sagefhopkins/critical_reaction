using UnityEngine;

public class ReactionStepRunner : MonoBehaviour
{
    private StepTimerController activeStepTimer;
    public void StartStep(ReactionStepData step)
    {

        if (step.hasTimeLimit)
        {
            activeStepTimer = new StepTimerController(
                step.timeLimit,
                OnStepTimerExpired
            );
        }
    }

    void Update()
    {
        activeStepTimer?.Tick(Time.deltaTime);
    }
    public void CompleteStep()
    {
        activeStepTimer?.Stop();
        activeStepTimer = null;
    }
    private void OnStepTimerExpired()
    {
        Debug.Log("Step failed - too slow.");
    }
}
