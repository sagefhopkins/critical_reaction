using System;
using Unity.Netcode;

namespace Gameplay.Items
{
    public struct RecipeStep : INetworkSerializable, IEquatable<RecipeStep>
    {
        public RecipeStepType StepType;
        public ushort RequiredItemId;
        public MeasurementTolerance Measurement;
        public ReactionWindow Timing;
        public TemperatureControl Temperature;
        public float WorkDuration;

        public bool RequiresItem => RequiredItemId != 0;
        public bool HasMeasurement => Measurement.IsValid;
        public bool HasTiming => Timing.IsValid;
        public bool HasTemperature => Temperature.IsValid;
        public bool HasDuration => WorkDuration > 0;

        public static RecipeStep CreateWeighing(ushort itemId, MeasurementTolerance measurement)
        {
            return new RecipeStep
            {
                StepType = RecipeStepType.Weighing,
                RequiredItemId = itemId,
                Measurement = measurement
            };
        }

        public static RecipeStep CreateVolumetric(ushort itemId, MeasurementTolerance measurement)
        {
            return new RecipeStep
            {
                StepType = RecipeStepType.Volumetric,
                RequiredItemId = itemId,
                Measurement = measurement
            };
        }

        public static RecipeStep CreateHeating(TemperatureControl temperature, float duration)
        {
            return new RecipeStep
            {
                StepType = RecipeStepType.Heating,
                Temperature = temperature,
                WorkDuration = duration
            };
        }

        public static RecipeStep CreateCooling(TemperatureControl temperature)
        {
            return new RecipeStep
            {
                StepType = RecipeStepType.Cooling,
                Temperature = temperature
            };
        }

        public static RecipeStep CreateMixing(float duration)
        {
            return new RecipeStep
            {
                StepType = RecipeStepType.Mixing,
                WorkDuration = duration
            };
        }

        public static RecipeStep CreateReaction(ReactionWindow timing)
        {
            return new RecipeStep
            {
                StepType = RecipeStepType.Reaction,
                Timing = timing
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            byte stepTypeByte = (byte)StepType;
            serializer.SerializeValue(ref stepTypeByte);
            StepType = (RecipeStepType)stepTypeByte;

            serializer.SerializeValue(ref RequiredItemId);
            Measurement.NetworkSerialize(serializer);
            Timing.NetworkSerialize(serializer);
            Temperature.NetworkSerialize(serializer);
            serializer.SerializeValue(ref WorkDuration);
        }

        public bool Equals(RecipeStep other)
        {
            return StepType == other.StepType
                && RequiredItemId == other.RequiredItemId
                && Measurement.Equals(other.Measurement)
                && Timing.Equals(other.Timing)
                && Temperature.Equals(other.Temperature)
                && WorkDuration == other.WorkDuration;
        }

        public override bool Equals(object obj) => obj is RecipeStep other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(StepType, RequiredItemId, Measurement, Timing, Temperature, WorkDuration);
        }

        public static bool operator ==(RecipeStep left, RecipeStep right) => left.Equals(right);
        public static bool operator !=(RecipeStep left, RecipeStep right) => !left.Equals(right);

        public override string ToString()
        {
            return $"RecipeStep({StepType} Item:{RequiredItemId} Temp:{Temperature.TargetTemperature:F1}°C Duration:{WorkDuration:F1}s)";
        }
    }
}
