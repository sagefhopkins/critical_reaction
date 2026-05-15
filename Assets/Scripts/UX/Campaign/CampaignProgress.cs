using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CampaignProgress
{
    public int highestUnlockedLevel = 1;

    public HashSet<string> completedSkills = new HashSet<string>();

    public void UnlockNextLevel(int currentLevel)
    {
        if (currentLevel >= highestUnlockedLevel)
        {
            highestUnlockedLevel = currentLevel + 1;
        }
    }

    public void AddCompletedSkill(string skillName)
    {
        completedSkills.Add(skillName);
    }

    public bool HasSkill(string skillName)
    {
        return completedSkills.Contains(skillName);
    }
}
