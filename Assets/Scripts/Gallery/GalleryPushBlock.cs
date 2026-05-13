using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GalleryPushBlock : MonoBehaviour
{
    [Header("推动参数")]
    [Tooltip("被推动后的初始速度")]
    [SerializeField] private float pushSpeed = 6f;
    [Tooltip("减速摩擦力")]
    [SerializeField] private float friction = 4f;
    [Tooltip("最大滑行距离")]
    [SerializeField] private float maxSlideDistance = 8f;
    [Tooltip("速度低于此值时停止")]
    [SerializeField] private float stopThreshold = 0.3f;
    [Tooltip("每次反弹后速度乘以此系数")]
    [SerializeField] private float bounceDecay = 0.6f;

    [Header("外观")]
    [Tooltip("方块贴图")]
    [SerializeField] private Sprite blockSprite;
    [Tooltip("方块颜色")]
    [SerializeField] private Color blockColor = new Color(0.7f, 0.5f, 0.3f);
    [Tooltip("方块大小")]
    [SerializeField] private float size = 0.6f;

    private Rigidbody2D rb;
    private Vector2 slideDir;
    private float slideSpeed;
    private Vector3 pushOrigin;
    private bool isSliding;
    private SpriteRenderer sr;
    private Camera cachedCam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = Vector2.one;

        var triggerChild = new GameObject("DoorSensor");
        triggerChild.transform.SetParent(transform);
        triggerChild.transform.localPosition = Vector3.zero;
        var triggerCol = triggerChild.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = Vector2.one * 1.1f;
        var relay = triggerChild.AddComponent<PushBlockTriggerRelay>();
        relay.owner = this;

        sr = GetComponent<SpriteRenderer>();
        if (blockSprite != null)
        {
            sr.sprite = blockSprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = blockColor;
        }
        sr.sortingOrder = 6;
        transform.localScale = Vector3.one * size;
        cachedCam = Camera.main;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSliding) return;
        if (collision.gameObject.GetComponent<GalleryPlayer>() == null) return;
        if (collision.contactCount == 0) return;

        Vector2 contactNormal = collision.GetContact(0).normal;
        slideDir = contactNormal.normalized;
        slideSpeed = pushSpeed;
        pushOrigin = transform.position;
        isSliding = true;
    }

    private void Update()
    {
        if (!isSliding) return;

        float distMoved = Vector3.Distance(transform.position, pushOrigin);
        if (distMoved >= maxSlideDistance || slideSpeed <= stopThreshold)
        {
            StopSliding();
            return;
        }

        slideSpeed = Mathf.Max(0, slideSpeed - friction * Time.deltaTime);
        Vector3 newPos = transform.position + (Vector3)(slideDir * slideSpeed * Time.deltaTime);

        Camera cam = cachedCam;
        if (cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 camPos = cam.transform.position;
            float halfBlock = size * 0.5f;

            float minX = camPos.x - halfW + halfBlock;
            float maxX = camPos.x + halfW - halfBlock;
            float minY = camPos.y - halfH + halfBlock;
            float maxY = camPos.y + halfH - halfBlock;

            if (newPos.x < minX) { newPos.x = minX; slideDir.x = -slideDir.x; slideSpeed *= bounceDecay; }
            else if (newPos.x > maxX) { newPos.x = maxX; slideDir.x = -slideDir.x; slideSpeed *= bounceDecay; }

            if (newPos.y < minY) { newPos.y = minY; slideDir.y = -slideDir.y; slideSpeed *= bounceDecay; }
            else if (newPos.y > maxY) { newPos.y = maxY; slideDir.y = -slideDir.y; slideSpeed *= bounceDecay; }
        }

        transform.position = newPos;
    }

    private void StopSliding()
    {
        isSliding = false;
        slideSpeed = 0;
    }

    public void OnDoorContact(Collider2D other)
    {
        var door = other.GetComponent<GalleryImageDoor>();
        if (door != null)
        {
            door.Activate();
            StopSliding();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.5f, 0.3f, 0.4f);
        Gizmos.DrawCube(transform.position, Vector3.one * size);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * size * 0.7f, "PushBlock");
#endif
    }
}
