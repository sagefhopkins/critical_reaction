using Unity.Netcode;

namespace Gameplay.Workstations.LiquidMeasurementBench
{
    public struct ChemicalVolume : INetworkSerializable, System.IEquatable<ChemicalVolume>
    {
        public ushort ChemicalId;
        public float Volume;

        public ChemicalVolume(ushort chemicalId, float volume)
        {
            ChemicalId = chemicalId;
            Volume = volume;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ChemicalId);
            serializer.SerializeValue(ref Volume);
        }

        public bool Equals(ChemicalVolume other)
        {
            return ChemicalId == other.ChemicalId && Volume.Equals(other.Volume);
        }

        public override bool Equals(object obj)
        {
            return obj is ChemicalVolume other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ChemicalId.GetHashCode() ^ Volume.GetHashCode();
        }
    }
}
