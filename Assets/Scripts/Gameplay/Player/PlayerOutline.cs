using UnityEngine;

namespace Gameplay.Player
{
    public class PlayerOutline : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private SpriteRenderer outlineRenderer;

        private MaterialPropertyBlock mpb;

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();
            if (outlineRenderer != null)
                outlineRenderer.enabled = false;
        }

        public void SetColor(Color color)
        {
            if (outlineRenderer == null) return;
            outlineRenderer.enabled = true;
            outlineRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", color);
            outlineRenderer.SetPropertyBlock(mpb);
        }

        private void LateUpdate()
        {
            if (source == null || outlineRenderer == null) return;

            outlineRenderer.sprite = source.sprite;
            outlineRenderer.flipX = source.flipX;
            outlineRenderer.flipY = source.flipY;
            outlineRenderer.sortingLayerID = source.sortingLayerID;
            outlineRenderer.sortingOrder = source.sortingOrder - 1;
        }
    }
}
