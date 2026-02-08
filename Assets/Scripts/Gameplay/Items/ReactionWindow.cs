using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public struct ReactionWindow : INetworkSerializable, IEquatable<ReactionWindow>
    {
        public float MinDuration;
        public float MaxDuration;
        public float OptimalStart;
        public float OptimalEnd;

        public bool IsValid => MaxDuration > 0
            && MinDuration >= 0
            && MinDuration <= OptimalStart
            && OptimalStart <= OptimalEnd
            && OptimalEnd <= MaxDuration;

        public float TotalDuration => MaxDuration;
        public float WindowDuration => MaxDuration - MinDuration;
        public float OptimalDuration => OptimalEnd - OptimalStart;
        public bool HasOptimalRange => OptimalStart < OptimalEnd;

        public static ReactionWindow Create(float min, float max, float optStart, float optEnd)
        {
            return new ReactionWindow
            {
                MinDuration = min,
                MaxDuration = max,
                OptimalStart = optStart,
                OptimalEnd = optEnd
            };
        }

        public static ReactionWindow CreateSimple(float duration)
        {
            return new ReactionWindow
            {
                MinDuration = 0f,
                MaxDuration = duration,
                OptimalStart = 0f,
                OptimalEnd = duration
            };
        }

        public static ReactionWindow CreateWithBuffer(float duration, float buffer)
        {
            return new ReactionWindow
            {
                MinDuration = 0f,
                MaxDuration = duration + buffer,
                OptimalStart = 0f,
                OptimalEnd = duration
            };
        }

        public ReactionTimingResult Evaluate(float elapsed)
        {
            if (elapsed < MinDuration)
                return ReactionTimingResult.TooEarly;
            if (elapsed > MaxDuration)
                return ReactionTimingResult.TooLate;
            if (elapsed >= OptimalStart && elapsed <= OptimalEnd)
                return ReactionTimingResult.Optimal;
            return ReactionTimingResult.Acceptable;
        }

        public float GetProgress(float elapsed)
        {
            if (MaxDuration <= 0f)
                return 0f;
            return Mathf.Clamp01(elapsed / MaxDuration);
        }

        public float GetRemainingTime(float elapsed)
        {
            return Mathf.Max(0f, MaxDuration - elapsed);
        }

        public bool IsWithinWindow(float elapsed)
        {
            return elapsed >= MinDuration && elapsed <= MaxDuration;
        }

        public bool IsOptimal(float elapsed)
        {
            return elapsed >= OptimalStart && elapsed <= OptimalEnd;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MinDuration);
            serializer.SerializeValue(ref MaxDuration);
            serializer.SerializeValue(ref OptimalStart);
            serializer.SerializeValue(ref OptimalEnd);
        }

        public bool Equals(ReactionWindow other)
        {
            return MinDuration == other.MinDuration
                && MaxDuration == other.MaxDuration
                && OptimalStart == other.OptimalStart
                && OptimalEnd == other.OptimalEnd;
        }

        public override bool Equals(object obj) => obj is ReactionWindow other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(MinDuration, MaxDuration, OptimalStart, OptimalEnd);
        }

        public static bool operator ==(ReactionWindow left, ReactionWindow right) => left.Equals(right);
        public static bool operator !=(ReactionWindow left, ReactionWindow right) => !left.Equals(right);

        public override string ToString()
        {
            return $"ReactionWindow(Min:{MinDuration:F1}s Max:{MaxDuration:F1}s Optimal:{OptimalStart:F1}-{OptimalEnd:F1}s)";
        }
    }
}
