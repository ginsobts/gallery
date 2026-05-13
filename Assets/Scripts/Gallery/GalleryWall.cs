using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryWall : MonoBehaviour
{
    [Tooltip("墙壁是否可见（调试用，正式可关闭）")]
    [SerializeField] private bool visible = false;
    [SerializeField] private Color wallColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;

        var sr = GetComponent<SpriteRenderer>();
        if (visible)
        {
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprite.Get();
            sr.color = wallColor;
            sr.sortingOrder = -1;
        }
        else if (sr != null)
        {
            sr.enabled = false;
        }
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
    }
}
