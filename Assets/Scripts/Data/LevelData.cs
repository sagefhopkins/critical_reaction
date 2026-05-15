using UnityEngine;
using System;

[Serializable]
public class LevelData
{
    public int LevelIndex;
    public bool unlocked;
    public bool completed;
    public int bestScore;
    public float bestTime;
}
