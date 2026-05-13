using UnityEngine;

public class GallerySecretDoor : MonoBehaviour
{
    public enum RevealMethod
    {
        PushBlock,
        Flashlight,
        Collectible
    }

    [Header("暗门设置")]
    [Tooltip("触发方式")]
    [SerializeField] private RevealMethod method = RevealMethod.PushBlock;
    [Tooltip("暗门遮挡物的 Sprite（墙面贴图）")]
    [SerializeField] private Sprite wallSprite;
    [Tooltip("暗门颜色")]
    [SerializeField] private Color wallColor = new Color(0.3f, 0.3f, 0.35f);
    [Tooltip("暗门大小")]
    [SerializeField] private Vector2 doorSize = new Vector2(2f, 2f);
    [Tooltip("需要的收集品 ID（Collectible 模式）")]
    [SerializeField] private string requiredCollectibleID = "";
    [Tooltip("开启动画时间")]
    [SerializeField] private float openDuration = 1f;
    [Tooltip("打开后是否移除碰撞")]
    [SerializeField] private bool removeCollider = true;

    [Header("隐藏房间")]
    [Tooltip("开门后激活的 GameObject（房间内容）")]
    [SerializeField] private GameObject hiddenRoom;

    private SpriteRenderer doorSR;
    private BoxCollider2D doorCollider;
    private bool isOpen;
    private bool isOpening;
    private float openTimer;
    private Vector3 originalScale;

    private void Awake()
    {
        doorSR = GetComponent<SpriteRenderer>();
        if (doorSR == null) doorSR = gameObject.AddComponent<SpriteRenderer>();

        if (wallSprite != null)
        {
            doorSR.sprite = wallSprite;
            doorSR.color = Color.white;
        }
        else
        {
            doorSR.sprite = RuntimeSprite.Get();
            doorSR.color = wallColor;
        }
        doorSR.sortingOrder = 4;

        doorCollider = GetComponent<BoxCollider2D>();
        if (doorCollider == null) doorCollider = gameObject.AddComponent<BoxCollider2D>();
        doorCollider.size = Vector2.one;
        doorCollider.isTrigger = (method == RevealMethod.PushBlock);

        transform.localScale = new Vector3(doorSize.x, doorSize.y, 1f);
        originalScale = transform.localScale;

        if (hiddenRoom != null)
            hiddenRoom.SetActive(false);
    }

    private void Update()
    {
        if (isOpen || !isOpening) return;

        openTimer += Time.deltaTime;
        float t = Mathf.Clamp01(openTimer / openDuration);

        doorSR.color = new Color(doorSR.color.r, doorSR.color.g, doorSR.color.b, 1f - t);
        transform.localScale = Vector3.Lerp(originalScale,
            new Vector3(originalScale.x, 0.01f, 1f), t);

        if (t >= 1f)
        {
            isOpen = true;
            isOpening = false;
            if (removeCollider) doorCollider.enabled = false;
            if (hiddenRoom != null) hiddenRoom.SetActive(true);
        }
    }

    public void Open()
    {
        if (isOpen || isOpening) return;
        isOpening = true;
        openTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen || isOpening) return;

        if (method == RevealMethod.PushBlock)
        {
            if (other.GetComponent<GalleryPushBlock>() != null)
                Open();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOpen || isOpening) return;

        if (method == RevealMethod.PushBlock)
        {
            if (collision.gameObject.GetComponent<GalleryPushBlock>() != null)
                Open();
        }
    }

    public void CheckFlashlight()
    {
        if (method == RevealMethod.Flashlight && !isOpen)
            Open();
    }

    public void CheckCollectible(string id)
    {
        if (method == RevealMethod.Collectible && !isOpen && id == requiredCollectibleID)
            Open();
    }

    private void OnDrawGizmos()
    {
        Color c = isOpen ? Color.green : wallColor;
        c.a = 0.3f;
        Gizmos.color = c;
        Gizmos.DrawCube(transform.position, (Vector3)doorSize);
        Gizmos.color = new Color(c.r, c.g, c.b, 0.7f);
        Gizmos.DrawWireCube(transform.position, (Vector3)doorSize);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * doorSize.y * 0.6f,
            $"SecretDoor [{method}]");
#endif
    }
}
