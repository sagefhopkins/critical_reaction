using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    public int levelIndex;
    public int requiredDeliveries = 5;
    public string skillUnlocked;

    private int deliveredCount = 0;

    public void OnDeliveryComplete()
    {
        deliveredCount++;

        if (deliveredCount >= requiredDeliveries)
        {
            LevelCompleted();
        }
    }

    void LevelCompleted()
    {
        CampaignManager.Instance.CompleteLevel(levelIndex, skillUnlocked);

        Debug.Log("Level Completed! Next Level Unlocked.");
    }
}
