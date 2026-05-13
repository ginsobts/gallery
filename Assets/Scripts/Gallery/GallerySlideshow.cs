using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GallerySlideshow : MonoBehaviour
{
    [Header("幻灯片")]
    [Tooltip("按顺序播放的图片列表")]
    [SerializeField] private Sprite[] slides = new Sprite[0];

    [Header("播放")]
    [Tooltip("每张图片停留时间(秒)")]
    [SerializeField] private float interval = 3f;
    [Tooltip("切换淡入淡出时间(秒)")]
    [SerializeField] private float crossfadeDuration = 0.5f;
    [Tooltip("是否循环")]
    [SerializeField] private bool loop = true;
    [Tooltip("自动播放")]
    [SerializeField] private bool autoPlay = true;
    [Tooltip("随机顺序")]
    [SerializeField] private bool shuffle = false;

    [Header("显示")]
    [Tooltip("排序层级")]
    [SerializeField] private int sortingOrder = 0;

    [Header("淡入触发")]
    [Tooltip("是否需要玩家走近才开始")]
    [SerializeField] private bool startOnApproach = false;
    [Tooltip("触发距离")]
    [SerializeField] private float triggerDistance = 5f;

    private SpriteRenderer srFront;
    private SpriteRenderer srBack;
    private int currentIndex = -1;
    private float timer;
    private bool isPlaying;
    private bool isCrossfading;
    private float fadeTimer;
    private Transform playerTransform;
    private bool triggered;
    private int[] playOrder;

    private void Awake()
    {
        srFront = GetComponent<SpriteRenderer>();
        srFront.sortingOrder = sortingOrder + 1;

        var backGO = new GameObject("SlideshowBack");
        backGO.transform.SetParent(transform);
        backGO.transform.localPosition = Vector3.zero;
        backGO.transform.localScale = Vector3.one;
        srBack = backGO.AddComponent<SpriteRenderer>();
        srBack.sortingOrder = sortingOrder;
        srBack.color = new Color(1, 1, 1, 0);
    }

    private void Start()
    {
        if (slides == null || slides.Length == 0) return;

        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null) playerTransform = player.transform;

        BuildPlayOrder();

        if (autoPlay && !startOnApproach)
            Play();
    }

    private void BuildPlayOrder()
    {
        playOrder = new int[slides.Length];
        for (int i = 0; i < slides.Length; i++) playOrder[i] = i;
        if (shuffle)
        {
            for (int i = slides.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = playOrder[i]; playOrder[i] = playOrder[j]; playOrder[j] = tmp;
            }
        }
    }

    public void Play()
    {
        if (slides == null || slides.Length == 0) return;
        isPlaying = true;
        triggered = true;
        ShowNext();
    }

    public void Stop()
    {
        isPlaying = false;
    }

    private void ShowNext()
    {
        currentIndex++;
        if (currentIndex >= slides.Length)
        {
            if (loop)
            {
                currentIndex = 0;
                if (shuffle) BuildPlayOrder();
            }
            else
            {
                isPlaying = false;
                return;
            }
        }

        Sprite nextSprite = slides[playOrder[currentIndex]];
        if (nextSprite == null) return;

        if (srFront.sprite == null)
        {
            srFront.sprite = nextSprite;
            srFront.color = Color.white;
            timer = interval;
            isCrossfading = false;
        }
        else
        {
            srBack.sprite = srFront.sprite;
            srBack.color = srFront.color;
            srFront.sprite = nextSprite;
            srFront.color = new Color(1, 1, 1, 0);
            isCrossfading = true;
            fadeTimer = 0f;
        }
    }

    private void Update()
    {
        if (slides == null || slides.Length == 0) return;

        if (startOnApproach && !triggered)
        {
            if (playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= triggerDistance) Play();
            }
            return;
        }

        if (!isPlaying) return;

        if (isCrossfading)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / Mathf.Max(crossfadeDuration, 0.01f));
            srFront.color = new Color(1, 1, 1, t);
            srBack.color = new Color(1, 1, 1, 1 - t);

            if (t >= 1f)
            {
                isCrossfading = false;
                srBack.color = new Color(1, 1, 1, 0);
                timer = interval;
            }
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            ShowNext();
    }

    private void OnDrawGizmosSelected()
    {
        if (startOnApproach)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
#if UNITY_EDITOR
        int count = slides != null ? slides.Length : 0;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            "Slideshow (" + count + " slides)");
#endif
    }
}
