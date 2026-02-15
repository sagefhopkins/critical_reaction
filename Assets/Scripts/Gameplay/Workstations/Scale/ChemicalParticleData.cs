using UnityEngine;

namespace Gameplay.Workstations.Scale
{
    [CreateAssetMenu(fileName = "ChemicalParticleData", menuName = "Critical Reaction/Particle Data")]
    public class ChemicalParticleData : ScriptableObject
    {
        [SerializeField] private string chemicalName = "Sodium Chloride";
        [SerializeField] private float particleMass = 0.1f;
        [SerializeField] private Color particleColor = Color.white;
        [SerializeField] private Sprite particleSprite;

        public string ChemicalName => chemicalName;
        public float ParticleMass => particleMass;
        public Color ParticleColor => particleColor;
        public Sprite ParticleSprite => particleSprite;
    }
}
