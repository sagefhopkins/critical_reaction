using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public static class GradeSystem
{
    public static LevelGrade GetGrade(int score)
    {
        if (score >= 90)
            return LevelGrade.Star;

        if (score >= 75)
            return LevelGrade.Gold;

        if (score >= 50)
            return LevelGrade.Silver;

        if (score >= 25)
            return LevelGrade.Bronze;

        return LevelGrade.Fail;
    }
}
