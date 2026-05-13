using UnityEngine;

public class GalleryParallaxFrame : MonoBehaviour
{
    [Header("图层")]
    [Tooltip("背景图（远处，移动慢）")]
    [SerializeField] private Sprite backgroundLayer;
    [Tooltip("前景图（近处，移动快）")]
    [SerializeField] private Sprite foregroundLayer;

    [Header("视差强度")]
    [Tooltip("背景层的偏移量（相对于玩家移动）")]
    [SerializeField] private float bgParallaxFactor = 0.02f;
    [Tooltip("前景层的偏移量（相对于玩家移动）")]
    [SerializeField] private float fgParallaxFactor = 0.06f;
    [Tooltip("视差最大偏移量")]
    [SerializeField] private float maxOffset = 0.3f;

    [Header("显示")]
    [SerializeField] private int sortingOrder = 0;
    [Tooltip("说明文字")]
    [SerializeField] private string caption = "";
    [SerializeField] private Color captionColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);

    private SpriteRenderer bgSR;
    private SpriteRenderer fgSR;
    private Transform playerTransform;
    private Vector3 bgBaseLocal;
    private Vector3 fgBaseLocal;

    private void Start()
    {
        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        CreateLayers();

        if (!string.IsNullOrEmpty(caption))
            CreateCaption();
    }

    private void CreateLayers()
    {
        // Background layer
        var bgGO = new GameObject("BG_Layer");
        bgGO.transform.SetParent(transform);
        bgGO.transform.localPosition = Vector3.zero;
        bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sortingOrder = sortingOrder;
        if (backgroundLayer != null)
        {
            bgSR.sprite = backgroundLayer;
            bgSR.color = Color.white;
        }
        else
        {
            bgSR.sprite = RuntimeSprite.Get();
            bgSR.color = new Color(0.2f, 0.25f, 0.35f);
        }
        bgBaseLocal = Vector3.zero;

        // Foreground layer
        var fgGO = new GameObject("FG_Layer");
        fgGO.transform.SetParent(transform);
        fgGO.transform.localPosition = Vector3.zero;
        fgSR = fgGO.AddComponent<SpriteRenderer>();
        fgSR.sortingOrder = sortingOrder + 1;
        if (foregroundLayer != null)
        {
            fgSR.sprite = foregroundLayer;
            fgSR.color = Color.white;
        }
        else
        {
            fgSR.sprite = RuntimeSprite.Get();
            fgSR.color = new Color(0.3f, 0.35f, 0.45f, 0.6f);
            fgGO.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        }
        fgBaseLocal = Vector3.zero;

        // Mask container
        var col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 delta = playerTransform.position - transform.position;

        Vector3 bgOffset = new Vector3(
            Mathf.Clamp(-delta.x * bgParallaxFactor, -maxOffset, maxOffset),
            Mathf.Clamp(-delta.y * bgParallaxFactor, -maxOffset, maxOffset),
            0);

        Vector3 fgOffset = new Vector3(
            Mathf.Clamp(-delta.x * fgParallaxFactor, -maxOffset, maxOffset),
            Mathf.Clamp(-delta.y * fgParallaxFactor, -maxOffset, maxOffset),
            0);

        if (bgSR != null)
            bgSR.transform.localPosition = bgBaseLocal + bgOffset;
        if (fgSR != null)
            fgSR.transform.localPosition = fgBaseLocal + fgOffset;
    }

    private void CreateCaption()
    {
        var textGO = new GameObject("Caption");
        textGO.transform.SetParent(transform);
        textGO.transform.localPosition = new Vector3(0, -0.6f, 0);

        Vector3 ps = transform.localScale;
        textGO.transform.localScale = new Vector3(
            ps.x != 0 ? 1f / ps.x : 1f,
            ps.y != 0 ? 1f / ps.y : 1f, 1f);

        var tm = textGO.AddComponent<TextMesh>();
        tm.text = caption;
        tm.characterSize = 0.15f;
        tm.fontSize = 60;
        tm.anchor = TextAnchor.UpperCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = captionColor;
        textGO.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 2;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "Parallax");
#endif
    }
}
