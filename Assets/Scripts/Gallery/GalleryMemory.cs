using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryMemory : MonoBehaviour
{
    [Header("交互")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRange = 1.5f;

    [Header("内容")]
    [Tooltip("记忆碎片文字")]
    [TextArea(3, 10)]
    [SerializeField] private string memoryText = "";
    [Tooltip("打字机速度（每个字符的间隔秒数）")]
    [SerializeField] private float typeSpeed = 0.05f;
    [Tooltip("全部显示后自动关闭时间（0 = 按键关闭）")]
    [SerializeField] private float autoCloseDelay = 0f;

    [Header("外观")]
    [Tooltip("记忆碎片在场景中的标记贴图")]
    [SerializeField] private Sprite markerSprite;
    [Tooltip("标记颜色")]
    [SerializeField] private Color markerColor = new Color(0.8f, 0.6f, 1f, 0.8f);
    [Tooltip("文字框背景颜色")]
    [SerializeField] private Color boxBgColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);
    [Tooltip("文字颜色")]
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("文字大小")]
    [SerializeField] private int fontSize = 28;

    private Transform playerTransform;
    private bool isShowing;
    private Canvas memoryCanvas;
    private Text displayText;
    private string fullText;
    private int charIndex;
    private float charTimer;
    private bool typingDone;
    private float closeTimer;
    private GameObject promptGO;
    private SpriteRenderer markerSR;

    private void Start()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        markerSR = GetComponent<SpriteRenderer>();
        if (markerSR == null) markerSR = gameObject.AddComponent<SpriteRenderer>();
        if (markerSprite != null)
        {
            markerSR.sprite = markerSprite;
            markerSR.color = Color.white;
        }
        else
        {
            markerSR.sprite = RuntimeSprite.GetCircle(16);
            markerSR.color = markerColor;
            transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        }
        markerSR.sortingOrder = 3;

        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        CreatePrompt();
        AnimateMarker();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRange;

        if (promptGO != null)
            promptGO.SetActive(inRange && !isShowing);

        if (markerSR != null)
        {
            float pulse = 0.8f + Mathf.Sin(Time.time * 2f) * 0.2f;
            markerSR.color = new Color(markerColor.r, markerColor.g, markerColor.b, markerColor.a * pulse);
        }

        if (!isShowing)
        {
            if (inRange && Input.GetKeyDown(interactKey))
                ShowMemory();
        }
        else
        {
            UpdateTyping();

            if (typingDone)
            {
                if (autoCloseDelay > 0)
                {
                    closeTimer += Time.deltaTime;
                    if (closeTimer >= autoCloseDelay)
                        HideMemory();
                }
                else if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape))
                {
                    HideMemory();
                }
            }
            else if (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0))
            {
                charIndex = fullText.Length;
                displayText.text = fullText;
                typingDone = true;
            }
        }
    }

    private void ShowMemory()
    {
        if (string.IsNullOrEmpty(memoryText)) return;
        isShowing = true;
        fullText = memoryText;
        charIndex = 0;
        charTimer = 0f;
        typingDone = false;
        closeTimer = 0f;

        CreateMemoryUI();
        GalleryPlayer.Freeze();
    }

    private void HideMemory()
    {
        isShowing = false;
        if (memoryCanvas != null)
            Destroy(memoryCanvas.gameObject);

        GalleryPlayer.Unfreeze();
    }

    private void OnDestroy()
    {
        if (isShowing) GalleryPlayer.Unfreeze();
    }

    private void UpdateTyping()
    {
        if (typingDone || displayText == null) return;

        charTimer += Time.deltaTime;
        int prevIndex = charIndex;
        while (charTimer >= typeSpeed && charIndex < fullText.Length)
        {
            charTimer -= typeSpeed;
            charIndex++;
        }

        if (charIndex != prevIndex)
            displayText.text = fullText.Substring(0, charIndex);

        if (charIndex >= fullText.Length)
            typingDone = true;
    }

    private void CreateMemoryUI()
    {
        var canvasGO = new GameObject("MemoryCanvas");
        memoryCanvas = canvasGO.AddComponent<Canvas>();
        memoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        memoryCanvas.sortingOrder = 8500;
        canvasGO.AddComponent<CanvasScaler>();

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = boxBgColor;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.1f, 0.2f);
        bgRT.anchorMax = new Vector2(0.9f, 0.6f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(bgGO.transform, false);
        displayText = textGO.AddComponent<Text>();
        displayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        displayText.fontSize = fontSize;
        displayText.color = textColor;
        displayText.alignment = TextAnchor.MiddleCenter;
        displayText.horizontalOverflow = HorizontalWrapMode.Wrap;
        displayText.verticalOverflow = VerticalWrapMode.Overflow;
        displayText.text = "";
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.05f, 0.05f);
        textRT.anchorMax = new Vector2(0.95f, 0.95f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hintText = hintGO.AddComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 16;
        hintText.color = new Color(1, 1, 1, 0.4f);
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.text = autoCloseDelay > 0 ? "" : "Click or press E to continue";
        var hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.3f, 0.12f);
        hintRT.anchorMax = new Vector2(0.7f, 0.18f);
        hintRT.offsetMin = Vector2.zero;
        hintRT.offsetMax = Vector2.zero;
    }

    private void CreatePrompt()
    {
        promptGO = new GameObject("Prompt");
        promptGO.transform.SetParent(transform);
        promptGO.transform.localPosition = new Vector3(0, 0.6f, 0);

        Vector3 ps = transform.localScale;
        float ix = ps.x != 0 ? 1f / ps.x : 1f;
        float iy = ps.y != 0 ? 1f / ps.y : 1f;

        var sr = promptGO.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = new Color(1f, 1f, 1f, 0.5f);
        sr.sortingOrder = 20;
        promptGO.transform.localScale = new Vector3(1.2f * ix, 0.5f * iy, 1f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(promptGO.transform);
        textGO.transform.localPosition = Vector3.zero;
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = "E";
        tm.characterSize = 0.12f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;
        textGO.GetComponent<MeshRenderer>().sortingOrder = 21;
        promptGO.SetActive(false);
    }

    private void AnimateMarker() { }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
