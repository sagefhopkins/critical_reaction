using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    public struct TemperatureControl : INetworkSerializable, IEquatable<TemperatureControl>
    {
        public float TargetTemperature;
        public float AcceptableRange;
        public float OptimalRange;
        public float HeatingRate;
        public float CoolingRate;

        public bool IsValid => AcceptableRange > 0f
            && OptimalRange >= 0f
            && OptimalRange <= AcceptableRange;

        public float MinAcceptable => TargetTemperature - AcceptableRange;
        public float MaxAcceptable => TargetTemperature + AcceptableRange;
        public float MinOptimal => TargetTemperature - OptimalRange;
        public float MaxOptimal => TargetTemperature + OptimalRange;
        public bool HasOptimalRange => OptimalRange > 0f;
        public bool HasHeatingRate => HeatingRate > 0f;
        public bool HasCoolingRate => CoolingRate > 0f;

        public static TemperatureControl Create(float target, float acceptable, float optimal,
            float heatingRate, float coolingRate)
        {
            return new TemperatureControl
            {
                TargetTemperature = target,
                AcceptableRange = acceptable,
                OptimalRange = optimal,
                HeatingRate = heatingRate,
                CoolingRate = coolingRate
            };
        }

        public static TemperatureControl CreateSimple(float target, float range, float rate)
        {
            return new TemperatureControl
            {
                TargetTemperature = target,
                AcceptableRange = range,
                OptimalRange = range,
                HeatingRate = rate,
                CoolingRate = rate
            };
        }

        public static TemperatureControl CreateHeating(float target, float acceptable, float optimal,
            float heatingRate)
        {
            return new TemperatureControl
            {
                TargetTemperature = target,
                AcceptableRange = acceptable,
                OptimalRange = optimal,
                HeatingRate = heatingRate
            };
        }

        public static TemperatureControl CreateCooling(float target, float acceptable, float optimal,
            float coolingRate)
        {
            return new TemperatureControl
            {
                TargetTemperature = target,
                AcceptableRange = acceptable,
                OptimalRange = optimal,
                CoolingRate = coolingRate
            };
        }

        public TemperatureResult Evaluate(float currentTemp)
        {
            float deviation = Mathf.Abs(currentTemp - TargetTemperature);
            if (deviation <= OptimalRange)
                return TemperatureResult.Optimal;
            if (deviation <= AcceptableRange)
                return TemperatureResult.Acceptable;
            if (currentTemp < TargetTemperature)
                return TemperatureResult.TooLow;
            return TemperatureResult.TooHigh;
        }

        public float GetAccuracy(float currentTemp)
        {
            if (AcceptableRange <= 0f)
                return 0f;
            float deviation = Mathf.Abs(currentTemp - TargetTemperature);
            return Mathf.Clamp01(1f - (deviation / AcceptableRange));
        }

        public float GetDeviation(float currentTemp)
        {
            return Mathf.Abs(currentTemp - TargetTemperature);
        }

        public float GetNormalizedDeviation(float currentTemp)
        {
            if (AcceptableRange <= 0f)
                return 1f;
            return Mathf.Clamp01(Mathf.Abs(currentTemp - TargetTemperature) / AcceptableRange);
        }

        public bool IsWithinRange(float currentTemp)
        {
            return Mathf.Abs(currentTemp - TargetTemperature) <= AcceptableRange;
        }

        public bool IsOptimal(float currentTemp)
        {
            return Mathf.Abs(currentTemp - TargetTemperature) <= OptimalRange;
        }

        public float SimulateHeating(float currentTemp, float deltaTime)
        {
            return currentTemp + HeatingRate * deltaTime;
        }

        public float SimulateCooling(float currentTemp, float deltaTime)
        {
            return currentTemp - CoolingRate * deltaTime;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TargetTemperature);
            serializer.SerializeValue(ref AcceptableRange);
            serializer.SerializeValue(ref OptimalRange);
            serializer.SerializeValue(ref HeatingRate);
            serializer.SerializeValue(ref CoolingRate);
        }

        public bool Equals(TemperatureControl other)
        {
            return TargetTemperature == other.TargetTemperature
                && AcceptableRange == other.AcceptableRange
                && OptimalRange == other.OptimalRange
                && HeatingRate == other.HeatingRate
                && CoolingRate == other.CoolingRate;
        }

        public override bool Equals(object obj) => obj is TemperatureControl other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(TargetTemperature, AcceptableRange, OptimalRange, HeatingRate, CoolingRate);
        }

        public static bool operator ==(TemperatureControl left, TemperatureControl right) => left.Equals(right);
        public static bool operator !=(TemperatureControl left, TemperatureControl right) => !left.Equals(right);

        public override string ToString()
        {
            return $"TemperatureControl(Target:{TargetTemperature:F1}°C Acceptable:±{AcceptableRange:F1} Optimal:±{OptimalRange:F1} Heat:{HeatingRate:F1}/s Cool:{CoolingRate:F1}/s)";
        }
    }
}
