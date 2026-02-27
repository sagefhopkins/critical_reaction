using System;

namespace Gameplay.Save
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string playerName;
        public LevelProgress[] levels;
        public LevelProgress[] coopLevels;
        public bool[] tutorialFlags;
        public SettingsData settings;
    }

    [Serializable]
    public class LevelProgress
    {
        public int levelId;
        public int bestScore;
        public float bestTime;
        public bool unlocked;
        public PlayerLevelStats[] playerStats;
    }

    [Serializable]
    public class PlayerLevelStats
    {
        public int deposits;
        public int collections;
        public int deliveries;
        public int failures;
    }

    [Serializable]
    public class SettingsData
    {
        public int musicVolume = 100;
        public int sfxVolume = 100;
        public int resolutionOption;
        public bool fullScreen = true;
    }
}
