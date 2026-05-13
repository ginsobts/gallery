using UnityEngine;

public class GalleryGroundTransition : MonoBehaviour
{
    [Header("过渡带设置")]
    [Tooltip("过渡带宽度（世界单位）")]
    [SerializeField] private float transitionWidth = 3f;
    [Tooltip("左侧地面颜色")]
    [SerializeField] private Color leftColor = new Color(0.35f, 0.55f, 0.25f);
    [Tooltip("右侧地面颜色")]
    [SerializeField] private Color rightColor = new Color(0.6f, 0.5f, 0.35f);
    [Tooltip("左侧地面贴图（可选）")]
    [SerializeField] private Sprite leftSprite;
    [Tooltip("右侧地面贴图（可选）")]
    [SerializeField] private Sprite rightSprite;
    [Tooltip("高度")]
    [SerializeField] private float height = 2f;
    [Tooltip("渐变分辨率（段数）")]
    [SerializeField] private int segments = 12;
    [Tooltip("排序层级")]
    [SerializeField] private int sortingOrder = -5;

    private void Start()
    {
        CreateTransitionStrip();
    }

    private void CreateTransitionStrip()
    {
        float segWidth = transitionWidth / segments;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Color c = Color.Lerp(leftColor, rightColor, t);

            var go = new GameObject($"TransSegment_{i}");
            go.transform.SetParent(transform);
            float xOff = -transitionWidth * 0.5f + segWidth * (i + 0.5f);
            go.transform.localPosition = new Vector3(xOff, 0, 0);
            go.transform.localScale = new Vector3(segWidth + 0.02f, height, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            if (leftSprite != null && rightSprite != null)
            {
                sr.sprite = t < 0.5f ? leftSprite : rightSprite;
                sr.color = new Color(1f, 1f, 1f, 1f - Mathf.Abs(t - 0.5f) * 0.5f);
            }
            else
            {
                sr.sprite = RuntimeSprite.Get();
                sr.color = c;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(leftColor.r, leftColor.g, leftColor.b, 0.3f);
        Gizmos.DrawCube(transform.position - Vector3.right * transitionWidth * 0.25f,
            new Vector3(transitionWidth * 0.5f, height, 0.1f));
        Gizmos.color = new Color(rightColor.r, rightColor.g, rightColor.b, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.right * transitionWidth * 0.25f,
            new Vector3(transitionWidth * 0.5f, height, 0.1f));
    }
}
