using UnityEngine;
using UnityEngine.UI;

public class GalleryPhotoViewer : MonoBehaviour
{
    [Header("照片组")]
    [Tooltip("这个展示区域包含的所有照片")]
    [SerializeField] private Sprite[] photos;
    [Tooltip("交互键")]
    [SerializeField] private KeyCode viewKey = KeyCode.E;
    [Tooltip("交互距离")]
    [SerializeField] private float interactRange = 2f;

    [Header("UI 样式")]
    [Tooltip("背景遮罩透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float overlayAlpha = 0.9f;
    [Tooltip("照片切换动画时间")]
    [SerializeField] private float transitionTime = 0.3f;

    private Transform playerTransform;
    private bool isViewing;
    private int currentIndex;

    private Canvas viewerCanvas;
    private Image bgImage;
    private Image photoImage;
    private Text indexText;
    private Text hintText;
    private GameObject promptGO;

    private float transitionTimer;
    private bool transitioning;
    private int targetIndex;

    private void Start()
    {
        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (photos != null && photos.Length > 0 && sr.sprite == null)
        {
            sr.sprite = photos[0];
            sr.color = Color.white;
        }
        else if (sr.sprite == null)
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = new Color(0.3f, 0.3f, 0.4f);
        }
        sr.sortingOrder = 1;

        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        CreatePrompt();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRange;

        if (promptGO != null)
            promptGO.SetActive(inRange && !isViewing);

        if (!isViewing)
        {
            if (inRange && Input.GetKeyDown(viewKey))
                OpenViewer();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(viewKey))
                CloseViewer();
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                Navigate(1);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                Navigate(-1);

            if (transitioning)
                UpdateTransition();
        }
    }

    private void OpenViewer()
    {
        if (photos == null || photos.Length == 0) return;
        isViewing = true;
        currentIndex = 0;
        CreateViewerUI();
        ShowPhoto(currentIndex);
        GalleryPlayer.Freeze();
    }

    private void CloseViewer()
    {
        isViewing = false;
        if (viewerCanvas != null)
            Destroy(viewerCanvas.gameObject);

        GalleryPlayer.Unfreeze();
    }

    private void OnDestroy()
    {
        if (isViewing) GalleryPlayer.Unfreeze();
    }

    private void Navigate(int dir)
    {
        if (photos == null || photos.Length <= 1) return;
        targetIndex = (currentIndex + dir + photos.Length) % photos.Length;
        transitioning = true;
        transitionTimer = 0f;
    }

    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = transitionTimer / transitionTime;

        if (t < 0.5f)
        {
            float alpha = 1f - (t * 2f);
            if (photoImage != null)
                photoImage.color = new Color(1, 1, 1, alpha);
        }
        else
        {
            if (t >= 0.5f && currentIndex != targetIndex)
            {
                currentIndex = targetIndex;
                ShowPhoto(currentIndex);
            }
            float alpha = (t - 0.5f) * 2f;
            if (photoImage != null)
                photoImage.color = new Color(1, 1, 1, Mathf.Min(alpha, 1f));
        }

        if (t >= 1f)
        {
            transitioning = false;
            if (photoImage != null)
                photoImage.color = Color.white;
        }
    }

    private void CreateViewerUI()
    {
        var canvasGO = new GameObject("PhotoViewerCanvas");
        viewerCanvas = canvasGO.AddComponent<Canvas>();
        viewerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        viewerCanvas.sortingOrder = 9000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, overlayAlpha);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        var photoGO = new GameObject("Photo");
        photoGO.transform.SetParent(canvasGO.transform, false);
        photoImage = photoGO.AddComponent<Image>();
        photoImage.preserveAspect = true;
        var photoRT = photoGO.GetComponent<RectTransform>();
        photoRT.anchorMin = new Vector2(0.05f, 0.05f);
        photoRT.anchorMax = new Vector2(0.95f, 0.9f);
        photoRT.offsetMin = Vector2.zero;
        photoRT.offsetMax = Vector2.zero;

        var indexGO = new GameObject("IndexText");
        indexGO.transform.SetParent(canvasGO.transform, false);
        indexText = indexGO.AddComponent<Text>();
        indexText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        indexText.fontSize = 24;
        indexText.alignment = TextAnchor.MiddleCenter;
        indexText.color = Color.white;
        var indexRT = indexGO.GetComponent<RectTransform>();
        indexRT.anchorMin = new Vector2(0.4f, 0.92f);
        indexRT.anchorMax = new Vector2(0.6f, 0.98f);
        indexRT.offsetMin = Vector2.zero;
        indexRT.offsetMax = Vector2.zero;

        var hintGO = new GameObject("HintText");
        hintGO.transform.SetParent(canvasGO.transform, false);
        hintText = hintGO.AddComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 18;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(1, 1, 1, 0.6f);
        hintText.text = "A/D or \u2190/\u2192  |  Esc close";
        var hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.3f, 0.01f);
        hintRT.anchorMax = new Vector2(0.7f, 0.05f);
        hintRT.offsetMin = Vector2.zero;
        hintRT.offsetMax = Vector2.zero;
    }

    private void ShowPhoto(int index)
    {
        if (photos == null || index < 0 || index >= photos.Length) return;
        if (photoImage != null)
            photoImage.sprite = photos[index];
        if (indexText != null)
            indexText.text = $"{index + 1} / {photos.Length}";
    }

    private void CreatePrompt()
    {
        promptGO = new GameObject("ViewPrompt");
        promptGO.transform.SetParent(transform);
        promptGO.transform.localPosition = new Vector3(0, -0.8f, 0);

        Vector3 ps = transform.localScale;
        float ix = ps.x != 0 ? 1f / ps.x : 1f;
        float iy = ps.y != 0 ? 1f / ps.y : 1f;

        var promptSR = promptGO.AddComponent<SpriteRenderer>();
        promptSR.sprite = RuntimeSprite.Get();
        promptSR.color = new Color(0.1f, 0.1f, 0.15f, 0.7f);
        promptSR.sortingOrder = 20;
        promptGO.transform.localScale = new Vector3(0.8f * ix, 0.3f * iy, 1f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(promptGO.transform);
        textGO.transform.localPosition = Vector3.zero;
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = "E - View";
        tm.characterSize = 0.12f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        textGO.GetComponent<MeshRenderer>().sortingOrder = 21;
        promptGO.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
