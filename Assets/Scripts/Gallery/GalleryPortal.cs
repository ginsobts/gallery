using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CircleCollider2D))]
public class GalleryPortal : MonoBehaviour
{
    public enum PortalMode
    {
        Teleport,
        LoadScene
    }

    [Header("传送设置")]
    [SerializeField] private PortalMode mode = PortalMode.Teleport;
    [Tooltip("目标位置（Teleport 模式）")]
    [SerializeField] private Transform targetPoint;
    [Tooltip("目标场景名（LoadScene 模式）")]
    [SerializeField] private string targetScene = "";

    [Header("触发")]
    [Tooltip("自动传送（踩上即触发）")]
    [SerializeField] private bool autoTrigger = true;
    [Tooltip("手动触发按键")]
    [SerializeField] private KeyCode triggerKey = KeyCode.E;

    [Header("过渡效果")]
    [Tooltip("过渡时间")]
    [SerializeField] private float transitionTime = 0.5f;
    [Tooltip("过渡颜色")]
    [SerializeField] private Color transitionColor = Color.black;

    [Header("外观")]
    [Tooltip("传送门贴图")]
    [SerializeField] private Sprite portalSprite;
    [Tooltip("传送门颜色")]
    [SerializeField] private Color portalColor = new Color(0.4f, 0.6f, 1f, 0.8f);
    [Tooltip("传送门大小")]
    [SerializeField] private float portalSize = 0.8f;
    [Tooltip("旋转动画速度")]
    [SerializeField] private float rotateSpeed = 30f;

    private SpriteRenderer sr;
    private SpriteRenderer glowSR;
    private bool playerInside;
    private Transform playerTransform;
    private bool isTransitioning;
    private GameObject promptGO;

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = portalSize;
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (portalSprite != null)
        {
            sr.sprite = portalSprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = RuntimeSprite.GetCircle(32);
            sr.color = portalColor;
        }
        sr.sortingOrder = 2;
        transform.localScale = Vector3.one * portalSize;

        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(transform);
        glowGO.transform.localPosition = Vector3.zero;
        glowGO.transform.localScale = Vector3.one * 1.8f;
        glowSR = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite = RuntimeSprite.GetCircle(32);
        glowSR.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.2f);
        glowSR.sortingOrder = 1;

        if (!autoTrigger)
            CreatePrompt();
    }

    private void Update()
    {
        if (isTransitioning) return;

        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
        if (glowSR != null)
            glowSR.transform.localScale = Vector3.one * 1.8f * pulse;

        if (rotateSpeed != 0 && sr != null)
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

        if (promptGO != null)
            promptGO.SetActive(playerInside && !autoTrigger);

        if (playerInside && !autoTrigger && Input.GetKeyDown(triggerKey))
            StartTransition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<GalleryPlayer>();
        if (player == null) return;

        playerInside = true;
        playerTransform = player.transform;

        if (autoTrigger && !isTransitioning)
            StartTransition();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<GalleryPlayer>() == null) return;
        playerInside = false;
    }

    private void StartTransition()
    {
        if (mode == PortalMode.Teleport && targetPoint == null) return;
        if (mode == PortalMode.LoadScene && string.IsNullOrEmpty(targetScene)) return;
        isTransitioning = true;
        StartCoroutine(DoTransition());
    }

    private System.Collections.IEnumerator DoTransition()
    {
        var canvasGO = new GameObject("PortalTransition");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("Fade");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = new Color(transitionColor.r, transitionColor.g, transitionColor.b, 0f);
        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float half = transitionTime * 0.5f;

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            img.color = new Color(transitionColor.r, transitionColor.g, transitionColor.b, t / half);
            yield return null;
        }
        img.color = new Color(transitionColor.r, transitionColor.g, transitionColor.b, 1f);

        if (mode == PortalMode.Teleport && targetPoint != null && playerTransform != null)
        {
            playerTransform.position = targetPoint.position;
        }
        else if (mode == PortalMode.LoadScene && !string.IsNullOrEmpty(targetScene))
        {
            DontDestroyOnLoad(canvasGO);
            SceneManager.LoadScene(targetScene);
            Destroy(canvasGO, transitionTime);
            yield break;
        }

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            img.color = new Color(transitionColor.r, transitionColor.g, transitionColor.b, 1f - t / half);
            yield return null;
        }

        Destroy(canvasGO);
        isTransitioning = false;
    }

    private void CreatePrompt()
    {
        promptGO = new GameObject("PortalPrompt");
        promptGO.transform.SetParent(transform);
        promptGO.transform.localPosition = new Vector3(0, 1.2f, 0);
        promptGO.transform.localScale = new Vector3(1f / portalSize, 1f / portalSize, 1f);

        var promptSR = promptGO.AddComponent<SpriteRenderer>();
        promptSR.sprite = RuntimeSprite.Get();
        promptSR.color = new Color(0.1f, 0.1f, 0.15f, 0.7f);
        promptSR.sortingOrder = 20;
        promptGO.transform.localScale = new Vector3(1f / portalSize * 0.8f, 1f / portalSize * 0.3f, 1f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(promptGO.transform);
        textGO.transform.localPosition = Vector3.zero;
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = "E - Enter";
        tm.characterSize = 0.12f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        textGO.GetComponent<MeshRenderer>().sortingOrder = 21;
        promptGO.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.4f);
        Gizmos.DrawSphere(transform.position, portalSize);

        if (mode == PortalMode.Teleport && targetPoint != null)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
            Gizmos.DrawLine(transform.position, targetPoint.position);
            Gizmos.DrawSphere(targetPoint.position, 0.2f);
        }
    }
}
