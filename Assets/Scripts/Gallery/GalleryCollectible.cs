using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class GalleryCollectible : MonoBehaviour
{
    [Header("纪念品")]
    [Tooltip("纪念品 ID（用于存档，每个必须唯一）")]
    [SerializeField] private string collectibleID = "";
    [Tooltip("纪念品名称")]
    [SerializeField] private string displayName = "纪念品";
    [Tooltip("纪念品贴图")]
    [SerializeField] private Sprite icon;

    [Header("显示")]
    [Tooltip("纪念品颜色（无贴图时）")]
    [SerializeField] private Color itemColor = new Color(1f, 0.85f, 0.3f);
    [Tooltip("物品大小")]
    [SerializeField] private float size = 0.3f;
    [Tooltip("是否隐藏（需要手电筒才能看到）")]
    [SerializeField] private bool hidden = false;
    [Tooltip("收集提示文字")]
    [SerializeField] private string pickupMessage = "";

    private SpriteRenderer sr;
    private bool collected;
    private float bobPhase;
    private float baseY;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = size * 2f;

        if (icon != null)
        {
            sr.sprite = icon;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = RuntimeSprite.GetCircle(16);
            sr.color = itemColor;
        }
        sr.sortingOrder = 8;
        transform.localScale = Vector3.one * size;

        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        baseY = transform.position.y;

        if (string.IsNullOrEmpty(collectibleID))
            collectibleID = gameObject.name + "_" + transform.position.GetHashCode();

        if (PlayerPrefs.GetInt("collect_" + collectibleID, 0) == 1)
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (collected) return;

        float bob = Mathf.Sin(Time.time * 2f + bobPhase) * 0.05f;
        transform.position = new Vector3(transform.position.x, baseY + bob, transform.position.z);

        float glow = (Mathf.Sin(Time.time * 3f + bobPhase) + 1f) * 0.5f;
        float alpha = hidden ? glow * 0.15f : 0.7f + glow * 0.3f;
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<GalleryPlayer>() == null) return;

        Collect();
    }

    private void Collect()
    {
        collected = true;
        PlayerPrefs.SetInt("collect_" + collectibleID, 1);
        PlayerPrefs.Save();

        GalleryShowcase.NotifyCollected(collectibleID, displayName, icon, itemColor);

        if (!string.IsNullOrEmpty(pickupMessage))
            ShowPickupMessage();

        StartCoroutine(CollectAnimation());
    }

    private void ShowPickupMessage()
    {
        var msgGO = new GameObject("PickupMsg");
        msgGO.transform.position = transform.position + Vector3.up * 0.5f;

        var tm = msgGO.AddComponent<TextMesh>();
        tm.text = pickupMessage;
        tm.characterSize = 0.12f;
        tm.fontSize = 60;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = itemColor;
        msgGO.GetComponent<MeshRenderer>().sortingOrder = 30;
        Destroy(msgGO, 2f);
    }

    private System.Collections.IEnumerator CollectAnimation()
    {
        float t = 0;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;

        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float p = t / 0.5f;
            transform.localScale = startScale * (1f + p * 0.5f);
            transform.position = startPos + Vector3.up * p * 0.3f;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - p);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public static void ResetAllCollections()
    {
        var all = FindObjectsOfType<GalleryCollectible>(true);
        foreach (var c in all)
        {
            PlayerPrefs.DeleteKey("collect_" + c.collectibleID);
            c.collected = false;
            c.gameObject.SetActive(true);
        }
        PlayerPrefs.Save();
    }

    private void OnDrawGizmos()
    {
        Color c = hidden ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(1f, 0.85f, 0.3f, 0.5f);
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, size * 0.5f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f,
            string.IsNullOrEmpty(displayName) ? collectibleID : displayName);
#endif
    }
}
