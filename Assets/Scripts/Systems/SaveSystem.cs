using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string path = Application.persistentDataPath + "/save.json";

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log("Game Saved: " + path);
    }
    public static GameData Load()
    {
        if (!File.Exists(path))
        {
            Debug.Log("No save file found. Creating new data.");
            return CreateNewData();
        }
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<GameData>(json);
    }
    private static GameData CreateNewData()
    {
        GameData data = new GameData();
        for (int i = 0; i < 10; i++)
        {
            LevelData level = new LevelData()
            {
                LevelIndex = i,
                unlocked = (i == 0),
                completed = false,
                bestScore = 0,
                bestTime = float.MaxValue
            };
            data.levels.Add(level);
        }
        return data;
    }
}
