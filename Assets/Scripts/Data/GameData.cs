using UnityEngine;
using System;
using System.Collections.Generic;
using Gameplay.Save;

[Serializable]
public class GameData
{
    public List<LevelData> levels = new List<LevelData>();
    public TutorialData tutorials = new TutorialData();
    public SettingsData settings = new SettingsData();
}
