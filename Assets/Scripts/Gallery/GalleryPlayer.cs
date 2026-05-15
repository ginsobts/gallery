using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class GalleryPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    public void SetMoveSpeed(float speed) { moveSpeed = speed; }
    public float GetMoveSpeed() { return moveSpeed; }

    [Header("Appearance")]
    [Tooltip("角色贴图（留空使用 SpriteRenderer 上已有的）")]
    [SerializeField] private Sprite playerSprite;

    public static GalleryPlayer Instance { get; private set; }

    private static int freezeCount;
    public static bool FreezeMovement => freezeCount > 0;
    public static void Freeze() { freezeCount++; }
    public static void Unfreeze() { freezeCount = Mathf.Max(0, freezeCount - 1); }
    public static void ForceUnfreeze() { freezeCount = 0; }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private DirectionalAnimator dirAnimator;

    [Header("Animation")]
    [Tooltip("行走帧动画序列（留空则无动画，仅作为向下行走的后备帧）")]
    [SerializeField] private Sprite[] walkFrames;
    [Tooltip("帧动画速度")]
    [SerializeField] private float animFps = 6f;

    private bool wasMoving;
    private Vector3 lastKnownPos;
    private DirectionalAnimator.Direction lastDirection = DirectionalAnimator.Direction.Down;

    private void Awake()
    {
        Instance = this;
        freezeCount = 0;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (playerSprite != null)
            spriteRenderer.sprite = playerSprite;
        else if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = RuntimeSprite.Get();

        spriteRenderer.sortingOrder = 10;
        lastKnownPos = transform.position;
    }

    private void Start()
    {
        dirAnimator = GetComponent<DirectionalAnimator>();
        if (dirAnimator == null)
            dirAnimator = gameObject.AddComponent<DirectionalAnimator>();

        if (walkFrames != null && walkFrames.Length > 0 && !dirAnimator.HasAnyFrames())
            dirAnimator.SetFrames(DirectionalAnimator.Direction.Down, true, walkFrames);

        dirAnimator.SetFPS(animFps);
        dirAnimator.SetDirection(DirectionalAnimator.Direction.Down);
        dirAnimator.SetWalking(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; freezeCount = 0; }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        if (playerSprite != null)
            sr.sprite = playerSprite;
        sr.sortingOrder = 10;
    }
#endif

    private void Update()
    {
        moveInput = Vector2.zero;
        if (FreezeMovement) return;
        if (Input.GetKey(KeyCode.W)) moveInput.y += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1;
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1;
        moveInput = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        Vector3 curPos = transform.position;
        float dx = curPos.x - lastKnownPos.x;
        float dy = curPos.y - lastKnownPos.y;
        if (dx * dx + dy * dy > 0.01f && moveInput == Vector2.zero)
        {
            rb.velocity = Vector2.zero;
            rb.MovePosition(curPos);
            lastKnownPos = curPos;
            return;
        }

        rb.velocity = FreezeMovement ? Vector2.zero : moveInput * moveSpeed;
        lastKnownPos = transform.position;

        bool moving = moveInput != Vector2.zero;

        if (moving)
        {
            DirectionalAnimator.Direction dir = lastDirection;
            if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
                dir = moveInput.x > 0 ? DirectionalAnimator.Direction.Right : DirectionalAnimator.Direction.Left;
            else
                dir = moveInput.y > 0 ? DirectionalAnimator.Direction.Up : DirectionalAnimator.Direction.Down;
            lastDirection = dir;
        }

        if (dirAnimator != null)
        {
            dirAnimator.SetDirection(lastDirection);
            dirAnimator.SetWalking(moving);
        }
        wasMoving = moving;
    }
}
