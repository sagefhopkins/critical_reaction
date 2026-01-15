using TMPro;
using UnityEngine;

namespace UX
{
    public class WorldContextPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [SerializeField] private bool faceCamera = true;

        private Camera cam;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            cam = Camera.main;
            Hide();
        }

        private void LateUpdate()
        {
            if (!faceCamera) return;

            if (cam == null)
                cam = Camera.main;

            if (cam == null) return;

            transform.rotation = cam.transform.rotation;
        }

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            if (root != null && !root.activeSelf)
                root.SetActive(true);

            if (label != null)
                label.text = message;
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);

            if (label != null)
                label.text = string.Empty;
        }
    }
}