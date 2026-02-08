using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public struct MeasurementTolerance : INetworkSerializable, IEquatable<MeasurementTolerance>
    {
        public float TargetValue;
        public float AcceptableRange;
        public float OptimalRange;

        public bool IsValid => AcceptableRange > 0f
            && OptimalRange >= 0f
            && OptimalRange <= AcceptableRange;

        public float MinAcceptable => TargetValue - AcceptableRange;
        public float MaxAcceptable => TargetValue + AcceptableRange;
        public float MinOptimal => TargetValue - OptimalRange;
        public float MaxOptimal => TargetValue + OptimalRange;
        public bool HasOptimalRange => OptimalRange > 0f;

        public static MeasurementTolerance Create(float target, float acceptable, float optimal)
        {
            return new MeasurementTolerance
            {
                TargetValue = target,
                AcceptableRange = acceptable,
                OptimalRange = optimal
            };
        }

        public static MeasurementTolerance CreateSimple(float target, float tolerance)
        {
            return new MeasurementTolerance
            {
                TargetValue = target,
                AcceptableRange = tolerance,
                OptimalRange = tolerance
            };
        }

        public MeasurementAccuracyResult Evaluate(float measured)
        {
            float deviation = Mathf.Abs(measured - TargetValue);
            if (deviation <= OptimalRange)
                return MeasurementAccuracyResult.Optimal;
            if (deviation <= AcceptableRange)
                return MeasurementAccuracyResult.Acceptable;
            if (measured < TargetValue)
                return MeasurementAccuracyResult.TooLow;
            return MeasurementAccuracyResult.TooHigh;
        }

        public float GetAccuracy(float measured)
        {
            if (AcceptableRange <= 0f)
                return 0f;
            float deviation = Mathf.Abs(measured - TargetValue);
            return Mathf.Clamp01(1f - (deviation / AcceptableRange));
        }

        public float GetDeviation(float measured)
        {
            return Mathf.Abs(measured - TargetValue);
        }

        public float GetNormalizedDeviation(float measured)
        {
            if (AcceptableRange <= 0f)
                return 1f;
            return Mathf.Clamp01(Mathf.Abs(measured - TargetValue) / AcceptableRange);
        }

        public bool IsWithinTolerance(float measured)
        {
            return Mathf.Abs(measured - TargetValue) <= AcceptableRange;
        }

        public bool IsOptimal(float measured)
        {
            return Mathf.Abs(measured - TargetValue) <= OptimalRange;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TargetValue);
            serializer.SerializeValue(ref AcceptableRange);
            serializer.SerializeValue(ref OptimalRange);
        }

        public bool Equals(MeasurementTolerance other)
        {
            return TargetValue == other.TargetValue
                && AcceptableRange == other.AcceptableRange
                && OptimalRange == other.OptimalRange;
        }

        public override bool Equals(object obj) => obj is MeasurementTolerance other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(TargetValue, AcceptableRange, OptimalRange);
        }

        public static bool operator ==(MeasurementTolerance left, MeasurementTolerance right) => left.Equals(right);
        public static bool operator !=(MeasurementTolerance left, MeasurementTolerance right) => !left.Equals(right);

        public override string ToString()
        {
            return $"MeasurementTolerance(Target:{TargetValue:F2} Acceptable:±{AcceptableRange:F2} Optimal:±{OptimalRange:F2})";
        }
    }
}
