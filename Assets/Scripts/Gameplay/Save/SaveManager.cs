using System;
using System.IO;
using UnityEngine;

namespace Gameplay.Save
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private SaveData data;
        private string savePath;

        public SaveData Data => data;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Path.Combine(Application.persistentDataPath, "save.json");
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Load()
        {
            if (File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    data = JsonUtility.FromJson<SaveData>(json);
                }
                catch (Exception e)
                {

                    data = new SaveData();
                }
            }
            else
            {
                data = new SaveData();
            }

            if (data.levels == null)
                data.levels = Array.Empty<LevelProgress>();
            if (data.coopLevels == null)
                data.coopLevels = Array.Empty<LevelProgress>();
            if (data.settings == null)
                data.settings = new SettingsData();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(savePath, json);
            }
            catch (Exception e)
            {

            }
        }

        public LevelProgress GetLevelProgress(int levelId, bool coop)
        {
            LevelProgress[] arr = coop ? data.coopLevels : data.levels;
            if (arr == null) return null;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].levelId == levelId)
                    return arr[i];
            }

            return null;
        }

        public void RecordLevelResult(int levelId, int score, float timeRemaining, PlayerLevelStats[] stats, bool coop)
        {
            LevelProgress progress = GetOrCreateProgress(levelId, coop);

            if (score > progress.bestScore)
                progress.bestScore = score;

            if (timeRemaining > progress.bestTime)
                progress.bestTime = timeRemaining;

            progress.unlocked = true;

            if (stats != null)
                progress.playerStats = stats;

            UnlockLevel(levelId + 1, coop);

            Save();
        }

        public void RecordLevelFailure(int levelId, PlayerLevelStats[] stats, bool coop)
        {
            LevelProgress progress = GetOrCreateProgress(levelId, coop);

            if (stats != null)
                progress.playerStats = stats;

            Save();
        }

        public void UnlockLevel(int levelId, bool coop)
        {
            LevelProgress progress = GetOrCreateProgress(levelId, coop);
            progress.unlocked = true;
        }

        public bool IsLevelUnlocked(int levelId, bool coop)
        {
            LevelProgress progress = GetLevelProgress(levelId, coop);
            if (progress == null) return false;
            return progress.unlocked || progress.bestScore > 0;
        }

        private LevelProgress GetOrCreateProgress(int levelId, bool coop)
        {
            LevelProgress existing = GetLevelProgress(levelId, coop);
            if (existing != null)
                return existing;

            LevelProgress newProgress = new LevelProgress { levelId = levelId };

            if (coop)
            {
                LevelProgress[] expanded = new LevelProgress[(data.coopLevels?.Length ?? 0) + 1];
                if (data.coopLevels != null)
                    Array.Copy(data.coopLevels, expanded, data.coopLevels.Length);
                expanded[expanded.Length - 1] = newProgress;
                data.coopLevels = expanded;
            }
            else
            {
                LevelProgress[] expanded = new LevelProgress[(data.levels?.Length ?? 0) + 1];
                if (data.levels != null)
                    Array.Copy(data.levels, expanded, data.levels.Length);
                expanded[expanded.Length - 1] = newProgress;
                data.levels = expanded;
            }

            return newProgress;
        }

        public void MigrateSettingsFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("MusicVolume"))
                data.settings.musicVolume = PlayerPrefs.GetInt("MusicVolume", 100);
            if (PlayerPrefs.HasKey("SfxVolume"))
                data.settings.sfxVolume = PlayerPrefs.GetInt("SfxVolume", 100);
            if (PlayerPrefs.HasKey("ResolutionOption"))
                data.settings.resolutionOption = PlayerPrefs.GetInt("ResolutionOption", 0);
            if (PlayerPrefs.HasKey("Fullscreen"))
                data.settings.fullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

            Save();
        }
    }
}
