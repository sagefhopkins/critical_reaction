using UnityEngine;
using System;

[Serializable]
public class SettingsData
{
    public float masterVolume = 1.0f;
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;

    public string moveLeftKey = "A";
    public string moveRightKey = "D";
    public string interactKey = "E";
}
