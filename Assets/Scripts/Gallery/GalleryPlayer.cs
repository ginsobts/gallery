using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class GalleryPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    public void SetMoveSpeed(float speed) { moveSpeed = speed; }

    [Header("Appearance")]
    [Tooltip("角色贴图（留空使用 SpriteRenderer 上已有的）")]
    [SerializeField] private Sprite playerSprite;

    public static GalleryPlayer Instance { get; private set; }

    private static int freezeCount;
    public static bool FreezeMovement => freezeCount > 0;
    public static void Freeze() { freezeCount++; }
    public static void Unfreeze() { freezeCount = Mathf.Max(0, freezeCount - 1); }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private FrameAnimator frameAnimator;

    [Header("Animation")]
    [Tooltip("行走帧动画序列（留空则无动画）")]
    [SerializeField] private Sprite[] walkFrames;
    [Tooltip("帧动画速度")]
    [SerializeField] private float animFps = 6f;

    private bool wasMoving;

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
    }

    private void Start()
    {
        if (walkFrames != null && walkFrames.Length > 0)
        {
            frameAnimator = GetComponent<FrameAnimator>();
            if (frameAnimator == null)
                frameAnimator = gameObject.AddComponent<FrameAnimator>();
            frameAnimator.FPS = animFps;
            frameAnimator.SetFramesAndPlay(walkFrames);
            frameAnimator.Pause();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; freezeCount = 0; }
    }

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
        rb.velocity = FreezeMovement ? Vector2.zero : moveInput * moveSpeed;

        bool moving = moveInput != Vector2.zero;
        if (frameAnimator != null)
        {
            if (moving && !wasMoving) frameAnimator.Resume();
            else if (!moving && wasMoving) frameAnimator.Pause();
        }
        wasMoving = moving;
    }
}
