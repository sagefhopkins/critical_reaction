using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Workstations.Scale
{
    public class ChemicalParticle : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;

        private float mass;
        private Vector2 velocity;
        private ChemicalParticleData data;

        public RectTransform RectTransform => rectTransform;
        public float Mass => mass;
        public Vector2 Velocity
        {
            get => velocity;
            set => velocity = value;
        }
        public ChemicalParticleData Data => data;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (image == null)
                image = GetComponent<Image>();
        }

        public void Initialize(ChemicalParticleData particleData)
        {
            data = particleData;
            mass = particleData.ParticleMass;

            if (image != null)
            {
                image.sprite = particleData.ParticleSprite;
                image.color = particleData.ParticleColor;
            }

            velocity = Vector2.zero;
        }

        public void ApplyForce(Vector2 force)
        {
            velocity += force / mass;
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            if (rectTransform != null)
                rectTransform.anchoredPosition = anchoredPosition;
        }

        public Vector2 GetPosition()
        {
            return rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        }
    }
}
