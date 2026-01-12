using Gameplay.Workstations.Scale;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(menuName = "Items", fileName = "LabItem")]
    public class LabItem : ScriptableObject
    {
        [SerializeField] private ushort id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;

        [Header("Mass Properties")]
        [SerializeField] private float emptyMass = 0f;
        [Tooltip("Whether this item can contain chemical particles (e.g., beaker, scoop)")]
        [SerializeField] private bool canContainParticles = false;
        [Tooltip("Maximum number of particles this container can hold")]
        [SerializeField] private int maxParticleCapacity = 0;

        [Header("Chemical Properties")]
        [Tooltip("If this item is a chemical/powder, reference its particle data here")]
        [SerializeField] private ChemicalParticleData chemicalParticleData;
        [Tooltip("Total available particle count when this chemical is used as a source")]
        [SerializeField] private int totalParticleCount = 100;

        public ushort Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public float EmptyMass => emptyMass;
        public bool CanContainParticles => canContainParticles;
        public int MaxParticleCapacity => maxParticleCapacity;
        public ChemicalParticleData ChemicalParticleData => chemicalParticleData;
        public int TotalParticleCount => totalParticleCount;
        public bool IsChemical => chemicalParticleData != null;
    }
}
