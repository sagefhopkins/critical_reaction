using UnityEngine;

public class LevelResult
{
    public int score { get; set; }
    public LevelGrade grade { get; set; }
}

public static class LevelEndProcessor
{
    public static LevelResult Evaluate(LevelPerformanceData data)
    {
        int score = LevelScoringSystem.CalculateScore(data);

        LevelGrade grade = GradeSystem.GetGrade(score);

        return new LevelResult
        {
            score = score,
            grade = grade
        };
    }
}
