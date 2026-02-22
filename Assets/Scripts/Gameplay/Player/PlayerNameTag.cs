using TMPro;
using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerNameTag : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.14f, 0f);

        private Transform target;
        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
            if (canvas != null)
                canvas.gameObject.SetActive(false);
        }

        public void SetTarget(Transform playerTransform)
        {
            target = playerTransform;
        }

        public void SetName(string playerName)
        {
            if (label != null)
                label.text = playerName;

            if (canvas != null && !string.IsNullOrEmpty(playerName))
                canvas.gameObject.SetActive(true);
        }

        public void SetColor(Color color)
        {
            if (label != null)
                label.color = color;
        }

        private void LateUpdate()
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
