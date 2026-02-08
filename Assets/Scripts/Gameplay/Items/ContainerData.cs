using System;
using Unity.Netcode;

namespace Gameplay.Items
{
    public struct ContainerData : INetworkSerializable, IEquatable<ContainerData>
    {
        public const float RoomTemperature = 20f;

        public ushort ContentItemId;
        public float VolumeMl;
        public int ParticleCount;
        public float TemperatureCelsius;
        public byte StepIndex;
        public ContainerStepState StepState;

        public bool IsEmpty => ContentItemId == 0;
        public bool HasContent => !IsEmpty;
        public bool IsFinalProduct => StepState == ContainerStepState.FinalizedProduct;
        public bool IsInvalid => StepState == ContainerStepState.Invalid;
        public bool IsInProgress => StepState == ContainerStepState.InProgress;
        public bool IsStepCompleted => StepState == ContainerStepState.Completed;
        public bool IsDefault => StepState == ContainerStepState.None;
        public bool HasVolume => VolumeMl > 0;
        public bool HasParticles => ParticleCount > 0;
        public bool IsTerminal => StepState == ContainerStepState.FinalizedProduct
            || StepState == ContainerStepState.Invalid;
        public bool CanBeginStep => StepState == ContainerStepState.Empty
            || StepState == ContainerStepState.None;
        public bool CanComplete => StepState == ContainerStepState.InProgress;

        public static ContainerData CreateEmpty()
        {
            return new ContainerData
            {
                TemperatureCelsius = RoomTemperature,
                StepState = ContainerStepState.Empty
            };
        }

        public bool TryBeginStep(ushort contentItemId)
        {
            if (!CanBeginStep)
                return false;
            ContentItemId = contentItemId;
            StepState = ContainerStepState.InProgress;
            return true;
        }

        public bool TryCompleteStep()
        {
            if (!CanComplete)
                return false;
            StepState = ContainerStepState.Completed;
            return true;
        }

        public bool TryAdvanceStep()
        {
            if (StepState != ContainerStepState.Completed)
                return false;
            StepIndex++;
            StepState = ContainerStepState.InProgress;
            return true;
        }

        public bool TryFinalize(ushort outputItemId)
        {
            if (StepState != ContainerStepState.Completed)
                return false;
            ContentItemId = outputItemId;
            StepState = ContainerStepState.FinalizedProduct;
            return true;
        }

        public void Invalidate()
        {
            if (IsTerminal)
                return;
            StepState = ContainerStepState.Invalid;
        }

        public bool TryCompleteStepOrFinalize(int totalSteps, ushort outputItemId)
        {
            if (!CanComplete)
                return false;
            if (StepIndex >= totalSteps - 1)
                return TryFinalize(outputItemId);
            return TryCompleteStep();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ContentItemId);
            serializer.SerializeValue(ref VolumeMl);
            serializer.SerializeValue(ref ParticleCount);
            serializer.SerializeValue(ref TemperatureCelsius);
            serializer.SerializeValue(ref StepIndex);

            byte stepStateByte = (byte)StepState;
            serializer.SerializeValue(ref stepStateByte);
            StepState = (ContainerStepState)stepStateByte;
        }

        public bool Equals(ContainerData other)
        {
            return ContentItemId == other.ContentItemId
                && VolumeMl == other.VolumeMl
                && ParticleCount == other.ParticleCount
                && TemperatureCelsius == other.TemperatureCelsius
                && StepIndex == other.StepIndex
                && StepState == other.StepState;
        }

        public override bool Equals(object obj) => obj is ContainerData other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentItemId, VolumeMl, ParticleCount, TemperatureCelsius, StepIndex, StepState);
        }

        public static bool operator ==(ContainerData left, ContainerData right) => left.Equals(right);
        public static bool operator !=(ContainerData left, ContainerData right) => !left.Equals(right);

        public override string ToString()
        {
            return $"Container(Item:{ContentItemId} Vol:{VolumeMl:F1}ml Particles:{ParticleCount} Temp:{TemperatureCelsius:F1}°C Step:{StepIndex} State:{StepState})";
        }
    }
}
