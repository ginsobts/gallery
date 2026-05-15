using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class GalleryFollower : MonoBehaviour
{
    [Header("外观")]
    [Tooltip("NPC 贴图")]
    [SerializeField] private Sprite npcSprite;

    [Header("跟随参数")]
    [Tooltip("跟随距离（覆盖全局配置）")]
    [SerializeField] private float followDistance = -1f;
    [Tooltip("跟随速度（覆盖全局配置）")]
    [SerializeField] private float followSpeed = -1f;
    [Tooltip("记录玩家位置的间隔距离")]
    [SerializeField] private float recordInterval = 0.1f;

    [Header("动画")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float animFps = 6f;

    private bool isFollowing;
    private Transform playerTransform;
    private SpriteRenderer sr;
    private CircleCollider2D col;
    private DirectionalAnimator dirAnimator;
    private Vector3 prevPos;

    private const int MaxHistory = 500;
    private Vector3[] historyBuffer = new Vector3[MaxHistory];
    private int historyStart;
    private int historyCount;
    private Vector3 lastRecordedPos;

    private float EffectiveFollowDistance
    {
        get
        {
            if (followDistance > 0) return followDistance;
            var mgr = GalleryManager.Instance;
            if (mgr != null && mgr.Settings != null)
                return mgr.Settings.followDistance;
            return 1.5f;
        }
    }

    private float EffectiveFollowSpeed
    {
        get
        {
            if (followSpeed > 0) return followSpeed;
            var mgr = GalleryManager.Instance;
            if (mgr != null && mgr.Settings != null)
                return mgr.Settings.followSpeed;
            return 4.5f;
        }
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (npcSprite != null)
            sr.sprite = npcSprite;
        else if (sr.sprite == null)
            sr.sprite = RuntimeSprite.Get();
        sr.sortingOrder = 9;

        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
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

        prevPos = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFollowing) return;
        var player = other.GetComponent<GalleryPlayer>();
        if (player == null) return;

        isFollowing = true;
        playerTransform = player.transform;
        historyStart = 0;
        historyCount = 1;
        historyBuffer[0] = playerTransform.position;
        lastRecordedPos = playerTransform.position;

        col.enabled = false;
    }

    private void Update()
    {
        if (!isFollowing || playerTransform == null) return;

        RecordPlayerPosition();
        FollowPath();
        UpdateDirectionalAnimation();
    }

    private Vector3 HistoryAt(int logicalIndex)
    {
        return historyBuffer[(historyStart + logicalIndex) % MaxHistory];
    }

    private void HistoryPush(Vector3 pos)
    {
        if (historyCount < MaxHistory)
        {
            historyBuffer[(historyStart + historyCount) % MaxHistory] = pos;
            historyCount++;
        }
        else
        {
            historyBuffer[historyStart] = pos;
            historyStart = (historyStart + 1) % MaxHistory;
        }
    }

    private void RecordPlayerPosition()
    {
        float dist = Vector3.Distance(playerTransform.position, lastRecordedPos);
        if (dist >= recordInterval)
        {
            HistoryPush(playerTransform.position);
            lastRecordedPos = playerTransform.position;
        }
    }

    private void FollowPath()
    {
        float targetDist = EffectiveFollowDistance;
        float accumulated = 0f;
        Vector3 targetPos = transform.position;

        for (int i = historyCount - 1; i > 0; i--)
        {
            Vector3 cur = HistoryAt(i);
            Vector3 prev = HistoryAt(i - 1);
            float segLen = Vector3.Distance(cur, prev);
            accumulated += segLen;
            if (accumulated >= targetDist)
            {
                float overshoot = accumulated - targetDist;
                Vector3 dir = (cur - prev).normalized;
                targetPos = prev + dir * overshoot;
                break;
            }
        }

        if (accumulated < targetDist && historyCount > 0)
            targetPos = HistoryAt(0);

        transform.position = Vector3.MoveTowards(
            transform.position, targetPos, EffectiveFollowSpeed * Time.deltaTime);
    }

    // ── Runtime setters ──

    public string ElementId { get; set; }

    public Sprite DefaultSprite => npcSprite;

    public void SetSprite(Sprite sprite)
    {
        npcSprite = sprite;
        if (sr != null) sr.sprite = sprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (npcSprite != null)
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sprite = npcSprite;
        }
    }
#endif

    public void SetWalkFrames(Sprite[] frames, float fps = 6f)
    {
        walkFrames = frames;
        animFps = fps;
        if (dirAnimator == null)
        {
            dirAnimator = GetComponent<DirectionalAnimator>();
            if (dirAnimator == null)
                dirAnimator = gameObject.AddComponent<DirectionalAnimator>();
        }
        if (frames != null && frames.Length > 0)
            dirAnimator.SetFrames(DirectionalAnimator.Direction.Down, true, frames);
        dirAnimator.SetFPS(fps);
    }

    public DirectionalAnimator GetDirectionalAnimator()
    {
        if (dirAnimator == null)
        {
            dirAnimator = GetComponent<DirectionalAnimator>();
            if (dirAnimator == null)
                dirAnimator = gameObject.AddComponent<DirectionalAnimator>();
        }
        return dirAnimator;
    }

    private void UpdateDirectionalAnimation()
    {
        if (dirAnimator == null) return;

        Vector3 delta = transform.position - prevPos;
        prevPos = transform.position;
        float sqrMag = delta.sqrMagnitude;
        bool moving = sqrMag > 0.0001f;

        if (moving)
        {
            DirectionalAnimator.Direction dir;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                dir = delta.x > 0 ? DirectionalAnimator.Direction.Right : DirectionalAnimator.Direction.Left;
            else
                dir = delta.y > 0 ? DirectionalAnimator.Direction.Up : DirectionalAnimator.Direction.Down;
            dirAnimator.SetDirection(dir);
        }
        dirAnimator.SetWalking(moving);
    }

    public void SetFollowParams(float distance, float speed, float interval)
    {
        followDistance = distance;
        followSpeed = speed;
        recordInterval = interval;
    }
}
