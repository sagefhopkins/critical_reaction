using UnityEngine;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance;

    public CampaignProgress progress = new CampaignProgress();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        } 
        else
        {
            Destroy(gameObject);
        }
    }

    public void CompleteLevel(int levelIndex, string skillUnlocked)
    {
        progress.UnlockNextLevel(levelIndex);

        if (!string.IsNullOrEmpty(skillUnlocked))
        {
            progress.AddCompletedSkill(skillUnlocked);
        }
        SaveProgress();
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex <= progress.highestUnlockedLevel;
    }

    void SaveProgress()
    {
        string json = JsonUtility.ToJson(progress);
        PlayerPrefs.SetString("CampaignProgress", json);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        if (PlayerPrefs.HasKey("CampaignProgress"))
        {
            string json = PlayerPrefs.GetString("CampaignProgress");
            progress = JsonUtility.FromJson<CampaignProgress>(json);
        }
    }
}
