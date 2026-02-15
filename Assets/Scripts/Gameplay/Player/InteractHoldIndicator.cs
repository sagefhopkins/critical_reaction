using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Player
{
    public class InteractHoldIndicator : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image fillImage;

        [Header("Offset")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

        private Transform target;
        private Camera cam;

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>(true);
            if (fillImage == null)
                fillImage = GetComponentInChildren<Image>(true);

            cam = Camera.main;
            Hide();
        }

        public void Show(Transform interactableTransform, float fillAmount)
        {
            target = interactableTransform;

            if (canvas != null)
                canvas.gameObject.SetActive(true);

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(fillAmount);

            UpdatePosition();
        }

        public void Hide()
        {
            target = null;

            if (canvas != null)
                canvas.gameObject.SetActive(false);

            if (fillImage != null)
                fillImage.fillAmount = 0f;
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (target == null) return;

            if (cam == null)
                cam = Camera.main;

            transform.position = target.position + worldOffset;

            if (cam != null)
                transform.rotation = cam.transform.rotation;
        }
    }
}
