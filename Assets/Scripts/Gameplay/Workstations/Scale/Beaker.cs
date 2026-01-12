using System;
using System.Collections.Generic;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Workstations.Scale
{
    public class Beaker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform particleContainer;
        [SerializeField] private RectTransform beakerBounds;
        [SerializeField] private GameObject particlePrefab;

        [Header("Particle Settings")]
        [SerializeField] private GameObject particlePrefabOverride;

        [Header("Physics")]
        [SerializeField] private float gravity = 500f;
        [SerializeField] private float damping = 0.95f;
        [SerializeField] private float bounciness = 0.3f;

        private List<ChemicalParticle> localParticles = new List<ChemicalParticle>();
        private Rect bounds;
        private int particleCount;
        private bool isConfigured;
        private ChemicalParticleData particleData;

        public event Action OnParticleCountChanged;
        public event Action OnParticleDataChanged;

        public int ParticleCount => particleCount;
        public ChemicalParticleData ParticleData => particleData;
        public bool IsConfigured => isConfigured || particleData != null;

        private void Start()
        {
            UpdateBounds();
        }

        private void OnEnable()
        {
            UpdateBounds();
            SyncLocalParticlesToCount();
        }

        private void Update()
        {
            UpdateBounds();
            SimulateParticles();
        }

        private void UpdateBounds()
        {
            if (beakerBounds == null) return;

            bounds = new Rect(
                -beakerBounds.rect.width / 2f,
                -beakerBounds.rect.height / 2f,
                beakerBounds.rect.width,
                beakerBounds.rect.height
            );
        }

        private void SimulateParticles()
        {
            foreach (var particle in localParticles)
            {
                if (particle == null) continue;

                Vector2 vel = particle.Velocity;
                vel.y -= gravity * Time.deltaTime;
                vel *= damping;
                particle.Velocity = vel;

                Vector2 pos = particle.GetPosition();
                pos += vel * Time.deltaTime;
                particle.SetPosition(pos);

                ConstrainToBounds(particle);
            }
        }

        private void ConstrainToBounds(ChemicalParticle particle)
        {
            Vector2 pos = particle.GetPosition();
            Vector2 vel = particle.Velocity;

            if (pos.x < bounds.xMin)
            {
                pos.x = bounds.xMin;
                vel.x = -vel.x * bounciness;
            }
            else if (pos.x > bounds.xMax)
            {
                pos.x = bounds.xMax;
                vel.x = -vel.x * bounciness;
            }

            if (pos.y < bounds.yMin)
            {
                pos.y = bounds.yMin;
                vel.y = -vel.y * bounciness;
            }
            else if (pos.y > bounds.yMax)
            {
                pos.y = bounds.yMax;
                vel.y = -vel.y * bounciness;
            }

            particle.SetPosition(pos);
            particle.Velocity = vel;
        }

        private void SyncLocalParticlesToCount()
        {
            int targetCount = particleCount;

            while (localParticles.Count > targetCount)
            {
                int lastIndex = localParticles.Count - 1;
                ChemicalParticle particle = localParticles[lastIndex];
                localParticles.RemoveAt(lastIndex);
                if (particle != null)
                    Destroy(particle.gameObject);
            }

            while (localParticles.Count < targetCount)
            {
                SpawnLocalParticle(GetRandomPositionInBounds());
            }
        }

        private Vector2 GetRandomPositionInBounds()
        {
            return new Vector2(
                UnityEngine.Random.Range(bounds.xMin, bounds.xMax),
                UnityEngine.Random.Range(bounds.yMin, bounds.yMax)
            );
        }

        private ChemicalParticle SpawnLocalParticle(Vector2 localPosition)
        {
            GameObject prefabToUse = particlePrefabOverride != null ? particlePrefabOverride : particlePrefab;

            if (prefabToUse == null || particleContainer == null || particleData == null)
                return null;

            GameObject go = Instantiate(prefabToUse, particleContainer);
            ChemicalParticle particle = go.GetComponent<ChemicalParticle>();

            if (particle == null)
            {
                Destroy(go);
                return null;
            }

            particle.Initialize(particleData);
            particle.SetPosition(localPosition);
            localParticles.Add(particle);
            return particle;
        }

        public float GetTotalMass()
        {
            if (particleData == null) return 0f;
            return particleCount * particleData.ParticleMass;
        }

        public List<ChemicalParticle> GetLocalParticles() => localParticles;

        public bool IsPositionInBounds(Vector2 worldPosition)
        {
            if (particleContainer == null) return false;
            Vector2 localPos = particleContainer.InverseTransformPoint(worldPosition);
            return bounds.Contains(localPos);
        }

        #region Particle Count Methods

        public void SetParticleCount(int count)
        {
            int oldCount = particleCount;
            particleCount = Mathf.Max(0, count);

            if (oldCount != particleCount)
            {
                SyncLocalParticlesToCount();
                OnParticleCountChanged?.Invoke();
            }
        }

        public void AddParticles(int count)
        {
            SetParticleCount(particleCount + count);
        }

        public void RemoveParticles(int count)
        {
            SetParticleCount(particleCount - count);
        }

        public int TakeParticles(int requestedCount)
        {
            int available = particleCount;
            int taken = Mathf.Min(available, requestedCount);
            SetParticleCount(particleCount - taken);
            return taken;
        }

        #endregion

        public void ApplyForceToLocalParticles(Vector2 force)
        {
            foreach (var particle in localParticles)
            {
                if (particle != null)
                    particle.ApplyForce(force);
            }
        }

        #region Configuration

        public void ConfigureFromLabItem(LabItem labItem, int newParticleCount)
        {
            if (labItem == null || !labItem.IsChemical)
                return;

            Configure(labItem.ChemicalParticleData, newParticleCount);
        }

        public void Configure(ChemicalParticleData newParticleData, int newParticleCount)
        {
            ClearAllLocalParticles();

            particleData = newParticleData;
            isConfigured = newParticleData != null;
            particleCount = newParticleCount;

            UpdateBounds();
            SyncLocalParticlesToCount();
            OnParticleDataChanged?.Invoke();
        }

        public void ClearConfiguration()
        {
            ClearAllLocalParticles();

            particleData = null;
            isConfigured = false;
            particleCount = 0;

            OnParticleDataChanged?.Invoke();
        }

        private void ClearAllLocalParticles()
        {
            foreach (var particle in localParticles)
            {
                if (particle != null)
                    Destroy(particle.gameObject);
            }
            localParticles.Clear();
        }

        #endregion
    }
}
