using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Workstations.Scale
{
    public class ScoopController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform scoopHead;
        [SerializeField] private RectTransform particleContainer;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject particlePrefab;

        [Header("Pickup Settings")]
        [SerializeField] private float pickupRadius = 30f;
        [SerializeField] private int maxParticlesHeld = 100;

        [Header("Loss Settings")]
        [SerializeField] private float velocityLossThreshold = 2000f;
        [SerializeField] private float lossChanceMultiplier = 0.0001f;
        [SerializeField] private int maxLossPerFrame = 1;

        [Header("Shake to Dump Settings")]
        [SerializeField] private float shakeThreshold = 200f;
        [SerializeField] private float shakeWindow = 0.3f;
        [SerializeField] private int shakesRequired = 3;
        [SerializeField] private float dumpForceMultiplier = 2f;

        [Header("Held Particle Physics")]
        [SerializeField] private float heldParticleDamping = 0.9f;
        [SerializeField] private float heldParticleBoundsWidth = 20f;
        [SerializeField] private float heldParticleBoundsHeight = 15f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private Vector2 lastPosition;
        private Vector2 currentVelocity;

        private List<ChemicalParticle> heldParticles = new List<ChemicalParticle>();
        private int heldParticleCount;
        private ChemicalParticleData heldParticleData;

        private List<float> shakeTimestamps = new List<float>();
        private Vector2 lastShakeDirection;
        private bool isDragging;

        private Beaker sourceBeaker;
        private Beaker currentOverBeaker;

        public int HeldParticleCount => heldParticleCount;
        public bool IsHoldingParticles => heldParticleCount > 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
                
            }
            else if (!graphic.raycastTarget)
            {

                graphic.raycastTarget = true;
            }

            if (rectTransform != null)
            {
                originalPosition = rectTransform.anchoredPosition;
                lastPosition = rectTransform.anchoredPosition;
            }

            if (particleContainer == null)
            {
                if (scoopHead != null)
                {
                    particleContainer = scoopHead;

                }
                else
                {
                    GameObject containerObj = new GameObject("ParticleContainer");
                    particleContainer = containerObj.AddComponent<RectTransform>();
                    particleContainer.SetParent(rectTransform, false);
                    particleContainer.anchoredPosition = Vector2.zero;
                    particleContainer.sizeDelta = new Vector2(heldParticleBoundsWidth * 2, heldParticleBoundsHeight * 2);

                }
            }
            else
            {

            }
        }

        private void Update()
        {
            Vector2 currentPosition = rectTransform.anchoredPosition;
            currentVelocity = (currentPosition - lastPosition) / Time.deltaTime;
            lastPosition = currentPosition;

            UpdateHeldParticles();
            CheckForParticleLoss();
            DetectShake();
        }

        private void UpdateHeldParticles()
        {
            foreach (var particle in heldParticles)
            {
                if (particle == null) continue;

                Vector2 vel = particle.Velocity;
                vel -= currentVelocity * 0.1f * Time.deltaTime;
                vel *= heldParticleDamping;
                particle.Velocity = vel;

                Vector2 localPos = particle.GetPosition();
                localPos += vel * Time.deltaTime;

                localPos.x = Mathf.Clamp(localPos.x, -heldParticleBoundsWidth, heldParticleBoundsWidth);
                localPos.y = Mathf.Clamp(localPos.y, -heldParticleBoundsHeight, heldParticleBoundsHeight);

                particle.SetPosition(localPos);
            }
        }

        private void CheckForParticleLoss()
        {
            if (heldParticleCount == 0) return;
            if (isDragging) return;

            float speed = currentVelocity.magnitude;
            if (speed < velocityLossThreshold) return;

            float lossChance = (speed - velocityLossThreshold) * lossChanceMultiplier;
            int lostThisFrame = 0;

            for (int i = heldParticles.Count - 1; i >= 0 && lostThisFrame < maxLossPerFrame; i--)
            {
                if (Random.value < lossChance)
                {
                    DropParticle(i);
                    lostThisFrame++;
                }
            }
        }

        private void DetectShake()
        {
            if (heldParticleCount == 0 || currentOverBeaker == null) return;

            float speed = currentVelocity.magnitude;
            Vector2 direction = currentVelocity.normalized;

            if (speed > shakeThreshold && Vector2.Dot(direction, lastShakeDirection) < -0.5f)
            {
                shakeTimestamps.Add(Time.time);
                lastShakeDirection = direction;

                shakeTimestamps.RemoveAll(t => Time.time - t > shakeWindow * shakesRequired);

                if (shakeTimestamps.Count >= shakesRequired)
                {
                    DumpParticles();
                    shakeTimestamps.Clear();
                }
            }

            if (speed > shakeThreshold)
            {
                lastShakeDirection = direction;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {

            isDragging = true;
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvas == null)
            {
                    canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                {

                    return;
                }
            }
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {

            isDragging = false;
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            if (currentOverBeaker != null && heldParticleCount > 0)
            {

                DumpParticles();
            }
        }

        public void SetSourceBeaker(Beaker beaker)
        {
            sourceBeaker = beaker;
        }

        public void SetOverBeaker(Beaker beaker)
        {
            currentOverBeaker = beaker;
        }

        public void ClearOverBeaker(Beaker beaker)
        {
            if (currentOverBeaker == beaker)
                currentOverBeaker = null;
        }

        public void TryPickupParticles(Beaker beaker)
        {
            if (beaker == null) return;
            if (heldParticleCount >= maxParticlesHeld) return;

            if (particlePrefab == null)
            {

                return;
            }
            if (particleContainer == null)
            {

                return;
            }
            if (beaker.ParticleData == null)
            {

                return;
            }

            ChemicalParticle touchedParticle = FindTouchedParticle(beaker);
            if (touchedParticle == null) return;

            int canPickup = maxParticlesHeld - heldParticleCount;
            if (canPickup <= 0) return;

            heldParticleData = beaker.ParticleData;

            beaker.RemoveParticleInstance(touchedParticle);
            heldParticleCount += 1;
            SpawnHeldParticle();


        }

        private ChemicalParticle FindTouchedParticle(Beaker beaker)
        {
            if (scoopHead == null) return null;

            Vector3 scoopWorldPos = scoopHead.position;
            var particles = beaker.GetLocalParticles();
            float pickupRadiusSq = pickupRadius * pickupRadius;

            foreach (var particle in particles)
            {
                if (particle == null) continue;

                Vector3 particleWorldPos = particle.RectTransform.position;
                float distSq = (scoopWorldPos - particleWorldPos).sqrMagnitude;

                if (distSq <= pickupRadiusSq)
                {
                    return particle;
                }
            }

            return null;
        }

        private void SpawnHeldParticle()
        {
            if (particlePrefab == null || particleContainer == null || heldParticleData == null)
            {

                return;
            }

            GameObject go = Instantiate(particlePrefab);
            RectTransform particleRect = go.GetComponent<RectTransform>();
            Vector2 spawnPosition = Vector2.zero;
            if (particleRect != null)
            {
                particleRect.SetParent(particleContainer, false);
                particleRect.anchorMin = new Vector2(0.5f, 0.5f);
                particleRect.anchorMax = new Vector2(0.5f, 0.5f);
                particleRect.pivot = new Vector2(0.5f, 0.5f);
                spawnPosition = new Vector2(
                    Random.Range(-heldParticleBoundsWidth, heldParticleBoundsWidth),
                    Random.Range(-heldParticleBoundsHeight, heldParticleBoundsHeight)
                );
                particleRect.anchoredPosition = spawnPosition;
                particleRect.localScale = Vector3.one;
            }
            go.SetActive(true);
            go.transform.SetAsLastSibling();

            ChemicalParticle particle = go.GetComponent<ChemicalParticle>();

            if (particle == null)
            {

                Destroy(go);
                return;
            }

            particle.Initialize(heldParticleData);
            heldParticles.Add(particle);


        }

        private void DropParticle(int index)
        {
            if (index < 0 || index >= heldParticles.Count) return;



            ChemicalParticle particle = heldParticles[index];
            heldParticles.RemoveAt(index);
            heldParticleCount = Mathf.Max(0, heldParticleCount - 1);

            if (particle != null)
                Destroy(particle.gameObject);
        }

        private void DumpParticles()
        {
            if (currentOverBeaker == null)
            {

                return;
            }
            if (heldParticleCount == 0)
            {

                return;
            }

            int toDump = heldParticleCount;


            if (!currentOverBeaker.IsConfigured && heldParticleData != null)
            {
                currentOverBeaker.Configure(heldParticleData, toDump);
            }
            else
            {
                currentOverBeaker.AddParticles(toDump);
            }

            currentOverBeaker.ApplyForceToLocalParticles(currentVelocity * dumpForceMultiplier);

            foreach (var particle in heldParticles)
            {
                if (particle != null)
                    Destroy(particle.gameObject);
            }

            heldParticles.Clear();
            heldParticleCount = 0;
            heldParticleData = null;
        }

        public void ResetToOriginalPosition()
        {
            if (rectTransform != null)
                rectTransform.anchoredPosition = originalPosition;
        }

        public void ClearAllHeldParticles()
        {
            foreach (var particle in heldParticles)
            {
                if (particle != null)
                    Destroy(particle.gameObject);
            }

            heldParticles.Clear();
            heldParticleCount = 0;
            heldParticleData = null;
        }
    }
}
