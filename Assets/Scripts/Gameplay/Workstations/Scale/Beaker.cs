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
        [SerializeField] private float gravity = 15000f;
        [SerializeField] private float damping = 0.85f;
        [SerializeField] private float bounciness = 0.1f;
        [SerializeField] private float particleRadius = 6f;
        [SerializeField] private int collisionIterations = 2;
        [SerializeField] private float separationStrength = 0.3f;

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

            ResolveParticleCollisions();

            foreach (var particle in localParticles)
            {
                if (particle != null)
                    ConstrainToBounds(particle);
            }
        }

        private void ResolveParticleCollisions()
        {
            float minDist = particleRadius * 2f;
            float minDistSq = minDist * minDist;

            for (int iter = 0; iter < collisionIterations; iter++)
            {
                for (int i = 0; i < localParticles.Count; i++)
                {
                    var particleA = localParticles[i];
                    if (particleA == null) continue;

                    Vector2 posA = particleA.GetPosition();

                    for (int j = i + 1; j < localParticles.Count; j++)
                    {
                        var particleB = localParticles[j];
                        if (particleB == null) continue;

                        Vector2 posB = particleB.GetPosition();
                        Vector2 delta = posB - posA;
                        float distSq = delta.sqrMagnitude;

                        if (distSq < minDistSq)
                        {
                            Vector2 normal;
                            float dist;

                            if (distSq < 0.0001f)
                            {
                                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                                normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                                dist = 0f;
                            }
                            else
                            {
                                dist = Mathf.Sqrt(distSq);
                                normal = delta / dist;
                            }

                            float overlap = minDist - dist;
                            Vector2 separation = normal * (overlap * separationStrength);

                            posA -= separation;
                            posB += separation;
                            particleA.SetPosition(posA);
                            particleB.SetPosition(posB);

                            Vector2 velA = particleA.Velocity;
                            Vector2 velB = particleB.Velocity;
                            float relativeVel = Vector2.Dot(velB - velA, normal);

                            if (relativeVel < 0)
                            {
                                Vector2 impulse = normal * (relativeVel * 0.5f);
                                particleA.Velocity = velA + impulse;
                                particleB.Velocity = velB - impulse;
                            }
                        }
                    }
                }
            }
        }

        private void ConstrainToBounds(ChemicalParticle particle)
        {
            Vector2 pos = particle.GetPosition();
            Vector2 vel = particle.Velocity;

            float minX = bounds.xMin + particleRadius;
            float maxX = bounds.xMax - particleRadius;
            float minY = bounds.yMin + particleRadius;
            float maxY = bounds.yMax - particleRadius;

            if (pos.x < minX)
            {
                pos.x = minX;
                vel.x = -vel.x * bounciness;
            }
            else if (pos.x > maxX)
            {
                pos.x = maxX;
                vel.x = -vel.x * bounciness;
            }

            if (pos.y < minY)
            {
                pos.y = minY;
                vel.y = -vel.y * bounciness;
            }
            else if (pos.y > maxY)
            {
                pos.y = maxY;
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
                SpawnLocalParticle(GetSpawnPositionAtTop());
            }
        }

        private Vector2 GetSpawnPositionAtTop()
        {
            return new Vector2(
                UnityEngine.Random.Range(bounds.xMin, bounds.xMax),
                bounds.yMax
            );
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

        public bool RemoveParticleInstance(ChemicalParticle particle)
        {
            if (particle == null) return false;

            if (localParticles.Remove(particle))
            {
                particleCount = Mathf.Max(0, particleCount - 1);
                Destroy(particle.gameObject);
                OnParticleCountChanged?.Invoke();
                return true;
            }

            return false;
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
