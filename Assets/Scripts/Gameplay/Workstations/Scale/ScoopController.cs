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
        [SerializeField] private int maxParticlesHeld = 10;

        [Header("Loss Settings")]
        [SerializeField] private float velocityLossThreshold = 500f;
        [SerializeField] private float lossChanceMultiplier = 0.001f;
        [SerializeField] private int maxLossPerFrame = 3;

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
                Debug.LogWarning("ScoopController: No Graphic component found. Drag events require an Image or other Graphic with Raycast Target enabled.");
            }
            else if (!graphic.raycastTarget)
            {
                Debug.LogWarning("ScoopController: Graphic.raycastTarget is disabled. Enabling it for drag events.");
                graphic.raycastTarget = true;
            }

            if (rectTransform != null)
            {
                originalPosition = rectTransform.anchoredPosition;
                lastPosition = rectTransform.anchoredPosition;
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
            Debug.Log("ScoopController: OnBeginDrag");
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
                    Debug.LogWarning("ScoopController: No Canvas found, cannot drag");
                    return;
                }
            }
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("ScoopController: OnEndDrag");
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
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

            int canPickup = maxParticlesHeld - heldParticleCount;
            int available = beaker.ParticleCount;
            int toPickup = Mathf.Min(canPickup, available, 1);

            if (toPickup <= 0) return;

            beaker.RemoveParticles(toPickup);

            heldParticleData = beaker.ParticleData;
            heldParticleCount += toPickup;

            for (int i = 0; i < toPickup; i++)
            {
                SpawnHeldParticle();
            }
        }

        private void SpawnHeldParticle()
        {
            if (particlePrefab == null || particleContainer == null || heldParticleData == null)
                return;

            GameObject go = Instantiate(particlePrefab, particleContainer);
            ChemicalParticle particle = go.GetComponent<ChemicalParticle>();

            if (particle == null)
            {
                Destroy(go);
                return;
            }

            particle.Initialize(heldParticleData);
            particle.SetPosition(Vector2.zero);
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
            if (currentOverBeaker == null || heldParticleCount == 0) return;

            int toDump = heldParticleCount;

            currentOverBeaker.AddParticles(toDump);

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
