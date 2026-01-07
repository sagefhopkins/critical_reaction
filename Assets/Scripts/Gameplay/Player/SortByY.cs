using UnityEngine;

public class SortByY : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private int offset;
    [SerializeField] private float scale = 100f;

    private void Awake()
    {
        if (target == null) target = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        target.sortingOrder = offset + Mathf.RoundToInt(-transform.position.y * scale);
    }
}
