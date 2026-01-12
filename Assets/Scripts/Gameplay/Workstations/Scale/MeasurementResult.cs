using System;

namespace Gameplay.Workstations.Scale
{
    [Serializable]
    public struct MeasurementResult
    {
        public bool IsCorrect;
        public string Feedback;
        public float Accuracy;

        public MeasurementResult(bool isCorrect, string feedback, float accuracy = 0f)
        {
            IsCorrect = isCorrect;
            Feedback = feedback;
            Accuracy = accuracy;
        }
    }
}
