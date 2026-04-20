using UnityEngine;

public static class LevelScoringSystem
{
    public static int CalculateScore(LevelPerformanceData data)
    {
        float score = 0f;

        score += data.completionPercent * 0.5f;

        score += Mathf.Clamp(data.output * 2f, 0, 25f);

        score -= data.waste * 1.5f;
        score -= data.mishaps * 3f;

        if (data.targetTime > 0)
        {
            float timeRatio = data.targetTime / Mathf.Max(data.timeTaken, 0.1f);
            score += Mathf.Clamp(timeRatio * 10f, -10f, 10f);
        }
        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }
}
