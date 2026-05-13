using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GallerySign : MonoBehaviour
{
    [Header("交互")]
    [Tooltip("玩家靠近后按 E 显示文字")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("交互距离")]
    [SerializeField] private float interactRange = 1.5f;

    [Header("显示内容")]
    [Tooltip("显示的文字")]
    [TextArea(2, 6)]
    [SerializeField] private string text = "Hello!";
    [Tooltip("文字显示时间（秒），0 表示按键关闭")]
    [SerializeField] private float displayDuration = 0f;

    [Header("外观")]
    [Tooltip("按钮贴图（留空使用默认样式）")]
    [SerializeField] private Sprite buttonSprite;
    [Tooltip("文字框背景颜色")]
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [Tooltip("文字颜色")]
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("文字大小")]
    [SerializeField] private float fontSize = 0.3f;

    private Transform playerTransform;
    private GameObject promptGO;
    private GameObject textBoxGO;
    private bool isShowing;
    private float showTimer;
    private bool playerInRange;

    private void Start()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (buttonSprite != null)
        {
            sr.sprite = buttonSprite;
            sr.color = Color.white;
        }
        else if (sr.sprite == null)
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        sr.sortingOrder = 5;

        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        CreatePrompt();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        playerInRange = dist <= interactRange;

        if (promptGO != null)
            promptGO.SetActive(playerInRange && !isShowing);

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (isShowing)
                HideText();
            else
                ShowText();
        }

        if (isShowing && displayDuration > 0)
        {
            showTimer += Time.deltaTime;
            if (showTimer >= displayDuration)
                HideText();
        }
    }

    private void CreatePrompt()
    {
        promptGO = new GameObject("Prompt");
        promptGO.transform.SetParent(transform);
        promptGO.transform.localPosition = new Vector3(0, 0.8f, 0);

        Vector3 ps = transform.localScale;
        float ix = ps.x != 0 ? 1f / ps.x : 1f;
        float iy = ps.y != 0 ? 1f / ps.y : 1f;

        var sr = promptGO.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = new Color(1f, 1f, 1f, 0.6f);
        sr.sortingOrder = 20;
        promptGO.transform.localScale = new Vector3(0.6f * ix, 0.25f * iy, 1f);

        var textGO = new GameObject("PromptText");
        textGO.transform.SetParent(promptGO.transform);
        textGO.transform.localPosition = Vector3.zero;
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = "E";
        tm.characterSize = 0.15f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;
        var tmRenderer = textGO.GetComponent<MeshRenderer>();
        tmRenderer.sortingOrder = 21;

        promptGO.SetActive(false);
    }

    private void ShowText()
    {
        if (textBoxGO != null) Destroy(textBoxGO);

        isShowing = true;
        showTimer = 0f;

        textBoxGO = new GameObject("TextBox");
        textBoxGO.transform.SetParent(transform);
        textBoxGO.transform.localPosition = new Vector3(0, 1.5f, 0);

        Vector3 ps = transform.localScale;
        float ix = ps.x != 0 ? 1f / ps.x : 1f;
        float iy = ps.y != 0 ? 1f / ps.y : 1f;

        var bgSR = textBoxGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = RuntimeSprite.Get();
        bgSR.color = bgColor;
        bgSR.sortingOrder = 25;

        float boxWidth = Mathf.Max(text.Length * fontSize * 0.6f, 2f);
        int lineCount = text.Split('\n').Length;
        float boxHeight = Mathf.Max(lineCount * fontSize * 1.5f + 0.3f, 0.6f);
        textBoxGO.transform.localScale = new Vector3(boxWidth * ix, boxHeight * iy, 1f);

        var textChild = new GameObject("Text");
        textChild.transform.SetParent(textBoxGO.transform);
        textChild.transform.localPosition = Vector3.zero;
        textChild.transform.localScale = new Vector3(1f / boxWidth, 1f / boxHeight, 1f);

        var tm = textChild.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = fontSize;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = textColor;
        var tmRenderer = textChild.GetComponent<MeshRenderer>();
        tmRenderer.sortingOrder = 26;
    }

    private void HideText()
    {
        isShowing = false;
        if (textBoxGO != null)
        {
            Destroy(textBoxGO);
            textBoxGO = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
