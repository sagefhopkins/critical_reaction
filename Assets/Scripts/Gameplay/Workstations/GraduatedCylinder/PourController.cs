using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Workstations.GraduatedCylinder
{
    public class PourController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform containerVisual;
        [SerializeField] private Canvas canvas;

        [Header("Pour Settings")]
        [SerializeField] private float pourAngleThreshold = 30f;
        [SerializeField] private float maxPourAngle = 90f;
        [SerializeField] private float maxPourRate = 20f;
        [SerializeField] private float pourStartDelay = 0.2f;

        [Header("Return Animation")]
        [SerializeField] private float returnSpeed = 10f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private float originalRotation;
        private bool isDragging;
        private float pourTimer;

        private FluidContainer sourceContainer;
        private FluidContainer targetContainer;
        private bool isOverTarget;

        public bool IsDragging => isDragging;
        public bool IsPouring => isDragging && isOverTarget && GetCurrentPourRate() > 0f;

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
                originalRotation = rectTransform.localEulerAngles.z;
            }

            if (containerVisual == null)
                containerVisual = rectTransform;
        }

        private void Update()
        {
            if (isDragging && isOverTarget && sourceContainer != null && targetContainer != null)
            {
                pourTimer += Time.deltaTime;

                if (pourTimer >= pourStartDelay)
                {
                    float pourRate = GetCurrentPourRate();
                    if (pourRate > 0f)
                    {
                        float amountToPour = pourRate * Time.deltaTime;
                        float actualPoured = sourceContainer.RemoveVolume(amountToPour);
                        targetContainer.AddVolume(actualPoured);
                    }
                }
            }

            if (!isDragging)
            {
                ReturnToOriginalPosition();
            }
        }

        private void ReturnToOriginalPosition()
        {
            if (rectTransform == null) return;

            Vector2 currentPos = rectTransform.anchoredPosition;
            float currentRot = rectTransform.localEulerAngles.z;

            if (Vector2.Distance(currentPos, originalPosition) > 0.1f ||
                Mathf.Abs(Mathf.DeltaAngle(currentRot, originalRotation)) > 0.1f)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(currentPos, originalPosition, Time.deltaTime * returnSpeed);

                float newRot = Mathf.LerpAngle(currentRot, originalRotation, Time.deltaTime * returnSpeed);
                rectTransform.localEulerAngles = new Vector3(0f, 0f, newRot);
            }
        }

        private float GetCurrentPourRate()
        {
            if (containerVisual == null) return 0f;

            float rotation = containerVisual.localEulerAngles.z;
            if (rotation > 180f) rotation -= 360f;

            float absRotation = Mathf.Abs(rotation);

            if (absRotation < pourAngleThreshold)
                return 0f;

            float pourProgress = Mathf.InverseLerp(pourAngleThreshold, maxPourAngle, absRotation);
            return maxPourRate * pourProgress;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            pourTimer = 0f;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas == null) return;
            }

            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

            Vector2 dragDirection = eventData.delta.normalized;
            float targetRotation = 0f;

            if (dragDirection.x > 0.3f)
                targetRotation = -45f;
            else if (dragDirection.x < -0.3f)
                targetRotation = 45f;

            if (Mathf.Abs(targetRotation) > 0.1f)
            {
                float currentRot = containerVisual.localEulerAngles.z;
                if (currentRot > 180f) currentRot -= 360f;

                float newRot = Mathf.Lerp(currentRot, targetRotation, Time.deltaTime * 5f);
                newRot = Mathf.Clamp(newRot, -maxPourAngle, maxPourAngle);
                containerVisual.localEulerAngles = new Vector3(0f, 0f, newRot);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            pourTimer = 0f;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
        }

        public void SetSourceContainer(FluidContainer container)
        {
            sourceContainer = container;
        }

        public void SetOverTarget(FluidContainer target)
        {
            targetContainer = target;
            isOverTarget = target != null;
        }

        public void ClearOverTarget()
        {
            targetContainer = null;
            isOverTarget = false;
        }

        public void ResetToOriginalPosition()
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
                rectTransform.localEulerAngles = new Vector3(0f, 0f, originalRotation);
            }
        }
    }
}
